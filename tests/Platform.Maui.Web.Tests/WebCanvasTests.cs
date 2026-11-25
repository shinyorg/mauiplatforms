using System.Text.Json;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Tests;

public class WebCanvasTests
{
	static JsonElement[] Commands(WebCanvas canvas) => JsonDocument.Parse(canvas.Flush()).RootElement.EnumerateArray().ToArray();

	[Fact]
	public void RecordsValidJsonCommands()
	{
		var canvas = new WebCanvas();
		canvas.FillColor = Colors.Red;
		canvas.FillRectangle(1, 2, 3, 4);

		var commands = Commands(canvas);
		Assert.Equal("fillStyle", commands[0][0].GetString());
		Assert.Equal("rgba(255,0,0,1)", commands[0][1].GetString());
		Assert.Equal("fillRect", commands[1][0].GetString());
		Assert.Equal(4, commands[1][4].GetDouble());
	}

	[Fact]
	public void TextIsJsonEscaped()
	{
		var canvas = new WebCanvas();
		canvas.DrawString("say \"hi\"\n\\", 0, 0, HorizontalAlignment.Left);
		var text = Commands(canvas).Single(c => c[0].GetString() == "text");
		Assert.Equal("say \"hi\"\n\\", text[1].GetString());
	}

	[Fact]
	public void DashPatternScalesWithCurrentStrokeSize()
	{
		var canvas = new WebCanvas();
		canvas.StrokeSize = 3;
		canvas.StrokeDashPattern = [2, 1];
		Assert.Equal("6 3", Commands(canvas).Single(c => c[0].GetString() == "dash")[1].GetString());
	}

	[Fact]
	public void RestoreWithoutSaveIsRejected() => Assert.False(new WebCanvas().RestoreState());

	[Fact]
	public void SaveAndRestoreAreBalanced()
	{
		var canvas = new WebCanvas();
		canvas.SaveState();
		canvas.Translate(10, 0);
		Assert.True(canvas.RestoreState());
		Assert.Equal(["save", "translate", "restore"], Commands(canvas).Select(c => c[0].GetString()));
	}

	[Fact]
	public void FlushResetsTheBuffer()
	{
		var canvas = new WebCanvas();
		canvas.FillRectangle(0, 0, 1, 1);
		canvas.Flush();
		Assert.Equal("[]", canvas.Flush());
	}

	[Fact]
	public void PathsUseSvgPathData()
	{
		var canvas = new WebCanvas();
		var path = new PathF();
		path.MoveTo(0, 0);
		path.LineTo(10, 10);
		canvas.DrawPath(path);
		var stroke = Commands(canvas).Single();
		Assert.Equal("stroke", stroke[0].GetString());
		Assert.Equal("M0 0 L10 10", stroke[1].GetString());
	}

	[Fact]
	public void ArcsAreWrittenAsSvgArcs()
	{
		// PathF.ToDefinitionString() drops arcs; DrawArc must still produce geometry.
		var canvas = new WebCanvas();
		canvas.DrawArc(0, 0, 100, 100, 0, 90, false, false);
		var d = Commands(canvas).Single(c => c[0].GetString() == "stroke")[1].GetString();
		// Counterclockwise quarter from 3 o'clock to 12 o'clock on a 100x100 circle.
		Assert.Equal("M100 50 A50 50 0 0 0 50 0", d);
	}

	[Fact]
	public void ClockwiseArcUsesTheLongWayRound()
	{
		var path = new PathF();
		path.AddArc(0, 0, 100, 100, 0, 90, true);
		Assert.Equal("M100 50 A50 50 0 1 1 50 0", WebCanvas.PathData(path));
	}

	[Fact]
	public void FullCircleArcIsTwoHalves()
	{
		var path = new PathF();
		path.MoveTo(0, 0);
		path.AddArc(0, 0, 100, 100, 0, 360, false);
		path.Close();
		Assert.Equal("M0 0 L100 50 A50 50 0 0 0 0 50 A50 50 0 0 0 100 50 Z", WebCanvas.PathData(path));
	}

	[Fact]
	public void LinesAndCurvesRoundTrip()
	{
		var path = new PathF();
		path.MoveTo(1, 2);
		path.LineTo(3, 4);
		path.QuadTo(5, 6, 7, 8);
		path.CurveTo(9, 10, 11, 12, 13, 14);
		Assert.Equal("M1 2 L3 4 Q5 6 7 8 C9 10 11 12 13 14", WebCanvas.PathData(path));
	}

	[Fact]
	public void APathDrawnAgainIsSentOnceThenReferencedById()
	{
		var canvas = new WebCanvas();
		var path = new PathF();
		path.MoveTo(0, 0);
		path.LineTo(10, 10);

		canvas.FillPath(path);
		var first = Commands(canvas).Single();
		Assert.Equal("fill", first[0].GetString());
		Assert.Equal("M0 0 L10 10", first[1].GetString());

		canvas.FillPath(path);
		var second = Commands(canvas);
		Assert.Equal("pathDef", second[0][0].GetString());
		var id = second[0][1].GetInt32();
		Assert.Equal("M0 0 L10 10", second[0][2].GetString());
		Assert.Equal("fill", second[1][0].GetString());
		Assert.Equal(id, second[1][1].GetInt32());

		canvas.FillPath(path);
		var third = Commands(canvas).Single();
		Assert.Equal(id, third[1].GetInt32());
	}

	[Fact]
	public void AChangedPathIsWrittenAgainAndTheOldOneFreed()
	{
		var canvas = new WebCanvas();
		var path = new PathF();
		path.MoveTo(0, 0);
		path.LineTo(10, 10);
		canvas.DrawPath(path);
		canvas.DrawPath(path);
		var id = Commands(canvas).Single(c => c[0].GetString() == "pathDef")[1].GetInt32();

		path.LineTo(20, 0);
		canvas.DrawPath(path);
		var commands = Commands(canvas);
		Assert.Equal("M0 0 L10 10 L20 0", commands[0][1].GetString());
		var freed = commands.Single(c => c[0].GetString() == "pathFree");
		Assert.Contains(id, freed.EnumerateArray().Skip(1).Select(e => e.GetInt32()));
	}

	[Theory]
	[InlineData(0d, "0")]
	[InlineData(12d, "12")]
	[InlineData(-1.5d, "-1.5")]
	[InlineData(0.05d, "0.05")]
	[InlineData(3.14159d, "3.142")]
	[InlineData(-0.0001d, "0")]
	[InlineData(1e-7d, "0")]
	[InlineData(123456.0009d, "123456.001")]
	[InlineData(double.NaN, "0")]
	public void NumbersAreRoundedToThreeDecimals(double value, string expected)
	{
		var sb = new System.Text.StringBuilder();
		WebCanvas.AppendNum(sb, value);
		Assert.Equal(expected, sb.ToString());
	}
}

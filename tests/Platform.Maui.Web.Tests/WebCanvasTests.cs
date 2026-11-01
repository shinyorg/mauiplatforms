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
}

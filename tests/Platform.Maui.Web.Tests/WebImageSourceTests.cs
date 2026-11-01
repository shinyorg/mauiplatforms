using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Tests;

public class WebImageSourceTests
{
	[Theory]
	[InlineData("https://example.com/a.png")]
	[InlineData("data:image/png;base64,AAAA")]
	[InlineData("/images/a.png")]
	public void AbsoluteSourcesAreUnchanged(string source) => Assert.Equal(source, WebImageSource.ResolveFile(source));

	[Fact]
	public void UnknownRasterNamesAreUnchanged() => Assert.Equal("photo.png", WebImageSource.ResolveFile("photo.png"));

	[Fact]
	public void EmptySourceResolvesToNull() => Assert.Null(WebImageSource.ResolveFile(" "));
}

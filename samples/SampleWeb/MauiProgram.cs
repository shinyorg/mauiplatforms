using Microsoft.Maui.Hosting;
using Microsoft.Maui.Essentials.Web;
using Microsoft.Maui.Platform.Web.Hosting;
using Shiny;

namespace SampleWeb;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiAppWeb<App>()
			.UseWebEssentials()
			// The Shiny Controls tab: the core controls plus the browser backends from this repo.
			.UseShinyControls()
			.UseShinyCameraWeb()
			.UseShinyMediaElementWeb()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("BricolageGrotesque.ttf", Theme.Display);
				fonts.AddFont("InstrumentSans.ttf", Theme.Body);
				fonts.AddFont("Lucide.ttf", Theme.Icons);
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});
		return builder.Build();
	}
}

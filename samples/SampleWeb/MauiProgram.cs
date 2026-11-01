using Microsoft.Maui.Hosting;
using Microsoft.Maui.Essentials.Web;
using Microsoft.Maui.Platform.Web.Hosting;

namespace SampleWeb;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiAppWeb<App>()
			.UseWebEssentials()
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

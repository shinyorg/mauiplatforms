using Microsoft.Maui.Hosting;
using Microsoft.Maui.Essentials.Web;
using Microsoft.Maui.Platform.Web.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Maui.Controls.Office;

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
			// The Office editors draw on SKCanvasView. UseShinyOffice registers SkiaSharp's own handlers,
			// so UseSkiaSharpWeb comes after it: the last registration for a view type wins.
			.UseShinyOffice()
			.UseSkiaSharpWeb()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("BricolageGrotesque.ttf", Theme.Display);
				fonts.AddFont("InstrumentSans.ttf", Theme.Body);
				fonts.AddFont("Lucide.ttf", Theme.Icons);
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});
		// The chat demo's canned conversation, as the controls sample registers it.
		builder.Services.AddSingleton<Sample.Features.Chat.InMemoryChatSessionProvider>();
		// The scheduler demos' events, as the controls sample registers them.
		builder.Services.AddSingleton<Shiny.Maui.Controls.Scheduler.ISchedulerEventProvider, Sample.Features.Scheduler.SampleSchedulerProvider>();
		return builder.Build();
	}
}

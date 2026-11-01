using System;
using System.Threading.Tasks;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.Web;

namespace SampleWeb;

public class SampleWebApp : WebMauiApplication
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

public static class Program
{
	public static async Task Main()
	{
		try
		{
			await new SampleWebApp().RunAsync();
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex);
		}
	}
}

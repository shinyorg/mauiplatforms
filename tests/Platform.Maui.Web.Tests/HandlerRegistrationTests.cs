using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.Web.Hosting;

namespace Microsoft.Maui.Platform.Web.Tests;

// Building a MAUI app writes to its shared static mappers, so the registration tests never run in parallel.
[Collection("MAUI app")]
public class HandlerRegistrationTests
{
	sealed class TestApp : Application;

	/// <summary>
	/// Controls registers its own handlers per concrete type, and in a net10.0 build those are stubs
	/// that throw NotImplementedException. Every registration must resolve to a web handler.
	/// </summary>
	[Fact]
	public void EveryRegisteredControlResolvesToAWebHandler()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiAppWeb<TestApp>();
		using var app = builder.Build();
		var factory = app.Services.GetRequiredService<IMauiHandlersFactory>();

		var stubs = factory.GetCollection()
			.Select(d => d.ServiceType)
			.Distinct()
			.Select(type => (Type: type, Handler: factory.GetHandlerType(type)))
			.Where(r => r.Handler is not null && r.Handler.Assembly != typeof(Microsoft.Maui.Platform.Web.Handlers.LayoutHandler).Assembly)
			.Select(r => $"{r.Type.FullName} -> {r.Handler!.FullName}")
			.OrderBy(s => s)
			.ToList();

		Assert.True(stubs.Count == 0, "Not handled by the web backend:\n" + string.Join("\n", stubs));
	}
}

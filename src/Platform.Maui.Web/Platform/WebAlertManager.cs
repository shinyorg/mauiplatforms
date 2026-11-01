using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Platform.Web.Interop;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// Shows DisplayAlert, DisplayActionSheet and DisplayPromptAsync as HTML &lt;dialog&gt; elements.
/// MAUI's IAlertManagerSubscription is internal, so a DispatchProxy implements it and is
/// registered in DI, where AlertManager.Subscribe() resolves it.
/// </summary>
public static class WebAlertManager
{
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The internal MAUI types are looked up by name and fail loudly when missing.")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The subscription interface is preserved by the DynamicDependency.")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "Microsoft.Maui.Controls.Platform.IAlertManagerSubscription", "Microsoft.Maui.Controls")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The nested interface lookup is guarded.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The proxy type is known when the backend is built.")]
	public static void Register(IServiceCollection services)
	{
		var controls = typeof(Window).Assembly;

		// MAUI 10.0.70 moved the interface out of AlertManager; support both shapes.
		var subscriptionType = controls.GetType("Microsoft.Maui.Controls.Platform.IAlertManagerSubscription")
			?? controls.GetType("Microsoft.Maui.Controls.Platform.AlertManager")
				?.GetNestedType("IAlertManagerSubscription", BindingFlags.Public | BindingFlags.NonPublic)
			?? throw new NotSupportedException($"Cannot register web dialogs: IAlertManagerSubscription was not found in {controls.FullName}.");

		services.AddSingleton(subscriptionType, DispatchProxy.Create(subscriptionType, typeof(AlertSubscriptionProxy)));
	}

	/// <summary>
	/// AlertManager only subscribes when a platform asks it to; call this once the window has content.
	/// </summary>
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Window.AlertManager is an internal MAUI member; failure only disables dialogs.")]
	internal static void Subscribe(Window window)
	{
		try
		{
			var alertManager = typeof(Window)
				.GetProperty("AlertManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				?.GetValue(window);
			alertManager?.GetType()
				.GetMethod("Subscribe", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				?.Invoke(alertManager, null);
		}
		catch (Exception ex)
		{
			Dom.Log($"Dialog subscription failed: {ex.Message}");
		}
	}

	public class AlertSubscriptionProxy : DispatchProxy
	{
		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
		{
			if (targetMethod is null || args is not { Length: >= 2 })
				return null;

			switch (args[1])
			{
				case AlertArguments alert when targetMethod.Name == "OnAlertRequested":
					ShowAlert(alert).FireAndForget("DisplayAlert");
					break;
				case ActionSheetArguments sheet when targetMethod.Name == "OnActionSheetRequested":
					ShowActionSheet(sheet).FireAndForget("DisplayActionSheet");
					break;
				case PromptArguments prompt when targetMethod.Name == "OnPromptRequested":
					ShowPrompt(prompt).FireAndForget("DisplayPromptAsync");
					break;
			}
			return null;
		}

		static async Task ShowAlert(AlertArguments args)
		{
			try
			{
				args.SetResult(await Dom.ShowAlert(args.Title, args.Message, args.Accept, args.Cancel));
			}
			catch (Exception ex)
			{
				args.Result.TrySetException(ex);
			}
		}

		static async Task ShowActionSheet(ActionSheetArguments args)
		{
			try
			{
				var buttons = args.Buttons?.Where(b => b is not null).ToArray() ?? [];
				args.SetResult((await Dom.ShowActionSheet(args.Title, args.Cancel, args.Destruction, buttons))!);
			}
			catch (Exception ex)
			{
				args.Result.TrySetException(ex);
			}
		}

		static async Task ShowPrompt(PromptArguments args)
		{
			try
			{
				var inputMode = args.Keyboard == Keyboard.Numeric ? "decimal"
					: args.Keyboard == Keyboard.Telephone ? "tel"
					: args.Keyboard == Keyboard.Email ? "email"
					: args.Keyboard == Keyboard.Url ? "url"
					: null;
				args.SetResult((await Dom.ShowPrompt(args.Title, args.Message, args.Accept, args.Cancel,
					args.Placeholder, args.InitialValue, args.MaxLength, inputMode))!);
			}
			catch (Exception ex)
			{
				args.Result.TrySetException(ex);
			}
		}
	}
}

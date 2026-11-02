using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS;

/// <summary>
/// DisplayAlertAsync, DisplayActionSheetAsync and DisplayPromptAsync on tvOS, shown with
/// UIAlertController. MAUI 10 looks these up as keyed delegate services, so no reflection or
/// runtime code generation is needed and they work under full AOT.
/// </summary>
public static class TvOSDialogs
{
    // Service keys MAUI's AlertManager resolves (Microsoft.Maui.Controls.Platform.AlertManager).
    const string DisplayAlertKey = "Microsoft.Maui.Controls.DisplayAlert";
    const string DisplayActionSheetKey = "Microsoft.Maui.Controls.DisplayActionSheet";
    const string DisplayPromptKey = "Microsoft.Maui.Controls.DisplayPrompt";

    public static void Register(IServiceCollection services)
    {
        services.AddKeyedSingleton<Func<Page, AlertArguments, Task<bool>>>(DisplayAlertKey, (_, _) => ShowAlert);
        services.AddKeyedSingleton<Func<Page, ActionSheetArguments, Task<string>>>(DisplayActionSheetKey, (_, _) => ShowActionSheet);
        services.AddKeyedSingleton<Func<Page, PromptArguments, Task<string>>>(DisplayPromptKey, (_, _) => ShowPrompt);
    }

    public static Task<bool> ShowAlert(Page page, AlertArguments arguments)
    {
        var result = new TaskCompletionSource<bool>();
        var alert = UIAlertController.Create(arguments.Title, arguments.Message, UIAlertControllerStyle.Alert);

        if (arguments.Cancel != null)
            alert.AddAction(UIAlertAction.Create(arguments.Cancel, UIAlertActionStyle.Cancel, _ => result.TrySetResult(false)));
        if (arguments.Accept != null)
            alert.AddAction(UIAlertAction.Create(arguments.Accept, UIAlertActionStyle.Default, _ => result.TrySetResult(true)));
        if (alert.Actions.Length == 0)
            alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, _ => result.TrySetResult(false)));

        Present(alert, () => result.TrySetResult(false));
        return result.Task;
    }

    public static Task<string> ShowActionSheet(Page page, ActionSheetArguments arguments)
    {
        var result = new TaskCompletionSource<string>();
        var sheet = UIAlertController.Create(arguments.Title, null, UIAlertControllerStyle.ActionSheet);

        if (arguments.Destruction != null)
            sheet.AddAction(UIAlertAction.Create(arguments.Destruction, UIAlertActionStyle.Destructive, _ => result.TrySetResult(arguments.Destruction)));

        foreach (var button in arguments.Buttons)
        {
            if (button == null)
                continue;
            var label = button;
            sheet.AddAction(UIAlertAction.Create(label, UIAlertActionStyle.Default, _ => result.TrySetResult(label)));
        }

        // tvOS shows the cancel action as a button too; Menu also dismisses the sheet.
        if (arguments.Cancel != null)
            sheet.AddAction(UIAlertAction.Create(arguments.Cancel, UIAlertActionStyle.Cancel, _ => result.TrySetResult(arguments.Cancel)));

        Present(sheet, () => result.TrySetResult(arguments.Cancel!));
        return result.Task;
    }

    public static Task<string> ShowPrompt(Page page, PromptArguments arguments)
    {
        var result = new TaskCompletionSource<string>();
        var alert = UIAlertController.Create(arguments.Title, arguments.Message, UIAlertControllerStyle.Alert);
        alert.AddTextField(field =>
        {
            field.Placeholder = arguments.Placeholder;
            field.Text = arguments.InitialValue;
            field.KeyboardType = arguments.Keyboard == Keyboard.Numeric ? UIKeyboardType.NumberPad
                : arguments.Keyboard == Keyboard.Email ? UIKeyboardType.EmailAddress
                : arguments.Keyboard == Keyboard.Url ? UIKeyboardType.Url
                : UIKeyboardType.Default;
            if (arguments.MaxLength > -1)
            {
                field.ShouldChangeCharacters = (f, range, replacement) =>
                    (f.Text?.Length ?? 0) + replacement.Length - (int)range.Length <= arguments.MaxLength;
            }
        });

        alert.AddAction(UIAlertAction.Create(arguments.Cancel, UIAlertActionStyle.Cancel, _ => result.TrySetResult(null!)));
        alert.AddAction(UIAlertAction.Create(arguments.Accept, UIAlertActionStyle.Default, _ => result.TrySetResult(alert.TextFields?[0].Text ?? string.Empty)));

        Present(alert, () => result.TrySetResult(null!));
        return result.Task;
    }

    static void Present(UIAlertController alert, Action onUnavailable)
    {
        var presenter = TopViewController();
        if (presenter == null)
        {
            onUnavailable();
            return;
        }
        presenter.PresentViewController(alert, true, null);
    }

    static UIViewController? TopViewController()
    {
        var vc = Hosting.TvOSMauiApplication.Current?.Window?.RootViewController;
        while (vc?.PresentedViewController != null)
            vc = vc.PresentedViewController;
        return vc;
    }
}

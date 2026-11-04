using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny.Maui.Controls.Dialogs;

using Shiny;

namespace Sample.Features.Dialogs;

public partial class DialogsViewModel(IDialogService dialogs) : ObservableObject
{
    [ObservableProperty]
    string statusMessage = "Pick an animation and try a dialog";

    public List<DialogAnimation> Animations { get; } = Enum.GetValues<DialogAnimation>().ToList();

    [ObservableProperty]
    DialogAnimation selectedAnimation = DialogAnimation.Pop;

    [RelayCommand]
    async Task Alert()
    {
        await dialogs.Alert("Heads up", "Your changes have been saved.", "Got it", c => c.Animation = this.SelectedAnimation);
        this.StatusMessage = "Alert dismissed";
    }

    [RelayCommand]
    async Task Confirm()
    {
        var ok = await dialogs.Confirm(
            "Delete item?",
            "This action cannot be undone.",
            okText: "Delete",
            cancelText: "Cancel",
            configure: c => c.Animation = this.SelectedAnimation
        );
        this.StatusMessage = ok ? "Confirmed: item deleted" : "Cancelled";
    }

    [RelayCommand]
    async Task Prompt()
    {
        var result = await dialogs.Prompt(
            "What's your name?",
            "We'll use this to personalize your experience.",
            placeholder: "e.g. Allan",
            okText: "Save",
            cancelText: "Skip",
            configure: c => c.Animation = this.SelectedAnimation
        );
        this.StatusMessage = result.Ok ? $"Hello, {result.Value}!" : "Prompt cancelled";
    }

    [RelayCommand]
    async Task ActionSheet()
    {
        var choice = await dialogs.ActionSheet(
            "Photo",
            ["Take Photo", "Choose from Library", "Delete Photo"],
            cancelText: "Cancel",
            destructive: "Delete Photo",
            configure: c => c.Animation = this.SelectedAnimation
        );
        this.StatusMessage = choice is null ? "Action sheet cancelled" : $"Picked: {choice}";
    }

    [RelayCommand]
    async Task Styled()
    {
        await dialogs.Confirm(
            "Custom styled",
            "Brand colors with a zoom animation, fully owned (no native dialog).",
            okText: "Nice",
            cancelText: "Close",
            configure: c =>
            {
                c.Animation = DialogAnimation.Zoom;
                c.BackgroundColor = Color.FromArgb("#312E81");
                c.TitleColor = Colors.White;
                c.MessageColor = Color.FromArgb("#C7D2FE");
                c.OkButtonColor = Color.FromArgb("#22D3EE");
                c.OkButtonTextColor = Color.FromArgb("#0F172A");
                c.CancelButtonColor = Color.FromArgb("#4338CA");
                c.CancelButtonTextColor = Colors.White;
                c.CornerRadius = 24;
            }
        );
        this.StatusMessage = "Styled dialog dismissed";
    }

    [RelayCommand]
    async Task StyledActionSheet()
    {
        var choice = await dialogs.ActionSheet(
            "Share",
            ["Copy Link", "Send by Email", "Remove Share"],
            cancelText: "Cancel",
            destructive: "Remove Share",
            configure: c =>
            {
                c.Animation = this.SelectedAnimation;
                c.BackgroundColor = Color.FromArgb("#312E81");
                c.TitleColor = Colors.White;
                c.ActionSheetButtonColor = Color.FromArgb("#4338CA");
                c.ActionSheetButtonTextColor = Colors.White;
                c.DestructiveButtonTextColor = Color.FromArgb("#FCA5A5");
                c.CancelButtonColor = Color.FromArgb("#22D3EE");
                c.CancelButtonTextColor = Color.FromArgb("#0F172A");
            }
        );
        this.StatusMessage = choice is null ? "Styled action sheet cancelled" : $"Picked: {choice}";
    }
}

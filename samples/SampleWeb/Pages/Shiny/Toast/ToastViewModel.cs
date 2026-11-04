using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny.Maui.Controls.Toast;

using Shiny;

namespace Sample.Features.Toast;

public partial class ToastViewModel(IToaster toaster) : ObservableObject
{
    IDisposable? manualToast;

    [ObservableProperty]
    string statusMessage = "Configure and tap Show Toast";

    [ObservableProperty]
    string toastText = "This is a really long toast message that is meant to demonstrate what happens when the text content exceeds the available width of the toast container";

    // --- Configuration properties ---

    public List<ToastPosition> Positions { get; } = Enum.GetValues<ToastPosition>().ToList();
    public List<ToastDisplayMode> DisplayModes { get; } = Enum.GetValues<ToastDisplayMode>().ToList();
    public List<ToastQueueMode> QueueModes { get; } = Enum.GetValues<ToastQueueMode>().ToList();
    public List<ToastSpinnerPosition> SpinnerPositions { get; } = Enum.GetValues<ToastSpinnerPosition>().ToList();
    public List<ToastTextOverflow> TextOverflows { get; } = Enum.GetValues<ToastTextOverflow>().ToList();

    [ObservableProperty]
    ToastPosition selectedPosition = ToastPosition.Bottom;

    [ObservableProperty]
    ToastDisplayMode selectedDisplayMode = ToastDisplayMode.Pill;

    [ObservableProperty]
    ToastQueueMode selectedQueueMode = ToastQueueMode.Queue;

    [ObservableProperty]
    ToastSpinnerPosition selectedSpinner = ToastSpinnerPosition.None;

    [ObservableProperty]
    double durationSeconds = 3;

    [ObservableProperty]
    double cornerRadius = 20;

    [ObservableProperty]
    bool dismissOnTap = true;

    [ObservableProperty]
    bool showProgressBar;

    [ObservableProperty]
    bool useFeedback = true;

    [ObservableProperty]
    bool announceToScreenReader = true;

    [ObservableProperty]
    ToastTextOverflow selectedTextOverflow = ToastTextOverflow.Ellipsis;

    [ObservableProperty]
    double marqueeSpeed = 40;

    [ObservableProperty]
    double marqueeLoops = 1;

    // --- Commands ---

    [RelayCommand]
    async Task ShowConfiguredToast()
    {
        await toaster.ShowAsync(ToastText, cfg =>
        {
            cfg.Position = SelectedPosition;
            cfg.DisplayMode = SelectedDisplayMode;
            cfg.QueueMode = SelectedQueueMode;
            cfg.Spinner = SelectedSpinner;
            cfg.Duration = TimeSpan.FromSeconds(DurationSeconds);
            cfg.CornerRadius = CornerRadius;
            cfg.DismissOnTap = DismissOnTap;
            cfg.ShowProgressBar = ShowProgressBar;
            cfg.UseFeedback = UseFeedback;
            cfg.AnnounceToScreenReader = AnnounceToScreenReader;
            cfg.TextOverflow = SelectedTextOverflow;
            cfg.MarqueeSpeedPixelsPerSecond = MarqueeSpeed;
            cfg.MarqueeLoops = (int)MarqueeLoops;
        });
        StatusMessage = "Configured toast shown";
    }

    [RelayCommand]
    async Task SpinnerToast()
    {
        manualToast?.Dispose();
        manualToast = await toaster.ShowAsync("Uploading file...", cfg =>
        {
            cfg.Spinner = ToastSpinnerPosition.Left;
            cfg.Duration = TimeSpan.Zero;
            cfg.DismissOnTap = false;
        });
        StatusMessage = "Persistent toast shown (tap Dismiss to close)";
    }

    [RelayCommand]
    void DismissManual()
    {
        manualToast?.Dispose();
        manualToast = null;
        StatusMessage = "Manual toast dismissed";
    }

    // --- Snackbars ---

    [RelayCommand]
    async Task UndoSnackbar()
    {
        StatusMessage = "Snackbar showing...";
        var result = await toaster.SnackbarAsync("Message deleted", "Undo", cfg => cfg.AutomationId = "UndoSnackbar");
        StatusMessage = result == SnackbarResult.ActionInvoked
            ? "Undo pressed - message restored"
            : $"Snackbar closed: {result}";
    }

    [RelayCommand]
    async Task MultilineSnackbar()
    {
        var result = await toaster.SnackbarAsync(
            "Your changes were saved, but two attachments could not be uploaded because the connection dropped. They will be retried when you are back online.",
            "Retry");
        StatusMessage = $"Multi-line snackbar closed: {result}";
    }

    [RelayCommand]
    async Task BodyActionSnackbar()
    {
        var result = await toaster.SnackbarAsync("New reply from Sam - tap to open", "Mark read", cfg =>
        {
            cfg.TapCommand = new Command(() => StatusMessage = "Body tapped - opening the reply");
            cfg.ActionCommand = new Command(() => StatusMessage = "Marked as read");
        });
        if (result == SnackbarResult.TimedOut)
            StatusMessage = "Snackbar timed out";
    }

    [RelayCommand]
    async Task MarqueeSnackbar()
    {
        var result = await toaster.SnackbarAsync(ToastText, "OK", cfg => cfg.TextOverflow = ToastTextOverflow.Marquee);
        StatusMessage = $"Marquee snackbar closed: {result}";
    }

    [RelayCommand]
    async Task CountdownSnackbar()
    {
        var result = await toaster.SnackbarAsync("Draft discarded", "Undo", cfg => cfg.ShowProgressBar = true);
        StatusMessage = $"Countdown snackbar closed: {result}";
    }

    [RelayCommand]
    async Task LoadingSnackbar()
    {
        using var cancel = new CancellationTokenSource();
        using var loading = await toaster.LoadingSnackbarAsync("Preparing upload...", cfg =>
        {
            cfg.ActionText = "Cancel";
            cfg.ActionCommand = new Command(cancel.Cancel);
        });
        StatusMessage = "Loading snackbar showing";

        try
        {
            await Task.Delay(1500, cancel.Token);
            for (var i = 0; i <= 20; i++)
            {
                loading.Text = $"Uploading photos... {i * 5}%";
                loading.Progress = i / 20.0;
                await Task.Delay(150, cancel.Token);
            }
            StatusMessage = "Upload finished";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Upload cancelled";
        }
    }

    [RelayCommand]
    async Task StyledToast()
    {
        await toaster.ShowAsync("Custom styled toast", cfg =>
        {
            cfg.BackgroundColor = Color.FromArgb("#312E81");
            cfg.TextColor = Color.FromArgb("#E0E7FF");
            cfg.BorderColor = Color.FromArgb("#6D28D9");
            cfg.BorderThickness = 1.5;
            cfg.CornerRadius = 12;
        });
        StatusMessage = "Styled toast shown";
    }

    [RelayCommand]
    async Task InfoToast()
    {
        await toaster.InfoAsync("New update available");
        StatusMessage = "Info toast shown";
    }

    [RelayCommand]
    async Task SuccessToast()
    {
        await toaster.SuccessAsync("File uploaded successfully");
        StatusMessage = "Success toast shown";
    }

    [RelayCommand]
    async Task WarningToast()
    {
        await toaster.WarningAsync("Storage almost full");
        StatusMessage = "Warning toast shown";
    }

    [RelayCommand]
    async Task DangerToast()
    {
        await toaster.DangerAsync("Failed to save changes");
        StatusMessage = "Danger toast shown";
    }

    [RelayCommand]
    async Task CriticalToast()
    {
        await toaster.CriticalAsync("System error - contact support");
        StatusMessage = "Critical toast shown";
    }
}

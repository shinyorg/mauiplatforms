using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Shiny;
using Shiny.Maui.Controls;

namespace Sample.Features.Buttons;

public partial class ButtonsViewModel : ObservableObject
{
    [ObservableProperty]
    string statusMessage = "Nothing has happened yet";

    [ObservableProperty]
    ButtonState manualState = ButtonState.Normal;

    /// <summary>Flipped by the toggle so the CanExecute wiring can be watched live.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardedCommand))]
    bool canSubmit = true;

    /// <summary>Whether the fake async work fails, so the Error path is reachable from the UI.</summary>
    [ObservableProperty]
    bool shouldFail;


    // The point of this page: AsyncRelayCommand exposes ExecutionTask, so ShinyButton drives its own
    // busy state for exactly as long as the command runs. Nothing here touches IsBusy.
    //
    // AllowConcurrentExecutions is on so the four busy-mode buttons can be compared side by side.
    // Without it, AsyncRelayCommand reports CanExecute=false while it runs, and since all four share
    // this one command, starting any of them disables the other three - correct behaviour, and exactly
    // what the CanExecute wiring is for, but it makes the modes impossible to see together.
    //
    // FlowExceptionsToTaskScheduler: the "This will fail" demo faults on purpose. Without it
    // AsyncRelayCommand.Execute rethrows that fault through async void - an unhandled exception on
    // MAUI - instead of leaving it on ExecutionTask, where the button reads it and shows Error.
    [RelayCommand(AllowConcurrentExecutions = true, FlowExceptionsToTaskScheduler = true)]
    async Task SaveAsync()
    {
        this.StatusMessage = "Saving...";
        await Task.Delay(1800);

        if (this.ShouldFail)
            throw new InvalidOperationException("The save failed (on purpose).");

        this.StatusMessage = "Saved";
    }

    /// <summary>
    /// An async command that reports its own outcome. The button must respect the Success it sets
    /// rather than resetting to Normal when the task finishes.
    /// </summary>
    [RelayCommand]
    async Task SubmitAsync()
    {
        await Task.Delay(1500);
        this.ManualState = ButtonState.Success;
        this.StatusMessage = "Submitted";
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    void Guarded()
        => this.StatusMessage = "The guarded command ran";

    [RelayCommand]
    void Note(string which)
        => this.StatusMessage = $"{which} tapped";

    [RelayCommand]
    void ToggleCanSubmit() => this.CanSubmit = !this.CanSubmit;

    [RelayCommand]
    void ShowSuccess() => this.ManualState = ButtonState.Success;

    [RelayCommand]
    void ShowError() => this.ManualState = ButtonState.Error;
}

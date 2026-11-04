using Shiny.Maui.Controls;

namespace Sample.Features.Chips;

public partial class ChipGroupPage : ContentPage
{
    public ChipGroupPage()
    {
        InitializeComponent();
    }


    void OnActionTapped(object sender, ChipTappedEventArgs e)
    {
        if (this.BindingContext is ChipGroupViewModel vm)
            vm.StatusMessage = $"Action: {e.Item}";
    }


    /// <summary>The cancellable seam: keep one chip whatever happens, so the group never empties.</summary>
    void OnChipRemoving(object sender, ChipRemovingEventArgs e)
    {
        if (this.BindingContext is not ChipGroupViewModel vm)
            return;

        if (vm.Recipients.Count == 1)
        {
            e.Cancel = true;
            vm.StatusMessage = "The last recipient cannot be removed";
        }
    }


    void OnResetRecipients(object sender, EventArgs e)
    {
        if (this.BindingContext is not ChipGroupViewModel vm)
            return;

        vm.Recipients.Clear();
        foreach (var recipient in new[] { "design", "engineering", "sales" })
            vm.Recipients.Add(recipient);
    }
}

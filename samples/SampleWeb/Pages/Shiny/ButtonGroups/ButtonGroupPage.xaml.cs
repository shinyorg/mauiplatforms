using Shiny.Maui.Controls;

namespace Sample.Features.ButtonGroups;

public partial class ButtonGroupPage : ContentPage
{
    public ButtonGroupPage()
    {
        InitializeComponent();
    }


    void OnFormatSelectionChanged(object sender, ButtonGroupSelectionChangedEventArgs e)
    {
        if (this.BindingContext is ButtonGroupViewModel vm)
            vm.ReportFormats(e.SelectedIndexes);
    }
}

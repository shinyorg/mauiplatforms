using Shiny.Maui.Controls;

namespace Sample.Features.Tags;

public partial class TagEntryPage : ContentPage
{
    public TagEntryPage()
    {
        InitializeComponent();
    }


    /// <summary>The validation seam: refuse anything too short, and leave the text to be corrected.</summary>
    void OnTagAdding(object sender, TagAddingEventArgs e)
        => e.Cancel = e.Tag.Length < 3;


    void OnClearTopics(object sender, EventArgs e)
    {
        if (this.BindingContext is TagEntryViewModel vm)
            vm.Topics.Clear();

        this.TopicField.Focus();
    }
}

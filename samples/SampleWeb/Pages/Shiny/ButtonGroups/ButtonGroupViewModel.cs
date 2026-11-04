using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;

namespace Sample.Features.ButtonGroups;

public partial class ButtonGroupViewModel : ObservableObject
{
    static readonly string[] Ranges = ["Day", "Week", "Month"];
    static readonly string[] Formats = ["Bold", "Italic", "Underline"];

    [ObservableProperty]
    string statusMessage = "Nothing has happened yet";

    [ObservableProperty]
    string formatSummary = "No formatting";

    [ObservableProperty]
    string amountSummary = "Amount: 0";

    int amount;

    /// <summary>Two-way from the group: the picker starts on Week, and a tap reports back here.</summary>
    [ObservableProperty]
    int rangeIndex = 1;

    public string RangeSummary => $"Showing: {Ranges[Math.Clamp(this.RangeIndex, 0, Ranges.Length - 1)]}";

    partial void OnRangeIndexChanged(int value) => this.OnPropertyChanged(nameof(this.RangeSummary));


    [RelayCommand]
    void Note(string what) => this.StatusMessage = $"{what} at {DateTime.Now:T}";


    [RelayCommand]
    void Adjust(string delta)
    {
        this.amount += Int32.Parse(delta);
        this.AmountSummary = $"Amount: {this.amount}";
    }


    public void ReportFormats(IReadOnlyList<int> indexes)
        => this.FormatSummary = indexes.Count == 0
            ? "No formatting"
            : String.Join(", ", indexes.Select(x => Formats[x]));
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny.Maui.Controls.DataGrid;

using Shiny;

namespace Sample.Features.DataGrid;

public partial class DataGridGroupingViewModel : ObservableObject
{
    public ObservableCollection<Sale> Sales { get; } =
    [
        new("Sales", "West", "Ada", 142000, 12),
        new("Sales", "West", "Grace", 165000, 18),
        new("Sales", "East", "Alan", 98000, 9),
        new("Sales", "East", "Katherine", 134000, 15),
        new("Support", "West", "Margaret", 88000, 22),
        new("Support", "East", "Edsger", 149000, 7),
        new("Support", "East", "Barbara", 121000, 11),
        new("Research", "West", "Donald", 158000, 4),
    ];

    /// <summary>
    /// Bound straight to <c>DataGrid.GroupBy</c>, so the two buttons below re-group the grid without
    /// the page ever touching the control.
    /// </summary>
    public ObservableCollection<string> GroupColumns { get; } = ["Department", "Region"];

    [ObservableProperty]
    public partial DataGridGroupSummaryPlacement Placement { get; set; } = DataGridGroupSummaryPlacement.Footer;

    [RelayCommand]
    void GroupByDepartment()
    {
        this.GroupColumns.Clear();
        this.GroupColumns.Add(nameof(Sale.Department));
    }

    [RelayCommand]
    void GroupByDepartmentAndRegion()
    {
        this.GroupColumns.Clear();
        this.GroupColumns.Add(nameof(Sale.Department));
        this.GroupColumns.Add(nameof(Sale.Region));
    }

    [RelayCommand]
    void Ungroup() => this.GroupColumns.Clear();

    [RelayCommand]
    void TogglePlacement()
        => this.Placement = this.Placement switch
        {
            DataGridGroupSummaryPlacement.Footer => DataGridGroupSummaryPlacement.Header,
            DataGridGroupSummaryPlacement.Header => DataGridGroupSummaryPlacement.Both,
            DataGridGroupSummaryPlacement.Both => DataGridGroupSummaryPlacement.None,
            _ => DataGridGroupSummaryPlacement.Footer
        };
}

public record Sale(string Department, string Region, string Name, decimal Revenue, int Deals);

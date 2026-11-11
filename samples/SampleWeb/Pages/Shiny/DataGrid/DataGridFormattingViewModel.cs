using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Shiny.Maui.Controls.DataGrid;

using Shiny;

namespace Sample.Features.DataGrid;

public partial class DataGridFormattingViewModel : ObservableObject
{
    public ObservableCollection<Person> People { get; } =
    [
        new("Ada", "Lovelace", 36, "Engineering", 142000, true),
        new("Alan", "Turing", 41, "Research", 98000, true),
        new("Grace", "Hopper", 52, "Engineering", 165000, false),
        new("Katherine", "Johnson", 44, "Mathematics", 134000, true),
        new("Margaret", "Hamilton", 39, "Software", 88000, true),
        new("Edsger", "Dijkstra", 47, "Research", 149000, false),
    ];

    /// <summary>
    /// The escape hatch that still is not a template: it takes the raw value and returns text, so
    /// prefix/suffix and the null placeholder keep working around it.
    /// </summary>
    public Func<object?, string?> AgeBand
        => value => value is int age
            ? age < 40 ? "Junior" : age < 50 ? "Senior" : "Principal"
            : null;

    /// <summary>Red for anyone under the band - the classic reason people reach for a cell template.</summary>
    public Func<object, DataGridCellStyle?> SalaryStyle
        => item => ((Person)item).Salary < 100000
            ? new DataGridCellStyle { TextColor = Colors.Firebrick, FontAttributes = FontAttributes.Bold }
            : null;

    /// <summary>
    /// Row-wide highlighting driven from the view model. A wash rather than a repaint, so the striping
    /// underneath it and the text on top of it both survive.
    /// </summary>
    public Func<object, DataGridCellStyle?> InactiveRowStyle
        => item => ((Person)item).Active
            ? null
            : new DataGridCellStyle
            {
                Fill = Colors.Firebrick,
                BorderColor = Colors.Firebrick,
                BorderStyle = DataGridBorderStyle.Dashed
            };

    /// <summary>
    /// The declarative form of the same idea, and the one that scales past a single rule: each entry
    /// covers a row, a column, one cell, or the whole grid depending only on which of its targeting
    /// members are set.
    /// </summary>
    public ObservableCollection<DataGridHighlight> Highlights { get; } =
    [
        // A column: named, no row - every cell of Salary is washed.
        new() { Column = nameof(Person.Salary), Fill = Colors.Gold },

        // A row: a predicate, no column - every cell of the rows it matches.
        new()
        {
            RowPredicate = item => ((Person)item).Salary > 140000,
            Fill = Colors.SeaGreen,
            BorderColor = Colors.SeaGreen,
            BorderStyle = DataGridBorderStyle.Solid
        },

        // A cell: both, so the stroke boxes the one place they cross rather than running off the
        // edge of a row or a column.
        new()
        {
            RowPredicate = item => ((Person)item).FirstName == "Margaret",
            Column = nameof(Person.Department),
            BorderColor = Colors.BlueViolet,
            BorderStyle = DataGridBorderStyle.Dotted,
            BorderWidth = 3
        }
    ];

    /// <summary>Tints the whole cell when a review is missing, so "Overdue" reads as a state, not a word.</summary>
    public Func<object, DataGridCellStyle?> ReviewStyle
        => item => ((Person)item).LastReview is null
            ? new DataGridCellStyle
            {
                BackgroundColor = Color.FromRgba(245, 158, 11, 46),
                FontAttributes = FontAttributes.Bold
            }
            : null;
}

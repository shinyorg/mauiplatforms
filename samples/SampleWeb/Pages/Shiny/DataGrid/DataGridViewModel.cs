using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Shiny.Maui.Controls;

using Shiny;

namespace Sample.Features.DataGrid;

public partial class DataGridViewModel : ObservableObject
{
    [ObservableProperty]
    string statusMessage = "Tap a row to select";

    [ObservableProperty]
    object? selectedItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrozenCount))]
    bool freezeFirstColumn = true;

    /// <summary>DataGrid.FrozenColumns takes a count, the demo toggle is a switch.</summary>
    public int FrozenCount => this.FreezeFirstColumn ? 1 : 0;

    public ObservableCollection<Person> People { get; } =
    [
        new("Ada", "Lovelace", 36, "Engineering", 142000, true),
        new("Alan", "Turing", 41, "Research", 158000, true),
        new("Grace", "Hopper", 52, "Engineering", 165000, false),
        new("Katherine", "Johnson", 44, "Mathematics", 134000, true),
        new("Margaret", "Hamilton", 39, "Software", 151000, true),
        new("Edsger", "Dijkstra", 47, "Research", 149000, false),
        new("Donald", "Knuth", 58, "Mathematics", 172000, true),
        new("Barbara", "Liskov", 49, "Software", 161000, true),
    ];

    /// <summary>
    /// Stands in for an API call. The loader returns nothing - it fills an observable property on the
    /// item, and the detail template binds to it as usual (and is not built until this completes).
    /// </summary>
    public Func<object, Task> LoadActivity
        => async item =>
        {
            var person = (Person)item;
            await Task.Delay(900);
            person.RecentActivity = $"{person.LastName.Length + 3} approvals this quarter";
        };

    partial void OnSelectedItemChanged(object? value)
    {
        if (value is Person p)
            this.StatusMessage = $"Selected {p.FirstName} {p.LastName}";
    }
}

public partial class Person(string firstName, string lastName, int age, string department, decimal salary, bool active) : ObservableObject
{
    [ObservableProperty] string firstName = firstName;
    [ObservableProperty] string lastName = lastName;
    [ObservableProperty] int age = age;
    [ObservableProperty] string department = department;
    [ObservableProperty] decimal salary = salary;
    [ObservableProperty] bool active = active;

    /// <summary>Filled by the grid's RowDetailLoader the first time this row is expanded.</summary>
    [ObservableProperty] string recentActivity = "";

    public string StatusText => this.Active ? "Active" : "Inactive";
    public PillType StatusPill => this.Active ? PillType.Success : PillType.Caution;

    // Derived rather than seeded, so the formatting page can exercise every preset (including a
    // genuine null) without another six constructor arguments.
    public double Utilisation => (this.Age % 30 + 55) / 100d;
    public long MailboxBytes => (long)this.Salary * 6_400;
    public DateTime StartDate => new DateTime(2014, 1, 1).AddMonths(this.Age % 40 * 3);
    public DateTime? LastReview => this.Active ? this.StartDate.AddYears(this.Age % 4 + 1) : null;
    public ReviewState State => this.Active ? ReviewState.SignedOff : ReviewState.AwaitingReview;
    public int YearsOfService => DateTime.Today.Year - this.StartDate.Year;

    public string Notes
        => $"{this.FirstName} {this.LastName} works in {this.Department} and has been with the company for {this.YearsOfService} years.";
}

public enum ReviewState
{
    [System.ComponentModel.Description("Signed off")]
    SignedOff,

    AwaitingReview
}

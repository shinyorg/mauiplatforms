using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny.Maui.Controls;

namespace Sample.Features.Avatars;

public partial class AvatarViewModel : ObservableObject
{
    static readonly string[] Names =
    [
        "Grace Hopper", "Ada Lovelace", "Alan Turing", "Linus Torvalds", "Margaret Hamilton",
        "Dennis Ritchie", "Barbara Liskov", "Ken Thompson", "Radia Perlman", "Donald Knuth"
    ];

    [ObservableProperty] AvatarStatus status = AvatarStatus.Online;
    [ObservableProperty] int totalPeople = 48;
    [ObservableProperty] string message = string.Empty;

    public ObservableCollection<string> People { get; } = new(Names.Take(7));

    [RelayCommand]
    void CycleStatus()
        => this.Status = this.Status == AvatarStatus.Offline ? AvatarStatus.None : this.Status + 1;

    [RelayCommand]
    void AddPerson()
    {
        this.People.Add(Names[this.People.Count % Names.Length]);
        this.TotalPeople++;
    }

    [RelayCommand]
    void RemovePerson()
    {
        if (this.People.Count == 0)
            return;

        this.People.RemoveAt(this.People.Count - 1);
        this.TotalPeople--;
    }

    [RelayCommand]
    void ShowEveryone(int hidden)
        => this.Message = $"Tapped +{hidden} - this is where the full list of {this.TotalPeople} would open.";
}

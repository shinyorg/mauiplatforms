using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Maui.Controls.Kanban;

namespace Sample.Features.Kanban;

public partial class KanbanViewModel : ObservableObject
{
    /// <summary>
    /// Every applied move, newest first. A <see cref="KanbanMove"/> records the column, swimlane and
    /// index the card came from, so undo is a stack of these and nothing else.
    /// </summary>
    readonly Stack<KanbanMove> undo = new();

    int added;

    [ObservableProperty]
    string statusMessage = "Drag a card between columns, or up and down inside one";

    [ObservableProperty]
    KanbanSwimlaneMode swimlaneMode = KanbanSwimlaneMode.None;

    [ObservableProperty]
    KanbanWipBehavior wipBehavior = KanbanWipBehavior.Warn;

    [ObservableProperty]
    KanbanCard? selectedCard;

    [ObservableProperty]
    bool canUndo;

    public ObservableCollection<KanbanColumn> Columns { get; } = SampleBoard.CreateColumns();
    public ObservableCollection<KanbanSwimlane> Swimlanes { get; } = SampleBoard.CreateSwimlanes();
    public ObservableCollection<KanbanCard> Cards { get; } = SampleBoard.CreateCards();


    /// <summary>Whether swimlanes are on, as a bool the sample's Switch can bind two-way to.</summary>
    public bool UseSwimlanes
    {
        get => this.SwimlaneMode == KanbanSwimlaneMode.Grouped;
        set
        {
            this.SwimlaneMode = value ? KanbanSwimlaneMode.Grouped : KanbanSwimlaneMode.None;
            this.OnPropertyChanged();
        }
    }


    /// <summary>Whether a full column refuses a drop, likewise.</summary>
    public bool BlockAtWipLimit
    {
        get => this.WipBehavior == KanbanWipBehavior.Block;
        set
        {
            this.WipBehavior = value ? KanbanWipBehavior.Block : KanbanWipBehavior.Warn;
            this.OnPropertyChanged();
        }
    }


    public void Record(KanbanMove move)
    {
        this.undo.Push(move);
        this.CanUndo = true;
        this.StatusMessage = $"Moved \"{move.Card.Title}\" to {this.TitleOf(move.ToColumnId)}";
    }


    [RelayCommand]
    void Undo()
    {
        if (!this.undo.TryPop(out var move))
            return;

        move.Card.ColumnId = move.FromColumnId;
        move.Card.SwimlaneId = move.FromSwimlaneId.Length == 0 ? null : move.FromSwimlaneId;
        move.Card.Order = move.FromIndex;

        this.CanUndo = this.undo.Count > 0;
        this.StatusMessage = $"Put \"{move.Card.Title}\" back in {this.TitleOf(move.FromColumnId)}";
    }


    [RelayCommand]
    void Reset()
    {
        this.undo.Clear();
        this.CanUndo = false;
        this.added = 0;

        this.Cards.Clear();
        foreach (var card in SampleBoard.CreateCards())
            this.Cards.Add(card);

        foreach (var column in this.Columns)
            column.IsCollapsed = false;

        foreach (var swimlane in this.Swimlanes)
            swimlane.IsCollapsed = false;

        this.StatusMessage = "Board reset";
    }


    /// <summary>
    /// The control raises the request and nothing else - creating the card is the app's job, which is
    /// the whole point of the contract. Here that means one line.
    /// </summary>
    public void AddCard(KanbanAddCardEventArgs e)
    {
        this.Cards.Add(new KanbanCard
        {
            Id = $"new-{++this.added}",
            Title = e.Title.Length > 0 ? e.Title : $"New card {this.added}",
            ColumnId = e.Column.Id,
            SwimlaneId = e.Swimlane?.Id,
            Order = int.MaxValue
        });

        this.StatusMessage = $"Added a card to {e.Column.Title}";
    }


    string TitleOf(string columnId)
        => this.Columns.FirstOrDefault(c => c.Id == columnId)?.Title ?? columnId;
}

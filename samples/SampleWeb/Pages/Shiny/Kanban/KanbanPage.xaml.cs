using Shiny.Maui.Controls.Kanban;

namespace Sample.Features.Kanban;

public partial class KanbanPage : ContentPage
{
    public KanbanPage()
    {
        InitializeComponent();
        this.FitColumnsToIdiom();
    }


    /// <summary>
    /// 260pt columns are a desktop layout. On a 420pt phone that is one and a half columns on
    /// screen, which makes a drag across the board a long haul even with auto-scroll.
    /// </summary>
    void FitColumnsToIdiom()
    {
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
            this.Board.ColumnWidth = 200;
    }


    KanbanViewModel? Model => this.BindingContext as KanbanViewModel;


    /// <summary>
    /// Refuses a move into Done unless the card has been through review. Shows the point of the
    /// cancellable event: the whole consequence is in hand before anything has moved.
    /// </summary>
    void OnCardMoving(object? sender, KanbanCardMovingEventArgs e)
    {
        if (e.Move.ToColumnId != "done" || e.Move.FromColumnId == "review")
            return;

        e.Cancel = true;

        if (this.Model is not null)
            this.Model.StatusMessage = $"\"{e.Card.Title}\" has to go through review first";
    }


    void OnCardMoved(object? sender, KanbanCardMovedEventArgs e) => this.Model?.Record(e.Move);


    void OnDropRejected(object? sender, KanbanDropRejectedEventArgs e)
    {
        if (this.Model is null)
            return;

        this.Model.StatusMessage = e.Reason switch
        {
            KanbanDropRejection.WipLimitReached => $"{e.Column?.Title} is at its WIP limit",
            KanbanDropRejection.CardLocked => $"\"{e.Card.Title}\" is locked",
            KanbanDropRejection.ColumnRejectsDrop => $"{e.Column?.Title} does not take drops",
            _ => "That card has nowhere to land"
        };
    }


    void OnAddCardRequested(object? sender, KanbanAddCardEventArgs e) => this.Model?.AddCard(e);

    void OnCollapseAll(object? sender, EventArgs e) => this.Board.CollapseAllColumns();

    void OnExpandAll(object? sender, EventArgs e) => this.Board.ExpandAllColumns();
}

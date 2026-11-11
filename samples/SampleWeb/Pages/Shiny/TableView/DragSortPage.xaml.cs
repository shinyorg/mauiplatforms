using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Shiny;
using Shiny.Maui.Controls;

namespace Sample.Features.TableView;

public partial class DragSortPage : ContentPage
{
    public DragSortPage()
    {
        InitializeComponent();
    }
}

public class DragSortViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public DragSortViewModel()
    {
        this.ItemDroppedCommand = new Command<ItemDroppedEventArgs>(this.OnItemDropped);
    }


    public ObservableCollection<Player> Players { get; } = new(
    [
        new Player("Ada Lovelace", "1"),
        new Player("Grace Hopper", "2"),
        new Player("Alan Turing", "3"),
        new Player("Katherine Johnson", "4")
    ]);

    public ICommand ItemDroppedCommand { get; }


    void OnItemDropped(ItemDroppedEventArgs args)
    {
        System.Diagnostics.Debug.WriteLine($"Moved item from index {args.FromIndex} to {args.ToIndex}");

        // Rows built from ItemsSource aren't owned by the section, so their order lives here.
        // args.Item is the moved row's binding context.
        if (args.Item is Player player)
        {
            var from = this.Players.IndexOf(player);
            if (from >= 0 && from != args.ToIndex)
                this.Players.Move(from, args.ToIndex);
        }
    }
}

public record Player(string Name, string Position);

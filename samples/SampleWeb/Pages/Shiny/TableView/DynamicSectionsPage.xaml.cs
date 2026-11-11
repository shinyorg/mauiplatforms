using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Shiny;

namespace Sample.Features.TableView;

public partial class DynamicSectionsPage : ContentPage
{
    public DynamicSectionsPage()
    {
        InitializeComponent();
    }
}

public class DynamicSectionsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    int counter = 1;

    public ObservableCollection<DynamicItem> Items { get; } = new()
    {
        new DynamicItem { Name = "Item 1", Value = "Value 1" },
        new DynamicItem { Name = "Item 2", Value = "Value 2" },
        new DynamicItem { Name = "Item 3", Value = "Value 3" }
    };

    public ICommand AddCommand { get; }
    public ICommand RemoveCommand { get; }

    public DynamicSectionsViewModel()
    {
        AddCommand = new Command(() =>
        {
            counter++;
            Items.Add(new DynamicItem { Name = $"Item {Items.Count + 1}", Value = $"Value {counter}" });
        });

        RemoveCommand = new Command(() =>
        {
            if (Items.Count > 0)
                Items.RemoveAt(Items.Count - 1);
        });
    }
}

public class DynamicItem : INotifyPropertyChanged
{
    string name = string.Empty;
    string _value = string.Empty;

    public string Name
    {
        get => name;
        set { name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
    }

    public string Value
    {
        get => _value;
        set { _value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

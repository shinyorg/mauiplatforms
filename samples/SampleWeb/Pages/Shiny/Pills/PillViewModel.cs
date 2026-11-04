using CommunityToolkit.Mvvm.ComponentModel;

using Shiny;

namespace Sample.Features.Pills;

public partial class PillViewModel : ObservableObject
{
    [ObservableProperty]
    string customPillText = "Custom";
}

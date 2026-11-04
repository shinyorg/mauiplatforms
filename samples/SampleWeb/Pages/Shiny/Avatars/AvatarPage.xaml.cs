namespace Sample.Features.Avatars;

public partial class AvatarPage : ContentPage
{
    public AvatarPage()
    {
        InitializeComponent();
        BindingContext = new AvatarViewModel();
    }
}

namespace Sample.Features.Marquee;

public record Review(string Name, string Handle, string Body);

public partial class MarqueePage : ContentPage
{
    public MarqueePage()
    {
        InitializeComponent();
        this.BindingContext = this;
    }

    public IReadOnlyList<Review> Reviews { get; } =
    [
        new("Jack", "@jack", "I've never seen anything like this before. It's amazing."),
        new("Jill", "@jill", "I don't know what to say. I'm speechless."),
        new("John", "@john", "I'm at a loss for words. This is amazing."),
        new("Jane", "@jane", "Honestly the smoothest marquee I've dropped into an app."),
        new("Jenny", "@jenny", "Works on my phone and my desktop with the same XAML."),
        new("James", "@james", "Any angle! Even the tilted one keeps its text level.")
    ];

    public IReadOnlyList<string> Tags { get; } =
    [
        ".NET MAUI", "Blazor", "Shiny", "C#", "XAML", "iOS", "Android", "Windows", "macOS", "Linux"
    ];
}

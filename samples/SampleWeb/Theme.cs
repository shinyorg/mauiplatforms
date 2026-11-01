using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Graphics;

namespace SampleWeb;

/// <summary>
/// The gallery's design tokens and implicit styles. Styles reference colors as dynamic
/// resources that <see cref="Apply"/> swaps when the theme changes, and views bound with
/// <see cref="Bind"/> use SetAppTheme, so everything follows light/dark and UserAppTheme.
/// The matching CSS variables for the Shell chrome live in wwwroot/index.html.
/// </summary>
public static class Theme
{
	public const string Display = "Bricolage";
	public const string Body = "InstrumentSans";
	public const string Icons = "Lucide";

	public static readonly Token Paper = new("Paper", Color.FromArgb("#FBFAFE"), Color.FromArgb("#121019"));
	public static readonly Token Ink = new("Ink", Color.FromArgb("#1B1530"), Color.FromArgb("#EEEAFB"));
	public static readonly Token Muted = new("Muted", Color.FromArgb("#6A6483"), Color.FromArgb("#A39DBB"));
	public static readonly Token Stage = new("Stage", Color.FromArgb("#F1EEFA"), Color.FromArgb("#1C1828"));
	public static readonly Token Line = new("Line", Color.FromArgb("#E2DDF2"), Color.FromArgb("#2C2740"));
	public static readonly Token Violet = new("Violet", Color.FromArgb("#5B3FE0"), Color.FromArgb("#9B87FF"));
	public static readonly Token OnViolet = new("OnViolet", Colors.White, Color.FromArgb("#120F1E"));
	public static readonly Token Lagoon = new("Lagoon", Color.FromArgb("#0E9F9A"), Color.FromArgb("#3DD6CF"));
	public static readonly Token VioletWash = new("VioletWash", Color.FromArgb("#E7E1FF"), Color.FromArgb("#2A2350"));

	/// <summary>Icon color that reads on both themes (rasterized icons can't follow theme changes).</summary>
	public static readonly Color IconTint = Color.FromArgb("#8A84A3");

	public sealed record Token(string Key, Color Light, Color Dark)
	{
		public string BrushKey => Key + "Brush";
	}

	static readonly Token[] Tokens = [Paper, Ink, Muted, Stage, Line, Violet, OnViolet, Lagoon, VioletWash];

	/// <summary>A style setter value that tracks the token across theme changes.</summary>
	public static DynamicResource Pick(Token token) => new(token.Key);

	public static DynamicResource PickBrush(Token token) => new(token.BrushKey);

	/// <summary>Writes every token's color (and brush) for the theme into the resources.</summary>
	public static void Apply(ResourceDictionary resources, Microsoft.Maui.ApplicationModel.AppTheme theme)
	{
		var dark = theme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;
		foreach (var token in Tokens)
		{
			var color = dark ? token.Dark : token.Light;
			resources[token.Key] = color;
			resources[token.BrushKey] = new SolidColorBrush(color);
		}
	}

	public static Color Current(Token pair)
		=> Application.Current?.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark ? pair.Dark : pair.Light;

	public static void Bind(BindableObject target, BindableProperty property, Token pair)
		=> target.SetAppTheme(property, pair.Light, pair.Dark);

	public static void BindBrush(BindableObject target, BindableProperty property, Token pair)
		=> target.SetAppTheme<Brush>(property, new SolidColorBrush(pair.Light), new SolidColorBrush(pair.Dark));

	public static ResourceDictionary CreateResources()
	{
		var resources = new ResourceDictionary();

		resources.Add(Style<ContentPage>(
			(VisualElement.BackgroundColorProperty, Pick(Paper))));

		resources.Add(Style<Label>(
			(Label.FontFamilyProperty, Body),
			(Label.FontSizeProperty, 15d),
			(Label.TextColorProperty, Pick(Ink))));

		var button = Style<Button>(
			(Button.FontFamilyProperty, Body),
			(Button.FontSizeProperty, 15d),
			(Button.FontAttributesProperty, FontAttributes.Bold),
			(Button.CornerRadiusProperty, 10),
			(Button.PaddingProperty, new Thickness(18, 10)),
			(VisualElement.BackgroundColorProperty, Pick(Violet)),
			(Button.TextColorProperty, Pick(OnViolet)));
		resources.Add(button);

		// StyleClass="secondary": a quiet outlined button for less important actions.
		resources.Add(new Style(typeof(Button))
		{
			Class = "secondary",
			BasedOn = button,
			Setters =
			{
				new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.Transparent },
				new Setter { Property = Button.TextColorProperty, Value = Pick(Violet) },
				new Setter { Property = Button.BorderColorProperty, Value = Pick(Line) },
				new Setter { Property = Button.BorderWidthProperty, Value = 1.5 },
			},
		});

		foreach (var type in new[] { typeof(Entry), typeof(Editor), typeof(SearchBar) })
		{
			resources.Add(new Style(type)
			{
				Setters =
				{
					new Setter { Property = InputView.FontFamilyProperty, Value = Body },
					new Setter { Property = InputView.FontSizeProperty, Value = 15d },
					new Setter { Property = InputView.TextColorProperty, Value = Pick(Ink) },
					new Setter { Property = InputView.PlaceholderColorProperty, Value = Pick(Muted) },
				},
			});
		}

		resources.Add(Style<Picker>((Picker.FontFamilyProperty, Body), (Picker.TextColorProperty, Pick(Ink))));
		resources.Add(Style<DatePicker>((DatePicker.FontFamilyProperty, Body), (DatePicker.TextColorProperty, Pick(Ink))));
		resources.Add(Style<TimePicker>((TimePicker.FontFamilyProperty, Body), (TimePicker.TextColorProperty, Pick(Ink))));
		resources.Add(Style<RadioButton>((RadioButton.FontFamilyProperty, Body), (RadioButton.TextColorProperty, Pick(Ink))));
		resources.Add(Style<CheckBox>((CheckBox.ColorProperty, Pick(Violet))));
		resources.Add(Style<Switch>((Switch.OnColorProperty, Pick(Lagoon))));
		resources.Add(Style<Slider>((Slider.MinimumTrackColorProperty, Pick(Violet))));
		resources.Add(Style<ProgressBar>((ProgressBar.ProgressColorProperty, Pick(Lagoon))));
		resources.Add(Style<ActivityIndicator>((ActivityIndicator.ColorProperty, Pick(Violet))));
		return resources;
	}

	static Style Style<T>(params (BindableProperty Property, object Value)[] setters) where T : BindableObject
	{
		var style = new Style(typeof(T)) { ApplyToDerivedTypes = true };
		foreach (var (property, value) in setters)
			style.Setters.Add(new Setter { Property = property, Value = value });
		return style;
	}

	public static FontImageSource Icon(string glyph, Color? color = null, double size = 20)
		=> new() { FontFamily = Icons, Glyph = glyph, Size = size, Color = color ?? IconTint };

	/// <summary>Lucide icon font code points used by the gallery.</summary>
	public static class Glyph
	{
		public const string House = "";
		public const string Toggle = "";
		public const string Type = "";
		public const string LayoutGrid = "";
		public const string Shapes = "";
		public const string List = "";
		public const string Panels = "";
		public const string Smartphone = "";
		public const string Globe = "";
		public const string Chart = "";
		public const string ChevronRight = "";
		public const string Layers = "";
		public const string Moon = "";
	}
}

/// <summary>
/// A gallery page: a title and introduction followed by specimens, each a short heading,
/// a sentence about what it shows, the live controls on a tinted stage, and a readout of
/// the MAUI values they produce.
/// </summary>
public class SpecimenPage : ContentPage
{
	readonly VerticalStackLayout _column;

	public SpecimenPage(string title, string intro)
	{
		Title = title;
		_column = new VerticalStackLayout
		{
			Spacing = 44,
			MaximumWidthRequest = 760,
			HorizontalOptions = LayoutOptions.Start,
			Children =
			{
				new VerticalStackLayout
				{
					Spacing = 10,
					Children =
					{
						new Label { Text = title, FontFamily = Theme.Display, FontSize = 36, FontAttributes = FontAttributes.Bold, CharacterSpacing = -0.6 },
						Muted(intro, 17),
					},
				},
			},
		};
		Content = new ScrollView { Content = _column };
	}

	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);
		var wide = width >= 700;
		var padding = wide ? new Thickness(48, 44, 48, 64) : new Thickness(20, 24, 20, 48);
		if (_column.Padding != padding)
			_column.Padding = padding;
	}

	protected void Add(View view) => _column.Add(view);

	/// <summary>Adds a specimen and returns its readout label (hidden until given text).</summary>
	protected Label Specimen(string title, string description, View stage, string? readout = null)
	{
		var readoutLabel = Muted(readout ?? string.Empty, 13);
		readoutLabel.IsVisible = readout is not null;
		readoutLabel.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Label.Text))
				readoutLabel.IsVisible = !string.IsNullOrEmpty(readoutLabel.Text);
		};

		var stageBorder = new Border
		{
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 16 },
			Padding = new Thickness(24, 22),
			Content = stage,
		};
		Theme.Bind(stageBorder, VisualElement.BackgroundColorProperty, Theme.Stage);

		_column.Add(new VerticalStackLayout
		{
			Spacing = 12,
			Children =
			{
				new VerticalStackLayout
				{
					Spacing = 4,
					Children =
					{
						new Label { Text = title, FontFamily = Theme.Display, FontSize = 21, FontAttributes = FontAttributes.Bold },
						Muted(description, 15),
					},
				},
				stageBorder,
				readoutLabel,
			},
		});
		return readoutLabel;
	}

	public static Label Muted(string text, double size = 15)
	{
		var label = new Label { Text = text, FontSize = size, LineHeight = 1.35 };
		Theme.Bind(label, Label.TextColorProperty, Theme.Muted);
		return label;
	}

	public static HorizontalStackLayout Row(double spacing, params IView[] views)
	{
		var row = new HorizontalStackLayout { Spacing = spacing };
		foreach (var view in views)
			row.Add(view);
		return row;
	}

	public static FlexLayout Wrap(double gap, params View[] views)
	{
		// FlexLayout has no gap property. Spacing comes from padding on a wrapper rather than item
		// margins, because FlexLayout under-reports its height when items have bottom margins.
		var flex = new FlexLayout
		{
			Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
			AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Center,
			Margin = new Thickness(0, 0, 0, -gap),
		};
		foreach (var view in views)
			flex.Add(new ContentView { Padding = new Thickness(0, 0, gap, gap), Content = view });
		return flex;
	}

	public static Button Secondary(string text) => new() { Text = text, StyleClass = ["secondary"] };

	/// <summary>A control with a text label beside it.</summary>
	public static HorizontalStackLayout Labeled(View control, string text)
		=> Row(10, control, new Label { Text = text, VerticalOptions = LayoutOptions.Center });
}

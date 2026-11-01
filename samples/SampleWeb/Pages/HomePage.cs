using System.Runtime.InteropServices;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;

namespace SampleWeb.Pages;

public class HomePage : ContentPage
{
	readonly VerticalStackLayout _column;
	readonly Label _headline;
	readonly WaveDrawable _wave = new();
	readonly GraphicsView _graphics;
	bool _animating;

	public HomePage()
	{
		Title = "Overview";

		_headline = new Label
		{
			Text = "A .NET MAUI app, running as a web page.",
			FontFamily = Theme.Display,
			FontSize = 60,
			FontAttributes = FontAttributes.Bold,
			LineHeight = 1.02,
			CharacterSpacing = -1.8,
			MaximumWidthRequest = 680,
			HorizontalOptions = LayoutOptions.Start,
		};

		var intro = SpecimenPage.Muted(
			"Every control in this gallery is a MAUI view rendered to real DOM elements from WebAssembly. Same handlers, same layout engine, same C#. No Blazor, no canvas. The pages are ported from the maui-labs Linux GTK4 sample.",
			18);
		intro.MaximumWidthRequest = 600;
		intro.HorizontalOptions = LayoutOptions.Start;

		_graphics = new GraphicsView { Drawable = _wave, HeightRequest = 200 };
		var frequency = new Slider { Minimum = 1, Maximum = 6, Value = _wave.Frequency };
		var amplitude = new Slider { Minimum = 0.1, Maximum = 1, Value = _wave.Amplitude };
		var animate = new Switch { IsToggled = true, VerticalOptions = LayoutOptions.Center };
		var readout = SpecimenPage.Muted(string.Empty, 13);

		void Update()
		{
			_wave.Frequency = (float)frequency.Value;
			_wave.Amplitude = (float)amplitude.Value;
			_graphics.Invalidate();
			readout.Text = $"Slider.Value {frequency.Value:F1} and {amplitude.Value:F2}, drawn by an IDrawable on a GraphicsView{(animate.IsToggled ? ", animated by the MAUI ticker" : string.Empty)}.";
		}

		frequency.ValueChanged += (_, _) => Update();
		amplitude.ValueChanged += (_, _) => Update();
		animate.Toggled += (_, e) => { if (e.Value) StartAnimation(); else _animating = false; Update(); };
		Update();

		var controls = new Grid
		{
			ColumnSpacing = 24,
			RowSpacing = 6,
			ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Auto) },
			RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto) },
		};
		controls.Add(new Label { Text = "Frequency", FontSize = 13 }, 0, 0);
		controls.Add(frequency, 0, 1);
		controls.Add(new Label { Text = "Amplitude", FontSize = 13 }, 1, 0);
		controls.Add(amplitude, 1, 1);
		controls.Add(new Label { Text = "Animate", FontSize = 13 }, 2, 0);
		controls.Add(animate, 2, 1);

		var stage = new Border
		{
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 20 },
			Padding = new Thickness(24, 20, 24, 22),
			Content = new VerticalStackLayout { Spacing = 18, Children = { _graphics, controls } },
		};
		Theme.Bind(stage, VisualElement.BackgroundColorProperty, Theme.Stage);

		var tour = new VerticalStackLayout { Spacing = 0 };
		tour.Add(new Label { Text = "Start here", FontFamily = Theme.Display, FontSize = 21, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 0, 0, 10) });
		foreach (var (icon, title, description) in Sections)
			tour.Add(TourRow(icon, title, description));

		var footer = SpecimenPage.Muted($"Running {RuntimeInformation.FrameworkDescription} in {DeviceInfo.Current.Model} on {DeviceInfo.Current.Name}.", 13);

		_column = new VerticalStackLayout
		{
			Spacing = 28,
			MaximumWidthRequest = 820,
			HorizontalOptions = LayoutOptions.Start,
			Children = { _headline, intro, new VerticalStackLayout { Spacing = 10, Children = { stage, readout } }, tour, footer },
		};

		Content = new ScrollView { Content = _column };
	}

	static readonly (string Icon, string Title, string Description)[] Sections =
	[
		("🎛️", "Controls", "Buttons, toggles, sliders, steppers and progress"),
		("📐", "Layouts", "Grid and stack layouts, measured by MAUI's own layout engine"),
		("📋", "Collection View", "Templates, selection, grouping and live updates"),
		("🎨", "Graphics (Canvas)", "Microsoft.Maui.Graphics drawing on a 2D canvas"),
		("🐚", "Shell Navigation", "A full Shell app with flyout, tabs and routes"),
		("📱", "Device & App Info", "MAUI Essentials backed by browser APIs"),
	];

	static View TourRow(string emoji, string title, string description)
	{
		var icon = new Border
		{
			WidthRequest = 40,
			HeightRequest = 40,
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 12 },
			VerticalOptions = LayoutOptions.Center,
			Content = new Label { Text = emoji, FontSize = 20, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center },
		};
		Theme.Bind(icon, VisualElement.BackgroundColorProperty, Theme.Stage);
		var chevron = new Label { Text = Theme.Glyph.ChevronRight, FontFamily = Theme.Icons, FontSize = 18, VerticalOptions = LayoutOptions.Center };
		Theme.Bind(chevron, Label.TextColorProperty, Theme.Muted);

		var text = new VerticalStackLayout
		{
			Spacing = 2,
			VerticalOptions = LayoutOptions.Center,
			Children = { new Label { Text = title, FontAttributes = FontAttributes.Bold }, SpecimenPage.Muted(description, 14) },
		};

		var row = new Grid
		{
			Padding = new Thickness(4, 14),
			ColumnSpacing = 16,
			ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
		};
		row.Add(icon, 0, 0);
		row.Add(text, 1, 0);
		row.Add(chevron, 2, 0);

		var divider = new BoxView { HeightRequest = 1 };
		Theme.Bind(divider, BoxView.ColorProperty, Theme.Line);

		var container = new VerticalStackLayout { Children = { divider, row } };
		container.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => MainShell.Current?.Navigate(title)) });
		SemanticProperties.SetDescription(container, $"{title}: {description}");
		return container;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		StartAnimation();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_animating = false;
	}

	void StartAnimation()
	{
		if (_animating)
			return;
		_animating = true;
		new Animation(v => { _wave.Phase = (float)v; _graphics.Invalidate(); }, 0, Math.PI * 2)
			.Commit(this, "wave", length: 2400, easing: Easing.Linear, repeat: () => _animating);
	}

	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);
		var wide = width >= 700;
		_headline.FontSize = wide ? 60 : 38;
		_headline.CharacterSpacing = wide ? -1.8 : -1;
		var padding = wide ? new Thickness(48, 56, 48, 64) : new Thickness(20, 28, 20, 48);
		if (_column.Padding != padding)
			_column.Padding = padding;
	}

	/// <summary>Three phase-shifted sine waves; the front one in the accent color.</summary>
	sealed class WaveDrawable : IDrawable
	{
		public float Frequency { get; set; } = 2.5f;
		public float Amplitude { get; set; } = 0.7f;
		public float Phase { get; set; }

		public void Draw(ICanvas canvas, RectF rect)
		{
			var mid = rect.Height / 2;
			canvas.StrokeColor = Theme.Current(Theme.Line);
			canvas.StrokeSize = 1;
			canvas.DrawLine(0, mid, rect.Width, mid);

			var layers = new (float Offset, float Scale, Color Color, float Size)[]
			{
				(1.4f, 0.55f, Theme.Current(Theme.Lagoon).WithAlpha(0.45f), 2),
				(0.7f, 0.8f, Theme.Current(Theme.Violet).WithAlpha(0.35f), 2),
				(0f, 1f, Theme.Current(Theme.Violet), 3.5f),
			};
			foreach (var (offset, scale, color, size) in layers)
			{
				var path = new PathF();
				for (var x = 0f; x <= rect.Width; x += 4)
				{
					var t = x / rect.Width;
					var y = mid - MathF.Sin(t * Frequency * MathF.PI * 2 + Phase + offset) * Amplitude * scale * (mid - 8);
					if (x == 0)
						path.MoveTo(x, y);
					else
						path.LineTo(x, y);
				}
				canvas.StrokeColor = color;
				canvas.StrokeSize = size;
				canvas.StrokeLineCap = LineCap.Round;
				canvas.StrokeLineJoin = LineJoin.Round;
				canvas.DrawPath(path);
			}
		}
	}
}

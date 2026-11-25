using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Platform.Web;
using Microsoft.Maui.Storage;
using Shiny.Maui.Controls;

namespace SampleWeb;

/// <summary>
/// An animated backdrop behind the whole gallery, chosen on the Animated Backgrounds page. It is a
/// Shiny <see cref="AnimatedBackground"/> on the window's underlay layer (behind every page), and
/// while one is chosen the gallery's pages are given a translucent background (see
/// <see cref="PageColor"/>) so it shows through. The choice is kept in Preferences.
/// </summary>
public static class AppBackground
{
	/// <summary>One backdrop: the effect and the colour it is drawn on, as the gallery card shows it.</summary>
	public sealed record Choice(string Key, string Name, Func<BackgroundEffect> CreateEffect, Func<Brush> CreateBackground);

	static Func<Brush> Solid(string color) => () => new SolidColorBrush(Color.FromArgb(color));

	public static IReadOnlyList<Choice> Choices { get; } =
	[
		new("Aurora", "Aurora", () => new AuroraEffect(), Solid("#0B1020")),
		new("Particles", "Particles", () => new ParticlesEffect { LinkDistance = 70 }, Solid("#0B1020")),
		new("Snow", "Snow", () => new SnowEffect { Wind = 0.4 }, Solid("#0B1020")),
		new("Rain", "Rain", () => new RainEffect(), Solid("#0F172A")),
		new("Meteors", "Meteors", () => new MeteorsEffect { Density = 1.5, MaxDelay = TimeSpan.FromSeconds(1.5), Length = 60 }, Solid("#020617")),
		new("Dots", "Dot spotlight", () => new DotPatternEffect { Spacing = 14, SpotlightColor = Color.FromArgb("#A78BFA"), SpotlightRadius = 70, Glow = 0.25 }, Solid("#09090B")),
		new("Noise", "Noise over a gradient", () => new NoiseEffect { Intensity = 0.25 }, () => new LinearGradientBrush(
			[new GradientStop(Color.FromArgb("#4F46E5"), 0), new GradientStop(Color.FromArgb("#DB2777"), 1)], new Point(0, 0), new Point(1, 1))),
		new("Warp", "Starfield (warp)", () => new StarfieldEffect { Warp = true, Density = 12 }, Solid("#000000")),
		new("Fireflies", "Fireflies", () => new FirefliesEffect { Density = 1.2 }, Solid("#052e16")),
		new("Bubbles", "Bubbles", () => new BubblesEffect { Density = 1.5, MaxSize = 12 }, Solid("#082F49")),
		new("Waves", "Waves", () => new WavesEffect { Level = 0.45, Amplitude = 10, Wavelength = 160 }, Solid("#0C1222")),
		new("Matrix", "Matrix", () => new MatrixEffect { FontSize = 12 }, Solid("#000000")),
	];

	const string PreferenceKey = "app-background";

	static Window? s_window;
	static AnimatedBackground? s_view;

	/// <summary>The chosen backdrop, or null for none.</summary>
	public static Choice? Current { get; private set; } = Restore();

	/// <summary>Raised after <see cref="Current"/> changes.</summary>
	public static event EventHandler? Changed;

	/// <summary>
	/// What a gallery page's background should be: the theme's paper colour, made translucent while a
	/// backdrop is showing. Dark pages let more of it through; light ones keep more paper so dark text
	/// stays easy to read over a dark effect.
	/// </summary>
	public static Color PageColor(Color paper, bool dark)
		=> Current is null ? paper : paper.WithAlpha(dark ? 0.68f : 0.8f);

	/// <summary>Shows the saved backdrop on the window, once it has a platform window to show it on.</summary>
	public static void Attach(Window window)
	{
		s_window = window;
		window.HandlerChanged += (_, _) => Apply();
		Apply();
	}

	/// <summary>Makes <paramref name="choice"/> the app background; null clears it.</summary>
	public static void Select(Choice? choice)
	{
		if (Current == choice)
			return;

		Current = choice;
		Save(choice);
		Apply();
		// The page background is a theme resource; rewriting the theme refreshes it everywhere.
		if (Application.Current is { } app)
			Theme.Apply(app.Resources, app.RequestedTheme);
		Changed?.Invoke(null, EventArgs.Empty);
	}

	static void Apply()
	{
		var choice = Current;
		if (s_view is null && choice is not null)
			s_view = Create();

		if (s_view is null)
			return;

		// No effect means no renderer, which stops its frame ticker: a cleared backdrop costs nothing.
		s_view.Effect = choice?.CreateEffect();
		s_view.Background = choice?.CreateBackground();
		s_view.IsVisible = choice is not null;
	}

	static AnimatedBackground? Create()
	{
		if (s_window?.Handler is not { MauiContext: { } context, PlatformView: WebWindow webWindow })
			return null;

		var view = new AnimatedBackground { InputTransparent = true };
		// Parented to the window so it counts as loaded (it only animates while loaded) and finds the dispatcher.
		view.Parent = s_window;
		webWindow.AddUnderlay(view, (WebElement)view.ToPlatform(context));
		return view;
	}

	static Choice? Restore()
	{
		try
		{
			var key = Preferences.Default.Get(PreferenceKey, string.Empty);
			return Choices.FirstOrDefault(c => c.Key == key);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"App background could not be restored: {ex.Message}");
			return null;
		}
	}

	static void Save(Choice? choice)
	{
		try
		{
			if (choice is null)
				Preferences.Default.Remove(PreferenceKey);
			else
				Preferences.Default.Set(PreferenceKey, choice.Key);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"App background could not be saved: {ex.Message}");
		}
	}
}

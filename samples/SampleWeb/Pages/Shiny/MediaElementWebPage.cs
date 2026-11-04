using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Shiny.Controls.Media;
using Shiny.Maui.Controls.Media;

namespace SampleWeb.Pages.Shiny;

/// <summary>The Shiny MediaElement on the browser backend (Shiny.Maui.Controls.MediaElement.Web).</summary>
public class MediaElementWebPage : SpecimenPage
{
	static readonly (string Name, string Url)[] Sources =
	[
		("Flower (MP4)", "https://interactive-examples.mdn.mozilla.net/media/cc0-videos/flower.mp4"),
		("Sintel trailer (MP4)", "https://media.w3.org/2010/05/sintel/trailer.mp4"),
		("SoundHelix song (MP3)", "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3"),
	];

	readonly MediaElement _player;
	readonly Label _status;

	public MediaElementWebPage() : base("MediaElement", "Shiny's MediaElement playing through an HTML video element: the same transport bar, fullscreen and picture-in-picture as the native backends.")
	{
		_player = new MediaElement
		{
			HeightRequest = 340,
			Source = MediaSource.FromUri(Sources[0].Url),
			Metadata = new MediaMetadata { Title = "Flower", Artist = "MDN (CC0)" },
			EnableBackgroundPlayback = true,
		};
		_status = Muted(string.Empty, 13);
		_player.StateChanged += (_, _) => UpdateStatus();
		_player.PositionChanged += (_, _) => UpdateStatus();
		_player.MediaFailed += (_, e) => _status.Text = $"Failed: {e.Message}";

		var stage = new VerticalStackLayout { Spacing = 14, Children = { _player, _status } };
		Specimen("Player", "Play, scrub, change volume, go fullscreen or open picture-in-picture from the transport bar.", stage);

		var sourcePicker = new Picker { Title = "Source", ItemsSource = Sources.Select(s => s.Name).ToList(), SelectedIndex = 0 };
		sourcePicker.SelectedIndexChanged += (_, _) =>
		{
			var source = Sources[Math.Max(0, sourcePicker.SelectedIndex)];
			_player.Metadata = new MediaMetadata { Title = source.Name };
			_player.Source = MediaSource.FromUri(source.Url);
		};

		var aspects = Enum.GetValues<MediaAspect>();
		var aspectPicker = new Picker { Title = "Aspect", ItemsSource = aspects.Select(a => a.ToString()).ToList(), SelectedIndex = 0 };
		aspectPicker.SelectedIndexChanged += (_, _) => _player.Aspect = aspects[Math.Max(0, aspectPicker.SelectedIndex)];

		double[] rates = [0.5, 1, 1.5, 2];
		var ratePicker = new Picker { Title = "Speed", ItemsSource = rates.Select(r => $"{r}×").ToList(), SelectedIndex = 1 };
		ratePicker.SelectedIndexChanged += (_, _) => _player.PlaybackRate = rates[Math.Max(0, ratePicker.SelectedIndex)];

		var loop = new Switch();
		loop.Toggled += (_, e) => _player.IsLooping = e.Value;

		var controls = new Grid
		{
			ColumnSpacing = 16,
			RowSpacing = 12,
			ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) },
			RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto) },
		};
		AddRow(controls, 0, "Source", sourcePicker);
		AddRow(controls, 1, "Aspect", aspectPicker);
		AddRow(controls, 2, "Speed", ratePicker);
		AddRow(controls, 3, "Loop", loop);
		Specimen("Options", "Each of these maps straight onto the video element.", controls);

		var buttons = new FlexLayout
		{
			Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
			Children =
			{
				Action("Play", _player.Play),
				Action("Pause", _player.Pause),
				Action("Stop", _player.Stop),
				Action("Back 10s", () => _ = _player.SeekAsync(_player.Position - TimeSpan.FromSeconds(10))),
				Action("Fullscreen", _player.ToggleFullScreen),
				Action("Picture-in-picture", () => _ = _player.TryEnterPictureInPictureAsync()),
			},
		};
		Specimen("Code", "The same commands, called from C#.", buttons);
	}

	static void AddRow(Grid grid, int row, string label, View view)
	{
		grid.Add(new Label { Text = label, VerticalOptions = LayoutOptions.Center }, 0, row);
		grid.Add(view, 1, row);
	}

	static Button Action(string text, Action action)
	{
		var button = new Button { Text = text, Margin = new Thickness(0, 0, 8, 8) };
		button.StyleClass = ["secondary"];
		button.Clicked += (_, _) => action();
		return button;
	}

	void UpdateStatus()
		=> _status.Text = $"{_player.CurrentState} · {_player.Position:mm\\:ss} / {_player.Duration:mm\\:ss}"
			+ (_player.VideoSize.Width > 0 ? $" · {_player.VideoSize.Width}×{_player.VideoSize.Height}" : string.Empty);

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_player.Pause();
	}
}

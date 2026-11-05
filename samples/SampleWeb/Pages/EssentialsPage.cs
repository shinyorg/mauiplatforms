using Microsoft.Maui;
using Microsoft.Maui.Accessibility;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace SampleWeb.Pages;

/// <summary>MAUI Essentials through their usual static facades, implemented with browser APIs.</summary>
public class EssentialsPage : SpecimenPage
{
	public EssentialsPage() : base("Device APIs", "MAUI Essentials, called exactly as in any MAUI app and answered by browser APIs. Features a browser can't provide throw instead of pretending.")
	{
		var display = new Label();
		var device = Table(
			("App", $"{AppInfo.Name} {AppInfo.VersionString} (build {AppInfo.BuildString})"),
			("Package", AppInfo.PackageName),
			("Device", $"{DeviceInfo.Name} {DeviceInfo.VersionString}, {DeviceInfo.Model}"),
			("Idiom", $"{DeviceInfo.Idiom} on {DeviceInfo.Platform}"),
			("Display", display),
			("Theme", AppInfo.RequestedTheme.ToString()));
		display.Text = Describe(DeviceDisplay.MainDisplayInfo);
		DeviceDisplay.MainDisplayInfoChanged += (_, e) => display.Text = Describe(e.DisplayInfo);
		Specimen("This device", "AppInfo, DeviceInfo and DeviceDisplay. Resize the window and the display row updates from MainDisplayInfoChanged.", device);

		var network = new Label { Text = DescribeNetwork() };
		var battery = new Label { Text = DescribeBattery() };
		Connectivity.ConnectivityChanged += (_, _) => network.Text = DescribeNetwork();
		Battery.BatteryInfoChanged += (_, _) => battery.Text = DescribeBattery();
		Specimen("Network & power", "Connectivity and Battery, with change events. Go offline in your browser's developer tools to see it update.",
			Table(("Network", network), ("Battery", battery)));

		var launches = Preferences.Default.Get("launches", 0) + 1;
		Preferences.Default.Set("launches", launches);
		var note = new Entry { Placeholder = "Type a note, then reload the page", Text = Preferences.Default.Get("note", string.Empty) };
		note.TextChanged += (_, e) => Preferences.Default.Set("note", e.NewTextValue ?? string.Empty);
		var secure = Secondary("Try SecureStorage");
		var storageReadout = Specimen("Preferences", "Preferences persist in localStorage. SecureStorage throws, because a browser has nowhere safe to keep secrets.",
			new VerticalStackLayout { Spacing = 12, Children = { note, secure } },
			$"This page has been opened {launches} time{(launches == 1 ? "" : "s")} in this browser.");
		secure.Clicked += async (_, _) =>
		{
			try
			{
				await SecureStorage.Default.SetAsync("token", "secret");
			}
			catch (PlatformNotSupportedException ex)
			{
				storageReadout.Text = ex.Message;
			}
		};

		var text = new Entry { Text = "Hello from .NET MAUI in the browser" };
		var actions = Specimen("Clipboard & sharing", "Clipboard, Share (the system share sheet where there is one), Browser and Map.",
			new VerticalStackLayout { Spacing = 12, Children = { text, ActionRow(out var copy, "Copy", out var paste, "Paste", out var share, "Share"), ActionRow(out var browser, "Open dotnet.microsoft.com", out var map, "Show Lisbon on a map") } },
			"Choose an action.");
		Run(copy, actions, async () => { await Clipboard.Default.SetTextAsync(text.Text); return "Copied to the clipboard."; });
		Run(paste, actions, async () => $"Clipboard contains \"{await Clipboard.Default.GetTextAsync()}\"");
		Run(share, actions, async () => { await Share.Default.RequestAsync(new ShareTextRequest(text.Text, "MAUI Web")); return "Shared, or copied where sharing isn't available."; });
		Run(browser, actions, async () => { await Browser.Default.OpenAsync("https://dotnet.microsoft.com"); return "Opened in a new tab."; });
		Run(map, actions, async () => { await Map.Default.OpenAsync(38.7223, -9.1393, new MapLaunchOptions { Name = "Lisbon" }); return "Opened a map."; });

		var files = Specimen("Files", "FilePicker copies the chosen file into the in-memory file system; read it with File.OpenRead(file.FullPath), since FileResult.OpenReadAsync is not implemented in MAUI's platform-neutral build. App package files are fetched from the site.",
			ActionRow(out var pick, "Pick a file", out var package, "Read a bundled asset", out var write, "Write app data"), "Choose an action.");
		Run(pick, files, async () =>
		{
			var file = await FilePicker.Default.PickAsync();
			if (file is null)
				return "No file chosen.";
			await using var stream = File.OpenRead(file.FullPath);
			return $"{file.FileName}, {file.ContentType}, {stream.Length:N0} bytes";
		});
		Run(package, files, async () =>
		{
			await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("about.txt");
			return (await new StreamReader(stream).ReadToEndAsync()).Trim();
		});
		Run(write, files, async () =>
		{
			var path = Path.Combine(FileSystem.Current.AppDataDirectory, "hello.txt");
			await File.WriteAllTextAsync(path, $"Written at {DateTime.Now:T}");
			return $"{path} now reads \"{await File.ReadAllTextAsync(path)}\"";
		});

		var preview = new Image { HeightRequest = 220, Aspect = Aspect.AspectFit, HorizontalOptions = LayoutOptions.Start, IsVisible = false };
		var pickPhoto = new Button { Text = "Pick a photo" };
		var pickPhotos = Secondary("Pick up to 3 photos");
		var pickVideo = Secondary("Pick a video");
		var capturePhoto = Secondary("Take a photo");
		var media = Specimen("Photos & video", "MediaPicker opens the browser's file dialog filtered to images or video. Taking a photo opens the camera on phones and tablets, and throws on desktop browsers.",
			new VerticalStackLayout { Spacing = 16, Children = { Wrap(12, pickPhoto, pickPhotos, pickVideo, capturePhoto), preview } },
			MediaPicker.Default.IsCaptureSupported ? "Choose an action." : "Choose an action. Camera capture isn't available on this device.");
		async Task<string> ShowPhoto(FileResult? photo)
		{
			if (photo is null)
				return "Nothing chosen.";
			using var memory = new MemoryStream();
			// Picked files live at FullPath. FileResult.OpenReadAsync() throws on the web: MAUI's
			// platform-neutral Essentials doesn't implement it, and backends can't override it.
			await using (var stream = File.OpenRead(photo.FullPath))
				await stream.CopyToAsync(memory);
			var bytes = memory.ToArray();
			preview.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
			preview.IsVisible = true;
			return $"{photo.FileName}, {photo.ContentType}, {bytes.Length:N0} bytes";
		}
		Run(pickPhoto, media, async () => await ShowPhoto(await MediaPicker.Default.PickPhotoAsync()));
		Run(capturePhoto, media, async () => await ShowPhoto(await MediaPicker.Default.CapturePhotoAsync()));
		Run(pickPhotos, media, async () =>
		{
			var photos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 3 });
			if (photos.Count == 0)
				return "Nothing chosen.";
			var first = await ShowPhoto(photos[0]);
			return photos.Count == 1 ? first : $"{photos.Count} photos: {string.Join(", ", photos.Select(p => p.FileName))}. Showing the first.";
		});
		Run(pickVideo, media, async () =>
		{
			var video = await MediaPicker.Default.PickVideoAsync();
			if (video is null)
				return "Nothing chosen.";
			await using var stream = File.OpenRead(video.FullPath);
			return $"{video.FileName}, {video.ContentType}, {stream.Length:N0} bytes";
		});

		var sensors = Specimen("Location & motion", "Geolocation asks for permission the first time. Motion sensors need a phone or tablet.",
			ActionRow(out var locate, "Get my location", out var motion, "Start accelerometer"), "Choose an action.");
		Run(locate, sensors, async () =>
		{
			var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
			return location is null ? "Location unavailable." : $"{location.Latitude:F4}, {location.Longitude:F4}, accurate to {location.Accuracy:F0} m";
		});
		Accelerometer.Default.ReadingChanged += (_, e) => sensors.Text = $"Acceleration {e.Reading.Acceleration.X:F2}, {e.Reading.Acceleration.Y:F2}, {e.Reading.Acceleration.Z:F2} g";
		Run(motion, sensors, () =>
		{
			if (Accelerometer.Default.IsMonitoring)
			{
				Accelerometer.Default.Stop();
				motion.Text = "Start accelerometer";
				return Task.FromResult("Stopped.");
			}
			Accelerometer.Default.Start(SensorSpeed.UI);
			motion.Text = "Stop accelerometer";
			return Task.FromResult("Listening for motion.");
		});

		var speech = Specimen("Speech & feedback", "TextToSpeech uses the browser's voices; Vibration works on phones that allow it; announcements reach screen readers.",
			ActionRow(out var speak, "Speak", out var vibrate, "Vibrate", out var announce, "Announce"), "Choose an action.");
		Run(speak, speech, async () => { await TextToSpeech.Default.SpeakAsync("Hello from dot net MAUI, running in your browser."); return "Spoken."; });
		Run(vibrate, speech, () =>
		{
			if (!Vibration.Default.IsSupported)
				return Task.FromResult("Vibration isn't supported in this browser.");
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
			return Task.FromResult("Vibrated.");
		});
		Run(announce, speech, () => { SemanticScreenReader.Default.Announce("Hello from MAUI"); return Task.FromResult("Announced to assistive technology."); });
	}

	static Grid Table(params (string Key, object Value)[] rows)
	{
		var grid = new Grid { ColumnSpacing = 24, RowSpacing = 10, ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
		for (var i = 0; i < rows.Length; i++)
		{
			grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			grid.Add(Muted(rows[i].Key, 14), 0, i);
			grid.Add(rows[i].Value as View ?? new Label { Text = rows[i].Value?.ToString() }, 1, i);
		}
		return grid;
	}

	static FlexLayout ActionRow(out Button first, string firstText, out Button second, string secondText)
	{
		first = new Button { Text = firstText };
		second = Secondary(secondText);
		return Wrap(12, first, second);
	}

	static FlexLayout ActionRow(out Button first, string firstText, out Button second, string secondText, out Button third, string thirdText)
	{
		first = new Button { Text = firstText };
		second = Secondary(secondText);
		third = Secondary(thirdText);
		return Wrap(12, first, second, third);
	}

	// Results and failures both land in the specimen's readout.
	static void Run(Button button, Label readout, Func<Task<string>> action)
		=> button.Clicked += async (_, _) =>
		{
			try
			{
				readout.Text = await action();
			}
			catch (Exception ex)
			{
				readout.Text = $"{ex.GetType().Name}: {ex.Message}";
			}
		};

	static string Describe(DisplayInfo info) => $"{info.Width:F0} by {info.Height:F0} pixels at {info.Density:0.##}x, {info.Orientation}";

	static string DescribeNetwork() => $"{Connectivity.NetworkAccess} ({string.Join(", ", Connectivity.ConnectionProfiles.DefaultIfEmpty(ConnectionProfile.Unknown))})";

	static string DescribeBattery() => $"{Battery.ChargeLevel:P0}, {Battery.State}, powered by {Battery.PowerSource}";
}

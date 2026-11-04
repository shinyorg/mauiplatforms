using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Shiny.Controls.Camera;
using Shiny.Maui.Controls.Camera;
using Shiny.Maui.Controls.Media;

namespace SampleWeb.Pages.Shiny;

/// <summary>The Shiny CameraView on the browser backend (Shiny.Maui.Controls.Camera.Web).</summary>
public class CameraWebPage : SpecimenPage
{
	readonly CameraView _camera;
	readonly Label _status;
	readonly Image _photo;
	readonly MediaElement _playback;
	readonly Slider _zoom;
	readonly Button _record;

	public CameraWebPage() : base("CameraView", "Shiny's CameraView streaming from getUserMedia. The browser asks for permission first, and only allows it over HTTPS or on localhost.")
	{
		_camera = new CameraView
		{
			HeightRequest = 360,
			IsActive = false,
			Facing = CameraFacing.Front,
		};
		_status = Muted("Starting…", 13);
		_camera.CameraError += (_, e) => _status.Text = e.Message;
		_camera.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName is nameof(CameraView.MinZoom) or nameof(CameraView.MaxZoom))
				UpdateZoomRange();
		};

		var stage = new VerticalStackLayout { Spacing = 14, Children = { _camera, _status } };
		Specimen("Preview", "A front camera (or a webcam) is mirrored, as on the native platforms.", stage);

		// ---- options ---------------------------------------------------------
		var flip = new Button { Text = "Switch camera" };
		flip.StyleClass = ["secondary"];
		flip.Clicked += (_, _) => _camera.Facing = _camera.Facing == CameraFacing.Front ? CameraFacing.Back : CameraFacing.Front;

		var devices = new Picker { Title = "Camera" };
		var deviceIds = new List<string>();
		devices.SelectedIndexChanged += (_, _) =>
		{
			if (devices.SelectedIndex >= 0 && devices.SelectedIndex < deviceIds.Count)
				_camera.CameraId = deviceIds[devices.SelectedIndex];
		};
		var listDevices = new Button { Text = "List cameras" };
		listDevices.StyleClass = ["secondary"];
		listDevices.Clicked += async (_, _) =>
		{
			var cameras = await _camera.GetAvailableCamerasAsync();
			deviceIds.Clear();
			deviceIds.AddRange(cameras.Select(c => c.Id));
			devices.ItemsSource = cameras.Select(c => $"{c.Name} ({c.Facing})").ToList();
			_status.Text = $"{cameras.Count} camera(s) found";
		};

		var filters = Enum.GetValues<CameraFilter>();
		var filterPicker = new Picker { Title = "Filter", ItemsSource = filters.Select(f => f.ToString()).ToList(), SelectedIndex = 0 };
		filterPicker.SelectedIndexChanged += (_, _) => _camera.Filter = filters[Math.Max(0, filterPicker.SelectedIndex)];

		var fill = new Switch { IsToggled = true };
		fill.Toggled += (_, e) => _camera.ScaleMode = e.Value ? PreviewScaleMode.AspectFill : PreviewScaleMode.AspectFit;

		var torch = new Switch();
		torch.Toggled += (_, e) => _camera.IsTorchOn = e.Value;

		_zoom = new Slider { Minimum = 0, Maximum = 1, IsEnabled = false };
		_zoom.ValueChanged += (_, e) => _camera.Zoom = e.NewValue;

		var options = new Grid
		{
			ColumnSpacing = 16,
			RowSpacing = 12,
			ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) },
		};
		var row = 0;
		AddRow(options, ref row, "Facing", flip);
		AddRow(options, ref row, "Device", new HorizontalStackLayout { Spacing = 10, Children = { listDevices, devices } });
		AddRow(options, ref row, "Filter", filterPicker);
		AddRow(options, ref row, "Fill", fill);
		AddRow(options, ref row, "Zoom", _zoom);
		AddRow(options, ref row, "Torch", torch);
		Specimen("Options", "Zoom and torch only light up on cameras whose browser exposes them (mostly Chrome on Android).", options);

		// ---- capture ---------------------------------------------------------
		_photo = new Image { HeightRequest = 220, Aspect = Aspect.AspectFit, IsVisible = false };
		var capture = new Button { Text = "Take photo" };
		capture.Clicked += async (_, _) =>
		{
			try
			{
				var photo = await _camera.CapturePhotoAsync();
				var bytes = photo.Data;
				_photo.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
				_photo.IsVisible = true;
				_status.Text = $"Photo: {photo.Width}×{photo.Height}, {bytes.Length / 1024} KB JPEG";
			}
			catch (Exception ex)
			{
				_status.Text = ex.Message;
			}
		};
		Specimen("Photo", "Captures the current frame as a JPEG, with the filter baked in.",
			new VerticalStackLayout { Spacing = 14, Children = { capture, _photo } });

		_playback = new MediaElement { HeightRequest = 240, IsVisible = false };
		_record = new Button { Text = "Start recording" };
		_record.Clicked += async (_, _) => await ToggleRecordingAsync();
		Specimen("Video", "Records with MediaRecorder, writes the clip to the app's file system and plays it back in a Shiny MediaElement.",
			new VerticalStackLayout { Spacing = 14, Children = { _record, _playback } });
	}

	async Task ToggleRecordingAsync()
	{
		try
		{
			if (!_camera.IsRecording)
			{
				_playback.Stop();
				await _camera.StartVideoRecordingAsync(new VideoRecordingOptions { IncludeAudio = true });
				_record.Text = "Stop recording";
				_status.Text = "Recording…";
			}
			else
			{
				var video = await _camera.StopVideoRecordingAsync();
				_record.Text = "Start recording";
				_status.Text = $"Recorded {video.Duration?.TotalSeconds:0.0}s to {video.FilePath}";
				_playback.Source = MediaSource.FromFile(video.FilePath);
				_playback.IsVisible = true;
			}
		}
		catch (Exception ex)
		{
			_record.Text = _camera.IsRecording ? "Stop recording" : "Start recording";
			_status.Text = ex.Message;
		}
	}

	void UpdateZoomRange()
	{
		var supported = _camera.MaxZoom > _camera.MinZoom;
		_zoom.IsEnabled = supported;
		if (!supported)
			return;
		_zoom.Maximum = _camera.MaxZoom;
		_zoom.Minimum = _camera.MinZoom;
		_zoom.Value = Math.Clamp(_camera.Zoom, _camera.MinZoom, _camera.MaxZoom);
	}

	static void AddRow(Grid grid, ref int row, string label, View view)
	{
		grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		grid.Add(new Label { Text = label, VerticalOptions = LayoutOptions.Center }, 0, row);
		grid.Add(view, 1, row);
		row++;
	}

	// Release the camera (and its light) whenever the page isn't showing.
	protected override void OnAppearing()
	{
		base.OnAppearing();
		_camera.IsActive = true;
		_status.Text = "Starting…";
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_camera.IsActive = false;
		_playback.Pause();
	}
}

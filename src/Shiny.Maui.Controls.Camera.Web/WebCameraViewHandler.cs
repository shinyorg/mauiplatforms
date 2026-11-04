using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;
using Microsoft.Maui.Platform.Web.Handlers;
using Shiny.Controls.Camera;
using Shiny.Maui.Controls.Camera.Web.Interop;

namespace Shiny.Maui.Controls.Camera.Web;

/// <summary>
/// <see cref="CameraView"/> on Platform.Maui.Web: a <c>getUserMedia</c> stream in a <c>&lt;video&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// Supported: camera selection (<see cref="CameraView.Facing"/> / <see cref="CameraView.CameraId"/>),
/// start/stop through <see cref="CameraView.IsActive"/>, zoom and torch where the browser exposes them
/// as track constraints (mostly Chrome on Android), <see cref="CameraView.ScaleMode"/>,
/// <see cref="CameraView.ShowPreview"/>, the built-in filters and any effect with a CSS form,
/// JPEG stills, and <c>MediaRecorder</c> video (WebM or MP4, depending on the browser).
/// </para>
/// <para>
/// Not supported: frame analyzers, flash, focus control and drawn or custom-shader effects. Browsers
/// only allow camera access in a secure context (HTTPS or localhost).
/// </para>
/// </remarks>
public class WebCameraViewHandler : WebElementHandler<CameraView, WebElement>, ICameraViewController
{
	public static IPropertyMapper<CameraView, WebCameraViewHandler> Mapper =
		new PropertyMapper<CameraView, WebCameraViewHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(CameraView.Facing)] = MapCamera,
			[nameof(CameraView.CameraId)] = MapCamera,
			[nameof(CameraView.IsActive)] = MapIsActive,
			[nameof(CameraView.IsTorchOn)] = MapTorch,
			[nameof(CameraView.Zoom)] = MapZoom,
			[nameof(CameraView.ScaleMode)] = MapScaleMode,
			[nameof(CameraView.ShowPreview)] = MapShowPreview,
			[nameof(CameraView.Filter)] = MapEffects,
			[nameof(CameraView.Effects)] = MapEffects,
		};

	public static CommandMapper<CameraView, WebCameraViewHandler> CommandMapper =
		new(WebElementHandler.ViewCommandMapper);

	public WebCameraViewHandler() : base(Mapper, CommandMapper)
	{
	}

	WebElement? video;
	bool running;
	bool torchSupported;
	string? filterCss;
	string? activeFacingMode;
	Task? starting;
	// Bumped by every start and stop, so a start that finishes after a newer request is discarded.
	int generation;

	/// <summary>The <c>&lt;video&gt;</c> showing the stream.</summary>
	public WebElement? Video => this.video;

	protected override WebElement CreatePlatformView()
	{
		var host = new WebElement("div", "shiny-camera-view");
		host.SetStyle("overflow", "hidden");
		host.SetStyle("background-color", "#000");

		var element = new WebElement("video", "shiny-camera-preview");
		element.SetProperty("muted", true);
		element.SetProperty("playsInline", true);
		element.SetProperty("autoplay", true);
		element.SetAttribute("playsinline", "");
		element.SetStyle("position", "absolute");
		element.SetStyle("inset", "0");
		element.SetStyle("width", "100%");
		element.SetStyle("height", "100%");
		element.SetStyle("object-fit", "cover");
		element.SetStyle("pointer-events", "none");
		host.Append(element);
		this.video = element;
		return host;
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		this.StopSession();
		base.DisconnectHandler(platformView);
	}

	// A camera preview has no natural size; it fills what the layout offers.
	protected override Size MeasureNative(double widthConstraint, double heightConstraint)
		=> new(double.IsFinite(widthConstraint) ? widthConstraint : 320, double.IsFinite(heightConstraint) ? heightConstraint : 240);

	// ---- ICameraViewController ------------------------------------------------

	public async Task<bool> RequestPermissionAsync(CancellationToken ct = default)
	{
		try
		{
			await CameraJs.EnsureLoadedAsync();
			return await CameraJs.RequestPermission(false);
		}
		catch (Exception ex)
		{
			this.CurrentView?.OnCameraError(ex.Message, ex);
			return false;
		}
	}

	public async Task<IReadOnlyList<CameraInfo>> GetAvailableCamerasAsync(CancellationToken ct = default)
	{
		await CameraJs.EnsureLoadedAsync();
		using var doc = JsonDocument.Parse(await CameraJs.ListCameras());

		var cameras = new List<CameraInfo>();
		var index = 0;
		foreach (var device in doc.RootElement.EnumerateArray())
		{
			var id = device.GetProperty("id").GetString() ?? string.Empty;
			var label = device.GetProperty("label").GetString();
			index++;
			cameras.Add(new CameraInfo(id, string.IsNullOrEmpty(label) ? $"Camera {index}" : label, GuessFacing(label), index == 1));
		}
		return cameras;
	}

	// Browsers only report facing for an open track, so the list guesses from the device label.
	static CameraFacing GuessFacing(string? label)
	{
		if (string.IsNullOrEmpty(label))
			return CameraFacing.External;
		if (label.Contains("back", StringComparison.OrdinalIgnoreCase) || label.Contains("rear", StringComparison.OrdinalIgnoreCase) || label.Contains("environment", StringComparison.OrdinalIgnoreCase))
			return CameraFacing.Back;
		if (label.Contains("front", StringComparison.OrdinalIgnoreCase) || label.Contains("user", StringComparison.OrdinalIgnoreCase) || label.Contains("facetime", StringComparison.OrdinalIgnoreCase))
			return CameraFacing.Front;
		return CameraFacing.External;
	}

	public Task StartAsync(CancellationToken ct = default)
	{
		if (this.running)
			return Task.CompletedTask;
		return this.starting ??= this.StartCoreAsync();
	}

	async Task StartCoreAsync()
	{
		var gen = ++this.generation;
		try
		{
			await CameraJs.EnsureLoadedAsync();
			if (gen != this.generation || this.video is not { } element || this.CurrentView is not { } view)
				return;

			var facingMode = view.Facing switch
			{
				CameraFacing.Front => "user",
				CameraFacing.Back => "environment",
				_ => null,
			};
			var json = await CameraJs.Start(element.Js, string.IsNullOrEmpty(view.CameraId) ? null : view.CameraId, facingMode);
			if (json is null || gen != this.generation || this.CurrentView is not { } current)
				return;

			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;
			this.running = true;
			this.torchSupported = root.GetProperty("torch").GetBoolean();
			// A front camera (or a desktop webcam, which reports no facing) is shown mirrored like a mirror.
			var reported = root.GetProperty("facingMode").GetString();
			this.activeFacingMode = string.IsNullOrEmpty(reported) ? facingMode : reported;
			this.ApplyMirror();

			current.OnZoomRangeChanged(root.GetProperty("zoomMin").GetDouble(), root.GetProperty("zoomMax").GetDouble());
			MapZoom(this, current);
			MapTorch(this, current);
		}
		catch (Exception ex)
		{
			if (gen == this.generation)
				this.CurrentView?.OnCameraError($"Failed to start camera: {ex.Message}", ex);
		}
		finally
		{
			if (gen == this.generation)
				this.starting = null;
		}
	}

	public Task StopAsync(CancellationToken ct = default)
	{
		this.StopSession();
		return Task.CompletedTask;
	}

	void StopSession()
	{
		this.generation++;
		this.starting = null;
		this.running = false;
		if (this.video is { } element)
		{
			try
			{
				CameraJs.Stop(element.Js);
			}
			catch (JSException)
			{
				// The module never loaded, so nothing was started.
			}
		}
	}

	async Task RestartAsync()
	{
		this.StopSession();
		await this.StartAsync();
	}

	public async Task<CameraPhoto> CapturePhotoAsync(CancellationToken ct = default)
	{
		if (!this.running || this.video is not { } element)
			throw new InvalidOperationException("The camera is not started.");

		var quality = Math.Clamp(this.CurrentView?.PhotoJpegQuality ?? 0.9, 0.05, 1);
		using var doc = JsonDocument.Parse(await CameraJs.Capture(element.Js, this.filterCss, quality));
		var root = doc.RootElement;
		var bytes = CameraJs.TakeBuffer(root.GetProperty("id").GetInt32());
		return new CameraPhoto(bytes, root.GetProperty("width").GetInt32(), root.GetProperty("height").GetInt32());
	}

	public async Task StartVideoRecordingAsync(VideoRecordingOptions options, CancellationToken ct = default)
	{
		if (!this.running || this.video is not { } element)
			throw new InvalidOperationException("The camera is not started.");
		this.pendingVideoPath = options.FilePath;
		await CameraJs.StartRecording(element.Js, options.IncludeAudio);
	}

	string? pendingVideoPath;

	/// <summary>
	/// Stops recording and writes the clip to the in-memory wasm file system. Read it with
	/// <see cref="CameraVideo.OpenRead"/>, or hand it to the user with Essentials' share or save.
	/// </summary>
	public async Task<CameraVideo> StopVideoRecordingAsync(CancellationToken ct = default)
	{
		if (this.video is not { } element)
			throw new InvalidOperationException("The camera is not started.");

		using var doc = JsonDocument.Parse(await CameraJs.StopRecording(element.Js));
		var root = doc.RootElement;
		var bytes = CameraJs.TakeBuffer(root.GetProperty("id").GetInt32());
		var mime = root.GetProperty("mime").GetString() ?? "video/webm";
		var extension = mime.StartsWith("video/mp4", StringComparison.OrdinalIgnoreCase) ? ".mp4" : ".webm";

		var path = this.pendingVideoPath;
		this.pendingVideoPath = null;
		if (string.IsNullOrEmpty(path))
			path = Path.Combine(Path.GetTempPath(), $"shiny-video-{DateTime.UtcNow:yyyyMMdd-HHmmss}{extension}");
		var directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);
		await File.WriteAllBytesAsync(path, bytes, ct);

		return new CameraVideo(path, TimeSpan.FromMilliseconds(root.GetProperty("durationMs").GetDouble()));
	}

	// ---- mappers ----------------------------------------------------------------

	public static void MapIsActive(WebCameraViewHandler handler, CameraView view)
	{
		if (view.IsActive)
			_ = handler.StartAsync();
		else
			handler.StopSession();
	}

	public static void MapCamera(WebCameraViewHandler handler, CameraView view)
	{
		if (handler.running || handler.starting is not null)
			_ = handler.RestartAsync();
	}

	public static async void MapZoom(WebCameraViewHandler handler, CameraView view)
	{
		if (!handler.running || handler.video is not { } element || view.MaxZoom <= view.MinZoom)
			return;
		try
		{
			await CameraJs.SetZoom(element.Js, Math.Clamp(view.Zoom, view.MinZoom, view.MaxZoom));
		}
		catch
		{
			// the track ended or the browser rejected the constraint
		}
	}

	public static async void MapTorch(WebCameraViewHandler handler, CameraView view)
	{
		if (!handler.running || !handler.torchSupported || handler.video is not { } element)
			return;
		try
		{
			await CameraJs.SetTorch(element.Js, view.IsTorchOn);
		}
		catch
		{
			// the track ended or the browser rejected the constraint
		}
	}

	public static void MapScaleMode(WebCameraViewHandler handler, CameraView view)
		=> handler.video?.SetStyle("object-fit", view.ScaleMode == PreviewScaleMode.AspectFit ? "contain" : "cover");

	// Hides the picture only; the stream (and any recording) keeps running.
	public static void MapShowPreview(WebCameraViewHandler handler, CameraView view)
		=> handler.video?.SetStyle("visibility", view.ShowPreview ? null : "hidden");

	public static void MapEffects(WebCameraViewHandler handler, CameraView view)
	{
		// The built-in filters and most colour effects carry a CSS filter for exactly this case. It is
		// applied to the preview and baked into captured stills; recordings come straight off the stream.
		var css = string.Join(' ', view.EffectChain.Effects
			.Where(e => e.IsEnabled)
			.OfType<INativeEffect>()
			.Select(e => e.Descriptor.Css)
			.Where(c => !string.IsNullOrWhiteSpace(c)));
		handler.filterCss = css.Length == 0 ? null : css;
		handler.video?.SetStyle("filter", handler.filterCss);
	}

	void ApplyMirror()
		=> this.video?.SetStyle("transform", this.activeFacingMode is "user" or null ? "scaleX(-1)" : null);
}

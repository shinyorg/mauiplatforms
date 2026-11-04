using System.Runtime.InteropServices.JavaScript;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;
using Shiny.Controls.Media;
using Shiny.Maui.Controls.Media.Web.Interop;

namespace Shiny.Maui.Controls.Media.Web;

/// <summary>
/// The browser backend: one HTML <c>&lt;video&gt;</c> element per player.
/// </summary>
/// <remarks>
/// <para>
/// The video element is both decoder and picture, so the backend owns it and
/// <see cref="SetOutput"/> moves it into whichever surface container is current. Entering
/// fullscreen therefore moves the same element to the modal page, and playback never re-buffers.
/// With no surface the element waits in a hidden parking div and keeps playing.
/// </para>
/// <para>
/// The JS module loads asynchronously. Everything asked of the backend before the element exists
/// is kept as state here and applied once it does, so a page built during startup works as normal.
/// </para>
/// </remarks>
public class WebMediaPlayerBackend : IMediaPlayerBackend
{
	JSObject? video;
	JSObject? output;
	bool disposed;

	// Desired state, replayed onto the element once the module has loaded.
	string? pendingUrl;
	(byte[] Bytes, string? Mime)? pendingBytes;
	bool hasPendingSource;
	bool wantPlay;
	double volume = 1d;
	bool muted;
	double rate = 1d;
	bool looping;
	MediaAspect aspect = MediaAspect.AspectFit;
	bool keepScreenOn;
	bool backgroundPlayback;
	MediaMetadata? metadata;

	// Stop() pauses the element, which fires "pause"; without this the state would read Paused.
	bool stopping;

	public WebMediaPlayerBackend()
	{
		if (MediaJs.IsLoaded)
			this.CreateElement();
		else
			_ = this.LoadAsync();
	}

	async Task LoadAsync()
	{
		try
		{
			await MediaJs.EnsureLoadedAsync();
		}
		catch (Exception ex)
		{
			this.RaiseFailed(new MediaFailure("The media player script could not be loaded", ex));
			return;
		}

		if (!this.disposed)
			this.CreateElement();
	}

	void CreateElement()
	{
		var element = MediaJs.Create(this.OnEvent);
		this.video = element;

		MediaJs.SetVolume(element, this.volume);
		MediaJs.SetMuted(element, this.muted);
		MediaJs.SetRate(element, this.rate);
		MediaJs.SetLoop(element, this.looping);
		MediaJs.SetObjectFit(element, ObjectFit(this.aspect));
		MediaJs.SetKeepScreenOn(element, this.keepScreenOn);
		if (this.output is not null)
			MediaJs.Attach(element, this.output);

		if (this.hasPendingSource)
		{
			this.hasPendingSource = false;
			this.ApplySource(element);
		}

		if (this.backgroundPlayback)
			this.ApplyMediaSession(element);
		if (this.wantPlay)
			MediaJs.Play(element);
	}

	public MediaPlaybackCapabilities Capabilities
	{
		get
		{
			var caps = MediaPlaybackCapabilities.Volume | MediaPlaybackCapabilities.PlaybackRate | MediaPlaybackCapabilities.BufferProgress;
			if (MediaJs.IsLoaded && MediaJs.SupportsPictureInPicture())
				caps |= MediaPlaybackCapabilities.PictureInPicture;
			// Browsers keep playing in background tabs; Media Session adds the OS transport controls.
			if (MediaJs.IsLoaded && MediaJs.SupportsMediaSession())
				caps |= MediaPlaybackCapabilities.BackgroundAudio;
			return caps;
		}
	}

	public MediaElementState State { get; private set; } = MediaElementState.None;

	public TimeSpan Position => this.video is null ? TimeSpan.Zero : TimeSpan.FromSeconds(MediaJs.CurrentTime(this.video));

	public TimeSpan Duration { get; private set; }

	public double BufferedProgress { get; private set; }

	public Size VideoSize { get; private set; }

	public bool IsPictureInPictureActive { get; private set; }

	public event EventHandler<MediaElementState>? StateChanged;
	public event EventHandler? MediaOpened;
	public event EventHandler? VideoSizeChanged;
	public event EventHandler? MediaEnded;
	public event EventHandler<MediaFailure>? Failed;
	public event EventHandler<bool>? PictureInPictureChanged;
	public event EventHandler<MediaRemoteCommand>? RemoteCommandReceived;

	/// <summary>Moves the video into a surface container (a <see cref="WebElement"/>), or parks it for <c>null</c>.</summary>
	public void SetOutput(object? nativeView)
	{
		this.output = nativeView switch
		{
			WebElement element => element.Js,
			JSObject js => js,
			_ => null,
		};

		if (this.video is not null)
			MediaJs.Attach(this.video, this.output);
	}

	public Task OpenAsync(MediaSource? source, CancellationToken ct = default)
	{
		this.Duration = TimeSpan.Zero;
		this.BufferedProgress = 0;
		this.wantPlay = false;
		this.stopping = false;
		this.pendingUrl = null;
		this.pendingBytes = null;
		if (this.VideoSize != Size.Zero)
		{
			this.VideoSize = Size.Zero;
			this.VideoSizeChanged?.Invoke(this, EventArgs.Empty);
		}

		if (source is null)
		{
			this.hasPendingSource = true;
			if (this.video is not null)
			{
				this.hasPendingSource = false;
				MediaJs.SetSource(this.video, null);
			}
			this.SetState(MediaElementState.None);
			return Task.CompletedTask;
		}

		try
		{
			switch (source)
			{
				case UriMediaSource { Uri: { } uri }:
					this.pendingUrl = Resolve(uri.OriginalString);
					break;
				case ResourceMediaSource { Path: { Length: > 0 } path }:
					// MauiAssets are served from the app's root, next to index.html.
					this.pendingUrl = Resolve(path.Replace('\\', '/'));
					break;
				case FileMediaSource { Path: { Length: > 0 } path }:
					// The wasm file system is in memory and the browser can't see it, so hand the
					// bytes over as a blob URL.
					this.pendingBytes = (File.ReadAllBytes(path), MimeFor(path));
					break;
				default:
					this.RaiseFailed(new MediaFailure($"Could not resolve media source '{source}'"));
					return Task.CompletedTask;
			}
		}
		catch (Exception ex)
		{
			this.RaiseFailed(new MediaFailure($"Could not open media source '{source}'", ex));
			return Task.CompletedTask;
		}

		this.SetState(MediaElementState.Opening);
		if (this.video is null)
			this.hasPendingSource = true;
		else
			this.ApplySource(this.video);
		return Task.CompletedTask;
	}

	void ApplySource(JSObject element)
	{
		if (this.pendingBytes is { } bytes)
			MediaJs.SetBytesSource(element, bytes.Bytes, bytes.Mime);
		else
			MediaJs.SetSource(element, this.pendingUrl);
		this.pendingBytes = null;
	}

	internal static string Resolve(string url)
		=> Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.Scheme is not "file"
			? absolute.ToString()
			: new Uri(new Uri(WebMauiApplication.BaseUri), url.TrimStart('/')).ToString();

	internal static string? MimeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
	{
		".mp4" or ".m4v" => "video/mp4",
		".webm" => "video/webm",
		".ogv" => "video/ogg",
		".mov" => "video/quicktime",
		".mp3" => "audio/mpeg",
		".m4a" or ".aac" => "audio/mp4",
		".ogg" or ".oga" => "audio/ogg",
		".wav" => "audio/wav",
		".flac" => "audio/flac",
		_ => null,
	};

	public void Play()
	{
		this.wantPlay = true;
		this.stopping = false;
		if (this.video is not null)
			MediaJs.Play(this.video);
	}

	public void Pause()
	{
		this.wantPlay = false;
		if (this.video is not null)
			MediaJs.Pause(this.video);
	}

	public void Stop()
	{
		this.wantPlay = false;
		this.stopping = true;
		if (this.video is not null)
			MediaJs.Stop(this.video);
		this.SetState(MediaElementState.Stopped);
	}

	public void Seek(TimeSpan position)
	{
		if (this.video is not null)
			MediaJs.Seek(this.video, position.TotalSeconds);
	}

	public void SetVolume(double volume)
	{
		this.volume = Math.Clamp(volume, 0d, 1d);
		if (this.video is not null)
			MediaJs.SetVolume(this.video, this.volume);
	}

	public void SetMuted(bool muted)
	{
		this.muted = muted;
		if (this.video is not null)
			MediaJs.SetMuted(this.video, muted);
	}

	public void SetRate(double rate)
	{
		if (rate <= 0)
			return;
		this.rate = rate;
		if (this.video is not null)
			MediaJs.SetRate(this.video, rate);
	}

	public void SetLooping(bool looping)
	{
		this.looping = looping;
		if (this.video is not null)
			MediaJs.SetLoop(this.video, looping);
	}

	public void SetAspect(MediaAspect aspect)
	{
		this.aspect = aspect;
		if (this.video is not null)
			MediaJs.SetObjectFit(this.video, ObjectFit(aspect));
	}

	static string ObjectFit(MediaAspect aspect) => aspect switch
	{
		MediaAspect.AspectFill => "cover",
		MediaAspect.Fill => "fill",
		_ => "contain",
	};

	/// <summary>Holds a Screen Wake Lock while playing, where the browser supports one.</summary>
	public void SetKeepScreenOn(bool keepOn)
	{
		this.keepScreenOn = keepOn;
		if (this.video is not null)
			MediaJs.SetKeepScreenOn(this.video, keepOn);
	}

	/// <summary>Publishes now-playing metadata and transport handlers through the Media Session API.</summary>
	public void SetBackgroundPlayback(bool enabled, MediaMetadata? metadata)
	{
		this.backgroundPlayback = enabled;
		this.metadata = metadata;
		if (this.video is not null)
			this.ApplyMediaSession(this.video);
	}

	void ApplyMediaSession(JSObject element)
	{
		var artwork = this.metadata?.ArtworkUri is { Length: > 0 } art ? Resolve(art) : null;
		MediaJs.SetMediaSession(element, this.backgroundPlayback,
			this.metadata?.Title, this.metadata?.Artist, this.metadata?.Album, artwork);
	}

	public async Task<bool> TryEnterPictureInPictureAsync()
	{
		if (this.video is null)
			return false;
		try
		{
			return await MediaJs.EnterPictureInPicture(this.video);
		}
		catch
		{
			return false;
		}
	}

	public async Task ExitPictureInPictureAsync()
	{
		if (this.video is not null)
			await MediaJs.ExitPictureInPicture(this.video);
	}

	void OnEvent(string name, double value)
	{
		if (this.disposed || this.video is not { } element)
			return;

		switch (name)
		{
			case "opened":
				this.Duration = TimeSpan.FromSeconds(value);
				this.UpdateVideoSize(element);
				if (this.State is MediaElementState.Opening or MediaElementState.Buffering)
					this.SetState(MediaElementState.Paused);
				this.MediaOpened?.Invoke(this, EventArgs.Empty);
				break;

			case "duration":
				this.Duration = TimeSpan.FromSeconds(value);
				break;

			case "size":
				this.UpdateVideoSize(element);
				break;

			case "playing":
				this.stopping = false;
				this.SetState(MediaElementState.Playing);
				break;

			case "paused":
				this.SetState(this.stopping ? MediaElementState.Stopped : MediaElementState.Paused);
				break;

			case "blocked":
				// Autoplay refused until the user interacts with the page.
				this.wantPlay = false;
				if (this.State is not MediaElementState.Failed)
					this.SetState(MediaElementState.Paused);
				break;

			case "buffering":
				if (this.State is not (MediaElementState.Opening or MediaElementState.Stopped))
					this.SetState(MediaElementState.Buffering);
				break;

			case "ended":
				this.wantPlay = false;
				this.SetState(MediaElementState.Paused);
				this.MediaEnded?.Invoke(this, EventArgs.Empty);
				break;

			case "progress":
				this.BufferedProgress = value;
				break;

			case "error":
				this.RaiseFailed(new MediaFailure(MediaJs.ErrorMessage(element)));
				break;

			case "pip":
				this.IsPictureInPictureActive = value != 0;
				this.PictureInPictureChanged?.Invoke(this, this.IsPictureInPictureActive);
				break;

			// Media Session actions replace the browser's defaults, so apply them here and tell the control.
			case "remote-play":
				this.Play();
				this.RemoteCommandReceived?.Invoke(this, MediaRemoteCommand.Play);
				break;
			case "remote-pause":
				this.Pause();
				this.RemoteCommandReceived?.Invoke(this, MediaRemoteCommand.Pause);
				break;
			case "remote-stop":
				this.Stop();
				this.RemoteCommandReceived?.Invoke(this, MediaRemoteCommand.Stop);
				break;
			case "remote-seek":
				this.RemoteCommandReceived?.Invoke(this, MediaRemoteCommand.Seek);
				break;
		}
	}

	void UpdateVideoSize(JSObject element)
	{
		var size = new Size(MediaJs.VideoWidth(element), MediaJs.VideoHeight(element));
		if (size == this.VideoSize)
			return;
		this.VideoSize = size;
		this.VideoSizeChanged?.Invoke(this, EventArgs.Empty);
	}

	void SetState(MediaElementState state)
	{
		if (this.State == state)
			return;
		this.State = state;
		this.StateChanged?.Invoke(this, state);
	}

	void RaiseFailed(MediaFailure failure)
	{
		this.wantPlay = false;
		if (this.State == MediaElementState.Failed)
			return;
		this.State = MediaElementState.Failed;
		this.StateChanged?.Invoke(this, MediaElementState.Failed);
		this.Failed?.Invoke(this, failure);
	}

	public void Dispose()
	{
		if (this.disposed)
			return;
		this.disposed = true;
		if (this.video is not null)
		{
			MediaJs.Dispose(this.video);
			this.video.Dispose();
			this.video = null;
		}
		this.output = null;
		GC.SuppressFinalize(this);
	}
}

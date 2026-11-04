using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;
using Microsoft.Maui.Platform.Web.Handlers;
using Shiny.Controls.Media;
using Shiny.Maui.Controls.Media.Web.Interop;

namespace Shiny.Maui.Controls.Media.Web;

/// <summary>
/// <see cref="VideoText"/> on Platform.Maui.Web: a looping, muted <c>&lt;video&gt;</c> that shows only
/// through the letters of the text, or behind an overlay with the text knocked out of it.
/// </summary>
/// <remarks>
/// <para>
/// The <c>net10.0</c> build of <see cref="VideoText"/> has no mask, so it fills itself with a plain
/// fallback label. This handler replaces the whole control instead: it reads the public properties,
/// leaves the fallback label unrendered, and draws the same SVG the Shiny Blazor VideoText does
/// (<see cref="VideoTextLayout"/>).
/// </para>
/// <para>
/// With an overlay (the default: black), the knocked-out sheet is inline SVG and uses the app's web
/// fonts. Without one, the video is clipped to the glyphs through a CSS mask image, and browsers draw
/// mask images with system fonts only, so a custom <see cref="VideoText.FontFamily"/> falls back there.
/// </para>
/// </remarks>
public class WebVideoTextHandler : WebElementHandler<VideoText, WebElement>
{
	public static IPropertyMapper<VideoText, WebVideoTextHandler> Mapper =
		new PropertyMapper<VideoText, WebVideoTextHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(VideoText.Source)] = MapSource,
			[nameof(VideoText.IsMuted)] = MapIsMuted,
			[nameof(VideoText.Text)] = MapText,
			[nameof(VideoText.FontFamily)] = MapText,
			[nameof(VideoText.FontSize)] = MapText,
			[nameof(VideoText.FontAttributes)] = MapText,
			[nameof(VideoText.OverlayColor)] = MapText,
			[nameof(VideoText.OverlayOpacity)] = MapText,
			[nameof(VideoText.Marquee)] = MapText,
			[nameof(VideoText.MarqueeDuration)] = MapText,
			// The fallback label is what net10.0 VideoText shows without a mask; this handler is the mask.
			[nameof(IContentView.Content)] = static (_, _) => { },
		};

	public static CommandMapper<VideoText, WebVideoTextHandler> CommandMapper =
		new(WebElementHandler.ViewCommandMapper);

	public WebVideoTextHandler() : base(Mapper, CommandMapper)
	{
	}

	readonly string maskId = "shiny-vt-" + Guid.NewGuid().ToString("N")[..12];
	WebElement? video;
	WebElement? overlay;
	bool loaded;
	bool disconnected;
	Size size;

	// Glyph widths at 100px for the current text and font, measured in the browser.
	string? measuredKey;
	string? measuringKey;
	double[]? lineWidths;

	protected override WebElement CreatePlatformView()
	{
		var host = new WebElement("div", "shiny-video-text");
		host.SetAttribute("role", "img");
		host.SetStyle("overflow", "hidden");
		host.SetStyle("mask-size", "100% 100%");
		host.SetStyle("-webkit-mask-size", "100% 100%");
		host.SetStyle("mask-repeat", "no-repeat");
		host.SetStyle("-webkit-mask-repeat", "no-repeat");

		var element = new WebElement("video", "shiny-video-text-video");
		element.SetProperty("muted", true);
		element.SetProperty("loop", true);
		element.SetProperty("autoplay", true);
		element.SetProperty("playsInline", true);
		element.SetProperty("disablePictureInPicture", true);
		element.SetAttribute("playsinline", "");
		element.SetAttribute("aria-hidden", "true");
		element.SetAttribute("preload", "auto");
		element.SetStyle("position", "absolute");
		element.SetStyle("inset", "0");
		element.SetStyle("width", "100%");
		element.SetStyle("height", "100%");
		element.SetStyle("object-fit", "cover");
		element.SetStyle("pointer-events", "none");
		host.Append(element);

		var sheet = new WebElement("div", "shiny-video-text-overlay");
		sheet.SetStyle("position", "absolute");
		sheet.SetStyle("inset", "0");
		sheet.SetStyle("pointer-events", "none");
		sheet.SetStyle("line-height", "0");
		host.Append(sheet);

		this.video = element;
		this.overlay = sheet;
		return host;
	}

	protected override async void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		this.disconnected = false;
		try
		{
			await MediaJs.EnsureLoadedAsync();
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[VideoText] The media script could not be loaded: {ex.Message}");
			return;
		}

		if (this.disconnected || this.CurrentView is not { } view)
			return;
		this.loaded = true;
		MapSource(this, view);
		MapText(this, view);
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		this.disconnected = true;
		if (this.loaded && this.video is { } element)
			MediaJs.VideoTextStop(element.Js);
		this.loaded = false;
		base.DisconnectHandler(platformView);
	}

	// Like the Blazor control: fill the width, and default to 200 high when nothing constrains it.
	protected override Size MeasureNative(double widthConstraint, double heightConstraint)
		=> new(double.IsFinite(widthConstraint) ? widthConstraint : 320, double.IsFinite(heightConstraint) ? heightConstraint : 200);

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);
		if (frame.Size == this.size)
			return;
		this.size = frame.Size;
		// A marquee's travel and a fixed-size fit are in pixels; plain auto-fit scales itself.
		if (this.CurrentView is { } view && (ActiveMarquee(view) != MarqueeDirection.None || view.FontSize > 0))
			this.Rebuild(view);
	}

	public static void MapSource(WebVideoTextHandler handler, VideoText view)
	{
		if (!handler.loaded || handler.video is not { } element)
			return;

		try
		{
			switch (view.Source)
			{
				case UriMediaSource { Uri: { } uri }:
					MediaJs.SetSource(element.Js, WebMediaPlayerBackend.Resolve(uri.OriginalString));
					break;
				case ResourceMediaSource { Path: { Length: > 0 } path }:
					MediaJs.SetSource(element.Js, WebMediaPlayerBackend.Resolve(path.Replace('\\', '/')));
					break;
				case FileMediaSource { Path: { Length: > 0 } path }:
					MediaJs.SetBytesSource(element.Js, File.ReadAllBytes(path), WebMediaPlayerBackend.MimeFor(path));
					break;
				default:
					MediaJs.SetSource(element.Js, null);
					return;
			}
			MediaJs.VideoTextSetMuted(element.Js, view.IsMuted);
		}
		catch (Exception ex)
		{
			// Decorative control: a dead source leaves the letters empty rather than taking the page down.
			Console.Error.WriteLine($"[VideoText] Could not open '{view.Source}': {ex.Message}");
		}
	}

	public static void MapIsMuted(WebVideoTextHandler handler, VideoText view)
	{
		if (handler.loaded && handler.video is { } element)
			MediaJs.VideoTextSetMuted(element.Js, view.IsMuted);
	}

	public static void MapText(WebVideoTextHandler handler, VideoText view)
	{
		handler.PlatformView?.SetAttribute("aria-label", string.IsNullOrEmpty(view.Text) ? null : view.Text);
		if (handler.loaded)
			handler.Rebuild(view);
	}

	static MarqueeDirection ActiveMarquee(VideoText view)
		=> view.MarqueeDuration > TimeSpan.Zero ? view.Marquee : MarqueeDirection.None;

	void Rebuild(VideoText view)
	{
		if (this.PlatformView is not { } host || this.overlay is not { } sheet)
			return;

		var lines = VideoTextLayout.SplitLines(view.Text);
		var family = this.ResolveFamily(view.FontFamily);
		var weight = view.FontAttributes.HasFlag(FontAttributes.Bold) ? "bold" : "normal";
		var style = view.FontAttributes.HasFlag(FontAttributes.Italic) ? "italic" : null;
		var marquee = ActiveMarquee(view);

		// Auto-fit and a marquee's travel need real glyph widths; until they arrive the layout estimates.
		var key = string.Join('\n', lines) + "\u0001" + family + "\u0001" + weight + "\u0001" + style;
		if ((view.FontSize <= 0 || marquee != MarqueeDirection.None) && lines.Count > 0 && this.measuredKey != key)
			this.Measure(key, lines, family, weight, style);
		var widths = this.measuredKey == key ? this.lineWidths : null;

		var seconds = view.MarqueeDuration.TotalSeconds;
		if (view.OverlayColor is { Alpha: > 0 } color)
		{
			// Inline SVG, so it can use the app's web fonts.
			host.SetStyle("mask-image", null);
			host.SetStyle("-webkit-mask-image", null);
			sheet.SetHtml(VideoTextLayout.BuildSvgOverlay(
				lines, view.FontSize, family, weight, style,
				Css(color), view.OverlayOpacity * color.Alpha, this.maskId,
				widths, marquee, seconds, this.size.Width, this.size.Height));
		}
		else
		{
			sheet.SetHtml(null);
			var url = $"url(\"{VideoTextLayout.ToDataUrl(VideoTextLayout.BuildSvgMask(
				lines, view.FontSize, family, weight, style,
				widths, marquee, seconds, this.size.Width, this.size.Height))}\")";
			// No text means nothing shows through; an empty mask image would show the whole video.
			host.SetStyle("mask-image", url);
			host.SetStyle("-webkit-mask-image", url);
		}
	}

	async void Measure(string key, IReadOnlyList<string> lines, string? family, string weight, string? style)
	{
		if (this.measuringKey == key)
			return;
		this.measuringKey = key;
		try
		{
			var json = await MediaJs.MeasureLines([.. lines], family, weight, style);
			if (this.measuringKey != key)
				return;
			using var doc = JsonDocument.Parse(json);
			this.lineWidths = doc.RootElement.EnumerateArray().Select(e => e.GetDouble()).ToArray();
			this.measuredKey = key;
		}
		catch
		{
			return; // keep the estimate
		}
		finally
		{
			if (this.measuringKey == key)
				this.measuringKey = null;
		}

		if (!this.disconnected && this.CurrentView is { } view)
			this.Rebuild(view);
	}

	// MAUI font aliases ("OpenSansRegular") map to the family name the web backend registered.
	string? ResolveFamily(string? family)
	{
		if (string.IsNullOrWhiteSpace(family))
			return "sans-serif";
		var hash = family.IndexOf('#');
		var name = hash > 0 ? family[(hash + 1)..] : family;
		var registrar = this.MauiContext?.Services.GetService<IFontRegistrar>();
		var resolved = registrar?.GetFont(family) ?? registrar?.GetFont(name) ?? name;
		return $"\"{resolved}\", sans-serif";
	}

	static string Css(Color color) => string.Create(CultureInfo.InvariantCulture,
		$"rgb({Math.Round(color.Red * 255)},{Math.Round(color.Green * 255)},{Math.Round(color.Blue * 255)})");
}

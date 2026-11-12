using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web.Interop;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>Owns an &lt;img&gt;: resolves sources, releases blob URLs and reports loading to MAUI.</summary>
internal sealed class WebImageLoader(WebElement image, Func<IImageSourcePart?> part, Func<IFontManager?> fonts) : IDisposable
{
	CancellationTokenSource? _load;
	string? _url;

	public Size NaturalSize
	{
		get
		{
			var size = Dom.NaturalSize(image.Js);
			return new Size(size[0], size[1]);
		}
	}

	public IDisposable[] Connect(Action onLoaded) =>
	[
		image.On("load", () =>
		{
			image.ToggleClass(FailedClass, false);
			part()?.UpdateIsLoading(false);
			onLoaded();
		}),
		image.On("error", () =>
		{
			// A missing image shows nothing, as on the native platforms, not the browser's broken-image box.
			image.ToggleClass(FailedClass, true);
			part()?.UpdateIsLoading(false);
		}),
	];

	const string FailedClass = "maui-image-failed";

	public async Task LoadAsync(IImageSource? source)
	{
		_load?.Cancel();
		var cts = _load = new CancellationTokenSource();
		part()?.UpdateIsLoading(source is not null);
		try
		{
			var url = await WebImageSource.ResolveAsync(source, fonts(), cts.Token);
			if (cts.IsCancellationRequested)
			{
				WebImageSource.Release(url);
				return;
			}

			WebImageSource.Release(_url);
			_url = url;
			image.ToggleClass(FailedClass, false);
			image.SetAttribute("src", url);
			if (url is null)
				part()?.UpdateIsLoading(false);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			Dom.Log($"Image failed to load: {ex.Message}");
			part()?.UpdateIsLoading(false);
		}
	}

	public void Dispose()
	{
		_load?.Cancel();
		WebImageSource.Release(_url);
		_url = null;
	}

	/// <summary>Scales a natural size down to fit the constraints, preserving its aspect ratio.</summary>
	public static Size Fit(Size natural, double widthConstraint, double heightConstraint)
	{
		if (natural.Width <= 0 || natural.Height <= 0)
			return Size.Zero;
		var scale = 1.0;
		if (natural.Width * scale > widthConstraint)
			scale = widthConstraint / natural.Width;
		if (natural.Height * scale > heightConstraint)
			scale = heightConstraint / natural.Height;
		return new Size(natural.Width * scale, natural.Height * scale);
	}

	public static string ObjectFit(Aspect aspect) => aspect switch
	{
		Aspect.AspectFill => "cover",
		Aspect.Fill => "fill",
		Aspect.Center => "none",
		_ => "contain",
	};
}

public class ImageHandler : WebElementHandler<IImage, WebElement>
{
	public static IPropertyMapper<IImage, ImageHandler> Mapper =
		new PropertyMapper<IImage, ImageHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IImage.Aspect)] = MapAspect,
			[nameof(IImageSourcePart.Source)] = MapSource,
		};

	public ImageHandler() : base(Mapper)
	{
	}

	WebImageLoader _loader = null!;

	protected override WebElement CreatePlatformView()
	{
		var img = new WebElement("img", "maui-image");
		img.SetAttribute("alt", "");
		img.SetAttribute("draggable", "false");
		_loader = new WebImageLoader(img, () => VirtualView as IImageSourcePart,
			() => MauiContext?.Services.GetService(typeof(IFontManager)) as IFontManager);
		return img;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		foreach (var subscription in _loader.Connect(() => CurrentView?.InvalidateMeasure()))
			Track(subscription);
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		_loader.Dispose();
		base.DisconnectHandler(platformView);
	}

	protected override Size MeasureNative(double widthConstraint, double heightConstraint)
		=> WebImageLoader.Fit(_loader.NaturalSize, widthConstraint, heightConstraint);

	public static void MapAspect(ImageHandler handler, IImage image)
		=> handler.PlatformView.SetStyle("object-fit", WebImageLoader.ObjectFit(image.Aspect));

	public static void MapSource(ImageHandler handler, IImage image)
		=> handler._loader.LoadAsync((image as IImageSourcePart)?.Source).FireAndForget("Image load");
}

public class ImageButtonHandler : WebElementHandler<IImageButton, WebElement>
{
	public static IPropertyMapper<IImageButton, ImageButtonHandler> Mapper =
		new PropertyMapper<IImageButton, ImageButtonHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IImage.Aspect)] = MapAspect,
			[nameof(IImageSourcePart.Source)] = MapSource,
			[nameof(IPadding.Padding)] = MapPadding,
			[nameof(IButtonStroke.CornerRadius)] = MapStroke,
			[nameof(IButtonStroke.StrokeColor)] = MapStroke,
			[nameof(IButtonStroke.StrokeThickness)] = MapStroke,
		};

	public ImageButtonHandler() : base(Mapper)
	{
	}

	WebElement _image = null!;
	WebImageLoader _loader = null!;

	protected override WebElement CreatePlatformView()
	{
		var button = new WebElement("button", "maui-image-button");
		button.SetAttribute("type", "button");
		_image = new WebElement("img");
		_image.SetAttribute("alt", "");
		button.Append(_image);
		_loader = new WebImageLoader(_image, () => VirtualView, () => MauiContext?.Services.GetService(typeof(IFontManager)) as IFontManager);
		return button;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		foreach (var subscription in _loader.Connect(() => CurrentView?.InvalidateMeasure()))
			Track(subscription);
		Track(platformView.On("click", () => CurrentView?.Clicked()));
		Track(platformView.On("pointerdown", () => CurrentView?.Pressed()));
		Track(platformView.On("pointerup", () => CurrentView?.Released()));
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		_loader.Dispose();
		base.DisconnectHandler(platformView);
	}

	protected override Size MeasureNative(double widthConstraint, double heightConstraint)
	{
		var padding = VirtualView.Padding.IsNaN ? Thickness.Zero : VirtualView.Padding;
		var image = WebImageLoader.Fit(_loader.NaturalSize,
			widthConstraint - padding.HorizontalThickness, heightConstraint - padding.VerticalThickness);
		return new Size(image.Width + padding.HorizontalThickness, image.Height + padding.VerticalThickness);
	}

	public static void MapAspect(ImageButtonHandler handler, IImageButton button)
		=> handler._image.SetStyle("object-fit", WebImageLoader.ObjectFit(button.Aspect));

	public static void MapSource(ImageButtonHandler handler, IImageButton button)
		=> handler._loader.LoadAsync(button.Source).FireAndForget("Image load");

	public static void MapPadding(ImageButtonHandler handler, IImageButton button)
		=> handler.PlatformView.SetStyle("padding", button.Padding.IsNaN || button.Padding.IsEmpty ? null : WebCss.Thickness(button.Padding));

	public static void MapStroke(ImageButtonHandler handler, IImageButton button)
	{
		var el = handler.PlatformView;
		el.SetStyle("border-radius", button.CornerRadius > 0 ? WebCss.Px(button.CornerRadius) : null);
		var hasStroke = button.StrokeColor is not null && button.StrokeThickness > 0;
		el.SetStyle("border-width", hasStroke ? WebCss.Px(button.StrokeThickness) : null);
		el.SetStyle("border-color", hasStroke ? WebCss.Color(button.StrokeColor) : null);
	}
}

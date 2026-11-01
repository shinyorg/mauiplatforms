using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

public class ButtonHandler : WebElementHandler<IButton, WebElement>
{
	public static IPropertyMapper<IButton, ButtonHandler> Mapper =
		new PropertyMapper<IButton, ButtonHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IText.Text)] = MapText,
			[nameof(Button.TextTransform)] = MapText,
			[nameof(ITextStyle.TextColor)] = MapTextColor,
			[nameof(ITextStyle.Font)] = MapFont,
			[nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
			[nameof(IPadding.Padding)] = MapPadding,
			[nameof(IButtonStroke.CornerRadius)] = MapStroke,
			[nameof(IButtonStroke.StrokeColor)] = MapStroke,
			[nameof(IButtonStroke.StrokeThickness)] = MapStroke,
			[nameof(IImage.Source)] = MapImageSource,
			[nameof(Button.ImageSource)] = MapImageSource,
			[nameof(Button.ContentLayout)] = MapContentLayout,
			[nameof(Button.LineBreakMode)] = MapLineBreakMode,
		};

	public ButtonHandler() : base(Mapper)
	{
	}

	public ButtonHandler(IPropertyMapper? mapper) : base(mapper ?? Mapper)
	{
	}

	WebElement _text = null!;
	WebElement? _image;
	string? _imageUrl;
	CancellationTokenSource? _imageLoad;

	protected override WebElement CreatePlatformView()
	{
		var button = new WebElement("button", "maui-button");
		button.SetAttribute("type", "button");
		_text = new WebElement("span");
		button.Append(_text);
		return button;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(platformView.On("click", () => VirtualView?.Clicked()));
		Track(platformView.On("pointerdown", () => VirtualView?.Pressed()));
		Track(platformView.On("pointerup", () => VirtualView?.Released()));
		Track(platformView.On("pointercancel", () => VirtualView?.Released()));
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		_imageLoad?.Cancel();
		WebImageSource.Release(_imageUrl);
		base.DisconnectHandler(platformView);
	}

	public static void MapText(ButtonHandler handler, IButton button)
	{
		var text = (button as IText)?.Text;
		if (button is Button { TextTransform: var transform })
			text = transform switch
			{
				TextTransform.Uppercase => text?.ToUpperInvariant(),
				TextTransform.Lowercase => text?.ToLowerInvariant(),
				_ => text,
			};
		handler._text.SetText(text);
		handler._text.ToggleClass("maui-collapsed", string.IsNullOrEmpty(text));
	}

	public static void MapTextColor(ButtonHandler handler, IButton button)
		=> handler.PlatformView.SetStyle("color", WebCss.Color((button as ITextStyle)?.TextColor));

	public static void MapFont(ButtonHandler handler, IButton button)
	{
		if (button is ITextStyle style && handler.MauiContext?.Services.GetService(typeof(IFontManager)) is WebFontManager fonts)
			fonts.Apply(handler.PlatformView, style.Font);
	}

	public static void MapCharacterSpacing(ButtonHandler handler, IButton button)
		=> handler.PlatformView.SetStyle("letter-spacing",
			button is ITextStyle { CharacterSpacing: not 0 } style ? WebCss.Px(style.CharacterSpacing) : null);

	public static void MapPadding(ButtonHandler handler, IButton button)
		=> handler.PlatformView.SetStyle("padding", button.Padding.IsNaN || button.Padding.IsEmpty ? null : WebCss.Thickness(button.Padding));

	public static void MapStroke(ButtonHandler handler, IButton button)
	{
		var el = handler.PlatformView;
		el.SetStyle("border-radius", button.CornerRadius >= 0 ? WebCss.Px(button.CornerRadius) : null);
		var thickness = button.StrokeThickness;
		var hasStroke = button.StrokeColor is not null && thickness > 0;
		el.SetStyle("border-width", hasStroke ? WebCss.Px(thickness) : null);
		el.SetStyle("border-color", hasStroke ? WebCss.Color(button.StrokeColor) : null);
	}

	public static void MapContentLayout(ButtonHandler handler, IButton button)
	{
		if (button is not Button b)
			return;
		var layout = b.ContentLayout;
		handler.PlatformView.SetStyle("flex-direction", layout.Position switch
		{
			Button.ButtonContentLayout.ImagePosition.Top => "column",
			Button.ButtonContentLayout.ImagePosition.Bottom => "column-reverse",
			Button.ButtonContentLayout.ImagePosition.Right => "row-reverse",
			_ => null,
		});
		handler.PlatformView.SetStyle("gap", WebCss.Px(layout.Spacing));
	}

	public static void MapLineBreakMode(ButtonHandler handler, IButton button)
		=> handler.PlatformView.SetStyle("white-space",
			button is Button { LineBreakMode: LineBreakMode.WordWrap or LineBreakMode.CharacterWrap } ? "normal" : null);

	public static void MapImageSource(ButtonHandler handler, IButton button)
		=> handler.LoadImageAsync((button as IImageSourcePart)?.Source ?? (button as Button)?.ImageSource).FireAndForget("Button image");

	async Task LoadImageAsync(IImageSource? source)
	{
		_imageLoad?.Cancel();
		var cts = _imageLoad = new CancellationTokenSource();
		try
		{
			var url = await WebImageSource.ResolveAsync(source, MauiContext?.Services.GetService(typeof(IFontManager)) as IFontManager, cts.Token);
			if (cts.IsCancellationRequested)
			{
				WebImageSource.Release(url);
				return;
			}

			WebImageSource.Release(_imageUrl);
			_imageUrl = url;
			if (url is null)
			{
				_image?.Detach();
				_image = null;
			}
			else
			{
				if (_image is null)
				{
					_image = new WebElement("img");
					_image.SetAttribute("alt", "");
					Track(_image.On("load", () => VirtualView?.InvalidateMeasure()));
					PlatformView.Insert(0, _image);
				}
				_image.SetAttribute("src", url);
			}
			VirtualView?.InvalidateMeasure();
		}
		catch (OperationCanceledException)
		{
		}
	}
}

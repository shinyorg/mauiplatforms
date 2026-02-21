using Microsoft.Maui.Handlers;
using AppKit;
using Foundation;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

public partial class ButtonHandler : MacOSViewHandler<IButton, NSButton>
{
    public static readonly IPropertyMapper<IButton, ButtonHandler> Mapper =
        new PropertyMapper<IButton, ButtonHandler>(ViewMapper)
        {
            [nameof(IText.Text)] = MapText,
            [nameof(ITextStyle.TextColor)] = MapTextColor,
            [nameof(ITextStyle.Font)] = MapFont,
            [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(IView.Background)] = MapBackground,
            [nameof(IButtonStroke.CornerRadius)] = MapCornerRadius,
            [nameof(IButtonStroke.StrokeColor)] = MapStrokeColor,
            [nameof(IButtonStroke.StrokeThickness)] = MapStrokeThickness,
            [nameof(IPadding.Padding)] = MapPadding,
            [nameof(IImage.Source)] = MapImageSource,
        };

    public ButtonHandler() : base(Mapper)
    {
    }

    protected override NSButton CreatePlatformView()
    {
        var button = new NSButton
        {
            BezelStyle = NSBezelStyle.Rounded,
            Title = string.Empty,
        };
        return button;
    }

    protected override void ConnectHandler(NSButton platformView)
    {
        base.ConnectHandler(platformView);
        platformView.Activated += OnActivated;
    }

    protected override void DisconnectHandler(NSButton platformView)
    {
        platformView.Activated -= OnActivated;
        base.DisconnectHandler(platformView);
    }

    void OnActivated(object? sender, EventArgs e)
    {
        VirtualView?.Clicked();
    }

    public static void MapText(ButtonHandler handler, IButton button)
    {
        if (button is IText textButton)
            handler.PlatformView.Title = textButton.Text ?? string.Empty;
    }

    public static void MapTextColor(ButtonHandler handler, IButton button)
    {
        if (button is ITextStyle textStyle && textStyle.TextColor != null)
            handler.PlatformView.ContentTintColor = textStyle.TextColor.ToPlatformColor();
    }

    public static void MapFont(ButtonHandler handler, IButton button)
    {
        if (button is ITextStyle textStyle)
            handler.PlatformView.Font = textStyle.Font.ToNSFont();
    }

    public static void MapCharacterSpacing(ButtonHandler handler, IButton button)
    {
        if (button is ITextStyle textStyle)
        {
            var title = handler.PlatformView.Title ?? string.Empty;
            var attributedTitle = new NSMutableAttributedString(title);
            if (title.Length > 0)
            {
                var range = new NSRange(0, title.Length);
                attributedTitle.AddAttribute(NSStringAttributeKey.KerningAdjustment,
                    new NSNumber(textStyle.CharacterSpacing), range);

                if (textStyle.TextColor != null)
                    attributedTitle.AddAttribute(NSStringAttributeKey.ForegroundColor,
                        textStyle.TextColor.ToPlatformColor(), range);

                attributedTitle.AddAttribute(NSStringAttributeKey.Font,
                    handler.PlatformView.Font, range);
            }
            handler.PlatformView.AttributedTitle = attributedTitle;
        }
    }

    public static void MapBackground(ButtonHandler handler, IButton button)
    {
        if (button.Background is Graphics.SolidPaint solidPaint && solidPaint.Color != null)
        {
            handler.PlatformView.WantsLayer = true;
            handler.PlatformView.Bordered = false;
            handler.PlatformView.Layer!.BackgroundColor = solidPaint.Color.ToPlatformColor().CGColor;
        }
        else
        {
            handler.PlatformView.Bordered = true;
            handler.PlatformView.BezelStyle = NSBezelStyle.Rounded;
        }
    }

    public static void MapCornerRadius(ButtonHandler handler, IButton button)
    {
        if (button is IButtonStroke stroke && stroke.CornerRadius >= 0)
        {
            handler.PlatformView.WantsLayer = true;
            handler.PlatformView.Layer!.CornerRadius = (nfloat)stroke.CornerRadius;
        }
    }

    public static void MapStrokeColor(ButtonHandler handler, IButton button)
    {
        if (button is IButtonStroke stroke && stroke.StrokeColor != null)
        {
            handler.PlatformView.WantsLayer = true;
            handler.PlatformView.Layer!.BorderColor = stroke.StrokeColor.ToPlatformColor().CGColor;
        }
    }

    public static void MapStrokeThickness(ButtonHandler handler, IButton button)
    {
        if (button is IButtonStroke stroke && stroke.StrokeThickness >= 0)
        {
            handler.PlatformView.WantsLayer = true;
            handler.PlatformView.Layer!.BorderWidth = (nfloat)stroke.StrokeThickness;
        }
    }

    public static void MapPadding(ButtonHandler handler, IButton button)
    {
        // NSButton doesn't expose direct padding — handled via bezel insets or attributed title
    }

    public static void MapImageSource(ButtonHandler handler, IButton button)
    {
        if (button is not IImage imageButton)
            return;

        if (imageButton.Source is IFileImageSource fileSource)
        {
            handler.PlatformView.Image = new AppKit.NSImage(fileSource.File);
            handler.PlatformView.ImagePosition = NSCellImagePosition.ImageLeft;
        }
        else if (imageButton.Source is IFontImageSource fontSource)
        {
            handler.PlatformView.Image = FontImageSourceHelper.CreateImage(fontSource, handler.MauiContext);
            handler.PlatformView.ImagePosition = NSCellImagePosition.ImageLeft;
        }
        else if (imageButton.Source is IUriImageSource uriSource)
        {
            _ = LoadButtonImageFromUri(handler, uriSource.Uri);
        }
        else
        {
            handler.PlatformView.Image = null;
        }
    }

    static async Task LoadButtonImageFromUri(ButtonHandler handler, Uri uri)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            var data = await client.GetByteArrayAsync(uri);
            var nsImage = new AppKit.NSImage(Foundation.NSData.FromArray(data));
            handler.PlatformView.Image = nsImage;
            handler.PlatformView.ImagePosition = NSCellImagePosition.ImageLeft;
        }
        catch { }
    }
}

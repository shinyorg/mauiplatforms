using Microsoft.Maui.Handlers;
using AppKit;
using CoreGraphics;
using Foundation;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

class MauiNSButton : NSButton
{
    static readonly Thickness DefaultNonBorderedPadding = new Thickness(8, 4, 8, 4);
    Thickness _padding;

    public Thickness MauiPadding
    {
        get => _padding;
        set
        {
            _padding = value;
            InvalidateIntrinsicContentSize();
            NeedsDisplay = true;
        }
    }

    Thickness EffectivePadding
    {
        get
        {
            if (_padding != default)
                return _padding;
            // When Bordered=false (custom background), native bezel padding is lost;
            // apply a default so content (especially images) doesn't sit flush at edges
            if (!Bordered)
                return DefaultNonBorderedPadding;
            return default;
        }
    }

    public override CGSize IntrinsicContentSize
    {
        get
        {
            // Compute content size without calling base.IntrinsicContentSize,
            // which can trigger recursive auto-layout in AppKit → NaN → exception.
            var size = AttributedTitle?.Size ?? CGSize.Empty;
            if (Image != null)
            {
                var imgSize = Image.Size;
                size.Width += imgSize.Width;
                if (imgSize.Height > size.Height)
                    size.Height = imgSize.Height;
                if (AttributedTitle?.Length > 0)
                    size.Width += 4; // spacing between image and title
            }

            // If no content, return NoIntrinsicMetric
            if (size.Width <= 0 && size.Height <= 0)
                return new CGSize(NSView.NoIntrinsicMetric, NSView.NoIntrinsicMetric);

            var pad = EffectivePadding;
            if (Bordered)
            {
                // Bordered buttons have native bezel chrome — add standard bezel padding
                size.Width += 20;
                size.Height += 8;
            }
            else if (pad != default)
            {
                size.Width += (nfloat)(pad.Left + pad.Right);
                size.Height += (nfloat)(pad.Top + pad.Bottom);
            }

            // Ensure minimum touch target
            if (size.Height < 21)
                size.Height = 21;

            return size;
        }
    }

    public override void DrawRect(CGRect dirtyRect)
    {
        var pad = EffectivePadding;
        if (pad != default && !Bordered)
        {
            // Inset the drawing frame so cell content (text + image) is padded
            var paddedFrame = new CGRect(
                (nfloat)pad.Left,
                IsFlipped ? (nfloat)pad.Top : (nfloat)pad.Bottom,
                Bounds.Width - (nfloat)(pad.Left + pad.Right),
                Bounds.Height - (nfloat)(pad.Top + pad.Bottom)
            );
            Cell.DrawInteriorWithFrame(paddedFrame, this);
        }
        else
        {
            base.DrawRect(dirtyRect);
        }
    }
}

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
        var button = new MauiNSButton
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
        {
            handler.PlatformView.Title = textButton.Text ?? string.Empty;
            handler.PlatformView.InvalidateIntrinsicContentSize();
        }
    }

    public static void MapTextColor(ButtonHandler handler, IButton button)
    {
        if (button is ITextStyle textStyle && textStyle.TextColor != null)
            handler.PlatformView.ContentTintColor = textStyle.TextColor.ToPlatformColor();
    }

    public static void MapFont(ButtonHandler handler, IButton button)
    {
        if (button is ITextStyle textStyle)
        {
            handler.PlatformView.Font = textStyle.Font.ToNSFont();
            handler.PlatformView.InvalidateIntrinsicContentSize();
        }
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
            handler.PlatformView.InvalidateIntrinsicContentSize();
        }
    }

    public static void MapBackground(ButtonHandler handler, IButton button)
    {
        var wasBordered = handler.PlatformView.Bordered;

        // Extract color from background paint or BackgroundColor
        Graphics.Color? bgColor = null;
        if (button.Background is Graphics.SolidPaint solidPaint)
            bgColor = solidPaint.Color;

        // Fallback: check BackgroundColor directly (handles SolidColorBrush / ImmutableBrush)
        if (bgColor == null && button is Microsoft.Maui.Controls.Button mauiButton && mauiButton.BackgroundColor != null)
            bgColor = mauiButton.BackgroundColor;

        if (bgColor != null)
        {
            handler.PlatformView.WantsLayer = true;
            handler.PlatformView.Bordered = false;
            handler.PlatformView.Layer!.BackgroundColor = bgColor.ToPlatformColor().CGColor;
        }
        else
        {
            handler.PlatformView.Bordered = true;
            handler.PlatformView.BezelStyle = NSBezelStyle.Rounded;
        }

        // Bordered state affects default padding — invalidate both native and MAUI layout
        if (wasBordered != handler.PlatformView.Bordered)
        {
            handler.PlatformView.InvalidateIntrinsicContentSize();
            handler.PlatformView.NeedsDisplay = true;
            handler.VirtualView?.InvalidateMeasure();
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
        if (handler.PlatformView is MauiNSButton mauiButton && button is IPadding padded)
        {
            mauiButton.MauiPadding = padded.Padding;
        }
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

        handler.PlatformView.InvalidateIntrinsicContentSize();
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

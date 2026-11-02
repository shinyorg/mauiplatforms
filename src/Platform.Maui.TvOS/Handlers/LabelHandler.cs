using CoreGraphics;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

/// <summary>UILabel with padding and vertical text alignment.</summary>
public class TvOSLabel : UILabel
{
    public UIEdgeInsets TextInsets { get; set; }
    public Microsoft.Maui.TextAlignment VerticalTextAlignment { get; set; } = Microsoft.Maui.TextAlignment.Start;

    public override void DrawText(CGRect rect)
    {
        var inner = TextInsets.InsetRect(rect);
        if (VerticalTextAlignment != Microsoft.Maui.TextAlignment.Center)
        {
            var textHeight = TextRectForBounds(inner, Lines).Height;
            var spare = inner.Height - textHeight;
            if (spare > 0)
            {
                inner = VerticalTextAlignment == Microsoft.Maui.TextAlignment.End
                    ? new CGRect(inner.X, inner.Y + spare, inner.Width, textHeight)
                    : new CGRect(inner.X, inner.Y, inner.Width, textHeight);
            }
        }
        base.DrawText(inner);
    }

    public override CGSize SizeThatFits(CGSize size)
    {
        var horizontal = TextInsets.Left + TextInsets.Right;
        var vertical = TextInsets.Top + TextInsets.Bottom;
        var inner = base.SizeThatFits(new CGSize(
            Math.Max(0, (double)(size.Width - horizontal)),
            Math.Max(0, (double)(size.Height - vertical))));

        // An empty label still takes the space its padding asks for.
        return new CGSize(inner.Width + horizontal, inner.Height + vertical);
    }
}

public partial class LabelHandler : TvOSViewHandler<ILabel, TvOSLabel>
{
    public static readonly IPropertyMapper<ILabel, LabelHandler> Mapper =
        new PropertyMapper<ILabel, LabelHandler>(ViewMapper)
        {
            [nameof(ILabel.Text)] = MapText,
            [nameof(ILabel.TextColor)] = MapTextColor,
            [nameof(ILabel.Font)] = MapFont,
            [nameof(ILabel.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            [nameof(ILabel.VerticalTextAlignment)] = MapVerticalTextAlignment,
            [nameof(ILabel.Padding)] = MapPadding,
            [nameof(ILabel.TextDecorations)] = MapText,
            [nameof(ILabel.CharacterSpacing)] = MapText,
            [nameof(Microsoft.Maui.Controls.Label.MaxLines)] = MapMaxLines,
            [nameof(Microsoft.Maui.Controls.Label.LineBreakMode)] = MapLineBreakMode,
        };

    public LabelHandler() : base(Mapper)
    {
    }

    protected override TvOSLabel CreatePlatformView()
    {
        return new TvOSLabel
        {
            Lines = 0,
            TextColor = UIColor.White,
        };
    }

    public static void MapText(LabelHandler handler, ILabel label)
    {
        var platformView = handler.PlatformView;
        var decorations = label.TextDecorations;
        var spacing = label.CharacterSpacing;

        if (decorations == TextDecorations.None && spacing == 0)
        {
            platformView.AttributedText = null;
            platformView.Text = label.Text;
        }
        else
        {
            var attributes = new UIStringAttributes { Font = platformView.Font, ForegroundColor = platformView.TextColor };
            if (spacing != 0)
                attributes.KerningAdjustment = (float)spacing;
            if (decorations.HasFlag(TextDecorations.Underline))
                attributes.UnderlineStyle = Foundation.NSUnderlineStyle.Single;
            if (decorations.HasFlag(TextDecorations.Strikethrough))
                attributes.StrikethroughStyle = Foundation.NSUnderlineStyle.Single;
            platformView.AttributedText = new Foundation.NSAttributedString(label.Text ?? string.Empty, attributes);
        }

        platformView.SetNeedsDisplay();
    }

    public static void MapTextColor(LabelHandler handler, ILabel label)
    {
        if (label.TextColor != null)
            handler.PlatformView.TextColor = label.TextColor.ToPlatformColor();
        if (handler.PlatformView.AttributedText != null)
            MapText(handler, label);
    }

    public static void MapFont(LabelHandler handler, ILabel label)
    {
        handler.PlatformView.Font = label.Font.ToUIFont();
        if (handler.PlatformView.AttributedText != null)
            MapText(handler, label);
    }

    public static void MapHorizontalTextAlignment(LabelHandler handler, ILabel label)
    {
        handler.PlatformView.TextAlignment = label.HorizontalTextAlignment switch
        {
            TextAlignment.Center => UITextAlignment.Center,
            TextAlignment.End => UITextAlignment.Right,
            _ => UITextAlignment.Left,
        };
    }

    public static void MapVerticalTextAlignment(LabelHandler handler, ILabel label)
    {
        handler.PlatformView.VerticalTextAlignment = label.VerticalTextAlignment;
        handler.PlatformView.SetNeedsDisplay();
    }

    public static void MapPadding(LabelHandler handler, ILabel label)
    {
        var p = label.Padding;
        handler.PlatformView.TextInsets = new UIEdgeInsets((nfloat)p.Top, (nfloat)p.Left, (nfloat)p.Bottom, (nfloat)p.Right);
        handler.PlatformView.SetNeedsDisplay();
    }

    public static void MapMaxLines(LabelHandler handler, ILabel label) => UpdateLines(handler, label);

    public static void MapLineBreakMode(LabelHandler handler, ILabel label) => UpdateLines(handler, label);

    static void UpdateLines(LabelHandler handler, ILabel label)
    {
        if (label is not Microsoft.Maui.Controls.Label controlsLabel)
            return;

        var mode = controlsLabel.LineBreakMode;
        handler.PlatformView.LineBreakMode = mode switch
        {
            LineBreakMode.NoWrap => UILineBreakMode.Clip,
            LineBreakMode.CharacterWrap => UILineBreakMode.CharacterWrap,
            LineBreakMode.HeadTruncation => UILineBreakMode.HeadTruncation,
            LineBreakMode.MiddleTruncation => UILineBreakMode.MiddleTruncation,
            LineBreakMode.TailTruncation => UILineBreakMode.TailTruncation,
            _ => UILineBreakMode.WordWrap,
        };

        // MaxLines wins when set; otherwise truncating or non-wrapping modes are single-line.
        var singleLine = mode is LineBreakMode.NoWrap or LineBreakMode.HeadTruncation
            or LineBreakMode.MiddleTruncation or LineBreakMode.TailTruncation;
        handler.PlatformView.Lines = controlsLabel.MaxLines >= 0 ? controlsLabel.MaxLines
            : singleLine ? 1
            : 0;
    }
}

internal static class ColorExtensions
{
    public static UIColor ToPlatformColor(this Graphics.Color color)
    {
        if (color == null)
            return UIColor.White;

        return new UIColor(
            (nfloat)color.Red,
            (nfloat)color.Green,
            (nfloat)color.Blue,
            (nfloat)color.Alpha);
    }
}

public static class FontExtensions
{
    /// <summary>Converts a MAUI font (family, size, weight, slant) to a UIFont.</summary>
    public static UIFont ToUIFont(this Font font, double defaultSize = 29)
    {
        var size = (nfloat)(font.Size > 0 ? font.Size : defaultSize);
        var weight = font.Weight switch
        {
            <= FontWeight.Thin => UIFontWeight.Thin,
            <= FontWeight.Ultralight => UIFontWeight.UltraLight,
            <= FontWeight.Light => UIFontWeight.Light,
            <= FontWeight.Regular => UIFontWeight.Regular,
            <= FontWeight.Medium => UIFontWeight.Medium,
            <= FontWeight.Semibold => UIFontWeight.Semibold,
            <= FontWeight.Bold => UIFontWeight.Bold,
            <= FontWeight.Heavy => UIFontWeight.Heavy,
            _ => UIFontWeight.Black,
        };

        UIFont? result = null;
        if (!string.IsNullOrEmpty(font.Family))
            result = UIFont.FromName(font.Family, size);
        result ??= UIFont.SystemFontOfSize(size, weight);

        if (font.Slant != FontSlant.Default)
        {
            var descriptor = result.FontDescriptor.CreateWithTraits(result.FontDescriptor.SymbolicTraits | UIFontDescriptorSymbolicTraits.Italic);
            if (descriptor != null)
                result = UIFont.FromDescriptor(descriptor, size);
        }

        return result;
    }
}

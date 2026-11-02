using CoreGraphics;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

/// <summary>
/// Button with the tvOS focus treatment. Without an app background it uses the system look:
/// a translucent platter that turns white with dark text when focused. With a background
/// color it keeps that color and lifts with a brighter tint and a shadow when focused.
/// </summary>
public class TvOSButton : UIButton
{
    static readonly UIColor DefaultBackground = UIColor.FromWhiteAlpha(1, 0.14f);
    static readonly UIColor DefaultFocusedText = UIColor.FromRGB(28, 28, 30);

    UIColor? _customBackground;
    UIColor _textColor = UIColor.White;

    public TvOSButton()
    {
        ClipsToBounds = false;
        Layer.CornerRadius = 12;
        UpdateColors(false);
    }

    public UIColor? CustomBackground
    {
        get => _customBackground;
        set { _customBackground = value; UpdateColors(Focused); }
    }

    public UIColor TextColor
    {
        get => _textColor;
        set { _textColor = value; UpdateColors(Focused); }
    }

    public override void DidUpdateFocus(UIFocusUpdateContext context, UIFocusAnimationCoordinator coordinator)
    {
        base.DidUpdateFocus(context, coordinator);

        bool? focused = context.NextFocusedView == this ? true
            : context.PreviouslyFocusedView == this ? false
            : null;
        if (focused is not bool isFocused)
            return;

        TvOSFocusEffect.Animate(this, isFocused, coordinator);
        coordinator.AddCoordinatedAnimations(() => UpdateColors(isFocused), null);
    }

    public override void PressesBegan(Foundation.NSSet<UIPress> presses, UIPressesEvent evt)
    {
        UIView.Animate(0.1, () => TvOSTransforms.SetFocusScale(this, TvOSFocusEffect.PressedScale));
        base.PressesBegan(presses, evt);
    }

    public override void PressesEnded(Foundation.NSSet<UIPress> presses, UIPressesEvent evt)
    {
        UIView.Animate(0.15, () => TvOSTransforms.SetFocusScale(this, Focused ? TvOSFocusEffect.FocusedScale : 1));
        base.PressesEnded(presses, evt);
    }

    public override void PressesCancelled(Foundation.NSSet<UIPress> presses, UIPressesEvent evt)
    {
        UIView.Animate(0.15, () => TvOSTransforms.SetFocusScale(this, Focused ? TvOSFocusEffect.FocusedScale : 1));
        base.PressesCancelled(presses, evt);
    }

    void UpdateColors(bool focused)
    {
        if (_customBackground == null)
        {
            BackgroundColor = focused ? UIColor.White : DefaultBackground;
            SetTitleColor(focused ? DefaultFocusedText : _textColor, UIControlState.Normal);
        }
        else
        {
            BackgroundColor = focused ? Brighten(_customBackground) : _customBackground;
            SetTitleColor(_textColor, UIControlState.Normal);
        }
        SetTitleColor(_textColor.ColorWithAlpha(0.4f), UIControlState.Disabled);
    }

    static UIColor Brighten(UIColor color)
    {
        color.GetHSBA(out var h, out var s, out var b, out var a);
        return UIColor.FromHSBA(h, (nfloat)Math.Max(0, s - 0.08), (nfloat)Math.Min(1, b + 0.12), a);
    }
}

public partial class ButtonHandler : TvOSViewHandler<IButton, TvOSButton>
{
    public static readonly IPropertyMapper<IButton, ButtonHandler> Mapper =
        new PropertyMapper<IButton, ButtonHandler>(ViewMapper)
        {
            [nameof(IText.Text)] = MapText,
            [nameof(ITextStyle.TextColor)] = MapTextColor,
            [nameof(ITextStyle.Font)] = MapFont,
            [nameof(IView.Background)] = MapBackground,
            [nameof(IButtonStroke.CornerRadius)] = MapCornerRadius,
            [nameof(IButtonStroke.StrokeColor)] = MapStroke,
            [nameof(IButtonStroke.StrokeThickness)] = MapStroke,
            [nameof(IPadding.Padding)] = MapPadding,
        };

    public ButtonHandler() : base(Mapper)
    {
    }

    protected override TvOSButton CreatePlatformView() => new();

    protected override void ConnectHandler(TvOSButton platformView)
    {
        base.ConnectHandler(platformView);
        platformView.PrimaryActionTriggered += OnPrimaryActionTriggered;
    }

    protected override void DisconnectHandler(TvOSButton platformView)
    {
        platformView.PrimaryActionTriggered -= OnPrimaryActionTriggered;
        base.DisconnectHandler(platformView);
    }

    void OnPrimaryActionTriggered(object? sender, EventArgs e)
    {
        VirtualView?.Pressed();
        VirtualView?.Released();
        VirtualView?.Clicked();
    }

    public static void MapText(ButtonHandler handler, IButton button)
    {
        if (button is IText textButton)
            handler.PlatformView.SetTitle(textButton.Text, UIControlState.Normal);
    }

    public static void MapTextColor(ButtonHandler handler, IButton button)
    {
        if (button is ITextStyle textStyle && textStyle.TextColor != null)
            handler.PlatformView.TextColor = textStyle.TextColor.ToPlatformColor();
    }

    public static void MapFont(ButtonHandler handler, IButton button)
    {
        if (button is ITextStyle textStyle && handler.PlatformView.TitleLabel is { } titleLabel)
        {
            var font = textStyle.Font;
            if (font.Weight == FontWeight.Regular)
                font = font.WithWeight(FontWeight.Medium);
            titleLabel.Font = font.ToUIFont(29);
        }
    }

    public static void MapBackground(ButtonHandler handler, IButton button)
    {
        handler.PlatformView.CustomBackground = button.Background switch
        {
            Graphics.SolidPaint { Color: not null } solid => solid.Color.ToPlatformColor(),
            Graphics.GradientPaint { GradientStops.Length: > 0 } gradient => gradient.GradientStops[0].Color?.ToPlatformColor(),
            _ => null,
        };
    }

    public static void MapCornerRadius(ButtonHandler handler, IButton button)
    {
        // MAUI's default of -1 means "platform default".
        handler.PlatformView.Layer.CornerRadius = button.CornerRadius >= 0 ? button.CornerRadius : 12;
    }

    public static void MapStroke(ButtonHandler handler, IButton button)
    {
        var layer = handler.PlatformView.Layer;
        layer.BorderWidth = (nfloat)Math.Max(0, button.StrokeThickness);
        layer.BorderColor = button.StrokeColor is Graphics.Color color ? color.ToPlatformColor().CGColor : null;
    }

#pragma warning disable CA1422 // ContentEdgeInsets: the button does not use UIButtonConfiguration
    public static void MapPadding(ButtonHandler handler, IButton button)
    {
        var p = button.Padding;
        // Leave room for the title when no padding was given.
        if (p.IsEmpty || double.IsNaN(p.Left))
            p = new Thickness(40, 18);
        handler.PlatformView.ContentEdgeInsets = new UIEdgeInsets((nfloat)p.Top, (nfloat)p.Left, (nfloat)p.Bottom, (nfloat)p.Right);
    }
#pragma warning restore CA1422
}

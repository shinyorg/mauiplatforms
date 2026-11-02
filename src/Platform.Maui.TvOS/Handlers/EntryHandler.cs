using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

/// <summary>
/// Text field that, when the app gives it a background color, swaps to the standard tvOS
/// focused look (white field, dark text) while focused, since the system focus platter
/// would otherwise be painted over.
/// </summary>
public class TvOSTextField : UITextField
{
    static readonly UIColor FocusedText = UIColor.FromRGB(28, 28, 30);
    UIColor? _customBackground;
    UIColor? _appTextColor;

    /// <summary>Corner radius of the field when the app gives it a background.</summary>
    public nfloat CornerRadius { get; set; } = 12;

    public UIColor? CustomBackground
    {
        get => _customBackground;
        set { _customBackground = value; Apply(Focused); }
    }

    public UIColor? AppTextColor
    {
        get => _appTextColor;
        set { _appTextColor = value; Apply(Focused); }
    }

    public override void DidUpdateFocus(UIFocusUpdateContext context, UIFocusAnimationCoordinator coordinator)
    {
        base.DidUpdateFocus(context, coordinator);
        if (_customBackground == null)
            return;

        bool? focused = context.NextFocusedView == this ? true
            : context.PreviouslyFocusedView == this ? false
            : null;
        if (focused is not bool isFocused)
            return;

        // Clipped to its rounded shape (the field paints its background in a subview), so it
        // lifts without a shadow.
        TvOSFocusEffect.Animate(this, isFocused, coordinator, shadow: false);
        coordinator.AddCoordinatedAnimations(() => Apply(isFocused), null);
    }

    void Apply(bool focused)
    {
        if (_customBackground == null)
        {
            ClipsToBounds = false;
            if (_appTextColor != null)
                TextColor = _appTextColor;
            return;
        }

        ClipsToBounds = true;

        BackgroundColor = focused ? UIColor.White : _customBackground;
        TextColor = focused ? FocusedText : _appTextColor ?? UIColor.White;
        // The system resets the corner radius as the field takes focus.
        Layer.CornerRadius = CornerRadius;
    }
}

public partial class EntryHandler : TvOSViewHandler<IEntry, TvOSTextField>
{
    public static readonly IPropertyMapper<IEntry, EntryHandler> Mapper =
        new PropertyMapper<IEntry, EntryHandler>(ViewMapper)
        {
            [nameof(ITextInput.Text)] = MapText,
            [nameof(ITextStyle.TextColor)] = MapTextColor,
            [nameof(ITextStyle.Font)] = MapFont,
            [nameof(IPlaceholder.Placeholder)] = MapPlaceholder,
            [nameof(IPlaceholder.PlaceholderColor)] = MapPlaceholderColor,
            [nameof(IEntry.IsPassword)] = MapIsPassword,
            [nameof(ITextInput.IsReadOnly)] = MapIsReadOnly,
            [nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            [nameof(IEntry.ReturnType)] = MapReturnType,
            [nameof(ITextInput.MaxLength)] = MapMaxLength,
            [nameof(IView.Background)] = MapBackground,
        };

    bool _updating;

    public EntryHandler() : base(Mapper)
    {
    }

    protected override TvOSTextField CreatePlatformView()
    {
        return new TvOSTextField
        {
            BorderStyle = UITextBorderStyle.RoundedRect,
        };
    }

    /// <summary>
    /// The system field draws its own rounded platter, so an app background replaces the
    /// platter rather than filling the rectangle behind it.
    /// </summary>
    public static void MapBackground(EntryHandler handler, IEntry entry)
    {
        var field = handler.PlatformView;
        if (entry.Background is Microsoft.Maui.Graphics.SolidPaint { Color: not null } solid)
        {
            field.BorderStyle = UITextBorderStyle.None;
            field.CustomBackground = solid.Color.ToPlatformColor();
            field.Layer.CornerRadius = field.CornerRadius;
            field.LeftView ??= new UIView(new CoreGraphics.CGRect(0, 0, 24, 1));
            field.LeftViewMode = UITextFieldViewMode.Always;
        }
        else
        {
            field.BorderStyle = UITextBorderStyle.RoundedRect;
            field.CustomBackground = null;
            field.BackgroundColor = null;
            field.Layer.CornerRadius = 0;
            field.LeftViewMode = UITextFieldViewMode.Never;
        }
    }

    protected override void ConnectHandler(TvOSTextField platformView)
    {
        base.ConnectHandler(platformView);
        platformView.EditingChanged += OnEditingChanged;
        platformView.ShouldReturn = OnShouldReturn;
    }

    protected override void DisconnectHandler(TvOSTextField platformView)
    {
        platformView.EditingChanged -= OnEditingChanged;
        platformView.ShouldReturn = null;
        base.DisconnectHandler(platformView);
    }

    void OnEditingChanged(object? sender, EventArgs e)
    {
        if (_updating || VirtualView == null)
            return;

        _updating = true;
        try
        {
            if (VirtualView is ITextInput textInput)
                textInput.Text = PlatformView.Text ?? string.Empty;
        }
        finally
        {
            _updating = false;
        }
    }

    bool OnShouldReturn(UITextField textField)
    {
        textField.ResignFirstResponder();
        VirtualView?.Completed();
        return true;
    }

    public static void MapText(EntryHandler handler, IEntry entry)
    {
        if (handler._updating)
            return;

        if (entry is ITextInput textInput)
            handler.PlatformView.Text = textInput.Text;
    }

    public static void MapTextColor(EntryHandler handler, IEntry entry)
    {
        if (entry is ITextStyle textStyle && textStyle.TextColor != null)
            handler.PlatformView.AppTextColor = textStyle.TextColor.ToPlatformColor();
    }

    public static void MapFont(EntryHandler handler, IEntry entry)
    {
        if (entry is ITextStyle textStyle)
        {
            var fontSize = textStyle.Font.Size > 0 ? (nfloat)textStyle.Font.Size : (nfloat)17.0;
            handler.PlatformView.Font = UIFont.SystemFontOfSize(fontSize);
        }
    }

    public static void MapPlaceholder(EntryHandler handler, IEntry entry)
    {
        if (entry is IPlaceholder placeholder)
            handler.PlatformView.Placeholder = placeholder.Placeholder;
    }

    public static void MapPlaceholderColor(EntryHandler handler, IEntry entry)
    {
        if (entry is IPlaceholder placeholder && placeholder.PlaceholderColor != null)
        {
            handler.PlatformView.AttributedPlaceholder = new NSAttributedString(
                placeholder.Placeholder ?? string.Empty,
                new UIStringAttributes
                {
                    ForegroundColor = placeholder.PlaceholderColor.ToPlatformColor(),
                });
        }
    }

    public static void MapIsPassword(EntryHandler handler, IEntry entry)
    {
        handler.PlatformView.SecureTextEntry = entry.IsPassword;
    }

    public static void MapIsReadOnly(EntryHandler handler, IEntry entry)
    {
        if (entry is ITextInput textInput)
            handler.PlatformView.Enabled = !textInput.IsReadOnly;
    }

    public static void MapHorizontalTextAlignment(EntryHandler handler, IEntry entry)
    {
        if (entry is ITextAlignment textAlignment)
        {
            handler.PlatformView.TextAlignment = textAlignment.HorizontalTextAlignment switch
            {
                TextAlignment.Center => UITextAlignment.Center,
                TextAlignment.End => UITextAlignment.Right,
                _ => UITextAlignment.Left,
            };
        }
    }

    public static void MapReturnType(EntryHandler handler, IEntry entry)
    {
        handler.PlatformView.ReturnKeyType = entry.ReturnType switch
        {
            ReturnType.Go => UIReturnKeyType.Go,
            ReturnType.Next => UIReturnKeyType.Next,
            ReturnType.Search => UIReturnKeyType.Search,
            ReturnType.Send => UIReturnKeyType.Send,
            ReturnType.Done => UIReturnKeyType.Done,
            _ => UIReturnKeyType.Default,
        };
    }

    public static void MapMaxLength(EntryHandler handler, IEntry entry)
    {
        // MaxLength enforcement is handled during text change events
        if (entry is ITextInput textInput && textInput.MaxLength >= 0)
        {
            var currentText = handler.PlatformView.Text ?? string.Empty;
            if (currentText.Length > textInput.MaxLength)
                handler.PlatformView.Text = currentText[..textInput.MaxLength];
        }
    }
}

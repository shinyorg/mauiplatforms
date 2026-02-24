using Foundation;
using Microsoft.Maui.Handlers;
using AppKit;
using CoreGraphics;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

public class EditorNSView : NSScrollView
{
    public NSTextView TextView { get; }

    public EditorNSView()
    {
        HasVerticalScroller = true;
        HasHorizontalScroller = false;
        AutohidesScrollers = true;

        TextView = new NSTextView
        {
            RichText = false,
            VerticallyResizable = true,
            HorizontallyResizable = false,
            AutoresizingMask = NSViewResizingMask.WidthSizable,
        };
        TextView.TextContainer!.WidthTracksTextView = true;

        DocumentView = TextView;
    }

    public override CGSize IntrinsicContentSize
    {
        get
        {
            // Compute height from actual text content, matching UITextView behavior.
            // Empty editors get a single-line default (~36px with system font).
            var tv = TextView;
            if (tv.LayoutManager != null && tv.TextContainer != null)
            {
                tv.LayoutManager.EnsureLayoutForTextContainer(tv.TextContainer);
                var usedRect = tv.LayoutManager.GetUsedRect(tv.TextContainer);
                var textHeight = usedRect.Height;
                // Add vertical inset/padding
                var inset = tv.TextContainerInset;
                var height = Math.Max(textHeight + inset.Height * 2, 36);
                return new CGSize(NSView.NoIntrinsicMetric, height);
            }
            return new CGSize(NSView.NoIntrinsicMetric, 36);
        }
    }
}

public class EditorHandler : MacOSViewHandler<IEditor, EditorNSView>
{
    public static readonly IPropertyMapper<IEditor, EditorHandler> Mapper =
        new PropertyMapper<IEditor, EditorHandler>(ViewMapper)
        {
            [nameof(ITextInput.Text)] = MapText,
            [nameof(ITextStyle.TextColor)] = MapTextColor,
            [nameof(ITextStyle.Font)] = MapFont,
            [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(IPlaceholder.Placeholder)] = MapPlaceholder,
            [nameof(ITextInput.IsReadOnly)] = MapIsReadOnly,
            [nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            [nameof(ITextInput.MaxLength)] = MapMaxLength,
        };

    bool _updating;

    public EditorHandler() : base(Mapper) { }

    protected override EditorNSView CreatePlatformView() => new();

    protected override void ConnectHandler(EditorNSView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.TextView.TextDidChange += OnTextChanged;
    }

    protected override void DisconnectHandler(EditorNSView platformView)
    {
        platformView.TextView.TextDidChange -= OnTextChanged;
        base.DisconnectHandler(platformView);
    }

    void OnTextChanged(object? sender, EventArgs e)
    {
        if (_updating || VirtualView == null)
            return;

        _updating = true;
        try
        {
            VirtualView.Text = PlatformView.TextView.Value ?? string.Empty;
        }
        finally
        {
            _updating = false;
        }
    }

    public static void MapText(EditorHandler handler, IEditor editor)
    {
        if (handler._updating)
            return;

        handler.PlatformView.TextView.Value = editor.Text ?? string.Empty;
    }

    public static void MapTextColor(EditorHandler handler, IEditor editor)
    {
        if (editor.TextColor is not null)
            handler.PlatformView.TextView.TextColor = editor.TextColor.ToPlatformColor();
    }

    public static void MapFont(EditorHandler handler, IEditor editor)
    {
        handler.PlatformView.TextView.Font = editor.Font.ToNSFont();
    }

    public static void MapPlaceholder(EditorHandler handler, IEditor editor)
    {
        // NSTextView doesn't have built-in placeholder support.
        // Use the PlaceholderString on the underlying text view's enclosing scroll view or
        // overlay a label. For now, set the accessibility placeholder.
        if (editor is IPlaceholder placeholder)
            handler.PlatformView.TextView.AccessibilityPlaceholderValue = placeholder.Placeholder;
    }

    public static void MapCharacterSpacing(EditorHandler handler, IEditor editor)
    {
        if (editor.CharacterSpacing != 0)
        {
            var text = handler.PlatformView.TextView.Value ?? string.Empty;
            var storage = handler.PlatformView.TextView.TextStorage;
            if (storage != null && text.Length > 0)
            {
                storage.AddAttribute(NSStringAttributeKey.KerningAdjustment,
                    NSNumber.FromDouble(editor.CharacterSpacing), new NSRange(0, text.Length));
            }
        }
    }

    public static void MapIsReadOnly(EditorHandler handler, IEditor editor)
    {
        handler.PlatformView.TextView.Editable = !editor.IsReadOnly;
    }

    public static void MapHorizontalTextAlignment(EditorHandler handler, IEditor editor)
    {
        handler.PlatformView.TextView.Alignment = editor.HorizontalTextAlignment switch
        {
            TextAlignment.Center => NSTextAlignment.Center,
            TextAlignment.End => NSTextAlignment.Right,
            _ => NSTextAlignment.Left,
        };
    }

    public static void MapMaxLength(EditorHandler handler, IEditor editor)
    {
        if (editor.MaxLength >= 0)
        {
            var currentText = handler.PlatformView.TextView.Value ?? string.Empty;
            if (currentText.Length > editor.MaxLength)
                handler.PlatformView.TextView.Value = currentText[..editor.MaxLength];
        }
    }
}

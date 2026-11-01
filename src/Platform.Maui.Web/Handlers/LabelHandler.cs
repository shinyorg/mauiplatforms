using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// Label as a flex column (for vertical alignment) wrapping a span that carries the text.
/// </summary>
public class LabelHandler : WebElementHandler<ILabel, WebElement>
{
	public static IPropertyMapper<ILabel, LabelHandler> Mapper =
		new PropertyMapper<ILabel, LabelHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(ILabel.Text)] = MapText,
			[nameof(Label.FormattedText)] = MapText,
			[nameof(Label.TextType)] = MapText,
			[nameof(Label.TextTransform)] = MapTextTransform,
			[nameof(ITextStyle.TextColor)] = MapTextColor,
			[nameof(ITextStyle.Font)] = MapFont,
			[nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
			[nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
			[nameof(ITextAlignment.VerticalTextAlignment)] = MapVerticalTextAlignment,
			[nameof(ILabel.TextDecorations)] = MapTextDecorations,
			[nameof(ILabel.LineHeight)] = MapLineHeight,
			[nameof(IPadding.Padding)] = MapPadding,
			[nameof(Label.LineBreakMode)] = MapLineBreakMode,
			[nameof(Label.MaxLines)] = MapLineBreakMode,
		};

	public LabelHandler() : base(Mapper)
	{
	}

	public LabelHandler(IPropertyMapper? mapper) : base(mapper ?? Mapper)
	{
	}

	public WebElement TextElement { get; private set; } = null!;

	protected override WebElement CreatePlatformView()
	{
		var label = new WebElement("div", "maui-label");
		TextElement = new WebElement("span");
		label.Append(TextElement);
		return label;
	}

	WebFontManager? FontManager => MauiContext?.Services.GetService(typeof(IFontManager)) as WebFontManager;

	public static void MapText(LabelHandler handler, ILabel label)
	{
		var span = handler.TextElement;
		if (label is Label { FormattedText: { Spans.Count: > 0 } formatted })
		{
			span.Clear();
			foreach (var s in formatted.Spans)
				span.Append(CreateSpan(handler, s));
		}
		else if (label is Label { TextType: TextType.Html })
		{
			span.SetHtml(label.Text);
		}
		else
		{
			span.SetText(label.Text);
		}
	}

	static WebElement CreateSpan(LabelHandler handler, Span span)
	{
		var el = new WebElement("span");
		el.SetText(span.TextTransform switch
		{
			TextTransform.Uppercase => span.Text?.ToUpperInvariant(),
			TextTransform.Lowercase => span.Text?.ToLowerInvariant(),
			_ => span.Text,
		});
		el.SetStyle("color", WebCss.Color(span.TextColor));
		el.SetStyle("background-color", WebCss.Color(span.BackgroundColor));
		if (span.FontSize > 0)
			el.SetStyle("font-size", WebCss.Px(span.FontSize));
		if (!string.IsNullOrEmpty(span.FontFamily))
			el.SetStyle("font-family", handler.FontManager?.GetFontFamily(span.FontFamily));
		if (span.FontAttributes.HasFlag(FontAttributes.Bold))
			el.SetStyle("font-weight", "700");
		if (span.FontAttributes.HasFlag(FontAttributes.Italic))
			el.SetStyle("font-style", "italic");
		el.SetStyle("text-decoration", WebCss.TextDecoration(span.TextDecorations));
		if (span.CharacterSpacing != 0)
			el.SetStyle("letter-spacing", WebCss.Px(span.CharacterSpacing));
		if (span.LineHeight > 0)
			el.SetStyle("line-height", WebCss.Num(span.LineHeight));
		return el;
	}

	public static void MapTextTransform(LabelHandler handler, ILabel label)
		=> handler.PlatformView.SetStyle("text-transform", label is Label l ? WebCss.TextTransform(l.TextTransform) : null);

	public static void MapTextColor(LabelHandler handler, ILabel label)
		=> handler.PlatformView.SetStyle("color", WebCss.Color(label.TextColor));

	public static void MapFont(LabelHandler handler, ILabel label)
		=> handler.FontManager?.Apply(handler.PlatformView, label.Font);

	public static void MapCharacterSpacing(LabelHandler handler, ILabel label)
		=> handler.PlatformView.SetStyle("letter-spacing", label.CharacterSpacing == 0 ? null : WebCss.Px(label.CharacterSpacing));

	public static void MapHorizontalTextAlignment(LabelHandler handler, ILabel label)
		=> handler.PlatformView.SetStyle("text-align", WebCss.TextAlign(label.HorizontalTextAlignment));

	public static void MapVerticalTextAlignment(LabelHandler handler, ILabel label)
		=> handler.PlatformView.SetStyle("justify-content", WebCss.FlexAlign(label.VerticalTextAlignment));

	public static void MapTextDecorations(LabelHandler handler, ILabel label)
		=> handler.PlatformView.SetStyle("text-decoration", WebCss.TextDecoration(label.TextDecorations));

	public static void MapLineHeight(LabelHandler handler, ILabel label)
		=> handler.PlatformView.SetStyle("line-height", label.LineHeight > 0 ? WebCss.Num(label.LineHeight) : null);

	public static void MapPadding(LabelHandler handler, ILabel label)
		=> handler.PlatformView.SetStyle("padding", label.Padding.IsEmpty ? null : WebCss.Thickness(label.Padding));

	public static void MapLineBreakMode(LabelHandler handler, ILabel label)
	{
		if (label is not Label l)
			return;

		var el = handler.PlatformView;
		var truncate = l.LineBreakMode is LineBreakMode.TailTruncation or LineBreakMode.HeadTruncation or LineBreakMode.MiddleTruncation;
		var clamp = l.MaxLines > 0;

		el.ToggleClass("nowrap", l.LineBreakMode == LineBreakMode.NoWrap);
		el.ToggleClass("truncate", truncate && !clamp);
		el.ToggleClass("clamp", clamp);
		el.SetStyle("word-break", l.LineBreakMode == LineBreakMode.CharacterWrap ? "break-all" : null);
		handler.TextElement.SetStyle("-webkit-line-clamp", clamp ? l.MaxLines.ToString() : null);
		// Head truncation renders the ellipsis at the start by flipping the inline direction.
		handler.TextElement.SetStyle("direction", l.LineBreakMode == LineBreakMode.HeadTruncation && !clamp ? "rtl" : null);
	}
}

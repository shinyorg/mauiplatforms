using Microsoft.Maui.Platform.Web.Interop;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// Shared mapping for &lt;input&gt; and &lt;textarea&gt; based controls (Entry, Editor, SearchBar).
/// </summary>
public abstract class TextInputHandler<TVirtualView> : WebElementHandler<TVirtualView, WebElement>
	where TVirtualView : class, IView, ITextInput
{
	public static IPropertyMapper<TVirtualView, TextInputHandler<TVirtualView>> TextInputMapper =
		new PropertyMapper<TVirtualView, TextInputHandler<TVirtualView>>(WebElementHandler.ViewMapper)
		{
			[nameof(ITextInput.Text)] = MapText,
			[nameof(ITextStyle.TextColor)] = MapTextColor,
			[nameof(ITextStyle.Font)] = MapFont,
			[nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
			[nameof(IPlaceholder.Placeholder)] = MapPlaceholder,
			[nameof(IPlaceholder.PlaceholderColor)] = MapPlaceholderColor,
			[nameof(ITextInput.IsReadOnly)] = MapIsReadOnly,
			[nameof(ITextInput.MaxLength)] = MapMaxLength,
			[nameof(ITextInput.IsTextPredictionEnabled)] = MapTextPrediction,
			[nameof(ITextInput.IsSpellCheckEnabled)] = MapTextPrediction,
			[nameof(ITextInput.Keyboard)] = MapKeyboard,
			[nameof(ITextInput.CursorPosition)] = MapSelection,
			[nameof(ITextInput.SelectionLength)] = MapSelection,
			[nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
		};

	protected TextInputHandler(IPropertyMapper mapper) : base(mapper)
	{
	}

	bool _updatingSelection;

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(platformView.OnValue("input", OnInput));
		Track(platformView.On("select", OnSelectionChanged));
		Track(platformView.On("keyup", OnSelectionChanged));
		Track(platformView.On("pointerup", OnSelectionChanged));
	}

	protected virtual void OnInput(string value)
	{
		if (VirtualView is { } view && view.Text != value)
			view.Text = value;
	}

	void OnSelectionChanged()
	{
		if (VirtualView is not { } view || _updatingSelection)
			return;
		var selection = Dom.GetInputSelection(PlatformView.Js);
		var start = (int)selection[0];
		var length = (int)selection[1] - start;
		_updatingSelection = true;
		try
		{
			if (view.CursorPosition != start)
				view.CursorPosition = start;
			if (view.SelectionLength != length)
				view.SelectionLength = length;
		}
		finally
		{
			_updatingSelection = false;
		}
	}

	public static void MapText(TextInputHandler<TVirtualView> handler, TVirtualView input)
	{
		var text = input.Text ?? string.Empty;
		if (input.MaxLength >= 0 && text.Length > input.MaxLength)
			text = text[..input.MaxLength];
		if (handler.PlatformView.GetString("value") != text)
			handler.PlatformView.SetProperty("value", text);
	}

	public static void MapTextColor(TextInputHandler<TVirtualView> handler, TVirtualView input)
		=> handler.PlatformView.SetStyle("color", WebCss.Color(input.TextColor));

	public static void MapFont(TextInputHandler<TVirtualView> handler, TVirtualView input)
	{
		if (handler.MauiContext?.Services.GetService(typeof(IFontManager)) is WebFontManager fonts)
			fonts.Apply(handler.PlatformView, input.Font);
	}

	public static void MapCharacterSpacing(TextInputHandler<TVirtualView> handler, TVirtualView input)
		=> handler.PlatformView.SetStyle("letter-spacing", input.CharacterSpacing == 0 ? null : WebCss.Px(input.CharacterSpacing));

	public static void MapPlaceholder(TextInputHandler<TVirtualView> handler, TVirtualView input)
		=> handler.PlatformView.SetAttribute("placeholder", string.IsNullOrEmpty(input.Placeholder) ? null : input.Placeholder);

	public static void MapPlaceholderColor(TextInputHandler<TVirtualView> handler, TVirtualView input)
		=> handler.PlatformView.SetStyle("--maui-placeholder", WebCss.Color(input.PlaceholderColor));

	public static void MapIsReadOnly(TextInputHandler<TVirtualView> handler, TVirtualView input)
		=> handler.PlatformView.SetProperty("readOnly", input.IsReadOnly);

	public static void MapMaxLength(TextInputHandler<TVirtualView> handler, TVirtualView input)
	{
		handler.PlatformView.SetAttribute("maxlength", input.MaxLength >= 0 ? input.MaxLength.ToString() : null);
		MapText(handler, input);
	}

	public static void MapTextPrediction(TextInputHandler<TVirtualView> handler, TVirtualView input)
	{
		handler.PlatformView.SetAttribute("autocorrect", input.IsTextPredictionEnabled ? null : "off");
		handler.PlatformView.SetAttribute("autocomplete", input.IsTextPredictionEnabled ? null : "off");
		handler.PlatformView.SetProperty("spellcheck", input.IsSpellCheckEnabled);
	}

	public static void MapKeyboard(TextInputHandler<TVirtualView> handler, TVirtualView input)
	{
		var keyboard = input.Keyboard;
		handler.PlatformView.SetAttribute("inputmode",
			keyboard == Keyboard.Numeric ? "decimal"
			: keyboard == Keyboard.Telephone ? "tel"
			: keyboard == Keyboard.Email ? "email"
			: keyboard == Keyboard.Url ? "url"
			: null);
		handler.PlatformView.SetAttribute("autocapitalize",
			keyboard == Keyboard.Email || keyboard == Keyboard.Url ? "off" : null);
	}

	public static void MapSelection(TextInputHandler<TVirtualView> handler, TVirtualView input)
	{
		if (handler._updatingSelection || !handler.PlatformView.HasFocus)
			return;
		var start = Math.Max(0, input.CursorPosition);
		handler._updatingSelection = true;
		try
		{
			Dom.SetInputSelection(handler.PlatformView.Js, start, start + Math.Max(0, input.SelectionLength));
		}
		finally
		{
			handler._updatingSelection = false;
		}
	}

	public static void MapHorizontalTextAlignment(TextInputHandler<TVirtualView> handler, TVirtualView input)
		=> handler.PlatformView.SetStyle("text-align", input is ITextAlignment a ? WebCss.TextAlign(a.HorizontalTextAlignment) : null);
}

public class EntryHandler : TextInputHandler<IEntry>
{
	public static IPropertyMapper<IEntry, EntryHandler> Mapper =
		new PropertyMapper<IEntry, EntryHandler>(TextInputMapper)
		{
			[nameof(IEntry.IsPassword)] = MapIsPassword,
			[nameof(IEntry.ReturnType)] = MapReturnType,
		};

	public EntryHandler() : base(Mapper)
	{
	}

	protected override WebElement CreatePlatformView()
	{
		var input = new WebElement("input", "maui-entry");
		input.SetAttribute("type", "text");
		return input;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(platformView.OnKey("keydown", key =>
		{
			if (key == "Enter")
				CurrentView?.Completed();
		}));
	}

	public static void MapIsPassword(EntryHandler handler, IEntry entry)
		=> handler.PlatformView.SetAttribute("type", entry.IsPassword ? "password" : "text");

	public static void MapReturnType(EntryHandler handler, IEntry entry)
		=> handler.PlatformView.SetAttribute("enterkeyhint", EnterKeyHint(entry.ReturnType));

	internal static string? EnterKeyHint(ReturnType returnType) => returnType switch
	{
		ReturnType.Done => "done",
		ReturnType.Go => "go",
		ReturnType.Next => "next",
		ReturnType.Search => "search",
		ReturnType.Send => "send",
		_ => null,
	};
}

public class SearchBarHandler : TextInputHandler<ISearchBar>
{
	public static IPropertyMapper<ISearchBar, SearchBarHandler> Mapper =
		new PropertyMapper<ISearchBar, SearchBarHandler>(TextInputMapper)
		{
			[nameof(ISearchBar.ReturnType)] = MapReturnType,
		};

	public SearchBarHandler() : base(Mapper)
	{
	}

	protected override WebElement CreatePlatformView()
	{
		var input = new WebElement("input", "maui-search-bar");
		input.SetAttribute("type", "search");
		input.SetAttribute("enterkeyhint", "search");
		return input;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(platformView.OnKey("keydown", key =>
		{
			if (key == "Enter")
				CurrentView?.SearchButtonPressed();
		}));
	}

	public static void MapReturnType(SearchBarHandler handler, ISearchBar searchBar)
		=> handler.PlatformView.SetAttribute("enterkeyhint", EntryHandler.EnterKeyHint(searchBar.ReturnType) ?? "search");
}

public class EditorHandler : TextInputHandler<IEditor>
{
	public static IPropertyMapper<IEditor, EditorHandler> Mapper =
		new PropertyMapper<IEditor, EditorHandler>(TextInputMapper);

	public EditorHandler() : base(Mapper)
	{
	}

	protected override WebElement CreatePlatformView() => new("textarea", "maui-editor");

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(platformView.On("blur", () => CurrentView?.Completed()));
	}

	protected override void OnInput(string value)
	{
		base.OnInput(value);
		if (VirtualView is Microsoft.Maui.Controls.Editor { AutoSize: Microsoft.Maui.Controls.EditorAutoSizeOption.TextChanges } editor)
			editor.InvalidateMeasure();
	}

	protected override Graphics.Size MeasureNative(double widthConstraint, double heightConstraint)
	{
		var autoSize = VirtualView is Microsoft.Maui.Controls.Editor { AutoSize: Microsoft.Maui.Controls.EditorAutoSizeOption.TextChanges };
		var width = double.IsFinite(widthConstraint) ? widthConstraint : PlatformView.Measure(widthConstraint, heightConstraint).Width;
		var height = autoSize
			? Dom.MeasureScrollHeight(PlatformView.Js, width)
			: PlatformView.Measure(width, heightConstraint).Height;
		return new Graphics.Size(width, Math.Ceiling(height));
	}
}

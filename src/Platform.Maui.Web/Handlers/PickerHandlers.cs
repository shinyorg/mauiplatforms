using System.Globalization;
using Microsoft.Maui.Platform.Web.Interop;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>Shared text styling for the native form pickers.</summary>
public abstract class FormControlHandler<TVirtualView> : WebElementHandler<TVirtualView, WebElement>
	where TVirtualView : class, IView, ITextStyle
{
	public static IPropertyMapper<TVirtualView, FormControlHandler<TVirtualView>> TextStyleMapper =
		new PropertyMapper<TVirtualView, FormControlHandler<TVirtualView>>(WebElementHandler.ViewMapper)
		{
			[nameof(ITextStyle.TextColor)] = MapTextColor,
			[nameof(ITextStyle.Font)] = MapFont,
			[nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
		};

	protected FormControlHandler(IPropertyMapper mapper) : base(mapper)
	{
	}

	public static void MapTextColor(FormControlHandler<TVirtualView> handler, TVirtualView view)
		=> handler.PlatformView.SetStyle("color", WebCss.Color(view.TextColor));

	public static void MapFont(FormControlHandler<TVirtualView> handler, TVirtualView view)
	{
		if (handler.MauiContext?.Services.GetService(typeof(IFontManager)) is WebFontManager fonts)
			fonts.Apply(handler.PlatformView, view.Font);
	}

	public static void MapCharacterSpacing(FormControlHandler<TVirtualView> handler, TVirtualView view)
		=> handler.PlatformView.SetStyle("letter-spacing", view.CharacterSpacing == 0 ? null : WebCss.Px(view.CharacterSpacing));
}

public class PickerHandler : FormControlHandler<IPicker>
{
	public static IPropertyMapper<IPicker, PickerHandler> Mapper =
		new PropertyMapper<IPicker, PickerHandler>(TextStyleMapper)
		{
			[nameof(IPicker.Items)] = MapItems,
			[nameof(Microsoft.Maui.Controls.Picker.ItemsSource)] = MapItems,
			[nameof(Microsoft.Maui.Controls.Picker.ItemDisplayBinding)] = MapItems,
			[nameof(IPicker.Title)] = MapTitle,
			[nameof(IPicker.SelectedIndex)] = MapSelectedIndex,
			[nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
		};

	public PickerHandler() : base(Mapper)
	{
	}

	protected override WebElement CreatePlatformView() => new("select", "maui-picker");

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(platformView.OnValue("change", value =>
		{
			if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
				VirtualView.SelectedIndex = index;
		}));
		Track(platformView.On("focus", () => VirtualView.IsOpen = true));
		Track(platformView.On("blur", () => VirtualView.IsOpen = false));
	}

	public static void MapItems(PickerHandler handler, IPicker picker)
	{
		var items = new string[picker.GetCount()];
		for (var i = 0; i < items.Length; i++)
			items[i] = picker.GetItem(i) ?? string.Empty;
		Dom.SetSelectOptions(handler.PlatformView.Js, items, picker.Title);
		MapSelectedIndex(handler, picker);
	}

	// The title doubles as the accessible name when there's no description.
	public static void MapTitle(PickerHandler handler, IPicker picker)
	{
		MapItems(handler, picker);
		WebElementHandler.MapSemantics(handler, picker);
	}

	public static void MapSelectedIndex(PickerHandler handler, IPicker picker)
		=> Dom.SetSelectedOption(handler.PlatformView.Js, picker.SelectedIndex);

	public static void MapHorizontalTextAlignment(PickerHandler handler, IPicker picker)
		=> handler.PlatformView.SetStyle("text-align", WebCss.TextAlign(picker.HorizontalTextAlignment));
}

/// <summary>DatePicker as &lt;input type="date"&gt;. The browser formats the date for the user's locale.</summary>
public class DatePickerHandler : FormControlHandler<IDatePicker>
{
	public static IPropertyMapper<IDatePicker, DatePickerHandler> Mapper =
		new PropertyMapper<IDatePicker, DatePickerHandler>(TextStyleMapper)
		{
			[nameof(IDatePicker.Date)] = MapDate,
			[nameof(IDatePicker.MinimumDate)] = MapRange,
			[nameof(IDatePicker.MaximumDate)] = MapRange,
		};

	const string DateFormat = "yyyy-MM-dd";

	public DatePickerHandler() : base(Mapper)
	{
	}

	protected override WebElement CreatePlatformView()
	{
		var input = new WebElement("input", "maui-date-picker");
		input.SetAttribute("type", "date");
		return input;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(platformView.OnValue("change", value =>
			VirtualView.Date = DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
				? date
				: null));
	}

	public static void MapDate(DatePickerHandler handler, IDatePicker picker)
		=> handler.PlatformView.SetProperty("value", picker.Date?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? string.Empty);

	public static void MapRange(DatePickerHandler handler, IDatePicker picker)
	{
		handler.PlatformView.SetAttribute("min", picker.MinimumDate?.ToString(DateFormat, CultureInfo.InvariantCulture));
		handler.PlatformView.SetAttribute("max", picker.MaximumDate?.ToString(DateFormat, CultureInfo.InvariantCulture));
	}
}

/// <summary>TimePicker as &lt;input type="time"&gt;.</summary>
public class TimePickerHandler : FormControlHandler<ITimePicker>
{
	public static IPropertyMapper<ITimePicker, TimePickerHandler> Mapper =
		new PropertyMapper<ITimePicker, TimePickerHandler>(TextStyleMapper)
		{
			[nameof(ITimePicker.Time)] = MapTime,
		};

	public TimePickerHandler() : base(Mapper)
	{
	}

	protected override WebElement CreatePlatformView()
	{
		var input = new WebElement("input", "maui-time-picker");
		input.SetAttribute("type", "time");
		return input;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(platformView.OnValue("change", value =>
			VirtualView.Time = TimeSpan.TryParseExact(value, ["hh\\:mm", "hh\\:mm\\:ss"], CultureInfo.InvariantCulture, out var time)
				? time
				: null));
	}

	public static void MapTime(TimePickerHandler handler, ITimePicker picker)
		=> handler.PlatformView.SetProperty("value", picker.Time?.ToString("hh\\:mm", CultureInfo.InvariantCulture) ?? string.Empty);
}

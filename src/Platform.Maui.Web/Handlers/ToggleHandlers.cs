using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

public class CheckBoxHandler : WebElementHandler<ICheckBox, WebElement>
{
	public static IPropertyMapper<ICheckBox, CheckBoxHandler> Mapper =
		new PropertyMapper<ICheckBox, CheckBoxHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(ICheckBox.IsChecked)] = MapIsChecked,
			[nameof(ICheckBox.Foreground)] = MapForeground,
			// Controls raises only its own property name when CheckBox.Color changes.
			[nameof(Microsoft.Maui.Controls.CheckBox.Color)] = MapForeground,
		};

	public CheckBoxHandler() : base(Mapper)
	{
	}

	protected override WebElement CreatePlatformView()
	{
		var input = new WebElement("input", "maui-check-box");
		input.SetAttribute("type", "checkbox");
		return input;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(platformView.OnValue("change", value => VirtualView.IsChecked = value == "true"));
	}

	// MAUI's default CheckBox is 32x32 on touch platforms; keep the box itself compact.
	protected override Size MeasureNative(double widthConstraint, double heightConstraint) => new(24, 24);

	public static void MapIsChecked(CheckBoxHandler handler, ICheckBox checkBox)
		=> handler.PlatformView.SetProperty("checked", checkBox.IsChecked);

	public static void MapForeground(CheckBoxHandler handler, ICheckBox checkBox)
		=> handler.PlatformView.SetStyle("--maui-check", WebCss.PaintColor(checkBox.Foreground));
}

/// <summary>Switch as a styled checkbox with role="switch" for assistive technology.</summary>
public class SwitchHandler : WebElementHandler<ISwitch, WebElement>, IWebSemanticTarget
{
	public static IPropertyMapper<ISwitch, SwitchHandler> Mapper =
		new PropertyMapper<ISwitch, SwitchHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(ISwitch.IsOn)] = MapIsOn,
			[nameof(ISwitch.TrackColor)] = MapTrackColor,
			[nameof(ISwitch.ThumbColor)] = MapThumbColor,
			[nameof(IView.IsEnabled)] = MapIsEnabled,
		};

	public SwitchHandler() : base(Mapper)
	{
	}

	WebElement _input = null!;

	WebElement IWebSemanticTarget.SemanticElement => _input;

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("label", "maui-switch");
		_input = new WebElement("input");
		_input.SetAttribute("type", "checkbox");
		_input.SetAttribute("role", "switch");
		root.Append(_input);
		root.Append(new WebElement("span", "track"));
		root.Append(new WebElement("span", "thumb"));
		return root;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(_input.OnValue("change", value => VirtualView.IsOn = value == "true"));
	}

	protected override Size MeasureNative(double widthConstraint, double heightConstraint) => new(46, 28);

	public static void MapIsOn(SwitchHandler handler, ISwitch view)
	{
		handler._input.SetProperty("checked", view.IsOn);
		handler.PlatformView.ToggleClass("on", view.IsOn);
	}

	public static void MapTrackColor(SwitchHandler handler, ISwitch view)
		=> handler.PlatformView.SetStyle("--maui-switch-on", WebCss.Color(view.TrackColor));

	public static void MapThumbColor(SwitchHandler handler, ISwitch view)
		=> handler.PlatformView.SetStyle("--maui-switch-thumb", WebCss.Color(view.ThumbColor));

	public static void MapIsEnabled(SwitchHandler handler, ISwitch view)
	{
		WebElementHandler.MapIsEnabled(handler, view);
		handler._input.SetProperty("disabled", !view.IsEnabled);
		handler.PlatformView.SetStyle("opacity", view.IsEnabled ? null : "0.45");
	}
}

/// <summary>
/// RadioButton as a native radio input with text. When MAUI applies a ControlTemplate (view content
/// or an explicit template), the presented template content is rendered instead.
/// </summary>
public class RadioButtonHandler : WebElementHandler<IRadioButton, WebElement>, IWebSemanticTarget
{
	public static IPropertyMapper<IRadioButton, RadioButtonHandler> Mapper =
		new PropertyMapper<IRadioButton, RadioButtonHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IRadioButton.IsChecked)] = MapIsChecked,
			[nameof(IContentView.Content)] = MapContent,
			[nameof(Microsoft.Maui.Controls.TemplatedView.ControlTemplate)] = MapContent,
			[nameof(ITextStyle.TextColor)] = MapTextColor,
			[nameof(ITextStyle.Font)] = MapFont,
			[nameof(Microsoft.Maui.Controls.RadioButton.GroupName)] = MapGroupName,
			[nameof(IView.IsEnabled)] = MapIsEnabled,
		};

	public RadioButtonHandler() : base(Mapper)
	{
	}

	WebElement _input = null!;
	WebElement _text = null!;

	WebElement IWebSemanticTarget.SemanticElement => _input;
	WebElement? _templated;

	IContentView? Templated => VirtualView is IContentView { PresentedContent: not null } content ? content : null;

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("label", "maui-radio");
		_input = new WebElement("input");
		_input.SetAttribute("type", "radio");
		_text = new WebElement("span");
		root.Append(_input);
		root.Append(_text);
		return root;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(_input.OnValue("change", value =>
		{
			if (value == "true")
				VirtualView.IsChecked = true;
		}));
	}

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
		=> Templated is { } templated
			? templated.CrossPlatformMeasure(widthConstraint, heightConstraint)
			: base.GetDesiredSize(widthConstraint, heightConstraint);

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);
		if (Templated is { } templated)
		{
			// Arrange from a measure at the final size (see LayoutHandler.PlatformArrange).
			templated.CrossPlatformMeasure(frame.Width, frame.Height);
			templated.CrossPlatformArrange(new Rect(Point.Zero, frame.Size));
		}
	}

	public static void MapIsChecked(RadioButtonHandler handler, IRadioButton radio)
		=> handler._input.SetProperty("checked", radio.IsChecked);

	public static void MapContent(RadioButtonHandler handler, IRadioButton radio)
	{
		var templated = handler.Templated;
		handler._input.ToggleClass("maui-collapsed", templated is not null);
		handler._text.ToggleClass("maui-collapsed", templated is not null);
		handler._templated?.Detach();
		handler._templated = templated is null ? null : handler.ToPlatform(templated.PresentedContent!);
		if (handler._templated is not null)
			handler.PlatformView.Append(handler._templated);
		else
			handler._text.SetText((radio as Microsoft.Maui.Controls.RadioButton)?.ContentAsString() ?? radio.Content?.ToString());
	}

	public static void MapTextColor(RadioButtonHandler handler, IRadioButton radio)
		=> handler.PlatformView.SetStyle("color", WebCss.Color((radio as ITextStyle)?.TextColor));

	public static void MapFont(RadioButtonHandler handler, IRadioButton radio)
	{
		if (radio is ITextStyle style && handler.MauiContext?.Services.GetService(typeof(IFontManager)) is WebFontManager fonts)
			fonts.Apply(handler.PlatformView, style.Font);
	}

	// MAUI coordinates group exclusivity itself; the name only groups native keyboard navigation.
	public static void MapGroupName(RadioButtonHandler handler, IRadioButton radio)
		=> handler._input.SetAttribute("name", (radio as Microsoft.Maui.Controls.RadioButton)?.GroupName);

	public static void MapIsEnabled(RadioButtonHandler handler, IRadioButton radio)
	{
		WebElementHandler.MapIsEnabled(handler, radio);
		handler._input.SetProperty("disabled", !radio.IsEnabled);
		handler.PlatformView.SetStyle("opacity", radio.IsEnabled ? null : "0.45");
	}
}

public class SliderHandler : WebElementHandler<ISlider, WebElement>
{
	public static IPropertyMapper<ISlider, SliderHandler> Mapper =
		new PropertyMapper<ISlider, SliderHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IRange.Minimum)] = MapRange,
			[nameof(IRange.Maximum)] = MapRange,
			[nameof(IRange.Value)] = MapRange,
			[nameof(ISlider.MinimumTrackColor)] = MapMinimumTrackColor,
			[nameof(ISlider.ThumbColor)] = MapThumbColor,
		};

	public SliderHandler() : base(Mapper)
	{
	}

	protected override WebElement CreatePlatformView()
	{
		var input = new WebElement("input", "maui-slider");
		input.SetAttribute("type", "range");
		input.SetAttribute("step", "any");
		return input;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(platformView.OnValue("input", value =>
		{
			if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v))
				VirtualView.Value = v;
		}));
		Track(platformView.On("pointerdown", () => VirtualView.DragStarted()));
		Track(platformView.On("pointerup", () => VirtualView.DragCompleted()));
	}

	protected override Size MeasureNative(double widthConstraint, double heightConstraint)
		=> new(double.IsFinite(widthConstraint) ? Math.Min(widthConstraint, 200) : 200, 28);

	public static void MapRange(SliderHandler handler, ISlider slider)
	{
		var el = handler.PlatformView;
		el.SetProperty("min", slider.Minimum.ToString(System.Globalization.CultureInfo.InvariantCulture));
		el.SetProperty("max", slider.Maximum.ToString(System.Globalization.CultureInfo.InvariantCulture));
		el.SetProperty("value", slider.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
	}

	public static void MapMinimumTrackColor(SliderHandler handler, ISlider slider)
		=> handler.PlatformView.SetStyle("--maui-slider-min", WebCss.Color(slider.MinimumTrackColor));

	public static void MapThumbColor(SliderHandler handler, ISlider slider)
	{
		if (slider.MinimumTrackColor is null)
			handler.PlatformView.SetStyle("--maui-slider-min", WebCss.Color(slider.ThumbColor));
	}
}

public class StepperHandler : WebElementHandler<IStepper, WebElement>
{
	public static IPropertyMapper<IStepper, StepperHandler> Mapper =
		new PropertyMapper<IStepper, StepperHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IRange.Minimum)] = MapState,
			[nameof(IRange.Maximum)] = MapState,
			[nameof(IRange.Value)] = MapState,
			[nameof(IView.IsEnabled)] = MapState,
		};

	public StepperHandler() : base(Mapper)
	{
	}

	WebElement _minus = null!;
	WebElement _plus = null!;

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-stepper");
		_minus = new WebElement("button");
		_minus.SetAttribute("type", "button");
		_minus.SetAttribute("aria-label", "Decrease");
		_minus.SetText("−");
		_plus = new WebElement("button");
		_plus.SetAttribute("type", "button");
		_plus.SetAttribute("aria-label", "Increase");
		_plus.SetText("+");
		root.Append(_minus);
		root.Append(_plus);
		return root;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(_minus.On("click", () => Step(-1)));
		Track(_plus.On("click", () => Step(1)));
	}

	void Step(int direction)
	{
		var view = VirtualView;
		view.Value = Math.Clamp(view.Value + direction * view.Interval, view.Minimum, view.Maximum);
	}

	protected override Size MeasureNative(double widthConstraint, double heightConstraint) => new(90, 34);

	public static void MapState(StepperHandler handler, IStepper stepper)
	{
		handler._minus.SetProperty("disabled", !stepper.IsEnabled || stepper.Value <= stepper.Minimum);
		handler._plus.SetProperty("disabled", !stepper.IsEnabled || stepper.Value >= stepper.Maximum);
	}
}

public class ProgressBarHandler : WebElementHandler<IProgress, WebElement>
{
	public static IPropertyMapper<IProgress, ProgressBarHandler> Mapper =
		new PropertyMapper<IProgress, ProgressBarHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IProgress.Progress)] = MapProgress,
			[nameof(IProgress.ProgressColor)] = MapProgressColor,
		};

	public ProgressBarHandler() : base(Mapper)
	{
	}

	protected override WebElement CreatePlatformView()
	{
		var progress = new WebElement("progress", "maui-progress");
		progress.SetProperty("max", 1d);
		return progress;
	}

	protected override Size MeasureNative(double widthConstraint, double heightConstraint)
		=> new(double.IsFinite(widthConstraint) ? Math.Min(widthConstraint, 200) : 200, 6);

	public static void MapProgress(ProgressBarHandler handler, IProgress progress)
		=> handler.PlatformView.SetProperty("value", Math.Clamp(progress.Progress, 0, 1));

	public static void MapProgressColor(ProgressBarHandler handler, IProgress progress)
		=> handler.PlatformView.SetStyle("--maui-progress", WebCss.Color(progress.ProgressColor));
}

public class ActivityIndicatorHandler : WebElementHandler<IActivityIndicator, WebElement>
{
	public static IPropertyMapper<IActivityIndicator, ActivityIndicatorHandler> Mapper =
		new PropertyMapper<IActivityIndicator, ActivityIndicatorHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IActivityIndicator.IsRunning)] = MapIsRunning,
			[nameof(IActivityIndicator.Color)] = MapColor,
		};

	public ActivityIndicatorHandler() : base(Mapper)
	{
	}

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-activity-indicator");
		root.SetAttribute("role", "progressbar");
		root.Append(new WebElement("div"));
		return root;
	}

	protected override Size MeasureNative(double widthConstraint, double heightConstraint) => new(32, 32);

	public static void MapIsRunning(ActivityIndicatorHandler handler, IActivityIndicator indicator)
	{
		handler.PlatformView.ToggleClass("running", indicator.IsRunning);
		handler.PlatformView.SetAttribute("aria-busy", indicator.IsRunning ? "true" : "false");
	}

	public static void MapColor(ActivityIndicatorHandler handler, IActivityIndicator indicator)
		=> handler.PlatformView.SetStyle("--maui-spinner", WebCss.Color(indicator.Color));
}

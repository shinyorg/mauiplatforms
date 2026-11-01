using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// FlyoutPage: side-by-side when split, otherwise the flyout slides over the detail with a
/// dismissing backdrop. Split mode is FlyoutPage's own decision (ShouldShowSplitMode): it also
/// decides when IsPresented may change, and disagreeing with it makes IsPresented throw. On a
/// desktop idiom Default is always split, as on WinUI; set FlyoutLayoutBehavior to Split or
/// Popover from the window width for a responsive layout.
/// </summary>
public class FlyoutPageHandler : WebElementHandler<IFlyoutView, WebElement>
{
	/// <summary>For IFlyoutView implementations other than FlyoutPage: split at least this wide.</summary>
	public static double SplitThreshold { get; set; } = 900;

	const double DefaultFlyoutWidth = 300;

	public static IPropertyMapper<IFlyoutView, FlyoutPageHandler> Mapper =
		new PropertyMapper<IFlyoutView, FlyoutPageHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IFlyoutView.Flyout)] = MapFlyout,
			[nameof(IFlyoutView.Detail)] = MapDetail,
			[nameof(IFlyoutView.IsPresented)] = MapIsPresented,
			[nameof(FlyoutPage.FlyoutLayoutBehavior)] = MapIsPresented,
			[nameof(IFlyoutView.FlyoutWidth)] = MapIsPresented,
		};

	public FlyoutPageHandler() : base(Mapper)
	{
	}

	WebElement _pane = null!;
	WebElement _backdrop = null!;
	WebElement _detailHost = null!;
	WebElement? _flyout;
	WebElement? _detail;
	bool _split;

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-page maui-flyout-page");
		_detailHost = new WebElement("div", "maui-view");
		_backdrop = new WebElement("div", "maui-flyout-backdrop");
		_pane = new WebElement("div", "maui-view maui-flyout-pane");
		root.Append(_detailHost);
		root.Append(_backdrop);
		root.Append(_pane);
		return root;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(_backdrop.On("click", () => VirtualView.IsPresented = false));
		// Until the first arrange decides split mode, the flyout can be toggled.
		if (VirtualView is IFlyoutPageController controller)
			controller.CanChangeIsPresented = true;
	}

	/// <summary>Whether the given FlyoutPage currently shows its flyout beside the detail.</summary>
	public static bool IsSplit(FlyoutPage page) => page.Handler is FlyoutPageHandler { _split: true };

	bool ComputeSplit(Size size)
		=> VirtualView is IFlyoutPageController controller
			? controller.ShouldShowSplitMode
			: size.Width >= SplitThreshold;

	double FlyoutWidth(double available)
	{
		var width = VirtualView.FlyoutWidth > 0 ? VirtualView.FlyoutWidth : DefaultFlyoutWidth;
		return Math.Min(width, available * 0.85);
	}

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
		=> new(double.IsFinite(widthConstraint) ? widthConstraint : 0, double.IsFinite(heightConstraint) ? heightConstraint : 0);

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);

		var split = ComputeSplit(frame.Size);
		var controller = VirtualView as IFlyoutPageController;
		if (split != _split)
		{
			_split = split;
			// MAUI's semantics: a split flyout is presented and can't be dismissed.
			if (split && controller is not null && !VirtualView.IsPresented)
			{
				controller.CanChangeIsPresented = true;
				VirtualView.IsPresented = true;
			}
			UpdatePresentation();
			// The detail's navigation bar shows a toggle only when the flyout can be dismissed.
			if (VirtualView.Detail?.Handler is NavigationPageHandler navigation)
				navigation.RefreshBar();
		}

		// FlyoutPage recomputes CanChangeIsPresented from the device idiom (OnAppearing,
		// UpdateFlyoutLayoutBehavior) and then rejects any IsPresented change; this handler knows
		// whether the flyout is actually docked, so keep re-asserting that.
		if (controller is not null && controller.CanChangeIsPresented == split)
			controller.CanChangeIsPresented = !split;

		var flyoutWidth = FlyoutWidth(frame.Width);
		var detailX = _split ? flyoutWidth : 0;
		_pane.SetFrame(0, 0, flyoutWidth, frame.Height);
		_detailHost.SetFrame(detailX, 0, frame.Width - detailX, frame.Height);

		if (VirtualView.Flyout is { } flyout)
		{
			flyout.Measure(flyoutWidth, frame.Height);
			flyout.Arrange(new Rect(0, 0, flyoutWidth, frame.Height));
		}
		if (VirtualView.Detail is { } detail)
		{
			detail.Measure(frame.Width - detailX, frame.Height);
			detail.Arrange(new Rect(0, 0, frame.Width - detailX, frame.Height));
		}
	}

	void UpdatePresentation()
	{
		var open = _split || VirtualView.IsPresented;
		_pane.ToggleClass("overlay", !_split);
		_pane.ToggleClass("closed", !open);
		_pane.SetAttribute("aria-hidden", open ? null : "true");
		_backdrop.SetProperty("hidden", _split || !VirtualView.IsPresented);
	}

	public static void MapFlyout(FlyoutPageHandler handler, IFlyoutView view)
	{
		handler._flyout?.Detach();
		handler._flyout = view.Flyout is null ? null : handler.ToPlatform(view.Flyout);
		if (handler._flyout is not null)
			handler._pane.Append(handler._flyout);
		WebLayoutScheduler.Request();
	}

	public static void MapDetail(FlyoutPageHandler handler, IFlyoutView view)
	{
		handler._detail?.Detach();
		handler._detail = view.Detail is null ? null : handler.ToPlatform(view.Detail);
		if (handler._detail is not null)
			handler._detailHost.Append(handler._detail);
		WebLayoutScheduler.Request();
	}

	public static void MapIsPresented(FlyoutPageHandler handler, IFlyoutView view)
	{
		handler.UpdatePresentation();
		WebLayoutScheduler.Request();
	}
}

using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Platform.Web;
using Controls = Microsoft.Maui.Controls;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>Implemented by every web handler so shared mappers can reach the DOM element.</summary>
public interface IWebElementHandler : IViewHandler
{
	WebElement? Element { get; }

	/// <summary>Discards cached native measurements after a change that affects natural size.</summary>
	void InvalidateNativeMeasure();
}

/// <summary>Mappers shared by all web handlers for the common <see cref="IView"/> properties.</summary>
public static class WebElementHandler
{
	/// <summary>Bumped when something global (fonts, theme) can change every element's natural size.</summary>
	public static int MeasureGeneration { get; private set; }

	public static void InvalidateAllMeasurements()
	{
		MeasureGeneration++;
		WebLayoutScheduler.Request();
	}

	public static IPropertyMapper<IView, IViewHandler> ViewMapper = new PropertyMapper<IView, IViewHandler>(ViewHandler.ViewMapper)
	{
		[nameof(IView.Background)] = MapBackground,
		[nameof(IView.Opacity)] = MapOpacity,
		[nameof(IView.Visibility)] = MapVisibility,
		[nameof(IView.IsEnabled)] = MapIsEnabled,
		[nameof(IView.Semantics)] = MapSemantics,
		// Controls replaces these with empty mappings on platforms it doesn't know; they all feed ARIA here.
		[Controls.AutomationProperties.IsInAccessibleTreeProperty.PropertyName] = WebSemantics.MapAutomationProperty,
		[Controls.AutomationProperties.ExcludedWithChildrenProperty.PropertyName] = WebSemantics.MapAutomationProperty,
		["Name"] = WebSemantics.MapAutomationProperty,
		["HelpText"] = WebSemantics.MapAutomationProperty,
		["LabeledBy"] = WebSemantics.MapAutomationProperty,
		[nameof(IView.AutomationId)] = MapAutomationId,
		[nameof(IView.Shadow)] = MapShadow,
		[nameof(IView.InputTransparent)] = MapInputTransparent,
		[nameof(IView.Clip)] = MapClip,
		[nameof(IView.FlowDirection)] = MapFlowDirection,
		[nameof(IView.ZIndex)] = MapZIndex,
		[nameof(IView.TranslationX)] = MapTransform,
		[nameof(IView.TranslationY)] = MapTransform,
		[nameof(IView.Scale)] = MapTransform,
		[nameof(IView.ScaleX)] = MapTransform,
		[nameof(IView.ScaleY)] = MapTransform,
		[nameof(IView.Rotation)] = MapTransform,
		[nameof(IView.RotationX)] = MapTransform,
		[nameof(IView.RotationY)] = MapTransform,
		[nameof(IView.AnchorX)] = MapAnchor,
		[nameof(IView.AnchorY)] = MapAnchor,
		[nameof(IToolTipElement.ToolTip)] = MapToolTip,
		// Sizes are resolved by the cross-platform layout; nothing to push to the DOM.
		[nameof(IView.Width)] = MapSize,
		[nameof(IView.Height)] = MapSize,
		[nameof(IView.MinimumWidth)] = MapSize,
		[nameof(IView.MinimumHeight)] = MapSize,
		[nameof(IView.MaximumWidth)] = MapSize,
		[nameof(IView.MaximumHeight)] = MapSize,
	};

	public static CommandMapper<IView, IViewHandler> ViewCommandMapper = new(ViewHandler.ViewCommandMapper)
	{
		[nameof(IView.InvalidateMeasure)] = MapInvalidateMeasure,
		[nameof(IView.Focus)] = MapFocus,
		[nameof(IView.Unfocus)] = MapUnfocus,
		[nameof(IView.ZIndex)] = (h, v, _) => MapZIndex(h, v),
	};

	static WebElement? El(IViewHandler handler) => (handler as IWebElementHandler)?.Element;

	public static void MapBackground(IViewHandler handler, IView view)
	{
		if (El(handler) is not { } el)
			return;
		var css = WebCss.Paint(view.Background);
		var gradient = WebCss.IsGradient(view.Background);
		el.SetStyle("background-color", gradient ? null : css);
		el.SetStyle("background-image", gradient ? css : null);
	}

	public static void MapOpacity(IViewHandler handler, IView view)
		=> El(handler)?.SetStyle("opacity", view.Opacity >= 1 ? null : WebCss.Num(Math.Max(0, view.Opacity)));

	public static void MapVisibility(IViewHandler handler, IView view)
	{
		if (El(handler) is not { } el)
			return;
		el.ToggleClass("maui-hidden", view.Visibility == Visibility.Hidden);
		el.ToggleClass("maui-collapsed", view.Visibility == Visibility.Collapsed);
	}

	public static void MapIsEnabled(IViewHandler handler, IView view)
	{
		if (El(handler) is not { } el)
			return;
		el.SetAttribute("aria-disabled", view.IsEnabled ? null : "true");
		el.SetProperty("disabled", !view.IsEnabled);
	}

	public static void MapSemantics(IViewHandler handler, IView view)
	{
		if (El(handler) is { } el)
			WebSemantics.Apply(handler, el, view);
	}

	public static void MapAutomationId(IViewHandler handler, IView view)
		=> El(handler)?.SetAttribute("data-automation-id", string.IsNullOrEmpty(view.AutomationId) ? null : view.AutomationId);

	public static void MapShadow(IViewHandler handler, IView view)
		=> El(handler)?.SetStyle("box-shadow", WebCss.Shadow(view.Shadow));

	public static void MapInputTransparent(IViewHandler handler, IView view)
		=> El(handler)?.SetStyle("pointer-events", view.InputTransparent ? "none" : null);

	public static void MapClip(IViewHandler handler, IView view)
	{
		if (El(handler) is not { } el)
			return;
		var frame = view.Frame;
		el.SetStyle("clip-path", WebCss.Clip(view.Clip, frame.Size));
	}

	public static void MapFlowDirection(IViewHandler handler, IView view)
		=> El(handler)?.SetAttribute("dir", view.FlowDirection switch
		{
			FlowDirection.RightToLeft => "rtl",
			FlowDirection.LeftToRight => "ltr",
			_ => null,
		});

	public static void MapZIndex(IViewHandler handler, IView view)
		=> El(handler)?.SetStyle("z-index", view.ZIndex == 0 ? null : view.ZIndex.ToString());

	public static void MapTransform(IViewHandler handler, IView view)
	{
		if (El(handler) is not { } el)
			return;
		var transform = WebCss.Transform(view);
		el.SetStyle("transform", transform.Length == 0 ? null : transform);
	}

	public static void MapAnchor(IViewHandler handler, IView view)
		=> El(handler)?.SetStyle("transform-origin",
			view.AnchorX == 0.5 && view.AnchorY == 0.5 ? null : $"{WebCss.Num(view.AnchorX * 100)}% {WebCss.Num(view.AnchorY * 100)}%");

	public static void MapToolTip(IViewHandler handler, IView view)
		=> El(handler)?.SetAttribute("title", (view as IToolTipElement)?.ToolTip?.Content?.ToString());

	static void MapSize(IViewHandler handler, IView view) => (handler as IWebElementHandler)?.InvalidateNativeMeasure();

	public static void MapInvalidateMeasure(IViewHandler handler, IView view, object? args)
	{
		(handler as IWebElementHandler)?.InvalidateNativeMeasure();
		WebLayoutScheduler.Request();
	}

	public static void MapFocus(IViewHandler handler, IView view, object? args)
	{
		var focused = El(handler)?.Focus() ?? false;
		if (args is FocusRequest request)
			request.TrySetResult(focused);
	}

	public static void MapUnfocus(IViewHandler handler, IView view, object? args) => El(handler)?.Blur();
}

/// <summary>
/// Base class for handlers whose platform view is a DOM element. MAUI's cross-platform layout
/// computes every frame; this class measures native content and applies frames as absolute positions.
/// </summary>
public abstract class WebElementHandler<TVirtualView, TPlatformView> : ViewHandler<TVirtualView, TPlatformView>, IWebElementHandler
	where TVirtualView : class, IView
	where TPlatformView : WebElement
{
	readonly List<IDisposable> _subscriptions = new();

	/// <summary>
	/// The virtual view, or null once the handler has been disconnected. Use this in deferred work
	/// (queued callbacks, image loads): the typed <c>VirtualView</c> throws instead of returning null.
	/// </summary>
	protected TVirtualView? CurrentView => ((IElementHandler)this).VirtualView as TVirtualView;
	readonly (double W, double H, Size Size)[] _measureCache = new (double, double, Size)[2];
	int _measureCacheCount;
	int _measureCacheGeneration = -1;
	WebGestureManager? _gestures;

	protected WebElementHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null)
		: base(mapper, commandMapper ?? WebElementHandler.ViewCommandMapper)
	{
	}

	public WebElement? Element => ((IElementHandler)this).PlatformView as WebElement;

	/// <summary>The window this view is displayed in, when known.</summary>
	protected WebWindow? PlatformWindow => MauiContext?.Services.GetService(typeof(WebWindow)) as WebWindow;

	/// <summary>Tracks a DOM subscription so it is released when the handler disconnects.</summary>
	protected T Track<T>(T subscription) where T : IDisposable
	{
		_subscriptions.Add(subscription);
		return subscription;
	}

	public override void SetVirtualView(IView view)
	{
		InvalidateNativeMeasure();
		base.SetVirtualView(view);
	}

	public override void UpdateValue(string property)
	{
		InvalidateNativeMeasure();
		base.UpdateValue(property);
	}

	public void InvalidateNativeMeasure() => _measureCacheCount = 0;

	protected override void ConnectHandler(TPlatformView platformView)
	{
		base.ConnectHandler(platformView);
		platformView.AddClass("maui-view");

		if (VirtualView is Controls.VisualElement)
		{
			Track(platformView.On("pointerenter", OnPointerEnter));
			Track(platformView.On("pointerleave", OnPointerLeave));
			Track(platformView.On("focusin", OnFocusIn));
			Track(platformView.On("focusout", OnFocusOut));
		}

		if (VirtualView is Controls.View view)
			_gestures = new WebGestureManager(view, platformView);
	}

	protected override void DisconnectHandler(TPlatformView platformView)
	{
		_gestures?.Dispose();
		_gestures = null;
		foreach (var subscription in _subscriptions)
			subscription.Dispose();
		_subscriptions.Clear();
		base.DisconnectHandler(platformView);
	}

	void OnPointerEnter()
	{
		if (VirtualView is Controls.VisualElement { IsEnabled: true } ve)
			Controls.VisualStateManager.GoToState(ve, Controls.VisualStateManager.CommonStates.PointerOver);
	}

	void OnPointerLeave()
	{
		if (VirtualView is Controls.VisualElement ve)
		{
			Controls.VisualStateManager.GoToState(ve,
				!ve.IsEnabled ? Controls.VisualStateManager.CommonStates.Disabled
				: ve.IsFocused ? Controls.VisualStateManager.CommonStates.Focused
				: Controls.VisualStateManager.CommonStates.Normal);
		}
	}

	void OnFocusIn()
	{
		if (VirtualView is { IsFocused: false } view)
			view.IsFocused = true;
	}

	void OnFocusOut()
	{
		// focusout bubbles from descendants; only clear when focus actually left this view.
		if (VirtualView is { IsFocused: true } view && Element is { HasFocus: false })
			view.IsFocused = false;
	}

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
	{
		var view = VirtualView;
		if (view is null || Element is null)
			return Size.Zero;

		// An explicit size becomes the constraint so wrapping content measures against it.
		var wc = Primitives.Dimension.IsExplicitSet(view.Width) ? Math.Min(view.Width, widthConstraint) : widthConstraint;
		var hc = Primitives.Dimension.IsExplicitSet(view.Height) ? Math.Min(view.Height, heightConstraint) : heightConstraint;
		wc = Math.Min(wc, view.MaximumWidth);
		hc = Math.Min(hc, view.MaximumHeight);

		var native = MeasureCached(wc, hc);

		return new Size(
			LayoutManager.ResolveConstraints(widthConstraint, view.Width, native.Width, view.MinimumWidth, view.MaximumWidth),
			LayoutManager.ResolveConstraints(heightConstraint, view.Height, native.Height, view.MinimumHeight, view.MaximumHeight));
	}

	Size MeasureCached(double widthConstraint, double heightConstraint)
	{
		if (_measureCacheGeneration != WebElementHandler.MeasureGeneration)
		{
			_measureCacheGeneration = WebElementHandler.MeasureGeneration;
			_measureCacheCount = 0;
		}

		for (var i = 0; i < _measureCacheCount; i++)
		{
			var entry = _measureCache[i];
			if (entry.W.Equals(widthConstraint) && entry.H.Equals(heightConstraint))
				return entry.Size;
		}

		var size = MeasureNative(widthConstraint, heightConstraint);
		_measureCache[1] = _measureCache[0];
		_measureCache[0] = (widthConstraint, heightConstraint, size);
		_measureCacheCount = Math.Min(_measureCacheCount + 1, _measureCache.Length);
		return size;
	}

	/// <summary>Measures the element's natural size. Override for controls with a fixed or computed size.</summary>
	protected virtual Size MeasureNative(double widthConstraint, double heightConstraint)
		=> PlatformView.Measure(widthConstraint, heightConstraint);

	public override void PlatformArrange(Rect frame)
	{
		var resized = frame.Size != ArrangedFrame.Size;
		ArrangedFrame = frame;
		PlatformView.SetFrame(frame);
		if (resized && CurrentView?.Clip is not null)
			WebElementHandler.MapClip(this, VirtualView);
	}

	/// <summary>The last frame applied by <see cref="PlatformArrange"/>.</summary>
	protected Rect ArrangedFrame { get; private set; }

	/// <summary>Converts a child view to its DOM element.</summary>
	protected WebElement ToPlatform(IElement view)
	{
		_ = MauiContext ?? throw new InvalidOperationException("MauiContext not set.");
		return (WebElement)Microsoft.Maui.Platform.ElementExtensions.ToPlatform(view, MauiContext);
	}
}

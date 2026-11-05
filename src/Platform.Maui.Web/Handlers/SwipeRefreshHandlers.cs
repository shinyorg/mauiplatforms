using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// RefreshView: pull down on the content (mouse, pen or touch) while it is scrolled to the top to
/// refresh. Releasing past the threshold sets IsRefreshing, which runs the Command; the content
/// stays lowered with a spinner until IsRefreshing is cleared.
/// </summary>
public class RefreshViewHandler : ContentViewHandler
{
	public static new IPropertyMapper<IContentView, RefreshViewHandler> Mapper =
		new PropertyMapper<IContentView, RefreshViewHandler>(ContentViewHandler.Mapper)
		{
			[nameof(IContentView.Content)] = MapRefreshContent,
			[nameof(IRefreshView.IsRefreshing)] = MapIsRefreshing,
			[nameof(IRefreshView.RefreshColor)] = MapRefreshColor,
		};

	/// <summary>How far (px) the content must be pulled before releasing refreshes.</summary>
	public const double PullThreshold = 64;
	const double RestOffset = 56;
	const double MaxPull = 110;
	const string Snap = "translate 200ms ease";

	public RefreshViewHandler() : base(Mapper)
	{
	}

	WebElement _spinner = null!;
	WebElement? _content;
	IDisposable? _drag;
	bool _pulling;

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-refresh-view");
		_spinner = new WebElement("div", "maui-activity-indicator maui-refresh-spinner");
		_spinner.Append(new WebElement("div"));
		root.Append(_spinner);
		return root;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		// On the root, not the content: the pull starts wherever the pointer lands inside it.
		_drag = platformView.OnDrag("down", OnDrag);
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		_drag?.Dispose();
		_drag = null;
		base.DisconnectHandler(platformView);
	}

	IRefreshView? Refresh => CurrentView as IRefreshView;

	void OnDrag(DragPhase phase, double dx, double dy)
	{
		if (Refresh is not { } view)
			return;

		if (phase == DragPhase.Started)
		{
			_pulling = view.IsEnabled && !view.IsRefreshing;
			if (_pulling)
			{
				_content?.SetStyle("transition", null);
				_spinner.SetStyle("transition", null);
				_spinner.AddClass("pulling");
			}
			return;
		}
		if (!_pulling)
			return;

		// Rubber-band: the content follows at half the pointer distance.
		var pull = Math.Clamp(dy * 0.5, 0, MaxPull);
		if (phase == DragPhase.Moved)
		{
			SetOffset(pull);
			_spinner.SetStyle("opacity", WebCss.Num(Math.Min(1, pull / PullThreshold)));
			return;
		}

		_pulling = false;
		_spinner.RemoveClass("pulling");
		_spinner.SetStyle("opacity", null);
		if (phase == DragPhase.Released && pull >= PullThreshold)
			view.IsRefreshing = true; // MapIsRefreshing settles the content and the Command runs.
		else
			Settle(false);
	}

	void SetOffset(double offset)
	{
		_content?.SetStyle("translate", offset > 0 ? $"0 {WebCss.Px(offset)}" : null);
		// The spinner slides down with the content and comes to rest in the gap above it.
		_spinner.SetStyle("translate", $"0 {WebCss.Px(Math.Min(0, offset - RestOffset))}");
	}

	void Settle(bool refreshing)
	{
		_content?.SetStyle("transition", Snap);
		_spinner.SetStyle("transition", Snap);
		SetOffset(refreshing ? RestOffset : 0);
		_spinner.ToggleClass("running", refreshing);
	}

	public static void MapRefreshContent(RefreshViewHandler handler, IContentView view)
	{
		handler._content?.SetStyle("translate", null);
		MapContent(handler, view);
		handler._content = view.PresentedContent?.Handler?.PlatformView as WebElement;
		handler.Settle(view is IRefreshView { IsRefreshing: true });
	}

	public static void MapIsRefreshing(RefreshViewHandler handler, IContentView view)
	{
		if (!handler._pulling)
			handler.Settle(view is IRefreshView { IsRefreshing: true });
	}

	public static void MapRefreshColor(RefreshViewHandler handler, IContentView view)
		=> handler._spinner.SetStyle("--maui-spinner", WebCss.PaintColor((view as IRefreshView)?.RefreshColor));
}

/// <summary>
/// SwipeView: drag the content to reveal Left/Right/Top/Bottom items. In Reveal mode a release past
/// the threshold leaves the items open; in Execute mode it invokes the first item. Supports
/// SwipeItem (text, icon, colors) and SwipeItemView (any view), both transition modes, and
/// SwipeView.Open/Close.
/// </summary>
public class SwipeViewHandler : ContentViewHandler
{
	public static new IPropertyMapper<IContentView, SwipeViewHandler> Mapper =
		new PropertyMapper<IContentView, SwipeViewHandler>(ContentViewHandler.Mapper)
		{
			[nameof(IContentView.Content)] = MapSwipeContent,
			[nameof(ISwipeView.LeftItems)] = MapItems,
			[nameof(ISwipeView.RightItems)] = MapItems,
			[nameof(ISwipeView.TopItems)] = MapItems,
			[nameof(ISwipeView.BottomItems)] = MapItems,
		};

	public static CommandMapper<ISwipeView, SwipeViewHandler> CommandMapper =
		new(WebElementHandler.ViewCommandMapper)
		{
			[nameof(ISwipeView.RequestOpen)] = MapRequestOpen,
			[nameof(ISwipeView.RequestClose)] = MapRequestClose,
		};

	/// <summary>Width of a SwipeItem in a left/right tray, and height of a top/bottom tray.</summary>
	public const double ItemSize = 80;
	const string Snap = "200ms ease";

	public SwipeViewHandler() : base(Mapper, CommandMapper)
	{
	}

	WebElement? _content;
	IDisposable? _drag;
	IDisposable? _contentClick;
	WebElement? _tray;
	readonly List<IDisposable> _traySubscriptions = new();
	readonly List<string> _trayUrls = new();
	Size _size;

	// Current state. _offset is along the active axis: positive reveals Left (or Top) items.
	SwipeDirection? _direction;
	ISwipeItems? _items;
	double _itemsSize;
	double _offset;
	double _dragBase;
	bool _dragging;
	bool? _dragHorizontal;
	bool _started;

	ISwipeView? Swipe => CurrentView as ISwipeView;

	protected override WebElement CreatePlatformView() => new("div", "maui-swipe-view");

	protected override void DisconnectHandler(WebElement platformView)
	{
		_drag?.Dispose();
		_drag = null;
		_contentClick?.Dispose();
		_contentClick = null;
		ClearTray();
		base.DisconnectHandler(platformView);
	}

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);
		_size = frame.Size;
		if (_direction is { } d && _offset != 0)
			Apply(_offset, animate: false); // keep an open tray sized to the new frame
	}

	public static void MapSwipeContent(SwipeViewHandler handler, IContentView view)
	{
		MapContent(handler, view);
		var content = view.PresentedContent?.Handler?.PlatformView as WebElement;
		if (content != handler._content)
		{
			handler._content?.SetStyle("translate", null);
			handler._content = content;
			handler._offset = 0;
			handler._direction = null;
		}
		handler.UpdateDrag();
	}

	public static void MapItems(SwipeViewHandler handler, IContentView view)
	{
		handler.Close(animate: false);
		handler.UpdateDrag();
	}

	public static void MapRequestOpen(SwipeViewHandler handler, ISwipeView view, object? args)
	{
		if (args is not SwipeViewOpenRequest request)
			return;
		var direction = request.OpenSwipeItem switch
		{
			OpenSwipeItem.LeftItems => SwipeDirection.Right,
			OpenSwipeItem.RightItems => SwipeDirection.Left,
			OpenSwipeItem.TopItems => SwipeDirection.Down,
			_ => SwipeDirection.Up,
		};
		if (!handler.Prepare(direction))
			return;
		var sign = direction is SwipeDirection.Right or SwipeDirection.Down ? 1 : -1;
		view.SwipeStarted(new SwipeViewSwipeStarted(direction));
		handler.Apply(sign * handler._itemsSize, request.Animated);
		view.SwipeEnded(new SwipeViewSwipeEnded(direction, true));
		view.IsOpen = true;
	}

	public static void MapRequestClose(SwipeViewHandler handler, ISwipeView view, object? args)
		=> handler.Close(args is not SwipeViewCloseRequest { Animated: false }, raiseEnded: true);

	static bool HasItems(ISwipeItems? items) => items is not null && Visible(items).Any();

	static IEnumerable<ISwipeItem> Visible(ISwipeItems items) => items.Where(i => i switch
	{
		ISwipeItemMenuItem m => m.Visibility == Visibility.Visible,
		ISwipeItemView v => v.Visibility == Visibility.Visible,
		_ => true,
	});

	void UpdateDrag()
	{
		_drag?.Dispose();
		_drag = null;
		_contentClick?.Dispose();
		_contentClick = null;
		if (_content is null || Swipe is not { } view)
			return;

		// Tapping the content of an open SwipeView closes it, as on the native platforms.
		_contentClick = _content.On("click", () =>
		{
			if (_offset != 0 && !_dragging)
				Close(animate: true, raiseEnded: true);
		});

		var h = HasItems(view.LeftItems) || HasItems(view.RightItems);
		var v = HasItems(view.TopItems) || HasItems(view.BottomItems);
		if (!h && !v)
			return;
		_drag = _content.OnDrag(h && v ? "hv" : h ? "h" : "v", OnDrag);
		// Let the browser keep scrolling along the other axis on touch screens.
		_content.SetStyle("touch-action", h && v ? "none" : h ? "pan-y" : "pan-x");
	}

	void OnDrag(DragPhase phase, double dx, double dy)
	{
		if (Swipe is not { } view || !view.IsEnabled)
			return;

		switch (phase)
		{
			case DragPhase.Started:
				_dragging = true;
				_started = false;
				_dragBase = _offset;
				// An open tray keeps its axis; otherwise the dominant drag axis wins.
				_dragHorizontal = _direction is { } open
					? open is SwipeDirection.Left or SwipeDirection.Right
					: Math.Abs(dx) >= Math.Abs(dy);
				return;

			case DragPhase.Moved:
				if (!_dragging)
					return;
				Track(view, _dragBase + (_dragHorizontal == true ? dx : dy));
				return;

			default:
				if (!_dragging)
					return;
				_dragging = false;
				if (phase == DragPhase.Cancelled)
					Close(animate: true);
				else
					Release(view);
				return;
		}
	}

	void Track(ISwipeView view, double raw)
	{
		var horizontal = _dragHorizontal == true;
		var direction = horizontal
			? (raw >= 0 ? SwipeDirection.Right : SwipeDirection.Left)
			: (raw >= 0 ? SwipeDirection.Down : SwipeDirection.Up);

		if (raw == 0 || !Prepare(direction))
		{
			Apply(0, animate: false);
			return;
		}

		// Reveal stops at the items (with a little give); Execute can be dragged across the view.
		var limit = _items!.Mode == SwipeMode.Execute
			? (horizontal ? _size.Width : _size.Height)
			: _itemsSize + 24;
		raw = Math.Sign(raw) * Math.Min(Math.Abs(raw), limit);

		if (!_started)
		{
			_started = true;
			view.SwipeStarted(new SwipeViewSwipeStarted(direction));
		}
		Apply(raw, animate: false);
		view.SwipeChanging(new SwipeViewSwipeChanging(direction, raw));
	}

	void Release(ISwipeView view)
	{
		if (_direction is not { } direction || _items is null || _offset == 0)
		{
			Close(animate: true);
			return;
		}

		var distance = Math.Abs(_offset);
		var horizontal = direction is SwipeDirection.Left or SwipeDirection.Right;
		var extent = horizontal ? _size.Width : _size.Height;

		if (_items.Mode == SwipeMode.Execute)
		{
			var threshold = view.Threshold > 0 ? view.Threshold : extent * 0.5;
			var item = distance >= threshold ? Visible(_items).FirstOrDefault() : null;
			Close(animate: true);
			view.SwipeEnded(new SwipeViewSwipeEnded(direction, false));
			item?.OnInvoked();
			return;
		}

		var revealThreshold = view.Threshold > 0 ? Math.Min(view.Threshold, _itemsSize) : _itemsSize * 0.5;
		var open = distance >= revealThreshold;
		if (open)
			Apply(Math.Sign(_offset) * _itemsSize, animate: true);
		else
			Close(animate: true, raiseEnded: false);
		view.SwipeEnded(new SwipeViewSwipeEnded(direction, open));
		view.IsOpen = open;
	}

	void Close(bool animate, bool raiseEnded = false)
	{
		var direction = _direction;
		Apply(0, animate);
		_direction = null;
		_items = null;
		if (Swipe is { } view)
		{
			if (raiseEnded && direction is { } d)
				view.SwipeEnded(new SwipeViewSwipeEnded(d, false));
			if (view.IsOpen)
				view.IsOpen = false;
		}
	}

	/// <summary>Builds the tray for <paramref name="direction"/>; false when there are no items that way.</summary>
	bool Prepare(SwipeDirection direction)
	{
		if (_direction == direction && _tray is not null)
			return true;
		if (Swipe is not { } view)
			return false;

		var items = direction switch
		{
			SwipeDirection.Right => view.LeftItems,
			SwipeDirection.Left => view.RightItems,
			SwipeDirection.Down => view.TopItems,
			_ => view.BottomItems,
		};
		if (!HasItems(items))
			return false;

		ClearTray();
		_direction = direction;
		_items = items;
		BuildTray(view, direction, items);
		return true;
	}

	void BuildTray(ISwipeView view, SwipeDirection direction, ISwipeItems items)
	{
		var horizontal = direction is SwipeDirection.Left or SwipeDirection.Right;
		var execute = items.Mode == SwipeMode.Execute;
		// Drag mode attaches the items to the content's moving edge; Reveal leaves them in place.
		var drag = view.SwipeTransitionMode == SwipeTransitionMode.Drag && !execute;
		var visible = Visible(items).ToList();

		var tray = new WebElement("div", "maui-swipe-tray");
		var strip = new WebElement("div", "maui-swipe-items");
		tray.Append(strip);

		// Which edge of the SwipeView the tray grows from, and which edge of the tray the strip hugs.
		var trayEdge = direction switch
		{
			SwipeDirection.Right => "left",
			SwipeDirection.Left => "right",
			SwipeDirection.Down => "top",
			_ => "bottom",
		};
		var stripEdge = !drag ? trayEdge : trayEdge switch
		{
			"left" => "right",
			"right" => "left",
			"top" => "bottom",
			_ => "top",
		};
		tray.SetStyle(trayEdge, "0");
		strip.SetStyle(stripEdge, "0");
		if (horizontal)
		{
			tray.SetStyle("top", "0");
			tray.SetStyle("height", "100%");
			strip.SetStyle("height", "100%");
		}
		else
		{
			tray.SetStyle("left", "0");
			tray.SetStyle("width", "100%");
			strip.SetStyle("width", "100%");
		}

		if (!horizontal)
			strip.AddClass("vertical");

		// Menu items are flex children sized by CSS; a SwipeItemView is measured and arranged inside a
		// fixed-size slot. Left/right trays grow with each item; top/bottom items share one row.
		var cross = horizontal ? _size.Height : _size.Width;
		var length = 0.0;
		foreach (var item in visible)
		{
			WebElement element;
			double extent;
			if (item is ISwipeItemView itemView)
			{
				element = new WebElement("div", "maui-swipe-item-slot");
				element.Append(ToPlatform(itemView));
				var slotCross = horizontal ? cross : cross / visible.Count;
				var measured = horizontal
					? itemView.Measure(double.PositiveInfinity, slotCross)
					: itemView.Measure(slotCross, double.PositiveInfinity);
				extent = Math.Max(1, horizontal ? measured.Width : measured.Height);
				element.SetStyle(horizontal ? "width" : "height", WebCss.Px(extent));
				itemView.Arrange(horizontal ? new Rect(0, 0, extent, slotCross) : new Rect(0, 0, slotCross, extent));
			}
			else
			{
				element = CreateMenuItem(item as ISwipeItemMenuItem);
				extent = ItemSize;
			}

			strip.Append(element);
			var captured = item;
			_traySubscriptions.Add(element.On("click", () => OnItemInvoked(captured)));
			length = horizontal ? length + extent : Math.Max(length, extent);
		}

		if (execute)
		{
			// One item fills whatever has been revealed, like the native Execute affordance.
			strip.AddClass("execute");
			_itemsSize = horizontal ? _size.Width : Math.Max(ItemSize, length);
		}
		else
		{
			_itemsSize = length;
			strip.SetStyle(horizontal ? "width" : "height", WebCss.Px(_itemsSize));
		}

		PlatformView.Insert(0, tray); // behind the content
		_tray = tray;
	}

	WebElement CreateMenuItem(ISwipeItemMenuItem? item)
	{
		var button = new WebElement("button", "maui-swipe-item");
		button.SetAttribute("type", "button");
		if (item is null)
			return button;

		button.SetStyle("background", WebCss.Paint(item.Background) ?? "var(--maui-accent)");
		if (WebCss.Color(item.TextColor) is { } color)
			button.SetStyle("color", color);
		if (!string.IsNullOrEmpty(item.AutomationId))
			button.SetAttribute("data-automation-id", item.AutomationId);

		if (item.Source is { } source)
		{
			var icon = new WebElement("img", "maui-swipe-item-icon");
			icon.SetAttribute("alt", "");
			button.Append(icon);
			_ = LoadIcon(icon, source);
		}
		if (!string.IsNullOrEmpty(item.Text))
		{
			var label = new WebElement("span");
			label.SetText(item.Text);
			button.Append(label);
		}
		else if (item.Source is null)
		{
			button.SetAttribute("aria-label", item.AutomationId);
		}
		return button;
	}

	async Task LoadIcon(WebElement icon, IImageSource source)
	{
		var fonts = MauiContext?.Services.GetService(typeof(IFontManager)) as IFontManager;
		var url = await WebImageSource.ResolveAsync(source, fonts);
		if (url is null)
			return;
		_trayUrls.Add(url);
		icon.SetAttribute("src", url);
	}

	void OnItemInvoked(ISwipeItem item)
	{
		var behavior = _items?.SwipeBehaviorOnInvoked ?? SwipeBehaviorOnInvoked.Auto;
		var direction = _direction;
		item.OnInvoked();
		if (behavior == SwipeBehaviorOnInvoked.RemainOpen)
			return;
		Close(animate: true);
		if (direction is { } d)
			Swipe?.SwipeEnded(new SwipeViewSwipeEnded(d, false));
	}

	void Apply(double offset, bool animate)
	{
		_offset = offset;
		if (_content is null)
			return;

		var horizontal = _direction is null or SwipeDirection.Left or SwipeDirection.Right;
		var transition = animate ? Snap : null;
		_content.SetStyle("transition", transition is null ? null : "translate " + transition);
		_content.SetStyle("translate", offset == 0 ? null : horizontal ? $"{WebCss.Px(offset)} 0" : $"0 {WebCss.Px(offset)}");

		if (_tray is null)
			return;
		var dimension = horizontal ? "width" : "height";
		_tray.SetStyle("transition", transition is null ? null : dimension + " " + transition);
		_tray.SetStyle(dimension, WebCss.Px(Math.Abs(offset)));
	}

	void ClearTray()
	{
		foreach (var s in _traySubscriptions)
			s.Dispose();
		_traySubscriptions.Clear();
		foreach (var url in _trayUrls)
			WebImageSource.Release(url);
		_trayUrls.Clear();
		if (_tray is not null)
			PlatformView.Remove(_tray);
		_tray = null;
	}
}

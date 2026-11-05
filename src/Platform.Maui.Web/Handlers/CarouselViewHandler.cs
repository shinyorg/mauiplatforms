using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// CarouselView: the virtualized CollectionView layout with one page per viewport and CSS scroll
/// snapping. Only the current page and two on either side have views. Position and CurrentItem
/// follow the page scrolling settles on.
/// </summary>
/// <remarks>
/// With <see cref="CarouselView.Loop"/>, the items are laid out several times end to end and the
/// carousel starts in the middle copy; whenever scrolling settles in an outer copy it jumps,
/// invisibly, to the same page in the middle one, so it can be swiped past either end indefinitely.
/// </remarks>
public class CarouselViewHandler : CollectionViewHandler
{
	public static new IPropertyMapper<ItemsView, CarouselViewHandler> Mapper =
		new PropertyMapper<ItemsView, CarouselViewHandler>(CollectionViewHandler.Mapper)
		{
			[nameof(CarouselView.Position)] = MapPosition,
			[nameof(CarouselView.CurrentItem)] = MapCurrentItem,
			[nameof(CarouselView.PeekAreaInsets)] = (h, v) => WebLayoutScheduler.Request(),
			[nameof(CarouselView.IsSwipeEnabled)] = MapIsSwipeEnabled,
			[nameof(CarouselView.ItemsLayout)] = MapCarouselItemsLayout,
			[nameof(CarouselView.Loop)] = MapLoop,
		};

	public CarouselViewHandler() : base(Mapper)
	{
	}

	const int LoopCopies = 5;

	CarouselView Carousel => (CarouselView)VirtualView;

	bool _settingPosition;
	bool _snapSuspended;
	bool _positionScrollPending = true;
	double _lastPageExtent;
	int _lastItemCount = -1;

	protected override IItemsLayout? ItemsLayout => Carousel.ItemsLayout;

	protected override double OverscanFactor => 2;

	protected override int ItemCopies => Carousel.Loop ? LoopCopies : 1;

	protected override WebElement CreatePlatformView()
	{
		var root = base.CreatePlatformView();
		root.AddClass("maui-carousel");
		root.SetAttribute("role", "region");
		root.SetAttribute("aria-roledescription", "carousel");
		return root;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		ApplySnap();
	}

	protected override void PrepareItemElement(WebElement element) => element.SetStyle("scroll-snap-align", "start");

	protected override double? FixedItemExtent(double viewportMain)
	{
		var peek = Carousel.PeekAreaInsets;
		var inset = IsVertical ? peek.Top + peek.Bottom : peek.Left + peek.Right;
		return Math.Max(0, viewportMain - inset);
	}

	void ApplySnap()
	{
		var el = PlatformView;
		_snapSuspended = false;
		el.SetStyle("scroll-snap-type", IsVertical ? "y mandatory" : "x mandatory");
		el.SetStyle("overflow-x", IsVertical ? "hidden" : Carousel.IsSwipeEnabled ? "auto" : "hidden");
		el.SetStyle("overflow-y", IsVertical ? (Carousel.IsSwipeEnabled ? "auto" : "hidden") : "hidden");
		el.SetStyle("scrollbar-width", "none");
	}

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);
		// Show the current page after a resize, a new source or a Loop change. Layout passes also run
		// while the user swipes, so don't scroll otherwise.
		var extent = PageExtent();
		var count = ItemCount;
		if (extent > 0 && (_positionScrollPending || extent != _lastPageExtent || count != _lastItemCount))
		{
			_positionScrollPending = false;
			_lastPageExtent = extent;
			_lastItemCount = count;
			ScrollToPosition(animated: false);
		}
	}

	protected override void OnScrollSettled()
	{
		base.OnScrollSettled();
		if (_snapSuspended)
			ApplySnap();
		var extent = PageExtent();
		var count = ItemCount;
		if (extent <= 0 || count == 0)
			return;

		// The browser snaps to an item's edge, which a template's margin can shift a little off the
		// page boundary, so round to the nearest page.
		var page = CurrentPage(extent);
		if (ItemCopies > 1)
		{
			var copy = page / count;
			if (copy == 0 || copy >= LoopCopies - 1)
			{
				page = LoopCopies / 2 * count + page % count;
				ScrollToOffset(PageOffset(page, extent), animated: false);
			}
		}

		var index = ItemIndexOfEntry(page);
		if (index >= 0 && Carousel.Position != index)
		{
			_settingPosition = true;
			try
			{
				Carousel.Position = index;
				Carousel.CurrentItem = ItemAt(index);
			}
			finally
			{
				_settingPosition = false;
			}
		}
	}

	double PageExtent()
	{
		var spacing = (Carousel.ItemsLayout as LinearItemsLayout)?.ItemSpacing ?? 0;
		return (FixedItemExtent(IsVertical ? ArrangedFrame.Height : ArrangedFrame.Width) ?? 0) + spacing;
	}

	int CurrentPage(double extent) => Math.Clamp((int)Math.Round(ScrollOffset / extent), 0, Math.Max(0, EntryCount - 1));

	/// <summary>A page's scroll offset; with peek insets the last pages stop where the content ends.</summary>
	double PageOffset(int page, double extent)
	{
		var viewport = IsVertical ? ArrangedFrame.Height : ArrangedFrame.Width;
		return Math.Clamp(page * extent, 0, Math.Max(0, ContentExtent - viewport));
	}

	/// <summary>The page showing item <paramref name="position"/>: with Loop, the copy nearest the current page.</summary>
	int PageFor(int position, double extent)
	{
		var count = ItemCount;
		if (count == 0)
			return 0;
		position = Math.Clamp(position, 0, count - 1);
		if (ItemCopies == 1)
			return position;

		var current = CurrentPage(extent);
		var copy = current / count;
		var best = LoopCopies / 2 * count + position;
		if (copy == 0 || copy >= LoopCopies - 1)
			return best; // start (or restart) from the middle copy
		best = copy * count + position;
		foreach (var candidate in new[] { best - count, best + count })
		{
			if (candidate >= 0 && candidate < count * LoopCopies && Math.Abs(candidate - current) < Math.Abs(best - current))
				best = candidate;
		}
		return best;
	}

	void ScrollToPosition(bool animated)
	{
		var extent = PageExtent();
		if (extent <= 0)
		{
			_positionScrollPending = true;
			return;
		}
		var offset = PageOffset(PageFor(Carousel.Position, extent), extent);
		if (animated && Math.Abs(offset - ScrollOffset) > 1)
		{
			// The destination page has no view (so no snap point) until the animation nears it, and
			// the browser would snap the scroll short to a page that has one. Snapping is restored
			// once scrolling settles.
			_snapSuspended = true;
			PlatformView.SetStyle("scroll-snap-type", "none");
		}
		ScrollToOffset(offset, animated);
	}

	public static void MapPosition(CarouselViewHandler handler, ItemsView view)
	{
		if (!handler._settingPosition)
			handler.ScrollToPosition(view is CarouselView { IsScrollAnimated: true });
	}

	public static void MapCurrentItem(CarouselViewHandler handler, ItemsView view)
	{
		if (handler._settingPosition || view is not CarouselView carousel || carousel.CurrentItem is null)
			return;
		var index = handler.IndexOfItem(carousel.CurrentItem);
		if (index >= 0 && index != carousel.Position)
			carousel.Position = index;
	}

	public static void MapIsSwipeEnabled(CarouselViewHandler handler, ItemsView view) => handler.ApplySnap();

	public static void MapCarouselItemsLayout(CarouselViewHandler handler, ItemsView view)
	{
		CollectionViewHandler.MapItemsLayout(handler, view);
		handler.ApplySnap();
	}

	public static void MapLoop(CarouselViewHandler handler, ItemsView view)
	{
		handler.InvalidateItems();
		handler._positionScrollPending = true;
	}
}

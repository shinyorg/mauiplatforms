using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web.Interop;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// CarouselView: the CollectionView layout with one page per viewport and CSS scroll snapping.
/// Position and CurrentItem follow the snapped page. Loop is not supported.
/// </summary>
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
		};

	public CarouselViewHandler() : base(Mapper)
	{
	}

	CarouselView Carousel => (CarouselView)VirtualView;

	bool _settingPosition;
	int _settleTimer;

	protected override IItemsLayout? ItemsLayout => Carousel.ItemsLayout;

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

	protected override double? FixedItemExtent(double viewportMain)
	{
		var peek = Carousel.PeekAreaInsets;
		var inset = IsVertical ? peek.Top + peek.Bottom : peek.Left + peek.Right;
		return Math.Max(0, viewportMain - inset);
	}

	void ApplySnap()
	{
		var el = PlatformView;
		el.SetStyle("scroll-snap-type", IsVertical ? "y mandatory" : "x mandatory");
		el.SetStyle("overflow-x", IsVertical ? "hidden" : Carousel.IsSwipeEnabled ? "auto" : "hidden");
		el.SetStyle("overflow-y", IsVertical ? (Carousel.IsSwipeEnabled ? "auto" : "hidden") : "hidden");
		el.SetStyle("scrollbar-width", "none");
	}

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);
		foreach (var element in RealizedItemElements)
			element.SetStyle("scroll-snap-align", "start");
		// Keep the current page in view when the viewport resizes.
		ScrollToPosition(animated: false);
	}

	protected override void OnScrolled(double x, double y)
	{
		base.OnScrolled(x, y);
		// Update Position once scrolling settles on a page rather than on every frame.
		if (_settleTimer != 0)
			Dom.StopTimeout(_settleTimer);
		_settleTimer = Dom.StartTimeout(() =>
		{
			_settleTimer = 0;
			var extent = PageExtent();
			if (extent <= 0)
				return;
			var index = (int)Math.Round((IsVertical ? y : x) / extent);
			var count = RealizedItems.Count();
			if (count == 0)
				return;
			index = Math.Clamp(index, 0, count - 1);
			if (Carousel.Position != index)
			{
				_settingPosition = true;
				try
				{
					Carousel.Position = index;
					Carousel.CurrentItem = RealizedItems.ElementAt(index);
				}
				finally
				{
					_settingPosition = false;
				}
			}
		}, 120);
	}

	double PageExtent()
	{
		var spacing = (Carousel.ItemsLayout as LinearItemsLayout)?.ItemSpacing ?? 0;
		return (FixedItemExtent(IsVertical ? ArrangedFrame.Height : ArrangedFrame.Width) ?? 0) + spacing;
	}

	void ScrollToPosition(bool animated)
	{
		var offset = Carousel.Position * PageExtent();
		Dom.ScrollTo(PlatformView.Js, IsVertical ? 0 : offset, IsVertical ? offset : 0, animated);
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
		var index = handler.RealizedItems.ToList().IndexOf(carousel.CurrentItem);
		if (index >= 0 && index != carousel.Position)
			carousel.Position = index;
	}

	public static void MapIsSwipeEnabled(CarouselViewHandler handler, ItemsView view) => handler.ApplySnap();

	public static void MapCarouselItemsLayout(CarouselViewHandler handler, ItemsView view)
	{
		CollectionViewHandler.MapItemsLayout(handler, view);
		handler.ApplySnap();
	}
}

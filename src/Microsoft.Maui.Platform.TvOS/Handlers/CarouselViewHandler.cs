using System.Collections;
using System.Collections.Specialized;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

public class CarouselViewHandler : TvOSViewHandler<CarouselView, UIScrollView>
{
    public static readonly IPropertyMapper<CarouselView, CarouselViewHandler> Mapper =
        new PropertyMapper<CarouselView, CarouselViewHandler>(ViewMapper)
        {
            [nameof(ItemsView.ItemsSource)] = MapItemsSource,
            [nameof(ItemsView.ItemTemplate)] = MapItemTemplate,
            [nameof(CarouselView.Position)] = MapPosition,
            [nameof(CarouselView.IsSwipeEnabled)] = MapIsSwipeEnabled,
            [nameof(CarouselView.IsBounceEnabled)] = MapIsBounceEnabled,
            [nameof(CarouselView.PeekAreaInsets)] = MapPeekAreaInsets,
        };

    UIView? _itemsContainer;
    INotifyCollectionChanged? _observableSource;
    bool _updatingPosition;
    int _itemCount;

    public CarouselViewHandler() : base(Mapper) { }

    protected override UIScrollView CreatePlatformView()
    {
        var scrollView = new UIScrollView
        {
            ShowsHorizontalScrollIndicator = false,
            ShowsVerticalScrollIndicator = false,
            Bounces = true,
            ClipsToBounds = true,
        };
        _itemsContainer = new UIView();
        scrollView.AddSubview(_itemsContainer);
        return scrollView;
    }

    protected override void ConnectHandler(UIScrollView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.DecelerationEnded += OnDecelerationEnded;
        platformView.DraggingEnded += OnDraggingEnded;
    }

    protected override void DisconnectHandler(UIScrollView platformView)
    {
        platformView.DecelerationEnded -= OnDecelerationEnded;
        platformView.DraggingEnded -= OnDraggingEnded;
        UnsubscribeCollection();
        base.DisconnectHandler(platformView);
    }

    public override void PlatformArrange(Rect rect)
    {
        base.PlatformArrange(rect);
        LayoutItems(rect);
    }

    void LayoutItems(Rect rect)
    {
        if (_itemsContainer == null)
            return;

        var subviews = _itemsContainer.Subviews;
        if (subviews.Length == 0)
            return;

        var width = (nfloat)rect.Width;
        var height = (nfloat)rect.Height;

        if (width <= 0 || height <= 0)
            return;

        var peek = VirtualView?.PeekAreaInsets ?? Thickness.Zero;
        var itemWidth = width - (nfloat)(peek.Left + peek.Right);

        for (int i = 0; i < subviews.Length; i++)
        {
            var x = (nfloat)peek.Left + (i * width);
            subviews[i].Frame = new CGRect(x, 0, itemWidth, height);
        }

        var totalWidth = width * subviews.Length;
        _itemsContainer.Frame = new CGRect(0, 0, totalWidth, height);
        PlatformView.ContentSize = new CGSize(totalWidth, height);

        // Restore position after layout
        if (VirtualView != null && VirtualView.Position >= 0 && VirtualView.Position < _itemCount)
            ScrollToPosition(VirtualView.Position, animated: false);
    }

    void OnDecelerationEnded(object? sender, EventArgs e) => UpdatePositionFromScroll();
    void OnDraggingEnded(object? sender, DraggingEventArgs e)
    {
        if (!e.Decelerate)
            UpdatePositionFromScroll();
    }

    void UpdatePositionFromScroll()
    {
        if (VirtualView == null || _itemCount == 0)
            return;

        var pageWidth = PlatformView.Frame.Width;
        if (pageWidth <= 0)
            return;

        var position = (int)Math.Round(PlatformView.ContentOffset.X / pageWidth);
        position = Math.Clamp(position, 0, _itemCount - 1);

        if (position != VirtualView.Position)
        {
            _updatingPosition = true;
            try
            {
                VirtualView.Position = position;

                var items = GetItemsList();
                if (items != null && position < items.Count)
                    VirtualView.CurrentItem = items[position];
            }
            finally
            {
                _updatingPosition = false;
            }
        }
    }

    void ScrollToPosition(int position, bool animated = true)
    {
        var pageWidth = PlatformView.Frame.Width;
        if (pageWidth <= 0)
            return;

        var offset = new CGPoint(position * pageWidth, 0);
        PlatformView.SetContentOffset(offset, animated);
    }

    IList? GetItemsList()
    {
        var source = VirtualView?.ItemsSource;
        if (source is IList list)
            return list;
        if (source != null)
            return source.Cast<object>().ToList();
        return null;
    }

    public static void MapItemsSource(CarouselViewHandler handler, CarouselView view)
    {
        handler.ReloadItems();
    }

    public static void MapItemTemplate(CarouselViewHandler handler, CarouselView view)
    {
        handler.ReloadItems();
    }

    public static void MapPosition(CarouselViewHandler handler, CarouselView view)
    {
        if (handler._updatingPosition)
            return;

        if (view.Position >= 0 && view.Position < handler._itemCount)
            handler.ScrollToPosition(view.Position, view.AnimatePositionChanges);
    }

    public static void MapIsSwipeEnabled(CarouselViewHandler handler, CarouselView view)
    {
        handler.PlatformView.ScrollEnabled = view.IsSwipeEnabled;
    }

    public static void MapIsBounceEnabled(CarouselViewHandler handler, CarouselView view)
    {
        handler.PlatformView.Bounces = view.IsBounceEnabled;
    }

    public static void MapPeekAreaInsets(CarouselViewHandler handler, CarouselView view)
    {
        // PeekAreaInsets changes item sizing — need to relayout
        if (handler.PlatformView.Frame.Width > 0)
            handler.LayoutItems(new Rect(0, 0, handler.PlatformView.Frame.Width, handler.PlatformView.Frame.Height));
    }

    void UnsubscribeCollection()
    {
        if (_observableSource != null)
        {
            _observableSource.CollectionChanged -= OnCollectionChanged;
            _observableSource = null;
        }
    }

    void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ReloadItems();
    }

    void ReloadItems()
    {
        if (_itemsContainer == null || MauiContext == null)
            return;

        UnsubscribeCollection();

        foreach (var subview in _itemsContainer.Subviews)
            subview.RemoveFromSuperview();

        var itemsSource = VirtualView?.ItemsSource;
        if (itemsSource == null)
        {
            _itemCount = 0;
            return;
        }

        if (itemsSource is INotifyCollectionChanged observable)
        {
            _observableSource = observable;
            _observableSource.CollectionChanged += OnCollectionChanged;
        }

        var template = VirtualView?.ItemTemplate;
        int count = 0;

        foreach (var item in itemsSource)
        {
            var view = CreateItemView(item, template);
            if (view != null)
            {
                var platformView = view.ToTvOSPlatform(MauiContext);
                _itemsContainer.AddSubview(platformView);
                count++;
            }
        }

        _itemCount = count;

        if (PlatformView.Frame.Width > 0)
            LayoutItems(new Rect(0, 0, PlatformView.Frame.Width, PlatformView.Frame.Height));

        // Set initial current item
        if (VirtualView != null && count > 0)
        {
            var items = GetItemsList();
            var pos = Math.Clamp(VirtualView.Position, 0, count - 1);
            if (items != null && pos < items.Count)
                VirtualView.CurrentItem = items[pos];
        }
    }

    static IView? CreateItemView(object item, DataTemplate? template)
    {
        if (template != null)
        {
            var content = template.CreateContent();
            if (content is View view)
            {
                view.BindingContext = item;
                return view;
            }
        }

        return new Label { Text = item?.ToString() ?? string.Empty };
    }
}

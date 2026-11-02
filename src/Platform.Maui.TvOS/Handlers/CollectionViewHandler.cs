using System.Collections;
using System.Collections.Specialized;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

/// <summary>
/// CollectionView on a native UICollectionView, so the Siri Remote can move through it:
/// every item is a focusable cell that lifts when focused, the list scrolls to keep the
/// focused item in view, and Select picks the item (SelectionMode) or raises the template's
/// TapGestureRecognizers. Cells are recycled, so long lists stay cheap.
///
/// Supports LinearItemsLayout (vertical or horizontal) and GridItemsLayout with spacing,
/// single and multiple selection with the "Selected" and "Focused" visual states,
/// DataTemplateSelector, EmptyView, ScrollTo, Scrolled and RemainingItemsThreshold.
/// Grouping, headers and footers are not supported yet.
/// </summary>
public partial class CollectionViewHandler : TvOSViewHandler<CollectionView, UICollectionView>
{
    public static readonly IPropertyMapper<CollectionView, CollectionViewHandler> Mapper =
        new PropertyMapper<CollectionView, CollectionViewHandler>(ViewMapper)
        {
            [nameof(ItemsView.ItemsSource)] = MapItemsSource,
            [nameof(ItemsView.ItemTemplate)] = MapItemTemplate,
            [nameof(StructuredItemsView.ItemsLayout)] = MapItemsLayout,
            [nameof(StructuredItemsView.ItemSizingStrategy)] = MapItemTemplate,
            [nameof(SelectableItemsView.SelectionMode)] = MapSelection,
            [nameof(SelectableItemsView.SelectedItem)] = MapSelection,
            [nameof(SelectableItemsView.SelectedItems)] = MapSelection,
            [nameof(ItemsView.EmptyView)] = MapEmptyView,
            [nameof(ItemsView.EmptyViewTemplate)] = MapEmptyView,
            [nameof(ItemsView.HorizontalScrollBarVisibility)] = MapScrollBars,
            [nameof(ItemsView.VerticalScrollBarVisibility)] = MapScrollBars,
        };

    /// <summary>
    /// Space kept around the items so a focused item has room to grow and cast its shadow
    /// without being clipped by the collection's edges.
    /// </summary>
    public static Thickness FocusInset { get; set; } = new(24, 28);

    const string CellId = "MauiTvOSCell";

    readonly List<object?> _items = new();
    readonly Dictionary<int, CGSize> _sizeCache = new();
    readonly Dictionary<DataTemplate, View> _sizingViews = new();
    INotifyCollectionChanged? _observableSource;
    IItemsLayout? _layout;
    nfloat _sizedForWidth = -1;
    nfloat _sizedForHeight = -1;
    UIView? _emptyView;
    int _thresholdReachedAt = -1;

    public CollectionViewHandler() : base(Mapper)
    {
    }

    UICollectionViewFlowLayout FlowLayout => (UICollectionViewFlowLayout)PlatformView.CollectionViewLayout;

    internal IReadOnlyList<object?> Items => _items;

    protected override UICollectionView CreatePlatformView()
    {
        var layout = new UICollectionViewFlowLayout
        {
            ScrollDirection = UICollectionViewScrollDirection.Vertical,
            MinimumLineSpacing = 0,
            MinimumInteritemSpacing = 0,
        };

        var collectionView = new UICollectionView(CGRect.Empty, layout)
        {
            BackgroundColor = UIColor.Clear,
            ClipsToBounds = true,
            RemembersLastFocusedIndexPath = true,
        };
        collectionView.RegisterClassForCell(typeof(MauiCollectionCell), CellId);
        return collectionView;
    }

    protected override void ConnectHandler(UICollectionView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.DataSource = new DataSource(this);
        platformView.Delegate = new FlowDelegate(this);
        VirtualView.ScrollToRequested += OnScrollToRequested;
        VirtualView.SelectionChanged += OnSelectionChanged;
    }

    void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => UpdateSelectionStates();

    protected override void DisconnectHandler(UICollectionView platformView)
    {
        VirtualView.ScrollToRequested -= OnScrollToRequested;
        VirtualView.SelectionChanged -= OnSelectionChanged;
        UnsubscribeCollection();

        foreach (var cell in platformView.VisibleCells.OfType<MauiCollectionCell>())
            cell.Unbind(VirtualView);
        foreach (var sizingView in _sizingViews.Values)
            VirtualView.RemoveLogicalChild(sizingView);
        _sizingViews.Clear();

        platformView.DataSource = null!;
        platformView.Delegate = null!;
        base.DisconnectHandler(platformView);
    }

    // ── Mappers ────────────────────────────────────────────────────────────

    public static void MapItemsSource(CollectionViewHandler handler, CollectionView view) => handler.ReloadItems();

    public static void MapItemTemplate(CollectionViewHandler handler, CollectionView view)
    {
        handler.ClearSizing();
        handler.PlatformView.ReloadData();
    }

    public static void MapItemsLayout(CollectionViewHandler handler, CollectionView view)
    {
        handler._layout = view.ItemsLayout;
        handler.UpdateFlowLayout();
    }

    public static void MapSelection(CollectionViewHandler handler, CollectionView view) => handler.UpdateSelectionStates();

    public static void MapEmptyView(CollectionViewHandler handler, CollectionView view) => handler.UpdateEmptyView();

    public static void MapScrollBars(CollectionViewHandler handler, CollectionView view)
    {
        handler.PlatformView.ShowsVerticalScrollIndicator = view.VerticalScrollBarVisibility != ScrollBarVisibility.Never;
        handler.PlatformView.ShowsHorizontalScrollIndicator = view.HorizontalScrollBarVisibility != ScrollBarVisibility.Never;
    }

    // ── Items ──────────────────────────────────────────────────────────────

    void ReloadItems()
    {
        UnsubscribeCollection();
        _items.Clear();

        if (VirtualView.ItemsSource is IEnumerable source)
        {
            foreach (var item in source)
                _items.Add(item);

            if (source is INotifyCollectionChanged observable)
            {
                _observableSource = observable;
                observable.CollectionChanged += OnCollectionChanged;
            }
        }

        _thresholdReachedAt = -1;
        ClearSizing();
        PlatformView.ReloadData();
        UpdateEmptyView();
        VirtualView.InvalidateMeasure();
    }

    void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Reload rather than patch: the source is small enough on a TV, and it keeps the
        // snapshot, size cache and cells consistent whatever the change was.
        _items.Clear();
        if (sender is IEnumerable source)
        {
            foreach (var item in source)
                _items.Add(item);
        }

        _thresholdReachedAt = -1;
        ClearSizing();
        PlatformView.ReloadData();
        UpdateEmptyView();
        VirtualView.InvalidateMeasure();
    }

    void UnsubscribeCollection()
    {
        if (_observableSource != null)
        {
            _observableSource.CollectionChanged -= OnCollectionChanged;
            _observableSource = null;
        }
    }

    internal DataTemplate? TemplateFor(object? item)
    {
        var template = VirtualView.ItemTemplate;
        if (template is DataTemplateSelector selector)
            return selector.SelectTemplate(item, VirtualView);
        return template;
    }

    internal View CreateItemView(object? item, DataTemplate? template)
    {
        View view = template?.CreateContent() as View
            ?? new Label { Text = item?.ToString() ?? string.Empty, FontSize = 29, Padding = new Thickness(24, 16) };
        view.BindingContext = item;
        VirtualView.AddLogicalChild(view);
        return view;
    }

    // ── Layout ─────────────────────────────────────────────────────────────

    bool IsHorizontal => _layout is ItemsLayout { Orientation: ItemsLayoutOrientation.Horizontal };

    int Span => _layout is GridItemsLayout grid ? Math.Max(1, grid.Span) : 1;

    void UpdateFlowLayout()
    {
        var flow = FlowLayout;
        flow.ScrollDirection = IsHorizontal ? UICollectionViewScrollDirection.Horizontal : UICollectionViewScrollDirection.Vertical;

        switch (_layout)
        {
            case GridItemsLayout grid:
                // Line spacing runs along the scroll direction, inter-item spacing across it.
                flow.MinimumLineSpacing = (nfloat)(IsHorizontal ? grid.HorizontalItemSpacing : grid.VerticalItemSpacing);
                flow.MinimumInteritemSpacing = (nfloat)(IsHorizontal ? grid.VerticalItemSpacing : grid.HorizontalItemSpacing);
                break;
            case LinearItemsLayout linear:
                flow.MinimumLineSpacing = (nfloat)linear.ItemSpacing;
                flow.MinimumInteritemSpacing = 0;
                break;
        }

        var inset = FocusInset;
        flow.SectionInset = new UIEdgeInsets((nfloat)inset.Top, (nfloat)inset.Left, (nfloat)inset.Bottom, (nfloat)inset.Right);

        ClearSizing();
        flow.InvalidateLayout();
        VirtualView.InvalidateMeasure();
    }

    void ClearSizing()
    {
        _sizeCache.Clear();
        _sizedForWidth = -1;
        _sizedForHeight = -1;
    }

    public override void PlatformArrange(Rect rect)
    {
        base.PlatformArrange(rect);

        // Item sizes depend on the collection's size across the scroll direction.
        var across = IsHorizontal ? (nfloat)rect.Height : (nfloat)rect.Width;
        var sizedFor = IsHorizontal ? _sizedForHeight : _sizedForWidth;
        if (across > 0 && Math.Abs((double)(across - sizedFor)) > 0.5)
        {
            _sizeCache.Clear();
            FlowLayout.InvalidateLayout();
        }

        if (_emptyView != null)
            _emptyView.Frame = PlatformView.Bounds;
    }

    /// <summary>The size of item <paramref name="index"/> when the collection is <paramref name="extent"/> across.</summary>
    internal CGSize SizeForItem(int index, CGSize extent)
    {
        if (IsHorizontal)
            _sizedForHeight = extent.Height;
        else
            _sizedForWidth = extent.Width;

        var key = VirtualView.ItemSizingStrategy == ItemSizingStrategy.MeasureFirstItem ? 0 : index;
        if (_sizeCache.TryGetValue(key, out var cached))
            return cached;

        var size = MeasureItem(key < _items.Count ? _items[key] : null, extent);
        _sizeCache[key] = size;
        return size;
    }

    CGSize MeasureItem(object? item, CGSize extent)
    {
        var flow = FlowLayout;
        var inset = flow.SectionInset;
        var span = Span;

        // The cell size across the scroll direction is fixed by the collection (and span);
        // along it the item's template decides.
        double across;
        if (IsHorizontal)
        {
            var available = extent.Height - inset.Top - inset.Bottom - (flow.MinimumInteritemSpacing * (span - 1));
            across = Math.Max(1, (double)available / span);
        }
        else
        {
            var available = extent.Width - inset.Left - inset.Right - (flow.MinimumInteritemSpacing * (span - 1));
            across = Math.Max(1, Math.Floor((double)available / span));
        }

        var template = TemplateFor(item);
        var view = SizingView(template, item);
        IView measured = view;
        var desired = IsHorizontal
            ? measured.Measure(double.PositiveInfinity, across)
            : measured.Measure(across, double.PositiveInfinity);

        return IsHorizontal
            ? new CGSize(Math.Max(1, Math.Ceiling(desired.Width)), across)
            : new CGSize(across, Math.Max(1, Math.Ceiling(desired.Height)));
    }

    View SizingView(DataTemplate? template, object? item)
    {
        if (template == null)
        {
            var label = CreateItemView(item, null);
            label.ToTvOSPlatform(MauiContext!);
            return label;
        }

        if (!_sizingViews.TryGetValue(template, out var view))
        {
            view = CreateItemView(item, template);
            view.ToTvOSPlatform(MauiContext!);
            _sizingViews[template] = view;
        }
        view.BindingContext = item;
        return view;
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        // An explicit size, or a constrained one in the scroll direction, is just taken.
        var explicitWidth = VirtualView.WidthRequest >= 0;
        var explicitHeight = VirtualView.HeightRequest >= 0;
        var scrollsUnbounded = IsHorizontal ? double.IsInfinity(widthConstraint) : double.IsInfinity(heightConstraint);

        if ((IsHorizontal ? explicitWidth : explicitHeight) || !scrollsUnbounded)
        {
            var width = explicitWidth ? VirtualView.WidthRequest : double.IsInfinity(widthConstraint) ? 0 : widthConstraint;
            var height = explicitHeight ? VirtualView.HeightRequest : double.IsInfinity(heightConstraint) ? 0 : heightConstraint;
            if (IsHorizontal && !explicitHeight)
                height = ContentExtent(new CGSize(width, double.IsInfinity(heightConstraint) ? 0 : heightConstraint)).Height;
            return new Size(width, height);
        }

        // Unbounded along the scroll direction (for example inside a ScrollView): size to the
        // content, so the page scrolls it as a whole.
        var extent = new CGSize(
            double.IsInfinity(widthConstraint) ? 1920 : widthConstraint,
            double.IsInfinity(heightConstraint) ? 0 : heightConstraint);
        var content = ContentExtent(extent);
        return new Size(
            explicitWidth ? VirtualView.WidthRequest : IsHorizontal ? content.Width : extent.Width,
            explicitHeight ? VirtualView.HeightRequest : IsHorizontal ? content.Height : content.Height);
    }

    /// <summary>The size the items take when laid out within <paramref name="extent"/>.</summary>
    CGSize ContentExtent(CGSize extent)
    {
        var flow = FlowLayout;
        var inset = flow.SectionInset;
        if (_items.Count == 0)
            return new CGSize(extent.Width, IsHorizontal ? inset.Top + inset.Bottom : 0);

        var span = Span;
        if (IsHorizontal)
        {
            // The tallest item sets the row height; each column is as wide as its widest item.
            double rowHeight = 0;
            foreach (var item in _items)
                rowHeight = Math.Max(rowHeight, MeasureFree(item).Height);

            double length = 0;
            var columns = (_items.Count + span - 1) / span;
            for (int column = 0; column < columns; column++)
            {
                double width = 0;
                for (int i = column * span; i < Math.Min(_items.Count, (column + 1) * span); i++)
                    width = Math.Max(width, MeasureFree(_items[i]).Width);
                length += width + (column > 0 ? flow.MinimumLineSpacing : 0);
            }

            var height = (rowHeight * span) + (flow.MinimumInteritemSpacing * (span - 1)) + inset.Top + inset.Bottom;
            return new CGSize(length + inset.Left + inset.Right, Math.Ceiling(height));
        }

        nfloat total = inset.Top + inset.Bottom;
        for (int row = 0; row * span < _items.Count; row++)
        {
            nfloat rowHeight = 0;
            for (int i = row * span; i < Math.Min(_items.Count, (row + 1) * span); i++)
                rowHeight = (nfloat)Math.Max(rowHeight, SizeForItem(i, extent).Height);
            total += rowHeight + (row > 0 ? flow.MinimumLineSpacing : 0);
        }
        return new CGSize(extent.Width, total);
    }

    Size MeasureFree(object? item)
    {
        IView view = SizingView(TemplateFor(item), item);
        return view.Measure(double.PositiveInfinity, double.PositiveInfinity);
    }

    // ── Selection ──────────────────────────────────────────────────────────

    internal bool IsSelected(object? item) => VirtualView.SelectionMode switch
    {
        SelectionMode.Single => Equals(VirtualView.SelectedItem, item) && item != null,
        SelectionMode.Multiple => VirtualView.SelectedItems?.Contains(item!) == true,
        _ => false,
    };

    internal void ItemActivated(int index, MauiCollectionCell cell)
    {
        if (index < 0 || index >= _items.Count)
            return;

        var item = _items[index];
        switch (VirtualView.SelectionMode)
        {
            case SelectionMode.Single:
                VirtualView.SelectedItem = item;
                break;
            case SelectionMode.Multiple:
                var selected = VirtualView.SelectedItems;
                if (selected == null)
                    break;
                if (selected.Contains(item!))
                    selected.Remove(item!);
                else
                    selected.Add(item!);
                break;
        }

        // Taps in the template still run, so a list without selection can act on Select.
        if (cell.MauiView is { } view && TvOSTapGestureManager.HasTap(view))
            TvOSTapGestureManager.RaiseTaps(view);

        UpdateSelectionStates();
    }

    void UpdateSelectionStates()
    {
        foreach (var cell in PlatformView.VisibleCells.OfType<MauiCollectionCell>())
            cell.UpdateVisualState(IsSelected(cell.MauiView?.BindingContext));
    }

    // ── Empty view, scrolling, thresholds ─────────────────────────────────

    void UpdateEmptyView()
    {
        _emptyView?.RemoveFromSuperview();
        _emptyView = null;
        PlatformView.BackgroundView = null;

        if (_items.Count > 0 || MauiContext == null)
            return;

        var empty = VirtualView.EmptyView;
        View? view = empty as View;
        if (view == null && VirtualView.EmptyViewTemplate?.CreateContent() is View templated)
        {
            templated.BindingContext = empty;
            view = templated;
        }
        if (view == null && empty != null)
        {
            view = new Label
            {
                Text = empty.ToString(),
                FontSize = 31,
                TextColor = Colors.Gray,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            };
        }
        if (view == null)
            return;

        VirtualView.AddLogicalChild(view);
        IView mauiView = view;
        var host = new TvOSContainerView
        {
            CrossPlatformMeasure = (w, h) => mauiView.Measure(w, h),
            CrossPlatformArrange = r => mauiView.Arrange(r),
            Frame = PlatformView.Bounds,
        };
        host.AddSubview(view.ToTvOSPlatform(MauiContext));
        _emptyView = host;
        PlatformView.BackgroundView = host;
    }

    void OnScrollToRequested(object? sender, ScrollToRequestEventArgs e)
    {
        var index = e.Mode == ScrollToMode.Position ? e.Index : _items.IndexOf(e.Item);
        if (index < 0 || index >= _items.Count)
            return;

        var position = e.ScrollToPosition switch
        {
            ScrollToPosition.Start => IsHorizontal ? UICollectionViewScrollPosition.Left : UICollectionViewScrollPosition.Top,
            ScrollToPosition.End => IsHorizontal ? UICollectionViewScrollPosition.Right : UICollectionViewScrollPosition.Bottom,
            ScrollToPosition.Center => IsHorizontal ? UICollectionViewScrollPosition.CenteredHorizontally : UICollectionViewScrollPosition.CenteredVertically,
            _ => UICollectionViewScrollPosition.None,
        };
        PlatformView.ScrollToItem(NSIndexPath.FromItemSection(index, 0), position, e.IsAnimated);
    }

    internal void WillDisplay(int index)
    {
        var threshold = VirtualView.RemainingItemsThreshold;
        if (threshold < 0 || _items.Count == 0)
            return;

        if (index >= _items.Count - 1 - threshold && _thresholdReachedAt != _items.Count)
        {
            _thresholdReachedAt = _items.Count;
            VirtualView.SendRemainingItemsThresholdReached();
        }
    }

    internal void Scrolled()
    {
        var offset = PlatformView.ContentOffset;
        var visible = PlatformView.IndexPathsForVisibleItems.Select(p => (int)p.Item).DefaultIfEmpty(-1).ToArray();
        VirtualView.SendScrolled(new ItemsViewScrolledEventArgs
        {
            HorizontalOffset = offset.X,
            VerticalOffset = offset.Y,
            FirstVisibleItemIndex = visible.Min(),
            LastVisibleItemIndex = visible.Max(),
            CenterItemIndex = visible.Length > 0 ? visible[visible.Length / 2] : -1,
        });
    }

    // ── UIKit plumbing ─────────────────────────────────────────────────────

    sealed class DataSource : UICollectionViewDataSource
    {
        readonly WeakReference<CollectionViewHandler> _handler;

        public DataSource(CollectionViewHandler handler) => _handler = new(handler);

        public override nint GetItemsCount(UICollectionView collectionView, nint section) =>
            _handler.TryGetTarget(out var handler) ? handler._items.Count : 0;

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var cell = (MauiCollectionCell)collectionView.DequeueReusableCell(CellId, indexPath);
            if (_handler.TryGetTarget(out var handler) && indexPath.Item < handler._items.Count)
            {
                var item = handler._items[(int)indexPath.Item];
                cell.Bind(handler, item);
                cell.UpdateVisualState(handler.IsSelected(item));
            }
            return cell;
        }
    }

    sealed class FlowDelegate : UICollectionViewDelegateFlowLayout
    {
        readonly WeakReference<CollectionViewHandler> _handler;

        public FlowDelegate(CollectionViewHandler handler) => _handler = new(handler);

        public override CGSize GetSizeForItem(UICollectionView collectionView, UICollectionViewLayout layout, NSIndexPath indexPath) =>
            _handler.TryGetTarget(out var handler)
                ? handler.SizeForItem((int)indexPath.Item, collectionView.Bounds.Size)
                : new CGSize(1, 1);

        public override bool CanFocusItem(UICollectionView collectionView, NSIndexPath indexPath) => true;

        public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            // UIKit's own selection is not used; MAUI's SelectedItem(s) is the source of truth.
            collectionView.DeselectItem(indexPath, false);
            if (_handler.TryGetTarget(out var handler) && collectionView.CellForItem(indexPath) is MauiCollectionCell cell)
                handler.ItemActivated((int)indexPath.Item, cell);
        }

        public override void WillDisplayCell(UICollectionView collectionView, UICollectionViewCell cell, NSIndexPath indexPath)
        {
            if (_handler.TryGetTarget(out var handler))
                handler.WillDisplay((int)indexPath.Item);
        }

        public override void Scrolled(UIScrollView scrollView)
        {
            if (_handler.TryGetTarget(out var handler))
                handler.Scrolled();
        }
    }
}

/// <summary>A collection cell hosting one MAUI item view.</summary>
public class MauiCollectionCell : UICollectionViewCell
{
    DataTemplate? _template;
    bool _selected;

    public MauiCollectionCell(ObjCRuntime.NativeHandle handle) : base(handle)
    {
        ClipsToBounds = false;
        ContentView.ClipsToBounds = false;
        BackgroundColor = UIColor.Clear;
    }

    public View? MauiView { get; private set; }

    internal void Bind(CollectionViewHandler handler, object? item)
    {
        var template = handler.TemplateFor(item);
        if (MauiView == null || template != _template || template == null)
        {
            Unbind(handler.VirtualView);
            _template = template;
            MauiView = handler.CreateItemView(item, template);
            ContentView.AddSubview(MauiView.ToTvOSPlatform(handler.MauiContext!));
        }
        else
        {
            MauiView.BindingContext = item;
        }
        SetNeedsLayout();
    }

    internal void Unbind(CollectionView owner)
    {
        if (MauiView == null)
            return;

        if (MauiView.Handler?.PlatformView is UIView platformView)
            platformView.RemoveFromSuperview();
        MauiView.Handler?.DisconnectHandler();
        owner.RemoveLogicalChild(MauiView);
        MauiView = null;
        _template = null;
    }

    internal void UpdateVisualState(bool selected)
    {
        _selected = selected;
        ApplyVisualState();
    }

    void ApplyVisualState()
    {
        if (MauiView == null)
            return;

        if (Focused && VisualStateManager.GoToState(MauiView, "Focused"))
            return;
        VisualStateManager.GoToState(MauiView, _selected ? "Selected" : "Normal");
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        if (MauiView is not IView view)
            return;

        var bounds = ContentView.Bounds;
        view.Measure(bounds.Width, bounds.Height);
        view.Arrange(new Rect(0, 0, bounds.Width, bounds.Height));
    }

    public override void DidUpdateFocus(UIFocusUpdateContext context, UIFocusAnimationCoordinator coordinator)
    {
        base.DidUpdateFocus(context, coordinator);

        if (context.NextFocusedView == this)
            TvOSFocusEffect.Animate(this, true, coordinator);
        else if (context.PreviouslyFocusedView == this)
            TvOSFocusEffect.Animate(this, false, coordinator);
        else
            return;

        ApplyVisualState();
    }

    public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (presses.ToArray().Any(p => p.Type == UIPressType.Select))
            UIView.Animate(0.1, () => TvOSTransforms.SetFocusScale(this, TvOSFocusEffect.PressedScale));
        base.PressesBegan(presses, evt);
    }

    public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (presses.ToArray().Any(p => p.Type == UIPressType.Select))
            UIView.Animate(0.15, () => TvOSTransforms.SetFocusScale(this, Focused ? TvOSFocusEffect.FocusedScale : 1));
        base.PressesEnded(presses, evt);
    }

    public override void PressesCancelled(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        UIView.Animate(0.15, () => TvOSTransforms.SetFocusScale(this, Focused ? TvOSFocusEffect.FocusedScale : 1));
        base.PressesCancelled(presses, evt);
    }
}

using System.Collections;
using System.Collections.Specialized;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

public partial class CollectionViewHandler : MacOSViewHandler<CollectionView, NSScrollView>
{
    public static readonly IPropertyMapper<CollectionView, CollectionViewHandler> Mapper =
        new PropertyMapper<CollectionView, CollectionViewHandler>(ViewMapper)
        {
            [nameof(ItemsView.ItemsSource)] = MapItemsSource,
            [nameof(ItemsView.ItemTemplate)] = MapItemTemplate,
            [nameof(StructuredItemsView.ItemsLayout)] = MapItemsLayout,
            [nameof(SelectableItemsView.SelectionMode)] = MapSelectionMode,
            [nameof(SelectableItemsView.SelectedItem)] = MapSelectedItem,
            [nameof(GroupableItemsView.IsGrouped)] = MapIsGrouped,
            [nameof(GroupableItemsView.GroupHeaderTemplate)] = MapGroupHeaderTemplate,
            [nameof(GroupableItemsView.GroupFooterTemplate)] = MapGroupFooterTemplate,
        };

    FlippedDocumentView? _documentView;
    MacOSContainerView? _itemsContainer;
    INotifyCollectionChanged? _observableSource;
    // Track item views for selection
    readonly List<(NSView view, object item, int flatIndex)> _itemEntries = new();
    int _selectedFlatIndex = -1;

    public CollectionViewHandler() : base(Mapper)
    {
    }

    protected override NSScrollView CreatePlatformView()
    {
        var scrollView = new NSScrollView
        {
            HasVerticalScroller = true,
            HasHorizontalScroller = true,
            AutohidesScrollers = true,
            DrawsBackground = false,
        };

        _documentView = new FlippedDocumentView();
        _itemsContainer = new MacOSContainerView();
        _documentView.AddSubview(_itemsContainer);
        scrollView.DocumentView = _documentView;

        return scrollView;
    }

    protected override void DisconnectHandler(NSScrollView platformView)
    {
        UnsubscribeCollection();
        base.DisconnectHandler(platformView);
    }

    public override void PlatformArrange(Rect rect)
    {
        base.PlatformArrange(rect);
        LayoutItems(rect);
    }

    #region Layout

    void LayoutItems(Rect rect)
    {
        if (_itemsContainer == null || _documentView == null)
            return;

        var subviews = _itemsContainer.Subviews;
        if (subviews.Length == 0)
        {
            _itemsContainer.Frame = new CGRect(0, 0, rect.Width, 0);
            _documentView.Frame = new CGRect(0, 0, rect.Width, 0);
            return;
        }

        var layout = (VirtualView as StructuredItemsView)?.ItemsLayout;
        var isHorizontal = GetOrientation(layout) == ItemsLayoutOrientation.Horizontal;
        var span = GetSpan(layout);
        var itemSpacing = GetItemSpacing(layout);
        var lineSpacing = GetLineSpacing(layout);

        if (span > 1)
            LayoutGrid(subviews, rect, span, itemSpacing, lineSpacing, isHorizontal);
        else if (isHorizontal)
            LayoutHorizontal(subviews, rect, itemSpacing);
        else
            LayoutVertical(subviews, rect, itemSpacing);
    }

    void LayoutVertical(NSView[] subviews, Rect rect, double spacing)
    {
        nfloat y = 0;
        var width = (nfloat)rect.Width;

        foreach (var subview in subviews)
        {
            var height = MeasureItemHeight(subview, width);
            subview.Frame = new CGRect(0, y, width, height);
            y += height + (nfloat)spacing;
        }

        var totalHeight = y - (nfloat)spacing;
        if (totalHeight < 0) totalHeight = 0;
        _itemsContainer!.Frame = new CGRect(0, 0, width, totalHeight);
        _documentView!.Frame = new CGRect(0, 0, width, totalHeight);
    }

    void LayoutHorizontal(NSView[] subviews, Rect rect, double spacing)
    {
        nfloat x = 0;
        var height = (nfloat)rect.Height;

        foreach (var subview in subviews)
        {
            var width = MeasureItemWidth(subview, height);
            subview.Frame = new CGRect(x, 0, width, height);
            x += width + (nfloat)spacing;
        }

        var totalWidth = x - (nfloat)spacing;
        if (totalWidth < 0) totalWidth = 0;
        _itemsContainer!.Frame = new CGRect(0, 0, totalWidth, height);
        _documentView!.Frame = new CGRect(0, 0, totalWidth, height);
    }

    void LayoutGrid(NSView[] subviews, Rect rect, int span, double hSpacing, double vSpacing, bool isHorizontal)
    {
        if (isHorizontal)
        {
            // Horizontal grid: span = number of rows, items flow left-to-right
            var rowHeight = ((nfloat)rect.Height - (nfloat)vSpacing * (span - 1)) / span;
            if (rowHeight < 20) rowHeight = 20;
            var colWidth = rowHeight; // square cells by default

            nfloat x = 0;
            int col = 0;
            for (int i = 0; i < subviews.Length; i++)
            {
                int row = i % span;
                if (row == 0 && i > 0)
                {
                    x += colWidth + (nfloat)hSpacing;
                    col++;
                }
                var y = row * (rowHeight + (nfloat)vSpacing);
                // Let items specify their own width
                colWidth = MeasureItemWidth(subviews[i], rowHeight);
                subviews[i].Frame = new CGRect(x, y, colWidth, rowHeight);
            }

            var totalWidth = x + colWidth;
            _itemsContainer!.Frame = new CGRect(0, 0, totalWidth, rect.Height);
            _documentView!.Frame = new CGRect(0, 0, totalWidth, rect.Height);
        }
        else
        {
            // Vertical grid: span = number of columns, items flow top-to-bottom
            var totalHSpacing = (nfloat)hSpacing * (span - 1);
            var colWidth = ((nfloat)rect.Width - totalHSpacing) / span;
            if (colWidth < 20) colWidth = 20;

            nfloat maxY = 0;
            var colTops = new nfloat[span];

            for (int i = 0; i < subviews.Length; i++)
            {
                int col = i % span;
                var x = col * (colWidth + (nfloat)hSpacing);
                var itemHeight = MeasureItemHeight(subviews[i], colWidth);

                subviews[i].Frame = new CGRect(x, colTops[col], colWidth, itemHeight);
                colTops[col] += itemHeight + (nfloat)vSpacing;
                if (colTops[col] > maxY) maxY = colTops[col];
            }

            _itemsContainer!.Frame = new CGRect(0, 0, rect.Width, maxY);
            _documentView!.Frame = new CGRect(0, 0, rect.Width, maxY);
        }
    }

    static nfloat MeasureItemHeight(NSView subview, nfloat width)
    {
        var fittingSize = subview is MacOSContainerView container
            ? container.SizeThatFits(new CGSize(width, nfloat.MaxValue))
            : subview.IntrinsicContentSize;
        var height = fittingSize.Height > 0 && fittingSize.Height < 10000
            ? fittingSize.Height : (nfloat)44;
        return height;
    }

    static nfloat MeasureItemWidth(NSView subview, nfloat height)
    {
        var fittingSize = subview is MacOSContainerView container
            ? container.SizeThatFits(new CGSize(nfloat.MaxValue, height))
            : subview.IntrinsicContentSize;
        var width = fittingSize.Width > 0 && fittingSize.Width < 10000
            ? fittingSize.Width : (nfloat)120;
        return width;
    }

    static ItemsLayoutOrientation GetOrientation(IItemsLayout? layout) => layout switch
    {
        LinearItemsLayout linear => linear.Orientation,
        GridItemsLayout grid => grid.Orientation,
        _ => ItemsLayoutOrientation.Vertical,
    };

    static int GetSpan(IItemsLayout? layout) => layout is GridItemsLayout grid ? grid.Span : 1;

    static double GetItemSpacing(IItemsLayout? layout) => layout switch
    {
        LinearItemsLayout linear => linear.ItemSpacing,
        GridItemsLayout grid => grid.HorizontalItemSpacing,
        _ => 0,
    };

    static double GetLineSpacing(IItemsLayout? layout) => layout switch
    {
        GridItemsLayout grid => grid.VerticalItemSpacing,
        _ => 0,
    };

    #endregion

    #region Property Mappers

    public static void MapItemsSource(CollectionViewHandler handler, CollectionView view)
        => handler.ReloadItems();

    public static void MapItemTemplate(CollectionViewHandler handler, CollectionView view)
        => handler.ReloadItems();

    public static void MapItemsLayout(CollectionViewHandler handler, CollectionView view)
    {
        // Update scroll direction based on layout orientation
        var layout = view.ItemsLayout;
        var isHorizontal = GetOrientation(layout) == ItemsLayoutOrientation.Horizontal;
        handler.PlatformView.HasVerticalScroller = !isHorizontal;
        handler.PlatformView.HasHorizontalScroller = isHorizontal;

        if (handler.PlatformView.Frame.Width > 0)
            handler.LayoutItems(new Rect(0, 0, handler.PlatformView.Frame.Width, handler.PlatformView.Frame.Height));
    }

    public static void MapSelectionMode(CollectionViewHandler handler, CollectionView view) { }
    public static void MapSelectedItem(CollectionViewHandler handler, CollectionView view) { }
    public static void MapIsGrouped(CollectionViewHandler handler, CollectionView view)
        => handler.ReloadItems();
    public static void MapGroupHeaderTemplate(CollectionViewHandler handler, CollectionView view)
        => handler.ReloadItems();
    public static void MapGroupFooterTemplate(CollectionViewHandler handler, CollectionView view)
        => handler.ReloadItems();

    #endregion

    #region Data Loading

    void UnsubscribeCollection()
    {
        if (_observableSource != null)
        {
            _observableSource.CollectionChanged -= OnCollectionChanged;
            _observableSource = null;
        }
    }

    void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => ReloadItems();

    void ReloadItems()
    {
        if (_itemsContainer == null || MauiContext == null)
            return;

        UnsubscribeCollection();
        _itemEntries.Clear();

        foreach (var subview in _itemsContainer.Subviews)
            subview.RemoveFromSuperview();

        var itemsSource = VirtualView?.ItemsSource;
        if (itemsSource == null)
            return;

        if (itemsSource is INotifyCollectionChanged observable)
        {
            _observableSource = observable;
            _observableSource.CollectionChanged += OnCollectionChanged;
        }

        var isGrouped = (VirtualView as GroupableItemsView)?.IsGrouped ?? false;
        var template = VirtualView?.ItemTemplate;
        var groupHeaderTemplate = (VirtualView as GroupableItemsView)?.GroupHeaderTemplate;
        var groupFooterTemplate = (VirtualView as GroupableItemsView)?.GroupFooterTemplate;

        int flatIndex = 0;

        if (isGrouped)
        {
            foreach (var group in itemsSource)
            {
                // Group header
                if (groupHeaderTemplate != null)
                    AddTemplatedView(group, groupHeaderTemplate, -1);

                // Group items
                if (group is IEnumerable groupItems)
                {
                    foreach (var item in groupItems)
                        AddItemView(item, template, flatIndex++);
                }

                // Group footer
                if (groupFooterTemplate != null)
                    AddTemplatedView(group, groupFooterTemplate, -1);
            }
        }
        else
        {
            foreach (var item in itemsSource)
                AddItemView(item, template, flatIndex++);
        }

        if (PlatformView.Frame.Width > 0)
            LayoutItems(new Rect(0, 0, PlatformView.Frame.Width, PlatformView.Frame.Height));
    }

    void AddItemView(object item, DataTemplate? template, int flatIndex)
    {
        var view = CreateItemView(item, template, VirtualView);
        if (view != null)
        {
            var platformView = view.ToMacOSPlatform(MauiContext!);
            _itemsContainer!.AddSubview(platformView);
            _itemEntries.Add((platformView, item, flatIndex));

            // Add tap gesture for selection
            AddSelectionGesture(platformView, item, flatIndex);
        }
    }

    void AddTemplatedView(object bindingContext, DataTemplate template, int flatIndex)
    {
        var content = template.CreateContent();
        if (content is View view)
        {
            view.BindingContext = bindingContext;
            var platformView = ((IView)view).ToMacOSPlatform(MauiContext!);
            _itemsContainer!.AddSubview(platformView);
        }
    }

    void AddSelectionGesture(NSView platformView, object item, int flatIndex)
    {
        var selectionMode = (VirtualView as SelectableItemsView)?.SelectionMode ?? SelectionMode.None;
        if (selectionMode == SelectionMode.None)
            return;

        var clickRecognizer = new NSClickGestureRecognizer(() =>
        {
            if (VirtualView is SelectableItemsView selectable)
            {
                if (selectionMode == SelectionMode.Single)
                {
                    selectable.SelectedItem = item;
                    _selectedFlatIndex = flatIndex;
                }
            }
        });
        platformView.AddGestureRecognizer(clickRecognizer);
    }

    static IView? CreateItemView(object item, DataTemplate? template, CollectionView? collectionView)
    {
        if (template is DataTemplateSelector selector && collectionView != null)
        {
            var selectedTemplate = selector.SelectTemplate(item, collectionView);
            if (selectedTemplate != null)
            {
                var content = selectedTemplate.CreateContent();
                if (content is View view)
                {
                    view.BindingContext = item;
                    return view;
                }
            }
        }
        else if (template != null)
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

    #endregion
}

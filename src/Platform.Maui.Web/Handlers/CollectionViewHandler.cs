using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web.Interop;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// CollectionView in a scrolling element. Items are MAUI views created from the item templates and
/// positioned by this handler according to the LinearItemsLayout or GridItemsLayout.
/// Views are reused per data item across source changes. Every item is realized (no virtualization),
/// so very large sources should be paged with RemainingItemsThreshold.
/// </summary>
public class CollectionViewHandler : WebElementHandler<ItemsView, WebElement>
{
	public static IPropertyMapper<ItemsView, CollectionViewHandler> Mapper =
		new PropertyMapper<ItemsView, CollectionViewHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(ItemsView.ItemsSource)] = MapItemsSource,
			[nameof(ItemsView.ItemTemplate)] = MapRebuild,
			[nameof(StructuredItemsView.ItemsLayout)] = MapItemsLayout,
			[nameof(StructuredItemsView.ItemSizingStrategy)] = MapRelayout,
			[nameof(GroupableItemsView.IsGrouped)] = MapItemsSource,
			[nameof(GroupableItemsView.GroupHeaderTemplate)] = MapRebuild,
			[nameof(GroupableItemsView.GroupFooterTemplate)] = MapRebuild,
			[nameof(StructuredItemsView.Header)] = MapRebuild,
			[nameof(StructuredItemsView.HeaderTemplate)] = MapRebuild,
			[nameof(StructuredItemsView.Footer)] = MapRebuild,
			[nameof(StructuredItemsView.FooterTemplate)] = MapRebuild,
			[nameof(ItemsView.EmptyView)] = MapRebuild,
			[nameof(ItemsView.EmptyViewTemplate)] = MapRebuild,
			[nameof(SelectableItemsView.SelectionMode)] = MapSelection,
			[nameof(SelectableItemsView.SelectedItem)] = MapSelection,
			[nameof(SelectableItemsView.SelectedItems)] = MapSelection,
			[nameof(ItemsView.HorizontalScrollBarVisibility)] = MapScrollBars,
			[nameof(ItemsView.VerticalScrollBarVisibility)] = MapScrollBars,
		};

	public CollectionViewHandler() : base(Mapper)
	{
	}

	protected CollectionViewHandler(IPropertyMapper mapper) : base(mapper)
	{
	}

	enum Kind { Header, Footer, Empty, GroupHeader, GroupFooter, Item }

	sealed record Realized(Kind Kind, object? Data, View View, WebElement Element, int Index);

	readonly List<Realized> _realized = new();
	readonly Dictionary<object, (View View, WebElement Element)> _viewCache = new(ReferenceEqualityComparer.Instance);
	readonly List<IDisposable> _itemSubscriptions = new();
	readonly List<INotifyCollectionChanged> _observedCollections = new();
	ItemsLayout? _observedLayout;
	bool _rebuildPending;
	int _thresholdCount = -1;
	double _contentHeight, _contentWidth;

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-collection");
		root.SetAttribute("role", "list");
		return root;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		VirtualView.ScrollToRequested += OnScrollToRequested;
		Track(platformView.OnScroll(OnScrolled));
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		VirtualView.ScrollToRequested -= OnScrollToRequested;
		ObserveSource(null);
		ObserveLayout(null);
		ClearRealized(dropCache: true);
		base.DisconnectHandler(platformView);
	}

	protected bool IsVertical => ItemsLayout is not LinearItemsLayout { Orientation: ItemsLayoutOrientation.Horizontal }
		and not GridItemsLayout { Orientation: ItemsLayoutOrientation.Horizontal };

	protected virtual IItemsLayout? ItemsLayout => (VirtualView as StructuredItemsView)?.ItemsLayout;

	/// <summary>A fixed main-axis size for every item (e.g. a carousel page), or null to measure.</summary>
	protected virtual double? FixedItemExtent(double viewportMain) => null;

	/// <summary>The data items in display order.</summary>
	protected IEnumerable<object?> RealizedItems => _realized.Where(r => r.Kind == Kind.Item).Select(r => r.Data);

	/// <summary>The elements of the realized data items, in display order.</summary>
	protected IEnumerable<WebElement> RealizedItemElements => _realized.Where(r => r.Kind == Kind.Item).Select(r => r.Element);

	public static void MapItemsSource(CollectionViewHandler handler, ItemsView view)
	{
		handler.ObserveSource(view.ItemsSource);
		handler.ScheduleRebuild();
	}

	public static void MapRebuild(CollectionViewHandler handler, ItemsView view) => handler.ScheduleRebuild();

	public static void MapRelayout(CollectionViewHandler handler, ItemsView view) => WebLayoutScheduler.Request();

	public static void MapItemsLayout(CollectionViewHandler handler, ItemsView view)
	{
		handler.ObserveLayout(handler.ItemsLayout as ItemsLayout);
		WebLayoutScheduler.Request();
	}

	public static void MapScrollBars(CollectionViewHandler handler, ItemsView view)
		=> handler.PlatformView.SetStyle("scrollbar-width",
			view.VerticalScrollBarVisibility == ScrollBarVisibility.Never && view.HorizontalScrollBarVisibility == ScrollBarVisibility.Never ? "none" : null);

	void ObserveLayout(ItemsLayout? layout)
	{
		if (_observedLayout is not null)
			_observedLayout.PropertyChanged -= OnLayoutPropertyChanged;
		_observedLayout = layout;
		if (layout is not null)
			layout.PropertyChanged += OnLayoutPropertyChanged;
	}

	void OnLayoutPropertyChanged(object? sender, PropertyChangedEventArgs e) => WebLayoutScheduler.Request();

	void ObserveSource(IEnumerable? source)
	{
		foreach (var collection in _observedCollections)
			collection.CollectionChanged -= OnSourceChanged;
		_observedCollections.Clear();

		if (source is INotifyCollectionChanged observable)
			Add(observable);

		// Grouped sources change at both levels.
		if (source is not null && VirtualView is GroupableItemsView { IsGrouped: true })
		{
			foreach (var group in source)
			{
				if (group is INotifyCollectionChanged inner)
					Add(inner);
			}
		}

		void Add(INotifyCollectionChanged collection)
		{
			collection.CollectionChanged += OnSourceChanged;
			_observedCollections.Add(collection);
		}
	}

	void OnSourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (VirtualView is GroupableItemsView { IsGrouped: true })
			ObserveSource(VirtualView.ItemsSource);
		ScheduleRebuild();
	}

	// Batch bursts of collection changes (e.g. adding items in a loop) into one rebuild.
	void ScheduleRebuild()
	{
		if (_rebuildPending)
			return;
		_rebuildPending = true;
		Dom.Enqueue(() =>
		{
			_rebuildPending = false;
			if (CurrentView is not null && MauiContext is not null)
				Rebuild();
		});
	}

	void ClearRealized(bool dropCache)
	{
		foreach (var s in _itemSubscriptions)
			s.Dispose();
		_itemSubscriptions.Clear();
		foreach (var r in _realized)
			r.Element.Detach();
		_realized.Clear();

		if (dropCache)
		{
			foreach (var (view, _) in _viewCache.Values)
				CurrentView?.RemoveLogicalChild(view);
			_viewCache.Clear();
		}
	}

	void Rebuild()
	{
		var view = VirtualView;
		var stale = new Dictionary<object, (View View, WebElement Element)>(_viewCache, ReferenceEqualityComparer.Instance);
		ClearRealized(dropCache: false);
		_viewCache.Clear();

		if (view is StructuredItemsView structured)
			AddChrome(Kind.Header, structured.Header, structured.HeaderTemplate);

		var index = 0;
		if (view.ItemsSource is { } source)
		{
			if (view is GroupableItemsView { IsGrouped: true } groupable)
			{
				foreach (var group in source)
				{
					AddChrome(Kind.GroupHeader, group, groupable.GroupHeaderTemplate);
					if (group is IEnumerable items)
					{
						foreach (var item in items)
							AddItem(item, index++, stale);
					}
					AddChrome(Kind.GroupFooter, group, groupable.GroupFooterTemplate);
				}
			}
			else
			{
				foreach (var item in source)
					AddItem(item, index++, stale);
			}
		}

		if (index == 0)
			AddChrome(Kind.Empty, view.EmptyView, view.EmptyViewTemplate);

		if (view is StructuredItemsView footerView)
			AddChrome(Kind.Footer, footerView.Footer, footerView.FooterTemplate);

		foreach (var (oldView, _) in stale.Values)
			view.RemoveLogicalChild(oldView);

		_thresholdCount = -1;
		UpdateSelection();
		view.InvalidateMeasure();
		WebLayoutScheduler.Request();
	}

	void AddChrome(Kind kind, object? content, DataTemplate? template)
	{
		if (content is null && template is null)
			return;
		// Group header/footer templates only apply when present; the group object itself is not shown.
		if (kind is Kind.GroupHeader or Kind.GroupFooter && template is null)
			return;

		var view = content as View;
		if (view is null && template is not null)
			view = CreateFromTemplate(template, content);
		if (view is null && content is not null)
			view = new Label { Text = content.ToString(), Padding = new Thickness(12, 8) };
		if (view is null)
			return;

		if (kind is Kind.GroupHeader or Kind.GroupFooter)
			view.BindingContext = content;
		Realize(kind, content, view, -1);
	}

	void AddItem(object? item, int index, Dictionary<object, (View View, WebElement Element)> stale)
	{
		View view;
		WebElement element;
		if (item is not null && stale.Remove(item, out var cached))
		{
			(view, element) = cached;
			Append(element);
		}
		else
		{
			var template = VirtualView.ItemTemplate;
			view = (template is null ? null : CreateFromTemplate(template, item))
				?? new Label { Text = item?.ToString(), Padding = new Thickness(12, 8) };
			view.BindingContext = item;
			element = Realize(Kind.Item, item, view, index, cache: false);
		}

		if (item is not null)
			_viewCache[item] = (view, element);
		_realized.Add(new Realized(Kind.Item, item, view, element, index));
		element.SetAttribute("role", "listitem");
		element.AddClass("maui-collection-item");

		var captured = item;
		_itemSubscriptions.Add(element.On("click", () => OnItemClicked(captured)));
	}

	WebElement Realize(Kind kind, object? data, View view, int index, bool cache = true)
	{
		VirtualView.AddLogicalChild(view);
		var element = ToPlatform(view);
		Append(element);
		if (cache)
			_realized.Add(new Realized(kind, data, view, element, index));
		return element;
	}

	void Append(WebElement element) => PlatformView.Append(element);

	View? CreateFromTemplate(DataTemplate template, object? item)
	{
		if (template is DataTemplateSelector selector)
			template = selector.SelectTemplate(item, VirtualView);
#pragma warning disable CS0618 // ViewCell is obsolete, but ListView-era templates still use it
		return template?.CreateContent() switch
		{
			View view => view,
			ViewCell cell => cell.View,
			_ => null,
		};
#pragma warning restore CS0618
	}

	void OnItemClicked(object? item)
	{
		if (VirtualView is not SelectableItemsView selectable)
			return;
		switch (selectable.SelectionMode)
		{
			case SelectionMode.Single:
				selectable.SelectedItem = item;
				break;
			case SelectionMode.Multiple:
				if (selectable.SelectedItems.Contains(item!))
					selectable.SelectedItems.Remove(item!);
				else
					selectable.SelectedItems.Add(item!);
				break;
		}
	}

	public static void MapSelection(CollectionViewHandler handler, ItemsView view) => handler.UpdateSelection();

	void UpdateSelection()
	{
		var selectable = VirtualView as SelectableItemsView;
		var mode = selectable?.SelectionMode ?? SelectionMode.None;
		PlatformView.ToggleClass("selectable", mode != SelectionMode.None);

		foreach (var r in _realized)
		{
			if (r.Kind != Kind.Item)
				continue;
			var selected = mode switch
			{
				SelectionMode.Single => Equals(selectable!.SelectedItem, r.Data),
				SelectionMode.Multiple => r.Data is not null && selectable!.SelectedItems.Contains(r.Data),
				_ => false,
			};
			r.Element.SetAttribute("aria-selected", mode == SelectionMode.None ? null : selected ? "true" : "false");
			VisualStateManager.GoToState(r.View, selected ? VisualStateManager.CommonStates.Selected : VisualStateManager.CommonStates.Normal);
		}
	}

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
	{
		var extent = LayoutItems(double.IsFinite(widthConstraint) ? widthConstraint : 0,
			double.IsFinite(heightConstraint) ? heightConstraint : 0, arrange: false);
		IView view = VirtualView;
		return new Size(
			Layouts.LayoutManager.ResolveConstraints(widthConstraint, view.Width, double.IsFinite(widthConstraint) ? widthConstraint : extent.Width, view.MinimumWidth, view.MaximumWidth),
			Layouts.LayoutManager.ResolveConstraints(heightConstraint, view.Height, double.IsFinite(heightConstraint) ? heightConstraint : extent.Height, view.MinimumHeight, view.MaximumHeight));
	}

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);
		var extent = LayoutItems(frame.Width, frame.Height, arrange: true);
		_contentWidth = extent.Width;
		_contentHeight = extent.Height;
	}

	/// <summary>Measures (and optionally arranges) the realized views, returning the content extent.</summary>
	Size LayoutItems(double width, double height, bool arrange)
	{
		var vertical = IsVertical;
		var (span, itemSpacing, crossSpacing) = ItemsLayout switch
		{
			GridItemsLayout grid => (Math.Max(1, grid.Span), vertical ? grid.VerticalItemSpacing : grid.HorizontalItemSpacing,
				vertical ? grid.HorizontalItemSpacing : grid.VerticalItemSpacing),
			LinearItemsLayout linear => (1, linear.ItemSpacing, 0d),
			_ => (1, 0d, 0d),
		};
		var measureFirst = (VirtualView as StructuredItemsView)?.ItemSizingStrategy == ItemSizingStrategy.MeasureFirstItem;
		var cross = vertical ? width : height;
		var cellCross = span > 1 ? (cross - crossSpacing * (span - 1)) / span : cross;
		var fixedExtent = FixedItemExtent(vertical ? height : width);

		double main = 0, maxCross = 0;
		Size? firstItemSize = null;
		var column = 0;
		double rowExtent = 0;

		foreach (var r in _realized)
		{
			IView view = r.View;
			var isItem = r.Kind == Kind.Item;
			var slotCross = isItem ? cellCross : cross;

			if (!isItem && column > 0)
			{
				// Headers and footers start on a new row.
				main += rowExtent + itemSpacing;
				column = 0;
				rowExtent = 0;
			}

			Size size;
			if (isItem && fixedExtent is { } extent)
			{
				view.Measure(vertical ? slotCross : extent, vertical ? extent : slotCross);
				size = vertical ? new Size(slotCross, extent) : new Size(extent, slotCross);
			}
			else if (isItem && measureFirst && firstItemSize is { } first)
				size = first;
			else
				size = vertical ? view.Measure(slotCross, double.PositiveInfinity) : view.Measure(double.PositiveInfinity, slotCross);
			if (isItem)
				firstItemSize ??= size;

			var mainSize = vertical ? size.Height : size.Width;
			var crossOffset = isItem ? column * (cellCross + crossSpacing) : 0;
			if (arrange)
			{
				var rect = vertical
					? new Rect(crossOffset, main, slotCross, mainSize)
					: new Rect(main, crossOffset, mainSize, slotCross);
				view.Arrange(rect);
			}
			maxCross = Math.Max(maxCross, (vertical ? size.Width : size.Height) + crossOffset);

			if (isItem && span > 1)
			{
				rowExtent = Math.Max(rowExtent, mainSize);
				if (++column == span)
				{
					main += rowExtent + itemSpacing;
					column = 0;
					rowExtent = 0;
				}
			}
			else
			{
				main += mainSize + (isItem ? itemSpacing : 0);
			}
		}

		if (column > 0)
			main += rowExtent;
		else if (main > 0 && itemSpacing > 0)
			main -= itemSpacing;

		return vertical ? new Size(maxCross, main) : new Size(main, maxCross);
	}

	protected virtual void OnScrolled(double x, double y)
	{
		var view = VirtualView;
		var extent = Dom.GetScrollExtent(PlatformView.Js);
		var vertical = IsVertical;
		var offset = vertical ? y : x;
		var viewport = vertical ? extent[3] : extent[2];

		int first = -1, last = -1;
		foreach (var r in _realized)
		{
			if (r.Kind != Kind.Item)
				continue;
			var f = r.View.Frame;
			var start = vertical ? f.Top : f.Left;
			var end = vertical ? f.Bottom : f.Right;
			if (end >= offset && start <= offset + viewport)
			{
				if (first < 0)
					first = r.Index;
				last = r.Index;
			}
		}

		view.SendScrolled(new ItemsViewScrolledEventArgs
		{
			HorizontalOffset = x,
			VerticalOffset = y,
			FirstVisibleItemIndex = first,
			CenterItemIndex = first < 0 ? -1 : (first + last) / 2,
			LastVisibleItemIndex = last,
		});

		var count = _realized.Count(r => r.Kind == Kind.Item);
		if (view.RemainingItemsThreshold >= 0 && last >= 0 && last >= count - 1 - view.RemainingItemsThreshold && _thresholdCount != count)
		{
			// Raised once per item count so a slow loader isn't asked again while scrolling.
			_thresholdCount = count;
			view.SendRemainingItemsThresholdReached();
		}
	}

	protected virtual void OnScrollToRequested(object? sender, ScrollToRequestEventArgs e)
	{
		WebLayoutScheduler.Flush();
		var target = e.Mode == ScrollToMode.Element
			? _realized.FirstOrDefault(r => r.Kind == Kind.Item && Equals(r.Data, e.Item))
			: _realized.Where(r => r.Kind == Kind.Item).ElementAtOrDefault(e.Index);
		if (target is null)
			return;

		var frame = target.View.Frame;
		var vertical = IsVertical;
		var viewport = vertical ? ArrangedFrame.Height : ArrangedFrame.Width;
		var start = vertical ? frame.Top : frame.Left;
		var length = vertical ? frame.Height : frame.Width;
		var offset = e.ScrollToPosition switch
		{
			ScrollToPosition.Center => start - (viewport - length) / 2,
			ScrollToPosition.End => start - viewport + length,
			_ => start,
		};
		var max = Math.Max(0, (vertical ? _contentHeight : _contentWidth) - viewport);
		offset = Math.Clamp(offset, 0, max);
		Dom.ScrollTo(PlatformView.Js, vertical ? 0 : offset, vertical ? offset : 0, e.IsAnimated);
	}
}

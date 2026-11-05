using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web.Interop;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// CollectionView in a scrolling element, virtualized. The source is flattened into entries (header,
/// group headers and footers, items, footer or empty view) laid out in rows by the LinearItemsLayout
/// or GridItemsLayout, but only the entries within about one viewport of the visible area have views.
/// Views come from the templates and are recycled per template as entries scroll in and out; a
/// spacer element gives the scroll area its full length.
/// </summary>
/// <remarks>
/// <para>Entries that have not been measured yet are given an estimated extent (the average of those
/// measured so far), which is replaced as they are realized. When that moves rows above the visible
/// area, the scroll position is adjusted so the visible content stays put.</para>
/// <para>Collection changes from an <see cref="INotifyCollectionChanged"/> source are applied
/// incrementally; a reset, a grouped source or a change that empties or fills the list re-reads the
/// source, which costs nothing in the DOM: the realized views are rebound rather than recreated.</para>
/// <para>Because rows far from the viewport are not in the DOM, the browser's find-in-page only sees
/// the realized rows. Each item carries <c>aria-posinset</c>/<c>aria-setsize</c> so assistive
/// technology still announces its position in the whole list.</para>
/// </remarks>
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

	/// <summary>
	/// One row-level element of the flattened source. <c>Index</c> is the item's position among all
	/// items (or the group's index for group chrome); <c>Group</c> is the item's group, or -1.
	/// </summary>
	readonly record struct Entry(Kind Kind, object? Data, int Index, int Group);

	/// <summary>A view created from a template, bound to one entry at a time.</summary>
	sealed class Slot(View view, WebElement element, object key)
	{
		public readonly View View = view;
		public readonly WebElement Element = element;
		public readonly object Key = key;
		public int Entry = -1;
		public object? Data;
		public bool Hidden;
		public bool DefaultLabel;
		public double Main;
		public IDisposable? Click;
	}

	/// <summary>The inputs that decide where entries go; a change invalidates offsets or extents.</summary>
	readonly record struct Geometry(bool Vertical, int Span, double Spacing, double CrossSpacing,
		double Cross, double CellCross, double? Fixed, bool MeasureFirst);

	const double DefaultEstimate = 44;
	// Pool key for items shown without a template (as a Label of the item's text).
	static readonly object DefaultItemKey = new();

	// The flattened source.
	readonly List<Entry> _entries = new();
	readonly List<double> _extents = new();  // measured main-axis size per entry; NaN until measured
	bool _entriesDirty = true;
	int _itemCount;
	int _firstItemEntry;

	// Rows: entry ranges sharing a main-axis position (one entry, or up to Span grid items).
	readonly List<int> _rowStart = new();
	readonly List<bool> _rowIsItem = new();
	int[] _entryRow = [];
	double[] _rowOffsets = [];
	double[] _rowExtents = [];
	double _total;
	bool _rowsDirty = true, _offsetsDirty = true;
	double _itemEstimate = DefaultEstimate, _chromeEstimate = DefaultEstimate;
	double? _firstItemExtent;
	Geometry? _geometry;
	int _measureGeneration = -1;

	// Views.
	readonly Dictionary<int, Slot> _realized = new();
	readonly Dictionary<object, Stack<Slot>> _pool = new();
	readonly Dictionary<Kind, Slot> _fixed = new();
	readonly List<int> _scratch = new();
	WebElement? _spacer;
	bool _updating;
	bool _realizedChanged;

	// Scrolling.
	double _scrollX, _scrollY;
	int _settleTimer;
	(int Entry, ScrollToPosition Position, bool Animated)? _pendingScroll;
	bool _pendingScrollWaitsForLayout;
	int _thresholdCount = -1;
	double _maxCross;
	bool _crossSizedByContent;

	readonly List<INotifyCollectionChanged> _observedCollections = new();
	ItemsLayout? _observedLayout;

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-collection");
		root.SetAttribute("role", "list");
		return root;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		_spacer = new WebElement("div", "maui-collection-spacer");
		_spacer.SetAttribute("aria-hidden", "true");
		platformView.Append(_spacer);
		VirtualView.ScrollToRequested += OnScrollToRequested;
		Track(platformView.OnScroll(OnScroll));
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		VirtualView.ScrollToRequested -= OnScrollToRequested;
		if (_settleTimer != 0)
			Dom.StopTimeout(_settleTimer);
		_settleTimer = 0;
		ObserveSource(null);
		ObserveLayout(null);
		DropAllViews();
		_spacer?.Detach();
		_spacer = null;
		base.DisconnectHandler(platformView);
	}

	protected bool IsVertical => ItemsLayout is not LinearItemsLayout { Orientation: ItemsLayoutOrientation.Horizontal }
		and not GridItemsLayout { Orientation: ItemsLayoutOrientation.Horizontal };

	protected virtual IItemsLayout? ItemsLayout => (VirtualView as StructuredItemsView)?.ItemsLayout;

	/// <summary>A fixed main-axis size for every item (e.g. a carousel page), or null to measure.</summary>
	protected virtual double? FixedItemExtent(double viewportMain) => null;

	/// <summary>How far beyond the viewport, in viewports, entries stay realized on each side.</summary>
	protected virtual double OverscanFactor => 1;

	/// <summary>
	/// How many times an ungrouped source is repeated end to end (a looping carousel shows copies so it
	/// can scroll past either end). Repeated copies are re-read on every collection change.
	/// </summary>
	protected virtual int ItemCopies => 1;

	/// <summary>Called once for each new item element, e.g. to add scroll snapping.</summary>
	protected virtual void PrepareItemElement(WebElement element)
	{
	}

	/// <summary>Called once scrolling has stopped for a moment.</summary>
	protected virtual void OnScrollSettled()
	{
		if (_pendingScroll is { } pending)
		{
			_pendingScroll = null;
			ScrollToEntry(pending.Entry, pending.Position, animated: false);
		}
	}

	/// <summary>The number of data items (one copy of the source).</summary>
	protected int ItemCount
	{
		get
		{
			EnsureModel();
			return _itemCount;
		}
	}

	/// <summary>The number of entries, including repeated copies, headers and footers.</summary>
	protected int EntryCount
	{
		get
		{
			EnsureModel();
			return _entries.Count;
		}
	}

	/// <summary>The item index shown by an entry, or -1 for a header, footer or group chrome.</summary>
	protected int ItemIndexOfEntry(int entry)
	{
		EnsureModel();
		return (uint)entry < (uint)_entries.Count && _entries[entry] is { Kind: Kind.Item } e ? e.Index : -1;
	}

	/// <summary>The data item at an item index, or null.</summary>
	protected object? ItemAt(int index)
	{
		var entry = EntryOfItem(index);
		return entry < 0 ? null : _entries[entry].Data;
	}

	/// <summary>The index of the first item equal to <paramref name="item"/>, or -1.</summary>
	protected int IndexOfItem(object? item)
	{
		EnsureModel();
		foreach (var e in _entries)
		{
			if (e.Kind == Kind.Item && Equals(e.Data, item))
				return e.Index;
		}
		return -1;
	}

	/// <summary>The length of the content along the layout's main axis (partly estimated).</summary>
	protected double ContentExtent => _total;

	/// <summary>The scroll offset along the layout's main axis.</summary>
	protected double ScrollOffset => IsVertical ? _scrollY : _scrollX;

	/// <summary>Scrolls along the main axis; an instant scroll realizes the new range immediately.</summary>
	protected void ScrollToOffset(double offset, bool animated)
	{
		var vertical = IsVertical;
		if (!animated)
		{
			// Realize the destination first: the browser snaps (carousel) and clamps against the
			// elements that exist when the scroll happens.
			if (vertical)
				_scrollY = offset;
			else
				_scrollX = offset;
			RefreshViewport();
			offset = vertical ? _scrollY : _scrollX;
		}
		Dom.ScrollTo(PlatformView.Js, vertical ? 0 : offset, vertical ? offset : 0, animated);
	}

	/// <summary>Re-reads the source on the next layout pass.</summary>
	protected void InvalidateItems() => MarkEntriesDirty();

	/// <summary>Realizes and arranges entries for the current scroll position, outside a layout pass.</summary>
	protected void RefreshViewport()
	{
		var frame = ArrangedFrame;
		if (frame.Width > 0 || frame.Height > 0)
			UpdateViewport(frame.Width, frame.Height);
	}

	// ---- property mapping ---------------------------------------------------

	public static void MapItemsSource(CollectionViewHandler handler, ItemsView view)
	{
		handler.ObserveSource(view.ItemsSource);
		handler.MarkEntriesDirty();
	}

	public static void MapRebuild(CollectionViewHandler handler, ItemsView view)
	{
		// Templates or chrome changed: existing views can't be rebound.
		handler.DropAllViews();
		handler.MarkEntriesDirty();
	}

	public static void MapRelayout(CollectionViewHandler handler, ItemsView view) => WebLayoutScheduler.Request();

	public static void MapItemsLayout(CollectionViewHandler handler, ItemsView view)
	{
		handler.ObserveLayout(handler.ItemsLayout as ItemsLayout);
		WebLayoutScheduler.Request();
	}

	public static void MapScrollBars(CollectionViewHandler handler, ItemsView view)
		=> handler.PlatformView.SetStyle("scrollbar-width",
			view.VerticalScrollBarVisibility == ScrollBarVisibility.Never && view.HorizontalScrollBarVisibility == ScrollBarVisibility.Never ? "none" : null);

	public static void MapSelection(CollectionViewHandler handler, ItemsView view) => handler.UpdateSelection();

	void ObserveLayout(ItemsLayout? layout)
	{
		if (_observedLayout is not null)
			_observedLayout.PropertyChanged -= OnLayoutPropertyChanged;
		_observedLayout = layout;
		if (layout is not null)
			layout.PropertyChanged += OnLayoutPropertyChanged;
	}

	void OnLayoutPropertyChanged(object? sender, PropertyChangedEventArgs e) => WebLayoutScheduler.Request();

	// ---- source -------------------------------------------------------------

	bool IsGrouped => VirtualView is GroupableItemsView { IsGrouped: true };

	void ObserveSource(IEnumerable? source)
	{
		foreach (var collection in _observedCollections)
			collection.CollectionChanged -= OnSourceChanged;
		_observedCollections.Clear();

		if (source is INotifyCollectionChanged observable)
			Add(observable);

		// Grouped sources change at both levels.
		if (source is not null && IsGrouped)
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
		if (IsGrouped)
		{
			ObserveSource(VirtualView.ItemsSource);
			MarkEntriesDirty();
			return;
		}

		if (_entriesDirty || ItemCopies != 1 || !ReferenceEquals(sender, VirtualView.ItemsSource) || !TryApplyChange(e))
		{
			MarkEntriesDirty();
			return;
		}

		_thresholdCount = -1;
		_rowsDirty = _offsetsDirty = true;
		RequestLayout();
	}

	void MarkEntriesDirty()
	{
		_entriesDirty = true;
		RequestLayout();
	}

	void RequestLayout()
	{
		CurrentView?.InvalidateMeasure();
		WebLayoutScheduler.Request();
	}

	/// <summary>Applies a change to an ungrouped source in place; false when the source must be re-read.</summary>
	bool TryApplyChange(NotifyCollectionChangedEventArgs e)
	{
		switch (e.Action)
		{
			case NotifyCollectionChangedAction.Add when e.NewItems is { Count: > 0 } added && e.NewStartingIndex >= 0 && _itemCount > 0:
				return Insert(e.NewStartingIndex, added);

			case NotifyCollectionChangedAction.Remove when e.OldItems is { Count: > 0 } removed && e.OldStartingIndex >= 0 && _itemCount > removed.Count:
				return RemoveRange(e.OldStartingIndex, removed.Count);

			case NotifyCollectionChangedAction.Replace when e.NewItems is { } items && e.OldItems?.Count == items.Count && e.NewStartingIndex >= 0:
				if (e.NewStartingIndex + items.Count > _itemCount)
					return false;
				for (var i = 0; i < items.Count; i++)
				{
					var at = _firstItemEntry + e.NewStartingIndex + i;
					_entries[at] = _entries[at] with { Data = items[i] };
					_extents[at] = double.NaN;
				}
				return true;

			case NotifyCollectionChangedAction.Move when e.NewItems is { Count: > 0 } moved && e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0:
				return RemoveRange(e.OldStartingIndex, moved.Count, allowEmpty: true) && Insert(e.NewStartingIndex, moved, allowFromEmpty: true);

			default:
				return false;
		}
	}

	bool Insert(int index, IList items, bool allowFromEmpty = false)
	{
		if (index > _itemCount || (_itemCount == 0 && !allowFromEmpty))
			return false;
		var at = _firstItemEntry + index;
		var inserted = new Entry[items.Count];
		for (var i = 0; i < items.Count; i++)
			inserted[i] = new Entry(Kind.Item, items[i], index + i, -1);
		_entries.InsertRange(at, inserted);
		_extents.InsertRange(at, Enumerable.Repeat(double.NaN, items.Count));
		_itemCount += items.Count;
		ShiftRealized(at, items.Count);
		Renumber(at + items.Count);
		return true;
	}

	bool RemoveRange(int index, int count, bool allowEmpty = false)
	{
		if (index + count > _itemCount || (_itemCount == count && !allowEmpty))
			return false;
		var at = _firstItemEntry + index;
		for (var e = at; e < at + count; e++)
		{
			if (_realized.Remove(e, out var slot))
				Release(slot);
		}
		_entries.RemoveRange(at, count);
		_extents.RemoveRange(at, count);
		_itemCount -= count;
		ShiftRealized(at + count, -count);
		Renumber(at);
		return true;
	}

	/// <summary>Moves realized slots at or after <paramref name="from"/> by <paramref name="delta"/> entries.</summary>
	void ShiftRealized(int from, int delta)
	{
		_scratch.Clear();
		foreach (var e in _realized.Keys)
		{
			if (e >= from)
				_scratch.Add(e);
		}
		var moved = _scratch.Select(e => (e, _realized[e])).ToList();
		foreach (var (e, _) in moved)
			_realized.Remove(e);
		foreach (var (e, slot) in moved)
		{
			slot.Entry = e + delta;
			_realized[e + delta] = slot;
		}
	}

	void Renumber(int from)
	{
		for (var e = from; e < _entries.Count; e++)
		{
			if (_entries[e].Kind == Kind.Item)
				_entries[e] = _entries[e] with { Index = e - _firstItemEntry };
		}
	}

	/// <summary>Re-reads the source into entries if it changed. Realized views are kept for rebinding.</summary>
	void EnsureModel()
	{
		if (!_entriesDirty || CurrentView is not { } view)
			return;
		_entriesDirty = false;

		foreach (var slot in _realized.Values)
			Release(slot);
		_realized.Clear();
		_entries.Clear();
		_itemCount = 0;

		if (view is StructuredItemsView { Header: not null } or StructuredItemsView { HeaderTemplate: not null })
			_entries.Add(new Entry(Kind.Header, null, -1, -1));
		_firstItemEntry = _entries.Count;

		if (view.ItemsSource is { } source)
		{
			if (view is GroupableItemsView { IsGrouped: true } groupable)
			{
				var groupIndex = 0;
				foreach (var group in source)
				{
					if (groupable.GroupHeaderTemplate is not null)
						_entries.Add(new Entry(Kind.GroupHeader, group, groupIndex, groupIndex));
					if (group is IEnumerable items)
					{
						foreach (var item in items)
							_entries.Add(new Entry(Kind.Item, item, _itemCount++, groupIndex));
					}
					if (groupable.GroupFooterTemplate is not null)
						_entries.Add(new Entry(Kind.GroupFooter, group, groupIndex, groupIndex));
					groupIndex++;
				}
			}
			else
			{
				foreach (var item in source)
					_entries.Add(new Entry(Kind.Item, item, _itemCount++, -1));

				var copies = _itemCount > 0 ? Math.Max(1, ItemCopies) : 1;
				for (var copy = 1; copy < copies; copy++)
				{
					for (var i = 0; i < _itemCount; i++)
						_entries.Add(_entries[_firstItemEntry + i]);
				}
			}
		}

		if (_itemCount == 0 && (view.EmptyView is not null || view.EmptyViewTemplate is not null))
			_entries.Add(new Entry(Kind.Empty, null, -1, -1));

		if (view is StructuredItemsView { Footer: not null } or StructuredItemsView { FooterTemplate: not null })
			_entries.Add(new Entry(Kind.Footer, null, -1, -1));

		_extents.Clear();
		_extents.AddRange(Enumerable.Repeat(double.NaN, _entries.Count));
		_firstItemExtent = null;
		_maxCross = 0;
		_thresholdCount = -1;
		_rowsDirty = _offsetsDirty = true;
	}

	int EntryOfItem(int index)
	{
		EnsureModel();
		if (index < 0 || index >= _itemCount)
			return -1;
		if (!IsGrouped)
			return _firstItemEntry + index;
		for (var e = 0; e < _entries.Count; e++)
		{
			if (_entries[e] is { Kind: Kind.Item } entry && entry.Index == index)
				return e;
		}
		return -1;
	}

	// ---- geometry -----------------------------------------------------------

	Geometry MakeGeometry(double width, double height)
	{
		var vertical = IsVertical;
		var (span, spacing, crossSpacing) = ItemsLayout switch
		{
			GridItemsLayout grid => (Math.Max(1, grid.Span), vertical ? grid.VerticalItemSpacing : grid.HorizontalItemSpacing,
				vertical ? grid.HorizontalItemSpacing : grid.VerticalItemSpacing),
			LinearItemsLayout linear => (1, linear.ItemSpacing, 0d),
			_ => (1, 0d, 0d),
		};
		var cross = vertical ? width : height;
		var cellCross = span > 1 ? Math.Max(0, (cross - crossSpacing * (span - 1)) / span) : cross;
		var measureFirst = (VirtualView as StructuredItemsView)?.ItemSizingStrategy == ItemSizingStrategy.MeasureFirstItem;
		// With no bound along the scroll axis there is no viewport to size pages by: measure them instead.
		var viewportMain = vertical ? height : width;
		var fixedExtent = double.IsFinite(viewportMain) ? FixedItemExtent(viewportMain) : null;
		return new Geometry(vertical, span, spacing, crossSpacing, cross, cellCross, fixedExtent, measureFirst);
	}

	/// <summary>Switches to a geometry, discarding whatever it invalidates.</summary>
	void UseGeometry(Geometry g)
	{
		if (_measureGeneration != WebElementHandler.MeasureGeneration)
		{
			_measureGeneration = WebElementHandler.MeasureGeneration;
			ClearExtents();
		}
		if (_geometry is { } old && old == g)
			return;
		if (_geometry is not { } previous || previous.Vertical != g.Vertical || previous.Span != g.Span
			|| previous.CellCross != g.CellCross || previous.Cross != g.Cross || previous.MeasureFirst != g.MeasureFirst)
			ClearExtents();
		if (_geometry?.Span != g.Span)
			_rowsDirty = true;
		_geometry = g;
		_offsetsDirty = true;
	}

	void ClearExtents()
	{
		for (var i = 0; i < _extents.Count; i++)
			_extents[i] = double.NaN;
		_firstItemExtent = null;
		_offsetsDirty = true;
	}

	void EnsureRows(int span)
	{
		if (!_rowsDirty)
			return;
		_rowsDirty = false;
		_offsetsDirty = true;
		_rowStart.Clear();
		_rowIsItem.Clear();
		if (_entryRow.Length != _entries.Count)
			_entryRow = new int[_entries.Count];

		var column = 0;
		for (var e = 0; e < _entries.Count; e++)
		{
			if (_entries[e].Kind == Kind.Item)
			{
				if (column == 0)
				{
					_rowStart.Add(e);
					_rowIsItem.Add(true);
				}
				column = (column + 1) % span;
			}
			else
			{
				// Headers, footers and group chrome take a row of their own.
				_rowStart.Add(e);
				_rowIsItem.Add(false);
				column = 0;
			}
			_entryRow[e] = _rowStart.Count - 1;
		}

		if (_rowOffsets.Length != _rowStart.Count)
		{
			_rowOffsets = new double[_rowStart.Count];
			_rowExtents = new double[_rowStart.Count];
		}
	}

	int RowEnd(int row) => row + 1 < _rowStart.Count ? _rowStart[row + 1] : _entries.Count;

	double ExtentOf(int e, in Geometry g)
	{
		var isItem = _entries[e].Kind == Kind.Item;
		if (isItem && g.Fixed is { } fixedExtent)
			return fixedExtent;
		if (isItem && g.MeasureFirst && _firstItemExtent is { } first)
			return first;
		var measured = _extents[e];
		return !double.IsNaN(measured) ? measured : isItem ? _itemEstimate : _chromeEstimate;
	}

	void EnsureOffsets(in Geometry g)
	{
		EnsureRows(g.Span);
		if (!_offsetsDirty)
			return;
		_offsetsDirty = false;

		double itemSum = 0, chromeSum = 0;
		int itemCount = 0, chromeCount = 0;
		for (var e = 0; e < _entries.Count; e++)
		{
			var measured = _extents[e];
			if (double.IsNaN(measured))
				continue;
			if (_entries[e].Kind == Kind.Item)
			{
				itemSum += measured;
				itemCount++;
			}
			else
			{
				chromeSum += measured;
				chromeCount++;
			}
		}
		_itemEstimate = g.Fixed ?? (g.MeasureFirst ? _firstItemExtent : null)
			?? (itemCount > 0 ? itemSum / itemCount : chromeCount > 0 ? chromeSum / chromeCount : DefaultEstimate);
		_chromeEstimate = chromeCount > 0 ? chromeSum / chromeCount : _itemEstimate;

		double main = 0;
		var rows = _rowStart.Count;
		for (var r = 0; r < rows; r++)
		{
			double extent = 0;
			for (int e = _rowStart[r], end = RowEnd(r); e < end; e++)
				extent = Math.Max(extent, ExtentOf(e, g));
			_rowOffsets[r] = main;
			_rowExtents[r] = extent;
			main += extent + (_rowIsItem[r] ? g.Spacing : 0);
		}
		if (rows > 0 && _rowIsItem[rows - 1])
			main -= g.Spacing;
		_total = Math.Max(0, main);
	}

	/// <summary>The last row starting at or before <paramref name="offset"/>, or -1 when there are none.</summary>
	int RowAt(double offset)
	{
		int lo = 0, hi = _rowStart.Count - 1, found = _rowStart.Count > 0 ? 0 : -1;
		while (lo <= hi)
		{
			var mid = (lo + hi) >>> 1;
			if (_rowOffsets[mid] <= offset)
			{
				found = mid;
				lo = mid + 1;
			}
			else
				hi = mid - 1;
		}
		return found;
	}

	/// <summary>The rows overlapping [start, end]; empty (first &gt; last) when there are none.</summary>
	(int First, int Last) RowRange(double start, double end)
	{
		if (_rowStart.Count == 0)
			return (0, -1);
		var first = RowAt(start);
		if (first >= 0 && _rowOffsets[first] + _rowExtents[first] < start && first + 1 < _rowStart.Count)
			first++;
		var last = RowAt(end);
		return (Math.Max(0, first), last);
	}

	// ---- views --------------------------------------------------------------

	/// <summary>Makes the realized set exactly the entries of rows [first, last].</summary>
	void RealizeRows(int first, int last)
	{
		var e0 = first <= last ? _rowStart[first] : 0;
		var e1 = first <= last ? RowEnd(last) : 0;

		_scratch.Clear();
		foreach (var (e, slot) in _realized)
		{
			if (e < e0 || e >= e1 || !Equals(slot.Data, _entries[e].Data))
				_scratch.Add(e);
		}
		foreach (var e in _scratch)
		{
			Release(_realized[e]);
			_realized.Remove(e);
		}

		for (var e = e0; e < e1; e++)
		{
			if (!_realized.ContainsKey(e) && Acquire(e) is { } slot)
			{
				_realized[e] = slot;
				_realizedChanged = true;
			}
		}
	}

	Slot? Acquire(int e)
	{
		var entry = _entries[e];
		var view = VirtualView;
		Slot? slot;

		if (entry.Kind is Kind.Header or Kind.Footer or Kind.Empty)
		{
			if (!_fixed.TryGetValue(entry.Kind, out slot))
			{
				if (CreateChrome(entry.Kind) is not { } created)
					return null;
				_fixed[entry.Kind] = slot = created;
			}
		}
		else
		{
			var template = entry.Kind switch
			{
				Kind.GroupHeader => (view as GroupableItemsView)?.GroupHeaderTemplate,
				Kind.GroupFooter => (view as GroupableItemsView)?.GroupFooterTemplate,
				_ => view.ItemTemplate,
			};
			if (template is DataTemplateSelector selector)
				template = selector.SelectTemplate(entry.Data, view);
			var key = (object?)template ?? DefaultItemKey;

			if (_pool.TryGetValue(key, out var pool) && pool.Count > 0)
				slot = pool.Pop();
			else
				slot = CreateSlot(entry, template, key);
		}

		Bind(slot, e, entry);
		if (slot.Hidden)
		{
			slot.Hidden = false;
			slot.Element.RemoveClass("maui-recycled");
		}
		return slot;
	}

	Slot CreateSlot(Entry entry, DataTemplate? template, object key)
	{
		var view = template is null ? null : CreateFromTemplate(template);
		var defaultLabel = view is null;
		view ??= new Label { Padding = new Thickness(12, 8) };
		// Bind before joining the logical tree so the view never inherits the ItemsView's context.
		view.BindingContext = entry.Data;
		if (defaultLabel)
			((Label)view).Text = entry.Data?.ToString();
		VirtualView.AddLogicalChild(view);

		var element = ToPlatform(view);
		PlatformView.Append(element);
		var slot = new Slot(view, element, key) { DefaultLabel = defaultLabel, Data = entry.Data };
		if (entry.Kind == Kind.Item)
		{
			element.SetAttribute("role", "listitem");
			element.AddClass("maui-collection-item");
			slot.Click = element.On("click", () => OnItemClicked(slot));
			PrepareItemElement(element);
		}
		return slot;
	}

	Slot? CreateChrome(Kind kind)
	{
		var view = VirtualView;
		var (content, template) = kind switch
		{
			Kind.Header => ((view as StructuredItemsView)?.Header, (view as StructuredItemsView)?.HeaderTemplate),
			Kind.Footer => ((view as StructuredItemsView)?.Footer, (view as StructuredItemsView)?.FooterTemplate),
			_ => (view.EmptyView, view.EmptyViewTemplate),
		};

		var chrome = content as View;
		if (chrome is null && template is not null)
		{
			if (template is DataTemplateSelector selector)
				template = selector.SelectTemplate(content, view);
			chrome = template is null ? null : CreateFromTemplate(template);
			if (chrome is not null && content is not null)
				chrome.BindingContext = content;
		}
		if (chrome is null && content is not null)
			chrome = new Label { Text = content.ToString(), Padding = new Thickness(12, 8) };
		if (chrome is null)
			return null;

		view.AddLogicalChild(chrome);
		var element = ToPlatform(chrome);
		PlatformView.Append(element);
		return new Slot(chrome, element, kind) { Data = null };
	}

	static View? CreateFromTemplate(DataTemplate template)
	{
#pragma warning disable CS0618 // ViewCell is obsolete, but ListView-era templates still use it
		return template.CreateContent() switch
		{
			View view => view,
			ViewCell cell => cell.View,
			_ => null,
		};
#pragma warning restore CS0618
	}

	void Bind(Slot slot, int e, Entry entry)
	{
		slot.Entry = e;
		if (entry.Kind is not (Kind.Header or Kind.Footer or Kind.Empty) && !ReferenceEquals(slot.View.BindingContext, entry.Data))
		{
			slot.View.BindingContext = entry.Data;
			if (slot.DefaultLabel)
				((Label)slot.View).Text = entry.Data?.ToString();
		}
		slot.Data = entry.Data;

		if (entry.Kind == Kind.Item)
		{
			slot.Element.SetAttribute("aria-posinset", (entry.Index + 1).ToString());
			slot.Element.SetAttribute("aria-setsize", _itemCount.ToString());
			ApplySelection(slot, entry);
		}
	}

	void Release(Slot slot)
	{
		slot.Entry = -1;
		if (!slot.Hidden)
		{
			slot.Hidden = true;
			slot.Element.AddClass("maui-recycled");
		}
		if (slot.Key is Kind)
			return; // header, footer and empty view are kept for reuse
		if (!_pool.TryGetValue(slot.Key, out var pool))
			_pool[slot.Key] = pool = new Stack<Slot>();
		pool.Push(slot);
	}

	/// <summary>Keeps enough spare views to refill the viewport and drops the rest.</summary>
	void TrimPool()
	{
		var keep = Math.Max(16, _realized.Count);
		foreach (var pool in _pool.Values)
		{
			while (pool.Count > keep)
				Destroy(pool.Pop());
		}
	}

	void Destroy(Slot slot)
	{
		slot.Click?.Dispose();
		slot.Element.Detach();
		CurrentView?.RemoveLogicalChild(slot.View);
	}

	void DropAllViews()
	{
		foreach (var slot in _realized.Values.Concat(_pool.Values.SelectMany(p => p)).Concat(_fixed.Values).Distinct())
			Destroy(slot);
		_realized.Clear();
		_pool.Clear();
		_fixed.Clear();
	}

	// ---- selection ----------------------------------------------------------

	void OnItemClicked(Slot slot)
	{
		if (VirtualView is not SelectableItemsView selectable || slot.Entry < 0)
			return;
		var item = slot.Data;
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

	void UpdateSelection()
	{
		var mode = (VirtualView as SelectableItemsView)?.SelectionMode ?? SelectionMode.None;
		PlatformView.ToggleClass("selectable", mode != SelectionMode.None);
		foreach (var (e, slot) in _realized)
		{
			if (_entries[e].Kind == Kind.Item)
				ApplySelection(slot, _entries[e]);
		}
	}

	void ApplySelection(Slot slot, Entry entry)
	{
		var selectable = VirtualView as SelectableItemsView;
		var mode = selectable?.SelectionMode ?? SelectionMode.None;
		var selected = mode switch
		{
			SelectionMode.Single => Equals(selectable!.SelectedItem, entry.Data),
			SelectionMode.Multiple => entry.Data is not null && selectable!.SelectedItems.Contains(entry.Data),
			_ => false,
		};
		slot.Element.SetAttribute("aria-selected", mode == SelectionMode.None ? null : selected ? "true" : "false");
		VisualStateManager.GoToState(slot.View, selected ? VisualStateManager.CommonStates.Selected : VisualStateManager.CommonStates.Normal);
	}

	// ---- layout -------------------------------------------------------------

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
	{
		var vertical = IsVertical;
		var mainConstraint = vertical ? heightConstraint : widthConstraint;
		var crossConstraint = vertical ? widthConstraint : heightConstraint;
		double width = widthConstraint, height = heightConstraint;

		_crossSizedByContent = !double.IsFinite(crossConstraint);
		if (!double.IsFinite(mainConstraint) || !double.IsFinite(crossConstraint))
		{
			var content = MeasureContent(widthConstraint, heightConstraint);
			if (!double.IsFinite(widthConstraint))
				width = content.Width;
			if (!double.IsFinite(heightConstraint))
				height = content.Height;
		}

		IView view = VirtualView;
		return new Size(
			Layouts.LayoutManager.ResolveConstraints(widthConstraint, view.Width, width, view.MinimumWidth, view.MaximumWidth),
			Layouts.LayoutManager.ResolveConstraints(heightConstraint, view.Height, height, view.MinimumHeight, view.MaximumHeight));
	}

	/// <summary>
	/// The content's size for an unbounded constraint. Unbounded along the scroll axis, nothing can be
	/// virtualized: every entry is realized and measured. Unbounded across it, the realized entries
	/// decide the cross size.
	/// </summary>
	Size MeasureContent(double widthConstraint, double heightConstraint)
	{
		EnsureModel();
		var vertical = IsVertical;
		var mainConstraint = vertical ? heightConstraint : widthConstraint;
		var g = MakeGeometry(widthConstraint, heightConstraint);

		if (!double.IsFinite(mainConstraint))
		{
			UseGeometry(g);
			EnsureOffsets(g);
			RealizeRows(0, _rowStart.Count - 1);
			if (MeasureRealized(g, out var cross))
				EnsureOffsets(g);
			_maxCross = Math.Max(_maxCross, cross);
			return vertical ? new Size(_maxCross, _total) : new Size(_total, _maxCross);
		}

		// Bounded along the scroll axis: keep the extents measured at the arranged cross size, and
		// only measure the realized entries' natural cross size.
		var arranged = _geometry ?? g;
		UseGeometry(arranged);
		EnsureOffsets(arranged);
		var scroll = vertical ? _scrollY : _scrollX;
		var overscan = mainConstraint * OverscanFactor;
		var (first, last) = RowRange(scroll - overscan, scroll + mainConstraint + overscan);
		RealizeRows(first, last);
		foreach (var (e, slot) in _realized)
		{
			var size = ((IView)slot.View).Measure(double.PositiveInfinity, double.PositiveInfinity);
			_maxCross = Math.Max(_maxCross, vertical ? size.Width : size.Height);
		}
		return vertical ? new Size(_maxCross, mainConstraint) : new Size(mainConstraint, _maxCross);
	}

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);
		UpdateViewport(frame.Width, frame.Height);

		if (_pendingScrollWaitsForLayout && _pendingScroll is { } pending)
		{
			_pendingScrollWaitsForLayout = false;
			_pendingScroll = null;
			ScrollToEntry(pending.Entry, pending.Position, pending.Animated);
		}
	}

	/// <summary>
	/// Realizes the entries within the viewport and overscan, measures them, and arranges them. Newly
	/// measured entries can move everything after them, so this repeats until the realized range is
	/// stable, keeping the first visible row where it was on screen.
	/// </summary>
	void UpdateViewport(double width, double height)
	{
		if (_updating || CurrentView is null || MauiContext is null)
			return;
		_updating = true;
		try
		{
			EnsureModel();
			var g = MakeGeometry(width, height);
			UseGeometry(g);
			EnsureOffsets(g);

			var viewport = g.Vertical ? height : width;
			var overscan = viewport * OverscanFactor;
			var current = g.Vertical ? _scrollY : _scrollX;
			var scroll = current;
			var anchorRow = RowAt(scroll);
			var anchorDelta = anchorRow >= 0 ? scroll - _rowOffsets[anchorRow] : 0;
			// A smooth programmatic scroll is corrected once it ends; adjusting now would cancel it.
			var anchor = _pendingScroll is not { Animated: true };

			for (var pass = 0; pass < 4; pass++)
			{
				var (first, last) = RowRange(scroll - overscan, scroll + viewport + overscan);
				RealizeRows(first, last);
				if (!MeasureRealized(g, out _))
					break;
				EnsureOffsets(g);
				if (anchor && anchorRow > 0)
					scroll = Math.Max(0, _rowOffsets[anchorRow] + anchorDelta);
			}

			ArrangeRealized(g);
			_spacer?.SetFrame(0, 0, g.Vertical ? 1 : _total, g.Vertical ? _total : 1);

			if (Math.Abs(scroll - current) > 0.5)
			{
				if (g.Vertical)
					_scrollY = scroll;
				else
					_scrollX = scroll;
				Dom.ScrollTo(PlatformView.Js, g.Vertical ? 0 : scroll, g.Vertical ? scroll : 0, false);
			}

			TrimPool();
		}
		finally
		{
			_updating = false;
		}
	}

	/// <summary>Measures the realized entries; true when an extent used for positioning changed.</summary>
	bool MeasureRealized(in Geometry g, out double maxCross)
	{
		var changed = false;
		maxCross = 0;
		foreach (var (e, slot) in _realized)
		{
			IView view = slot.View;
			var isItem = _entries[e].Kind == Kind.Item;
			var slotCross = isItem ? g.CellCross : g.Cross;
			var used = ExtentOf(e, g);
			double main, cross;

			if (isItem && g.Fixed is { } fixedExtent)
			{
				view.Measure(g.Vertical ? slotCross : fixedExtent, g.Vertical ? fixedExtent : slotCross);
				main = fixedExtent;
				cross = slotCross;
			}
			else if (isItem && g.MeasureFirst && _firstItemExtent is { } first)
			{
				main = first;
				cross = slotCross;
			}
			else
			{
				var size = g.Vertical ? view.Measure(slotCross, double.PositiveInfinity) : view.Measure(double.PositiveInfinity, slotCross);
				main = g.Vertical ? size.Height : size.Width;
				cross = g.Vertical ? size.Width : size.Height;
				if (isItem && g.MeasureFirst)
				{
					_firstItemExtent = main;
					_offsetsDirty = changed = true;
				}
				else
				{
					_extents[e] = main;
					if (Math.Abs(main - used) > 0.01)
						_offsetsDirty = changed = true;
				}
			}

			slot.Main = main;
			if (double.IsFinite(cross))
				maxCross = Math.Max(maxCross, cross);
		}
		return changed;
	}

	void ArrangeRealized(in Geometry g)
	{
		foreach (var (e, slot) in _realized)
		{
			var row = _entryRow[e];
			var isItem = _entries[e].Kind == Kind.Item;
			var slotCross = isItem ? g.CellCross : g.Cross;
			var crossOffset = isItem ? (e - _rowStart[row]) * (g.CellCross + g.CrossSpacing) : 0;
			var start = _rowOffsets[row];
			IView view = slot.View;
			view.Arrange(g.Vertical
				? new Rect(crossOffset, start, slotCross, slot.Main)
				: new Rect(start, crossOffset, slot.Main, slotCross));
		}
	}

	// ---- scrolling ----------------------------------------------------------

	void OnScroll(double x, double y)
	{
		_scrollX = x;
		_scrollY = y;
		_realizedChanged = false;
		RefreshViewport();
		// The cross size follows the realized entries when it isn't constrained.
		if (_realizedChanged && _crossSizedByContent)
			RequestLayout();
		RaiseScrolled(x, y);

		if (_settleTimer != 0)
			Dom.StopTimeout(_settleTimer);
		_settleTimer = Dom.StartTimeout(() =>
		{
			_settleTimer = 0;
			if (CurrentView is not null)
				OnScrollSettled();
		}, 120);
	}

	void RaiseScrolled(double x, double y)
	{
		var view = VirtualView;
		var vertical = IsVertical;
		var offset = vertical ? y : x;
		var viewport = vertical ? ArrangedFrame.Height : ArrangedFrame.Width;

		int first = -1, last = -1;
		if (_geometry is { } g)
		{
			EnsureModel();
			EnsureOffsets(g);
		}
		var (firstRow, lastRow) = RowRange(offset, offset + viewport);
		if (firstRow <= lastRow)
		{
			for (int e = _rowStart[firstRow], end = RowEnd(lastRow); e < end; e++)
			{
				if (_entries[e].Kind != Kind.Item)
					continue;
				if (first < 0)
					first = _entries[e].Index;
				last = _entries[e].Index;
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

		var count = _itemCount;
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
		EnsureModel();
		var entry = FindEntry(e);
		if (entry < 0)
			return;

		if (ArrangedFrame.Width <= 0 && ArrangedFrame.Height <= 0)
		{
			// Not laid out yet: scroll once it is.
			_pendingScroll = (entry, e.ScrollToPosition, e.IsAnimated);
			_pendingScrollWaitsForLayout = true;
			return;
		}
		ScrollToEntry(entry, e.ScrollToPosition, e.IsAnimated);
	}

	int FindEntry(ScrollToRequestEventArgs e)
	{
		if (e.Mode == ScrollToMode.Element)
		{
			for (var i = 0; i < _entries.Count; i++)
			{
				var entry = _entries[i];
				if (entry.Kind == Kind.Item && Equals(entry.Data, e.Item)
					&& (e.Group is null || entry.Group < 0 || Equals(GroupAt(entry.Group), e.Group)))
					return i;
			}
			return -1;
		}

		if (!IsGrouped || e.GroupIndex < 0)
			return EntryOfItem(e.Index);

		var inGroup = 0;
		for (var i = 0; i < _entries.Count; i++)
		{
			if (_entries[i] is { Kind: Kind.Item } entry && entry.Group == e.GroupIndex && inGroup++ == e.Index)
				return i;
		}
		return -1;
	}

	object? GroupAt(int groupIndex) => VirtualView.ItemsSource?.Cast<object?>().ElementAtOrDefault(groupIndex);

	/// <summary>
	/// Scrolls an entry into view. Its offset may rest on estimated extents, so an instant scroll is
	/// corrected after the entries around it are measured, and a smooth one once it ends. A long
	/// distance is jumped rather than animated through thousands of rows.
	/// </summary>
	void ScrollToEntry(int entry, ScrollToPosition position, bool animated)
	{
		RefreshViewport();
		if (_geometry is not { } g || entry >= _entries.Count)
			return;
		var viewport = g.Vertical ? ArrangedFrame.Height : ArrangedFrame.Width;
		var current = ScrollOffset;
		var target = TargetOffset(entry, position, viewport, current);

		if (Math.Abs(target - current) < 1)
		{
			_pendingScroll = null;
			return;
		}

		if (animated && Math.Abs(target - current) <= viewport * 10)
		{
			_pendingScroll = (entry, position, true);
			ScrollToOffset(target, animated: true);
			return;
		}

		_pendingScroll = null;
		for (var attempt = 0; attempt < 4; attempt++)
		{
			ScrollToOffset(target, animated: false);
			var corrected = TargetOffset(entry, position, viewport, target);
			if (Math.Abs(corrected - target) < 1)
				break;
			target = corrected;
		}
	}

	double TargetOffset(int entry, ScrollToPosition position, double viewport, double current)
	{
		var row = _entryRow[entry];
		var start = _rowOffsets[row];
		var length = _rowExtents[row];
		var offset = position switch
		{
			ScrollToPosition.Center => start - (viewport - length) / 2,
			ScrollToPosition.End => start - viewport + length,
			ScrollToPosition.Start => start,
			// MakeVisible: scroll only as far as needed.
			_ => start < current ? start : start + length > current + viewport ? start - viewport + length : current,
		};
		return Math.Clamp(offset, 0, Math.Max(0, _total - viewport));
	}
}

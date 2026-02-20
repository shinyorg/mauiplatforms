using System.Collections.Specialized;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using AppKit;
using Foundation;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// NSObject wrapper for MacOSSidebarItem, required for NSOutlineView identity tracking.
/// </summary>
internal class SidebarItemWrapper : NSObject
{
	public MacOSSidebarItem Item { get; }
	public SidebarItemWrapper(MacOSSidebarItem item) => Item = item;
}

/// <summary>
/// NSOutlineView data source for native sidebar items.
/// </summary>
internal class SidebarOutlineViewDataSource : NSOutlineViewDataSource
{
	readonly IList<MacOSSidebarItem> _items;
	readonly Dictionary<MacOSSidebarItem, SidebarItemWrapper> _wrappers = new();

	public SidebarOutlineViewDataSource(IList<MacOSSidebarItem> items)
	{
		_items = items;
		BuildWrappers(items);
	}

	void BuildWrappers(IList<MacOSSidebarItem> items)
	{
		foreach (var item in items)
		{
			_wrappers[item] = new SidebarItemWrapper(item);
			if (item.Children != null)
				BuildWrappers(item.Children);
		}
	}

	public SidebarItemWrapper GetWrapper(MacOSSidebarItem item)
	{
		if (!_wrappers.TryGetValue(item, out var wrapper))
		{
			wrapper = new SidebarItemWrapper(item);
			_wrappers[item] = wrapper;
		}
		return wrapper;
	}

	public MacOSSidebarItem? GetItem(NSObject? obj)
		=> (obj as SidebarItemWrapper)?.Item;

	public override nint GetChildrenCount(NSOutlineView outlineView, NSObject? item)
	{
		if (item == null)
			return _items.Count;

		var sidebarItem = GetItem(item);
		return sidebarItem?.Children?.Count ?? 0;
	}

	public override NSObject GetChild(NSOutlineView outlineView, nint childIndex, NSObject? item)
	{
		MacOSSidebarItem child;
		if (item == null)
			child = _items[(int)childIndex];
		else
		{
			var parent = GetItem(item)!;
			child = parent.Children![(int)childIndex];
		}
		return GetWrapper(child);
	}

	public override bool ItemExpandable(NSOutlineView outlineView, NSObject item)
	{
		var sidebarItem = GetItem(item);
		return sidebarItem?.IsGroup ?? false;
	}
}

/// <summary>
/// NSOutlineView delegate for native sidebar rendering and selection.
/// </summary>
internal class SidebarOutlineViewDelegate : NSOutlineViewDelegate
{
	readonly SidebarOutlineViewDataSource _dataSource;
	readonly Action<MacOSSidebarItem> _onSelectionChanged;

	public SidebarOutlineViewDelegate(SidebarOutlineViewDataSource dataSource, Action<MacOSSidebarItem> onSelectionChanged)
	{
		_dataSource = dataSource;
		_onSelectionChanged = onSelectionChanged;
	}

	public override bool IsGroupItem(NSOutlineView outlineView, NSObject item)
	{
		var sidebarItem = _dataSource.GetItem(item);
		return sidebarItem?.IsGroup ?? false;
	}

	public override NSView GetView(NSOutlineView outlineView, NSTableColumn? tableColumn, NSObject item)
	{
		var sidebarItem = _dataSource.GetItem(item);
		if (sidebarItem == null)
			return new NSView();

		if (sidebarItem.IsGroup)
		{
			// Section header — plain text, uppercase-ish system style
			var headerView = outlineView.MakeView("HeaderCell", this) as NSTableCellView
				?? CreateHeaderCellView();
			headerView.TextField!.StringValue = sidebarItem.Title;
			return headerView;
		}

		// Regular item — icon + text
		var cellView = outlineView.MakeView("DataCell", this) as NSTableCellView
			?? CreateDataCellView();
		cellView.TextField!.StringValue = sidebarItem.Title;

		if (cellView.ImageView != null)
		{
			NSImage? image = null;
			if (!string.IsNullOrEmpty(sidebarItem.SystemImage))
			{
				image = NSImage.GetSystemSymbol(sidebarItem.SystemImage, null);
			}
			cellView.ImageView.Image = image;
			cellView.ImageView.Hidden = image == null;
		}

		return cellView;
	}

	NSTableCellView CreateHeaderCellView()
	{
		var cell = new NSTableCellView { Identifier = "HeaderCell" };
		var textField = new NSTextField
		{
			Editable = false,
			Bordered = false,
			DrawsBackground = false,
			Font = NSFont.BoldSystemFontOfSize(11),
			TextColor = NSColor.SecondaryLabel,
			TranslatesAutoresizingMaskIntoConstraints = false,
		};
		cell.AddSubview(textField);
		cell.TextField = textField;

		NSLayoutConstraint.ActivateConstraints(new[]
		{
			textField.LeadingAnchor.ConstraintEqualTo(cell.LeadingAnchor, 4),
			textField.TrailingAnchor.ConstraintEqualTo(cell.TrailingAnchor, -4),
			textField.CenterYAnchor.ConstraintEqualTo(cell.CenterYAnchor),
		});

		return cell;
	}

	NSTableCellView CreateDataCellView()
	{
		var cell = new NSTableCellView { Identifier = "DataCell" };

		var imageView = new NSImageView
		{
			TranslatesAutoresizingMaskIntoConstraints = false,
			ImageScaling = NSImageScale.ProportionallyUpOrDown,
		};
		imageView.SetContentHuggingPriorityForOrientation(251, NSLayoutConstraintOrientation.Horizontal);

		var textField = new NSTextField
		{
			Editable = false,
			Bordered = false,
			DrawsBackground = false,
			Font = NSFont.SystemFontOfSize(13),
			TextColor = NSColor.Label,
			LineBreakMode = NSLineBreakMode.TruncatingTail,
			TranslatesAutoresizingMaskIntoConstraints = false,
		};

		cell.AddSubview(imageView);
		cell.AddSubview(textField);
		cell.ImageView = imageView;
		cell.TextField = textField;

		NSLayoutConstraint.ActivateConstraints(new[]
		{
			imageView.LeadingAnchor.ConstraintEqualTo(cell.LeadingAnchor, 4),
			imageView.CenterYAnchor.ConstraintEqualTo(cell.CenterYAnchor),
			imageView.WidthAnchor.ConstraintEqualTo(18),
			imageView.HeightAnchor.ConstraintEqualTo(18),

			textField.LeadingAnchor.ConstraintEqualTo(imageView.TrailingAnchor, 6),
			textField.TrailingAnchor.ConstraintEqualTo(cell.TrailingAnchor, -4),
			textField.CenterYAnchor.ConstraintEqualTo(cell.CenterYAnchor),
		});

		return cell;
	}

	public override void SelectionDidChange(NSNotification notification)
	{
		if (notification.Object is not NSOutlineView outlineView)
			return;

		var selectedRow = outlineView.SelectedRow;
		if (selectedRow < 0)
			return;

		var item = outlineView.ItemAtRow(selectedRow);
		var sidebarItem = _dataSource.GetItem(item as NSObject);
		if (sidebarItem != null && !sidebarItem.IsGroup)
		{
			_onSelectionChanged(sidebarItem);
		}
	}

	public override nfloat GetRowHeight(NSOutlineView outlineView, NSObject item)
	{
		var sidebarItem = _dataSource.GetItem(item);
		return sidebarItem?.IsGroup == true ? 24 : 28;
	}

	public override bool ShouldSelectItem(NSOutlineView outlineView, NSObject item)
	{
		var sidebarItem = _dataSource.GetItem(item);
		return sidebarItem != null && !sidebarItem.IsGroup;
	}
}

/// <summary>
/// Alternative FlyoutPage handler that uses a native NSOutlineView source list for the sidebar.
/// Opt in by registering this handler and providing items via <see cref="MacOSFlyoutPage.SidebarItemsProperty"/>.
/// 
/// <code>
/// // In MauiProgram.cs:
/// builder.ConfigureMauiHandlers(handlers =>
///     handlers.AddHandler&lt;FlyoutPage, NativeSidebarFlyoutPageHandler&gt;());
///
/// // In page setup:
/// MacOSFlyoutPage.SetSidebarItems(flyoutPage, items);
/// MacOSFlyoutPage.SetSidebarSelectionChanged(flyoutPage, item => { ... });
/// </code>
/// </summary>
public partial class NativeSidebarFlyoutPageHandler : MacOSViewHandler<IFlyoutView, NSSplitView>
{
	public static readonly IPropertyMapper<IFlyoutView, NativeSidebarFlyoutPageHandler> Mapper =
		new PropertyMapper<IFlyoutView, NativeSidebarFlyoutPageHandler>(ViewMapper)
		{
			[nameof(IFlyoutView.Detail)] = MapDetail,
			[nameof(IFlyoutView.IsPresented)] = MapIsPresented,
			[nameof(IFlyoutView.FlyoutBehavior)] = MapFlyoutBehavior,
			[nameof(IFlyoutView.FlyoutWidth)] = MapFlyoutWidth,
		};

	NSScrollView? _scrollView;
	NSOutlineView? _outlineView;
	NSView? _sidebarContainer;
	NSView? _detailContainer;
	NSView? _currentDetailView;
	NSLayoutConstraint? _sidebarWidthConstraint;

	SidebarOutlineViewDataSource? _dataSource;
	SidebarOutlineViewDelegate? _delegate;

	double _flyoutWidth = 220;

	FlyoutPage? FlyoutPage => VirtualView as FlyoutPage;

	public NativeSidebarFlyoutPageHandler() : base(Mapper) { }

	protected override NSSplitView CreatePlatformView()
	{
		var splitView = new NSSplitView
		{
			IsVertical = true,
			DividerStyle = NSSplitViewDividerStyle.Thin,
		};

		// Sidebar container
		_sidebarContainer = new NSView
		{
			WantsLayer = true,
			TranslatesAutoresizingMaskIntoConstraints = false,
		};

		// Create outline view configured as source list
		_outlineView = new NSOutlineView
		{
			Style = NSTableViewStyle.SourceList,
			FloatsGroupRows = false,
			IndentationPerLevel = 0,
			HeaderView = null,
		};

		var column = new NSTableColumn("SidebarColumn")
		{
			Editable = false,
		};
		_outlineView.AddColumn(column);
		_outlineView.OutlineTableColumn = column;

		_scrollView = new NSScrollView
		{
			HasVerticalScroller = true,
			HasHorizontalScroller = false,
			AutohidesScrollers = true,
			DrawsBackground = false,
			DocumentView = _outlineView,
			TranslatesAutoresizingMaskIntoConstraints = false,
		};

		_sidebarContainer.AddSubview(_scrollView);

		// Pin scroll view to sidebar container
		NSLayoutConstraint.ActivateConstraints(new[]
		{
			_scrollView.TopAnchor.ConstraintEqualTo(_sidebarContainer.TopAnchor),
			_scrollView.LeadingAnchor.ConstraintEqualTo(_sidebarContainer.LeadingAnchor),
			_scrollView.TrailingAnchor.ConstraintEqualTo(_sidebarContainer.TrailingAnchor),
			_scrollView.BottomAnchor.ConstraintEqualTo(_sidebarContainer.BottomAnchor),
		});

		// Detail container
		_detailContainer = new NSView { WantsLayer = true };

		splitView.AddArrangedSubview(_sidebarContainer);
		splitView.AddArrangedSubview(_detailContainer);

		// Sidebar width constraint
		_sidebarWidthConstraint = _sidebarContainer.WidthAnchor.ConstraintEqualTo((nfloat)_flyoutWidth);
		_sidebarWidthConstraint.Priority = (float)NSLayoutPriority.DefaultHigh;
		_sidebarWidthConstraint.Active = true;

		// Sidebar holds width, detail flexes
		splitView.SetHoldingPriority(251, 0);
		splitView.SetHoldingPriority(249, 1);

		return splitView;
	}

	protected override void ConnectHandler(NSSplitView platformView)
	{
		base.ConnectHandler(platformView);
		LoadSidebarItems();
	}

	protected override void DisconnectHandler(NSSplitView platformView)
	{
		_dataSource = null;
		_delegate = null;
		base.DisconnectHandler(platformView);
	}

	void LoadSidebarItems()
	{
		if (_outlineView == null || FlyoutPage == null)
			return;

		var items = MacOSFlyoutPage.GetSidebarItems(FlyoutPage);
		if (items == null || items.Count == 0)
			return;

		_dataSource = new SidebarOutlineViewDataSource(items);
		_delegate = new SidebarOutlineViewDelegate(_dataSource, OnSidebarItemSelected);

		_outlineView.DataSource = _dataSource;
		_outlineView.Delegate = _delegate;
		_outlineView.ReloadData();

		// Expand all group items
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].IsGroup)
			{
				var wrapper = _dataSource.GetWrapper(items[i]);
				_outlineView.ExpandItem(wrapper);
			}
		}

		// Select the first selectable item
		SelectFirstItem(items);

		// Watch for collection changes
		if (items is INotifyCollectionChanged observable)
		{
			observable.CollectionChanged += (s, e) => ReloadSidebar();
		}
	}

	void SelectFirstItem(IList<MacOSSidebarItem> items)
	{
		foreach (var item in items)
		{
			if (item.IsGroup && item.Children != null)
			{
				foreach (var child in item.Children)
				{
					var wrapper = _dataSource!.GetWrapper(child);
					var row = _outlineView!.RowForItem(wrapper);
					if (row >= 0)
					{
						_outlineView.SelectRow(row, false);
						return;
					}
				}
			}
			else if (!item.IsGroup)
			{
				var wrapper = _dataSource!.GetWrapper(item);
				var row = _outlineView!.RowForItem(wrapper);
				if (row >= 0)
				{
					_outlineView.SelectRow(row, false);
					return;
				}
			}
		}
	}

	void ReloadSidebar()
	{
		if (_outlineView == null || FlyoutPage == null)
			return;

		var items = MacOSFlyoutPage.GetSidebarItems(FlyoutPage);
		if (items == null)
			return;

		_dataSource = new SidebarOutlineViewDataSource(items);
		_delegate = new SidebarOutlineViewDelegate(_dataSource, OnSidebarItemSelected);
		_outlineView.DataSource = _dataSource;
		_outlineView.Delegate = _delegate;
		_outlineView.ReloadData();

		// Expand all groups
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].IsGroup)
			{
				var wrapper = _dataSource.GetWrapper(items[i]);
				_outlineView.ExpandItem(wrapper);
			}
		}
	}

	void OnSidebarItemSelected(MacOSSidebarItem item)
	{
		if (FlyoutPage == null)
			return;

		var callback = MacOSFlyoutPage.GetSidebarSelectionChanged(FlyoutPage);
		callback?.Invoke(item);
	}

	void ShowDetail(NSView view)
	{
		if (_detailContainer == null)
			return;

		_currentDetailView?.RemoveFromSuperview();
		_currentDetailView = view;

		view.Frame = _detailContainer.Bounds;
		view.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
		_detailContainer.AddSubview(view);
	}

	void LayoutDetail()
	{
		if (_detailContainer == null || _currentDetailView == null)
			return;

		_currentDetailView.Frame = _detailContainer.Bounds;

		var detail = VirtualView?.Detail;
		if (detail != null)
		{
			var bounds = _detailContainer.Bounds;
			if (bounds.Width > 0 && bounds.Height > 0)
			{
				detail.Measure((double)bounds.Width, (double)bounds.Height);
				detail.Arrange(new Rect(0, 0, (double)bounds.Width, (double)bounds.Height));
			}
		}
	}

	public override void PlatformArrange(Rect rect)
	{
		base.PlatformArrange(rect);
		LayoutDetail();
	}

	// Property mappers

	public static void MapDetail(NativeSidebarFlyoutPageHandler handler, IFlyoutView view)
	{
		if (handler.MauiContext == null || view.Detail == null)
			return;

		var platformView = view.Detail.ToMacOSPlatform(handler.MauiContext);
		handler.ShowDetail(platformView);
	}

	public static void MapIsPresented(NativeSidebarFlyoutPageHandler handler, IFlyoutView view)
	{
		if (handler._sidebarContainer == null)
			return;

		handler._sidebarContainer.Hidden = !view.IsPresented;
	}

	public static void MapFlyoutBehavior(NativeSidebarFlyoutPageHandler handler, IFlyoutView view)
	{
		if (handler._sidebarContainer == null)
			return;

		switch (view.FlyoutBehavior)
		{
			case FlyoutBehavior.Disabled:
				handler._sidebarContainer.Hidden = true;
				break;
			case FlyoutBehavior.Locked:
				handler._sidebarContainer.Hidden = false;
				break;
			case FlyoutBehavior.Flyout:
				handler._sidebarContainer.Hidden = !view.IsPresented;
				break;
		}
	}

	public static void MapFlyoutWidth(NativeSidebarFlyoutPageHandler handler, IFlyoutView view)
	{
		var width = view.FlyoutWidth > 0 ? view.FlyoutWidth : 220;
		handler._flyoutWidth = width;
		if (handler._sidebarWidthConstraint != null)
			handler._sidebarWidthConstraint.Constant = (nfloat)width;
	}
}

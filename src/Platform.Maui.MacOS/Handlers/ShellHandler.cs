using System.Collections.Specialized;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using AppKit;
using Foundation;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// Shell handler for macOS. Renders Shell as a split view with:
/// - Left sidebar (flyout) showing Shell items
/// - Right content area showing the current page
/// On macOS, the flyout is always visible (like a source list sidebar).
/// </summary>
public partial class ShellHandler : ViewHandler<Shell, NSView>
{
	public static readonly IPropertyMapper<Shell, ShellHandler> Mapper =
		new PropertyMapper<Shell, ShellHandler>(ViewMapper)
		{
			[nameof(Shell.CurrentItem)] = MapCurrentItem,
			[nameof(Shell.FlyoutBackgroundColor)] = MapFlyoutBackground,
			[nameof(Shell.FlyoutHeaderTemplate)] = MapFlyoutHeader,
			[nameof(Shell.FlyoutHeader)] = MapFlyoutHeader,
			[nameof(Shell.Items)] = MapItems,
			[nameof(Shell.FlyoutItems)] = MapFlyoutItems,
			[nameof(IFlyoutView.FlyoutBehavior)] = MapFlyoutBehavior,
			[nameof(IFlyoutView.IsPresented)] = MapIsPresented,
			[nameof(IFlyoutView.FlyoutWidth)] = MapFlyoutWidth,
		};

	public static readonly CommandMapper<Shell, ShellHandler> CommandMapper =
		new(ViewCommandMapper);

	NSView? _container;
	NSView? _sidebarView;
	NSScrollView? _sidebarScrollView;
	FlippedDocumentView? _sidebarContent;
	NSView? _contentView;
	NSView? _currentPageView;
	Page? _currentPage;
	Shell? _shell;
	nfloat _flyoutWidth = 220;

	public ShellHandler() : base(Mapper, CommandMapper)
	{
	}

	protected override NSView CreatePlatformView()
	{
		_container = new FlippedDocumentView();

		// Sidebar
		_sidebarView = new NSView();
		_sidebarView.WantsLayer = true;
		_sidebarView.Layer!.BackgroundColor = NSColor.UnderPageBackground.CGColor;

		_sidebarScrollView = new NSScrollView
		{
			HasVerticalScroller = true,
			HasHorizontalScroller = false,
			AutohidesScrollers = true,
			DrawsBackground = false,
		};

		_sidebarContent = new FlippedDocumentView();
		_sidebarScrollView.DocumentView = _sidebarContent;

		_sidebarView.AddSubview(_sidebarScrollView);

		// Content area
		_contentView = new FlippedDocumentView();
		_contentView.WantsLayer = true;
		_contentView.Layer!.MasksToBounds = true;

		_container.AddSubview(_sidebarView);
		_container.AddSubview(_contentView);

		return _container;
	}

	protected override void ConnectHandler(NSView platformView)
	{
		base.ConnectHandler(platformView);
		_shell = VirtualView;

		if (_shell != null)
		{
			((INotifyCollectionChanged)_shell.Items).CollectionChanged += OnShellItemsChanged;
		}

		BuildSidebar();
	}

	protected override void DisconnectHandler(NSView platformView)
	{
		if (_shell != null)
		{
			((INotifyCollectionChanged)_shell.Items).CollectionChanged -= OnShellItemsChanged;
		}
		_shell = null;
		base.DisconnectHandler(platformView);
	}

	void OnShellItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		BuildSidebar();
	}

	public override void PlatformArrange(Rect rect)
	{
		base.PlatformArrange(rect);

		if (_container == null || _sidebarView == null || _contentView == null)
			return;

		_container.Frame = new CGRect(0, 0, rect.Width, rect.Height);

		var sidebarWidth = Math.Min((double)_flyoutWidth, rect.Width * 0.4);
		_sidebarView.Frame = new CGRect(0, 0, sidebarWidth, rect.Height);

		if (_sidebarScrollView != null)
			_sidebarScrollView.Frame = _sidebarView.Bounds;

		var contentX = sidebarWidth + 1; // 1px divider
		var contentWidth = rect.Width - contentX;
		_contentView.Frame = new CGRect(contentX, 0, contentWidth, rect.Height);

		if (_currentPageView != null)
		{
			_currentPageView.Frame = _contentView.Bounds;
			LayoutCurrentPage(rect);
		}

		LayoutSidebarContent();
	}

	void LayoutSidebarContent()
	{
		if (_sidebarContent == null || _sidebarView == null)
			return;

		var subviews = _sidebarContent.Subviews;
		if (subviews.Length == 0)
			return;

		var width = _sidebarView.Bounds.Width;
		nfloat y = 0;

		foreach (var subview in subviews)
		{
			var height = subview.IntrinsicContentSize.Height;
			if (height <= 0 || height > 10000)
				height = 36;
			subview.Frame = new CGRect(0, y, width, height);
			y += height;
		}

		_sidebarContent.Frame = new CGRect(0, 0, width, y);
	}

	void LayoutCurrentPage(Rect rect)
	{
		if (_currentPage == null || _contentView == null)
			return;

		var contentBounds = _contentView.Bounds;
		_currentPage.Measure((double)contentBounds.Width, (double)contentBounds.Height);
		_currentPage.Arrange(new Rect(0, 0, (double)contentBounds.Width, (double)contentBounds.Height));
	}

	void BuildSidebar()
	{
		if (_sidebarContent == null || _shell == null || MauiContext == null)
			return;

		// Clear existing sidebar items
		foreach (var subview in _sidebarContent.Subviews)
			subview.RemoveFromSuperview();

		// Add items from Shell.Items (ShellItem collection)
		foreach (var shellItem in _shell.Items)
		{
			if (shellItem is not ShellItem item)
				continue;

			var sections = item.Items.ToList();
			var allContents = sections.SelectMany(s => s.Items).ToList();
			var hasMultipleContents = allContents.Count > 1;

			// Show group header if FlyoutItem has multiple children
			if (hasMultipleContents && !string.IsNullOrEmpty(item.Title))
			{
				_sidebarContent.AddSubview(new SidebarGroupHeaderView(item.Title));
			}

			foreach (var section in sections)
			{
				if (section is ShellSection shellSection)
				{
					foreach (var content in shellSection.Items)
					{
						AddSidebarItem(content.Title ?? shellSection.Title ?? item.Title ?? "Page",
							content.Icon ?? shellSection.Icon ?? item.Icon,
							shellItem, shellSection, content, hasMultipleContents);
					}
				}
			}
		}

		LayoutSidebarContent();
	}

	void AddSidebarItem(string title, ImageSource? icon, ShellItem shellItem, ShellSection section, ShellContent content, bool indented)
	{
		var itemView = new SidebarItemView(title, indented, () =>
		{
			if (_shell == null)
				return;

			// Navigate to the selected item
			_shell.CurrentItem = shellItem;
			shellItem.CurrentItem = section;
			section.CurrentItem = content;
		});

		// Highlight if this is the current item
		if (_shell?.CurrentItem == shellItem &&
			shellItem.CurrentItem == section &&
			section.CurrentItem == content)
		{
			itemView.SetSelected(true);
		}

		_sidebarContent!.AddSubview(itemView);
	}

	void ShowCurrentPage()
	{
		if (_contentView == null || _shell == null || MauiContext == null)
			return;


		// Remove old page
		if (_currentPageView != null)
		{
			_currentPageView.RemoveFromSuperview();
			_currentPageView = null;
			_currentPage = null;
		}

		// Get the current page from Shell
		var currentItem = _shell.CurrentItem;
		if (currentItem?.CurrentItem?.CurrentItem is ShellContent shellContent)
		{
			Page? page = null;

			// Try to get the page from the controller (may be lazily created)
			if (shellContent is IShellContentController controller)
				page = controller.Page;

			// If still null, create from ContentTemplate
			if (page == null && shellContent.ContentTemplate != null)
			{
				page = shellContent.ContentTemplate.CreateContent() as Page;
				if (page != null)
					page.Parent = shellContent;
			}

			if (page != null)
			{
				_currentPage = page;
				var platformView = ((IView)page).ToMacOSPlatform(MauiContext);
				platformView.Frame = _contentView.Bounds;
				_contentView.AddSubview(platformView);
				_currentPageView = platformView;

				// Measure and arrange
				var bounds = _contentView.Bounds;
				if (bounds.Width > 0 && bounds.Height > 0)
				{
					page.Measure((double)bounds.Width, (double)bounds.Height);
					page.Arrange(new Rect(0, 0, (double)bounds.Width, (double)bounds.Height));
				}
			}
		}

		// Rebuild sidebar to update selection
		BuildSidebar();
	}

	// Property mappers

	public static void MapCurrentItem(ShellHandler handler, Shell shell)
	{
		handler.ShowCurrentPage();
	}

	public static void MapFlyoutBackground(ShellHandler handler, Shell shell)
	{
		if (handler._sidebarView != null && shell.FlyoutBackgroundColor != null)
		{
			handler._sidebarView.WantsLayer = true;
			handler._sidebarView.Layer!.BackgroundColor = shell.FlyoutBackgroundColor.ToPlatformColor().CGColor;
		}
	}

	public static void MapFlyoutHeader(ShellHandler handler, Shell shell)
	{
		// Header support could be added via FlyoutHeaderTemplate
	}

	public static void MapItems(ShellHandler handler, Shell shell)
	{
		handler.BuildSidebar();
		handler.UpdateValue(nameof(Shell.CurrentItem));
	}

	public static void MapFlyoutItems(ShellHandler handler, Shell shell)
	{
		handler.BuildSidebar();
	}

	public static void MapFlyoutBehavior(ShellHandler handler, Shell shell)
	{
		if (handler._sidebarView == null)
			return;

		var behavior = shell.FlyoutBehavior;
		handler._sidebarView.Hidden = behavior == FlyoutBehavior.Disabled;
		if (behavior == FlyoutBehavior.Locked)
			handler._sidebarView.Hidden = false;
	}

	public static void MapIsPresented(ShellHandler handler, Shell shell)
	{
		if (handler._sidebarView == null)
			return;

		// When FlyoutBehavior is Locked, sidebar is always visible
		if (shell.FlyoutBehavior == FlyoutBehavior.Locked)
			return;

		handler._sidebarView.Hidden = !shell.FlyoutIsPresented;
	}

	public static void MapFlyoutWidth(ShellHandler handler, Shell shell)
	{
		if (shell.FlyoutWidth > 0)
		{
			handler._flyoutWidth = (nfloat)shell.FlyoutWidth;
			if (handler._sidebarView != null && handler.PlatformView.Frame.Width > 0)
			{
				var rect = handler.PlatformView.Frame;
				handler.PlatformArrange(new Rect(0, 0, rect.Width, rect.Height));
			}
		}
	}

	/// <summary>
	/// A non-interactive group header label for the sidebar.
	/// </summary>
	class SidebarGroupHeaderView : NSView
	{
		readonly NSTextField _label;

		public SidebarGroupHeaderView(string title)
		{
			_label = new NSTextField
			{
				StringValue = title.ToUpperInvariant(),
				Editable = false,
				Bordered = false,
				DrawsBackground = false,
				Font = NSFont.BoldSystemFontOfSize(10),
				TextColor = NSColor.SecondaryLabel,
				LineBreakMode = NSLineBreakMode.TruncatingTail,
			};
			AddSubview(_label);
		}

		public override CGSize IntrinsicContentSize => new CGSize(NSView.NoIntrinsicMetric, 28);

		public override void Layout()
		{
			base.Layout();
			_label.Frame = new CGRect(16, 10, Bounds.Width - 32, 14);
		}
	}

	/// <summary>
	/// A simple sidebar item view using native NSTextField and NSView.
	/// </summary>
	class SidebarItemView : NSView
	{
		readonly NSTextField _label;
		readonly Action _onTap;
		readonly bool _indented;
		bool _isSelected;
		NSTrackingArea? _trackingArea;

		public SidebarItemView(string title, bool indented, Action onTap)
		{
			_onTap = onTap;
			_indented = indented;
			WantsLayer = true;
			Layer!.CornerRadius = 6;

			_label = new NSTextField
			{
				StringValue = title,
				Editable = false,
				Bordered = false,
				DrawsBackground = false,
				Font = NSFont.SystemFontOfSize(13),
				TextColor = NSColor.Label,
				LineBreakMode = NSLineBreakMode.TruncatingTail,
			};

			AddSubview(_label);
		}

		public override CGSize IntrinsicContentSize => new CGSize(NSView.NoIntrinsicMetric, 30);

		public override void Layout()
		{
			base.Layout();
			var leftPad = _indented ? (nfloat)28 : (nfloat)16;
			_label.Frame = new CGRect(leftPad, 5, Bounds.Width - leftPad - 12, 20);
		}

		public void SetSelected(bool selected)
		{
			_isSelected = selected;
			Layer!.BackgroundColor = selected
				? NSColor.SelectedContentBackground.CGColor
				: NSColor.Clear.CGColor;
			_label.TextColor = selected ? NSColor.White : NSColor.Label;
		}

		public override void MouseDown(NSEvent theEvent)
		{
			base.MouseDown(theEvent);
			_onTap();
		}

		public override void UpdateTrackingAreas()
		{
			base.UpdateTrackingAreas();

			if (_trackingArea != null)
				RemoveTrackingArea(_trackingArea);

			_trackingArea = new NSTrackingArea(
				Bounds,
				NSTrackingAreaOptions.MouseEnteredAndExited | NSTrackingAreaOptions.ActiveInKeyWindow,
				this,
				null);
			AddTrackingArea(_trackingArea);
		}

		public override void MouseEntered(NSEvent theEvent)
		{
			base.MouseEntered(theEvent);
			if (!_isSelected)
				Layer!.BackgroundColor = NSColor.UnemphasizedSelectedContentBackground.CGColor;
		}

		public override void MouseExited(NSEvent theEvent)
		{
			base.MouseExited(theEvent);
			if (!_isSelected)
				Layer!.BackgroundColor = NSColor.Clear.CGColor;
		}
	}
}

using System.Collections.Specialized;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform.MacOS;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// Manages an NSToolbar on the NSWindow, populated from Page.ToolbarItems.
/// Also renders a navigation back button and sets the window title from NavigationPage.
/// </summary>
public class MacOSToolbarManager : NSObject, INSToolbarDelegate
{
    const string ToolbarIdPrefix = "MauiToolbar_";
    const string ItemIdPrefix = "MauiToolbarItem_";
    const string SidebarItemIdPrefix = "MauiSidebarItem_";
    const string FlexibleSpaceId = "NSToolbarFlexibleSpaceItem";
    const string SidebarToggleId = "MauiSidebarToggle";
    const string TrackingSeparatorId = "MauiTrackingSeparator";
    const string BackButtonId = "MauiBackButton";
    const string TitleId = "MauiTitle";
    const nint SidebarItemTagOffset = 100000;
    static int _toolbarCounter;

    NSWindow? _window;
    NSToolbar? _toolbar;
    readonly List<ToolbarItem> _items = new();
    readonly List<ToolbarItem> _sidebarItems = new();
    readonly List<string> _itemIdentifiers = new();
    Page? _currentPage;
    FlyoutPage? _flyoutPage;
    NavigationPage? _navigationPage;
    Shell? _shell;
    NSSplitView? _splitView;

    public void AttachToWindow(NSWindow window)
    {
        _window = window;
        var toolbarId = $"{ToolbarIdPrefix}{_toolbarCounter++}";
        _toolbar = new NSToolbar(toolbarId)
        {
            Delegate = this,
            DisplayMode = NSToolbarDisplayMode.Icon,
            AllowsUserCustomization = false,
        };
        _window.Toolbar = _toolbar;
    }

    /// <summary>
    /// Sets the split view used by NSTrackingSeparatorToolbarItem to align the
    /// toolbar divider with the sidebar/content divider.
    /// </summary>
    public void SetSplitView(NSSplitView? splitView)
    {
        _splitView = splitView;
    }

    public void SetPage(Page? page)
    {
        if (_currentPage != null)
        {
            UnsubscribeCommands();
            if (_currentPage.ToolbarItems is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= OnToolbarItemsChanged;
        }

        _currentPage = page;

        // Walk up the page hierarchy to find FlyoutPage and NavigationPage
        _flyoutPage = FindAncestor<FlyoutPage>(page);
        _navigationPage = FindAncestor<NavigationPage>(page);
        _shell = FindShell(page);

        // Update the window title from the current page
        if (_window != null && page != null)
            _window.Title = page.Title ?? string.Empty;

        if (_currentPage != null)
        {
            if (_currentPage.ToolbarItems is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += OnToolbarItemsChanged;
            RefreshToolbar(_currentPage.ToolbarItems);
        }
        else
        {
            RefreshToolbar(null);
        }
    }

    static T? FindAncestor<T>(Page? page) where T : Page
    {
        while (page != null)
        {
            if (page is T match)
                return match;
            page = page.Parent as Page;
        }
        return null;
    }

    static Shell? FindShell(Page? page)
    {
        Element? element = page;
        while (element != null)
        {
            if (element is Shell shell)
                return shell;
            element = element.Parent;
        }
        // Pushed pages may not have a parent chain to Shell yet.
        // Fall back to checking the Window's root page.
        try
        {
            if (page?.Window?.Page is Shell windowShell)
                return windowShell;
        }
        catch { }
        return null;
    }

    void OnToolbarItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_currentPage != null)
            RefreshToolbar(_currentPage.ToolbarItems);
    }

    void UnsubscribeCommands()
    {
        foreach (var item in _items)
            item.PropertyChanged -= OnToolbarItemPropertyChanged;
        foreach (var item in _sidebarItems)
            item.PropertyChanged -= OnToolbarItemPropertyChanged;
    }

    bool ShouldShowBackButton()
    {
        // Respect HasNavigationBar on the current page
        if (_currentPage != null && !NavigationPage.GetHasNavigationBar(_currentPage))
            return false;

        if (_navigationPage != null)
            return _navigationPage.Navigation.NavigationStack.Count > 1;

        // Shell: check ShellSection's navigation stack
        if (_shell?.CurrentItem?.CurrentItem is ShellSection section)
        {
            var navStack = section.Navigation?.NavigationStack;
            if (navStack != null && navStack.Count > 1)
                return true;
        }

        return false;
    }

    string? GetBackButtonTitle()
    {
        IReadOnlyList<Page>? stack = null;

        if (_navigationPage != null)
            stack = _navigationPage.Navigation.NavigationStack;
        else if (_shell?.CurrentItem?.CurrentItem is ShellSection section)
            stack = section.Navigation?.NavigationStack;

        if (stack == null || stack.Count <= 1)
            return null;

        var previousPage = stack[stack.Count - 2];
        if (previousPage == null)
            return "Back";
        var backTitle = NavigationPage.GetBackButtonTitle(previousPage);
        if (string.IsNullOrEmpty(backTitle))
            backTitle = previousPage.Title;
        return string.IsNullOrEmpty(backTitle) ? "Back" : backTitle;
    }

    void RefreshToolbar(IList<ToolbarItem>? toolbarItems)
    {
        UnsubscribeCommands();
        _items.Clear();
        _sidebarItems.Clear();
        _itemIdentifiers.Clear();

        bool hasBackButton = ShouldShowBackButton();
        bool hasFlyoutToggle = _flyoutPage != null;

        // Partition toolbar items into sidebar and content placement
        var contentItems = new List<ToolbarItem>();
        var sidebarLeading = new List<ToolbarItem>();
        var sidebarCenter = new List<ToolbarItem>();
        var sidebarTrailing = new List<ToolbarItem>();
        if (toolbarItems != null)
        {
            foreach (var item in toolbarItems)
            {
                if (item.Order == ToolbarItemOrder.Secondary)
                    continue;
                var placement = MacOSToolbarItem.GetPlacement(item);
                switch (placement)
                {
                    case MacOSToolbarItemPlacement.SidebarLeading:
                        sidebarLeading.Add(item);
                        break;
                    case MacOSToolbarItemPlacement.SidebarCenter:
                        sidebarCenter.Add(item);
                        break;
                    case MacOSToolbarItemPlacement.SidebarTrailing:
                        sidebarTrailing.Add(item);
                        break;
                    default:
                        contentItems.Add(item);
                        break;
                }
            }
        }

        bool hasContentItems = contentItems.Count > 0;
        bool hasSidebarItems = sidebarLeading.Count > 0 || sidebarCenter.Count > 0 || sidebarTrailing.Count > 0;
        bool hasToolbarItems = hasContentItems || hasSidebarItems;

        // Only show the toolbar if there's meaningful content
        bool needsToolbar = hasBackButton || hasFlyoutToggle || hasToolbarItems;

        if (!needsToolbar)
        {
            // When a sidebar split view controller is active (Shell or FlyoutPage
            // with UseNativeSidebar), the toolbar must remain attached (even if empty)
            // for AllowsFullHeightLayout to extend the sidebar under the titlebar.
            bool hasNativeSidebar = _shell != null
                || (_flyoutPage != null && MacOSFlyoutPage.GetUseNativeSidebar(_flyoutPage));

            if (!hasNativeSidebar)
            {
                if (_window?.Toolbar != null)
                    _window.Toolbar = null;
                return;
            }

            // Keep toolbar attached but clear its items — add tracking separator
            // if we have a split view so the divider aligns
            if (_window != null && _window.Toolbar == null && _toolbar != null)
                _window.Toolbar = _toolbar;

            if (_toolbar != null)
            {
                while (_toolbar.Items.Length > 0)
                    _toolbar.RemoveItem(0);

                if (_splitView != null)
                    _toolbar.InsertItem(TrackingSeparatorId, 0);
            }
            return;
        }

        // === Build toolbar item list ===

        // Sidebar area items (before tracking separator)
        if (hasFlyoutToggle)
            _itemIdentifiers.Add(SidebarToggleId);

        if (hasBackButton)
            _itemIdentifiers.Add(BackButtonId);

        // Sidebar-placed toolbar items: [Leading] <flex> [Center] <flex> [Trailing]
        int sidebarIdx = 0;

        foreach (var item in sidebarLeading)
        {
            var id = $"{SidebarItemIdPrefix}{sidebarIdx}";
            _sidebarItems.Add(item);
            _itemIdentifiers.Add(id);
            item.PropertyChanged += OnToolbarItemPropertyChanged;
            sidebarIdx++;
        }

        // Insert flex space if there are center or trailing items after leading
        if (sidebarLeading.Count > 0 && (sidebarCenter.Count > 0 || sidebarTrailing.Count > 0))
            _itemIdentifiers.Add(FlexibleSpaceId);
        // Also insert flex space if no leading items but we have center items
        // (to push center away from the left edge toggle/back buttons)
        else if (sidebarLeading.Count == 0 && sidebarCenter.Count > 0)
            _itemIdentifiers.Add(FlexibleSpaceId);

        foreach (var item in sidebarCenter)
        {
            var id = $"{SidebarItemIdPrefix}{sidebarIdx}";
            _sidebarItems.Add(item);
            _itemIdentifiers.Add(id);
            item.PropertyChanged += OnToolbarItemPropertyChanged;
            sidebarIdx++;
        }

        // Insert flex space between center and trailing (or leading and trailing if no center)
        if (sidebarCenter.Count > 0 && sidebarTrailing.Count > 0)
            _itemIdentifiers.Add(FlexibleSpaceId);
        else if (sidebarCenter.Count == 0 && sidebarLeading.Count == 0 && sidebarTrailing.Count > 0)
            _itemIdentifiers.Add(FlexibleSpaceId);

        foreach (var item in sidebarTrailing)
        {
            var id = $"{SidebarItemIdPrefix}{sidebarIdx}";
            _sidebarItems.Add(item);
            _itemIdentifiers.Add(id);
            item.PropertyChanged += OnToolbarItemPropertyChanged;
            sidebarIdx++;
        }

        // Tracking separator — divides sidebar area from content area
        if (_splitView != null)
            _itemIdentifiers.Add(TrackingSeparatorId);

        // Content area items (after tracking separator)
        // Flexible space between left nav items and centered title
        _itemIdentifiers.Add(FlexibleSpaceId);

        // Centered title (since window title is hidden)
        _itemIdentifiers.Add(TitleId);

        // Flexible space between title and right-side toolbar items
        _itemIdentifiers.Add(FlexibleSpaceId);

        int contentIdx = 0;
        foreach (var item in contentItems)
        {
            var id = $"{ItemIdPrefix}{contentIdx}";
            _items.Add(item);
            _itemIdentifiers.Add(id);
            item.PropertyChanged += OnToolbarItemPropertyChanged;
            contentIdx++;
        }

        // Attach toolbar if not already attached
        if (_window != null && _window.Toolbar == null && _toolbar != null)
            _window.Toolbar = _toolbar;

        // Force NSToolbar to reload by removing and re-inserting items
        if (_toolbar != null)
        {
            while (_toolbar.Items.Length > 0)
                _toolbar.RemoveItem(0);

            for (int i = 0; i < _itemIdentifiers.Count; i++)
                _toolbar.InsertItem(_itemIdentifiers[i], i);
        }
    }

    void OnToolbarItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_toolbar != null)
            _toolbar.ValidateVisibleItems();
    }

    // INSToolbarDelegate
    [Export("toolbar:itemForItemIdentifier:willBeInsertedIntoToolbar:")]
    public NSToolbarItem ToolbarItemForIdentifier(NSToolbar toolbar, string itemIdentifier, bool willBeInserted)
    {
        // Tracking separator — divides sidebar area from content area
        if (itemIdentifier == TrackingSeparatorId && _splitView != null)
        {
            return NSTrackingSeparatorToolbarItem.GetTrackingSeparatorToolbar(
                TrackingSeparatorId, _splitView, 0);
        }

        // Sidebar toggle button (hamburger menu)
        if (itemIdentifier == SidebarToggleId)
        {
            var nsItem = new NSToolbarItem(SidebarToggleId)
            {
                Label = "Sidebar",
                PaletteLabel = "Toggle Sidebar",
                ToolTip = "Toggle Sidebar",
                Target = this,
                Action = new ObjCRuntime.Selector("sidebarToggleClicked:"),
            };

            var button = new NSButton
            {
                Title = "☰",
                BezelStyle = NSBezelStyle.TexturedRounded,
                Target = this,
                Action = new ObjCRuntime.Selector("sidebarToggleClicked:"),
            };
            button.SetButtonType(NSButtonType.MomentaryPushIn);
            nsItem.View = button;
            return nsItem;
        }

        // Navigation back button
        if (itemIdentifier == BackButtonId)
        {
            var backTitle = GetBackButtonTitle() ?? "Back";
            var nsItem = new NSToolbarItem(BackButtonId)
            {
                Label = backTitle,
                PaletteLabel = "Back",
                ToolTip = $"Back to {backTitle}",
                Target = this,
                Action = new ObjCRuntime.Selector("backButtonClicked:"),
            };

            var button = new NSButton
            {
                BezelStyle = NSBezelStyle.TexturedRounded,
                Target = this,
                Action = new ObjCRuntime.Selector("backButtonClicked:"),
            };
            button.SetButtonType(NSButtonType.MomentaryPushIn);

            // Use SF Symbol chevron for a native Finder-like look
            var chevronImage = NSImage.GetSystemSymbol("chevron.left", null);
            if (chevronImage != null)
            {
                button.Image = chevronImage;
                button.Title = backTitle;
                button.ImagePosition = NSCellImagePosition.ImageLeading;
            }
            else
            {
                button.Title = $"‹ {backTitle}";
            }

            nsItem.View = button;
            return nsItem;
        }

        // Centered page title
        if (itemIdentifier == TitleId)
        {
            var title = _currentPage?.Title ?? string.Empty;
            var nsItem = new NSToolbarItem(TitleId)
            {
                Label = "",
                PaletteLabel = "Title",
            };

            var label = new NSTextField
            {
                StringValue = title,
                Editable = false,
                Bordered = false,
                DrawsBackground = false,
                Font = NSFont.BoldSystemFontOfSize(13),
                TextColor = NSColor.Label,
                Alignment = NSTextAlignment.Center,
            };
            label.SizeToFit();
            nsItem.View = label;
            return nsItem;
        }

        if (itemIdentifier.StartsWith(SidebarItemIdPrefix))
        {
            var indexStr = itemIdentifier.Substring(SidebarItemIdPrefix.Length);
            if (int.TryParse(indexStr, out int sIdx) && sIdx >= 0 && sIdx < _sidebarItems.Count)
            {
                var mauiItem = _sidebarItems[sIdx];
                return CreateToolbarButton(itemIdentifier, mauiItem, sIdx);
            }
        }

        if (itemIdentifier.StartsWith(ItemIdPrefix))
        {
            var indexStr = itemIdentifier.Substring(ItemIdPrefix.Length);
            if (int.TryParse(indexStr, out int index) && index >= 0 && index < _items.Count)
            {
                var mauiItem = _items[index];
                return CreateToolbarButton(itemIdentifier, mauiItem, index);
            }
        }

        return new NSToolbarItem(itemIdentifier);
    }

    NSToolbarItem CreateToolbarButton(string identifier, ToolbarItem mauiItem, int tag)
    {
        // Use a tag offset to distinguish sidebar items from content items
        nint effectiveTag = identifier.StartsWith(SidebarItemIdPrefix) ? tag + SidebarItemTagOffset : tag;

        var nsItem = new NSToolbarItem(identifier)
        {
            Label = mauiItem.Text ?? string.Empty,
            PaletteLabel = mauiItem.Text ?? string.Empty,
            ToolTip = mauiItem.Text ?? string.Empty,
            Enabled = mauiItem.IsEnabled,
            Target = this,
            Action = new ObjCRuntime.Selector("toolbarItemClicked:"),
            Tag = effectiveTag,
        };

        var button = new NSButton
        {
            BezelStyle = NSBezelStyle.TexturedRounded,
            Tag = effectiveTag,
            Target = this,
            Action = new ObjCRuntime.Selector("toolbarItemClicked:"),
        };

        // Check for SF Symbol icon via MacOSToolbarItem or IconImageSource
        var iconSource = mauiItem.IconImageSource;
        NSImage? image = null;
        if (iconSource is FileImageSource fileSource && !string.IsNullOrEmpty(fileSource.File))
            image = NSImage.GetSystemSymbol(fileSource.File, null) ?? new NSImage(fileSource.File);

        if (image != null)
        {
            button.Image = image;
            button.Title = string.Empty;
            button.ImagePosition = NSCellImagePosition.ImageOnly;
        }
        else
        {
            button.Title = mauiItem.Text ?? string.Empty;
        }

        button.SetButtonType(NSButtonType.MomentaryPushIn);
        nsItem.View = button;
        return nsItem;
    }

    [Export("toolbarAllowedItemIdentifiers:")]
    public string[] ToolbarAllowedItemIdentifiers(NSToolbar toolbar)
    {
        // Always include all possible identifiers so NSToolbar doesn't reject
        // items added later (e.g., back button appearing after a push navigation)
        var ids = new List<string>(_itemIdentifiers)
        {
            FlexibleSpaceId, BackButtonId, SidebarToggleId, TitleId, TrackingSeparatorId,
        };
        return ids.Distinct().ToArray();
    }

    [Export("toolbarDefaultItemIdentifiers:")]
    public string[] ToolbarDefaultItemIdentifiers(NSToolbar toolbar)
    {
        return _itemIdentifiers.ToArray();
    }

    [Export("toolbarItemClicked:")]
    void OnToolbarItemClicked(NSObject sender)
    {
        nint tag = -1;
        if (sender is NSToolbarItem item)
            tag = item.Tag;
        else if (sender is NSButton button)
            tag = button.Tag;

        ToolbarItem? mauiItem = null;
        if (tag >= SidebarItemTagOffset)
        {
            int sIdx = (int)(tag - SidebarItemTagOffset);
            if (sIdx >= 0 && sIdx < _sidebarItems.Count)
                mauiItem = _sidebarItems[sIdx];
        }
        else if (tag >= 0 && tag < _items.Count)
        {
            mauiItem = _items[(int)tag];
        }

        if (mauiItem != null && mauiItem.IsEnabled)
            ((IMenuItemController)mauiItem).Activate();
    }

    [Export("sidebarToggleClicked:")]
    void OnSidebarToggleClicked(NSObject sender)
    {
        if (_flyoutPage != null)
            _flyoutPage.IsPresented = !_flyoutPage.IsPresented;
    }

    [Export("backButtonClicked:")]
    void OnBackButtonClicked(NSObject sender)
    {
        if (_navigationPage != null && _navigationPage.Navigation.NavigationStack.Count > 1)
        {
            _navigationPage.PopAsync();
            return;
        }

        // Shell: pop via ShellSection navigation
        if (_shell?.CurrentItem?.CurrentItem is ShellSection section)
        {
            var navStack = section.Navigation?.NavigationStack;
            if (navStack != null && navStack.Count > 1)
                section.Navigation.PopAsync();
        }
    }

    public void Detach()
    {
        SetPage(null);
        if (_window != null)
        {
            _window.Toolbar = null;
            _window = null;
        }
        _toolbar = null;
    }
}

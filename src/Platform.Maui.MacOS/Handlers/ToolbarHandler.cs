using System.Collections.Specialized;
using Foundation;
using Microsoft.Maui.Controls;
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
    const string FlexibleSpaceId = "NSToolbarFlexibleSpaceItem";
    const string SidebarToggleId = "MauiSidebarToggle";
    const string BackButtonId = "MauiBackButton";
    const string TitleId = "MauiTitle";
    static int _toolbarCounter;

    NSWindow? _window;
    NSToolbar? _toolbar;
    readonly List<ToolbarItem> _items = new();
    readonly List<string> _itemIdentifiers = new();
    Page? _currentPage;
    FlyoutPage? _flyoutPage;
    NavigationPage? _navigationPage;
    Shell? _shell;

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
        _itemIdentifiers.Clear();

        bool hasBackButton = ShouldShowBackButton();
        bool hasFlyoutToggle = _flyoutPage != null;
        bool hasToolbarItems = toolbarItems != null && toolbarItems.Any(i => i.Order != ToolbarItemOrder.Secondary);

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

            // Keep toolbar attached but clear its items
            if (_window != null && _window.Toolbar == null && _toolbar != null)
                _window.Toolbar = _toolbar;

            if (_toolbar != null)
            {
                while (_toolbar.Items.Length > 0)
                    _toolbar.RemoveItem(0);
            }
            return;
        }

        // Left side: sidebar toggle, then back button
        if (hasFlyoutToggle)
            _itemIdentifiers.Add(SidebarToggleId);

        if (hasBackButton)
            _itemIdentifiers.Add(BackButtonId);

        // Flexible space between left nav items and centered title
        _itemIdentifiers.Add(FlexibleSpaceId);

        // Centered title (since window title is hidden)
        _itemIdentifiers.Add(TitleId);

        // Flexible space between title and right-side toolbar items
        _itemIdentifiers.Add(FlexibleSpaceId);

        if (toolbarItems != null)
        {
            int index = 0;
            foreach (var item in toolbarItems)
            {
                if (item.Order == ToolbarItemOrder.Secondary)
                    continue;

                var id = $"{ItemIdPrefix}{index}";
                _items.Add(item);
                _itemIdentifiers.Add(id);
                item.PropertyChanged += OnToolbarItemPropertyChanged;
                index++;
            }
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

        if (itemIdentifier.StartsWith(ItemIdPrefix))
        {
            var indexStr = itemIdentifier.Substring(ItemIdPrefix.Length);
            if (int.TryParse(indexStr, out int index) && index >= 0 && index < _items.Count)
            {
                var mauiItem = _items[index];
                var nsItem = new NSToolbarItem(itemIdentifier)
                {
                    Label = mauiItem.Text ?? string.Empty,
                    PaletteLabel = mauiItem.Text ?? string.Empty,
                    ToolTip = mauiItem.Text ?? string.Empty,
                    Enabled = mauiItem.IsEnabled,
                    Target = this,
                    Action = new ObjCRuntime.Selector("toolbarItemClicked:"),
                    Tag = index,
                };

                var button = new NSButton
                {
                    Title = mauiItem.Text ?? string.Empty,
                    BezelStyle = NSBezelStyle.TexturedRounded,
                    Tag = index,
                    Target = this,
                    Action = new ObjCRuntime.Selector("toolbarItemClicked:"),
                };
                nsItem.View = button;

                return nsItem;
            }
        }

        return new NSToolbarItem(itemIdentifier);
    }

    [Export("toolbarAllowedItemIdentifiers:")]
    public string[] ToolbarAllowedItemIdentifiers(NSToolbar toolbar)
    {
        // Always include all possible identifiers so NSToolbar doesn't reject
        // items added later (e.g., back button appearing after a push navigation)
        var ids = new List<string>(_itemIdentifiers)
        {
            FlexibleSpaceId, BackButtonId, SidebarToggleId, TitleId,
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

        if (tag >= 0 && tag < _items.Count)
        {
            var mauiItem = _items[(int)tag];
            if (mauiItem.IsEnabled)
                ((IMenuItemController)mauiItem).Activate();
        }
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

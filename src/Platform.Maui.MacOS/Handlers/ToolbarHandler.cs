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
    const string ToolbarId = "MauiToolbar";
    const string ItemIdPrefix = "MauiToolbarItem_";
    const string FlexibleSpaceId = "NSToolbarFlexibleSpaceItem";
    const string SidebarToggleId = "MauiSidebarToggle";
    const string BackButtonId = "MauiBackButton";

    NSWindow? _window;
    NSToolbar? _toolbar;
    readonly List<ToolbarItem> _items = new();
    readonly List<string> _itemIdentifiers = new();
    Page? _currentPage;
    FlyoutPage? _flyoutPage;
    NavigationPage? _navigationPage;

    public void AttachToWindow(NSWindow window)
    {
        _window = window;
        _toolbar = new NSToolbar(ToolbarId)
        {
            Delegate = this,
            DisplayMode = NSToolbarDisplayMode.IconAndLabel,
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
        if (_navigationPage == null)
            return false;
        if (_navigationPage.Navigation.NavigationStack.Count <= 1)
            return false;
        // Respect HasNavigationBar on the current page
        if (_currentPage != null && !NavigationPage.GetHasNavigationBar(_currentPage))
            return false;
        return true;
    }

    string? GetBackButtonTitle()
    {
        if (_navigationPage == null)
            return null;
        var stack = _navigationPage.Navigation.NavigationStack;
        if (stack.Count <= 1)
            return null;
        var previousPage = stack[stack.Count - 2];
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

        // Add sidebar toggle if there's a FlyoutPage
        if (_flyoutPage != null)
            _itemIdentifiers.Add(SidebarToggleId);

        // Add back button if NavigationPage has depth > 1
        if (ShouldShowBackButton())
            _itemIdentifiers.Add(BackButtonId);

        if (toolbarItems != null)
        {
            int index = 0;
            foreach (var item in toolbarItems)
            {
                if (item.Order == ToolbarItemOrder.Secondary)
                    continue; // Only primary items in the NSToolbar

                var id = $"{ItemIdPrefix}{index}";
                _items.Add(item);
                _itemIdentifiers.Add(id);
                item.PropertyChanged += OnToolbarItemPropertyChanged;
                index++;
            }
        }

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
                Title = $"‹ {backTitle}",
                BezelStyle = NSBezelStyle.TexturedRounded,
                Target = this,
                Action = new ObjCRuntime.Selector("backButtonClicked:"),
            };
            button.SetButtonType(NSButtonType.MomentaryPushIn);
            nsItem.View = button;
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
        var ids = new List<string>(_itemIdentifiers) { FlexibleSpaceId };
        return ids.ToArray();
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
            _navigationPage.PopAsync();
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

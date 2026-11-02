using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

/// <summary>A tab in the tvOS tab bar: dim when idle, bright when selected, white when focused.</summary>
public class TvOSTabButton : UIButton
{
    bool _selected;

    public TvOSTabButton()
    {
        Layer.CornerRadius = 30;
        TitleLabel!.Font = UIFont.SystemFontOfSize(29, UIFontWeight.Semibold);
#pragma warning disable CA1422 // ContentEdgeInsets: no UIButtonConfiguration
        ContentEdgeInsets = new UIEdgeInsets(12, 36, 12, 36);
#pragma warning restore CA1422
        UpdateAppearance();
    }

    /// <summary>Raised when the focus engine moves onto this tab.</summary>
    public Action? GotFocus { get; set; }

    public bool IsSelectedTab
    {
        get => _selected;
        set { _selected = value; UpdateAppearance(); }
    }

    public override void DidUpdateFocus(UIFocusUpdateContext context, UIFocusAnimationCoordinator coordinator)
    {
        base.DidUpdateFocus(context, coordinator);
        if (context.NextFocusedView == this)
        {
            coordinator.AddCoordinatedAnimations(() =>
            {
                UpdateAppearance();
                TvOSTransforms.SetFocusScale(this, 1.08f);
            }, null);
            GotFocus?.Invoke();
        }
        else if (context.PreviouslyFocusedView == this)
        {
            coordinator.AddCoordinatedAnimations(() =>
            {
                UpdateAppearance();
                TvOSTransforms.SetFocusScale(this, 1);
            }, null);
        }
    }

    void UpdateAppearance()
    {
        var focused = Focused;
        BackgroundColor = focused ? UIColor.White
            : _selected ? UIColor.FromWhiteAlpha(1, 0.18f)
            : UIColor.Clear;
        SetTitleColor(focused ? UIColor.FromRGB(20, 20, 24)
            : _selected ? UIColor.White
            : UIColor.FromWhiteAlpha(1, 0.6f), UIControlState.Normal);
    }
}

public class TabbedContainerView : TvOSContainerView
{
    readonly UIVisualEffectView _tabBarBackground;
    readonly UIStackView _tabBar;
    readonly UIView _contentArea;
    readonly UIFocusGuide _toSelectedTab = new();
    readonly List<TvOSTabButton> _tabButtons = new();

    UIView? _currentPageView;
    int _selectedIndex = -1;

    public Action<int>? OnTabSelected { get; set; }
    public Action<CGRect>? OnContentLayout { get; set; }

    public TabbedContainerView()
    {
        _tabBarBackground = new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.Dark))
        {
            TranslatesAutoresizingMaskIntoConstraints = false,
            ClipsToBounds = true,
        };
        _tabBarBackground.Layer.CornerRadius = 40;

        _tabBar = new UIStackView
        {
            Axis = UILayoutConstraintAxis.Horizontal,
            Distribution = UIStackViewDistribution.Fill,
            Alignment = UIStackViewAlignment.Center,
            Spacing = 8,
            TranslatesAutoresizingMaskIntoConstraints = false,
        };

        _contentArea = new UIView
        {
            TranslatesAutoresizingMaskIntoConstraints = false,
            BackgroundColor = UIColor.Clear,
        };

        AddSubview(_contentArea);
        AddSubview(_tabBarBackground);
        _tabBarBackground.ContentView.AddSubview(_tabBar);
        AddLayoutGuide(_toSelectedTab);

        NSLayoutConstraint.ActivateConstraints(new[]
        {
            _tabBarBackground.TopAnchor.ConstraintEqualTo(TopAnchor, 36),
            _tabBarBackground.CenterXAnchor.ConstraintEqualTo(CenterXAnchor),
            _tabBarBackground.HeightAnchor.ConstraintEqualTo(80),

            _tabBar.LeadingAnchor.ConstraintEqualTo(_tabBarBackground.ContentView.LeadingAnchor, 10),
            _tabBar.TrailingAnchor.ConstraintEqualTo(_tabBarBackground.ContentView.TrailingAnchor, -10),
            _tabBar.CenterYAnchor.ConstraintEqualTo(_tabBarBackground.ContentView.CenterYAnchor),

            _contentArea.TopAnchor.ConstraintEqualTo(_tabBarBackground.BottomAnchor, 12),
            _contentArea.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
            _contentArea.TrailingAnchor.ConstraintEqualTo(TrailingAnchor),
            _contentArea.BottomAnchor.ConstraintEqualTo(BottomAnchor),

            // Moving up from anywhere under the bar lands on the selected tab, not the
            // tab that happens to sit above the focused item.
            _toSelectedTab.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
            _toSelectedTab.TrailingAnchor.ConstraintEqualTo(TrailingAnchor),
            _toSelectedTab.TopAnchor.ConstraintEqualTo(_tabBarBackground.TopAnchor),
            _toSelectedTab.BottomAnchor.ConstraintEqualTo(_tabBarBackground.BottomAnchor),
        });
    }

    public void SetTabs(IList<string> titles)
    {
        foreach (var btn in _tabButtons)
            btn.RemoveFromSuperview();
        _tabButtons.Clear();

        for (int i = 0; i < titles.Count; i++)
        {
            var index = i;
            var btn = new TvOSTabButton();
            btn.SetTitle(titles[i], UIControlState.Normal);
            btn.GotFocus = () => OnTabSelected?.Invoke(index);
            btn.PrimaryActionTriggered += (s, e) => OnTabSelected?.Invoke(index);
            _tabButtons.Add(btn);
            _tabBar.AddArrangedSubview(btn);
        }

        UpdateTabAppearance();
    }

    public void SelectTab(int index)
    {
        if (index == _selectedIndex)
            return;

        _selectedIndex = index;
        UpdateTabAppearance();
    }

    void UpdateTabAppearance()
    {
        for (int i = 0; i < _tabButtons.Count; i++)
            _tabButtons[i].IsSelectedTab = i == _selectedIndex;

        var selected = _selectedIndex >= 0 && _selectedIndex < _tabButtons.Count ? _tabButtons[_selectedIndex] : null;
        _toSelectedTab.PreferredFocusEnvironments = selected != null ? new IUIFocusEnvironment[] { selected } : Array.Empty<IUIFocusEnvironment>();
        // The guide only matters while focus is below the bar.
        _toSelectedTab.Enabled = selected != null && !_tabButtons.Any(b => b.Focused);
    }

    public override void DidUpdateFocus(UIFocusUpdateContext context, UIFocusAnimationCoordinator coordinator)
    {
        base.DidUpdateFocus(context, coordinator);
        _toSelectedTab.Enabled = _selectedIndex >= 0 && !_tabButtons.Any(b => b.Focused);
    }

    public void ShowContent(UIView view)
    {
        _currentPageView?.RemoveFromSuperview();
        _currentPageView = view;

        view.Frame = _contentArea.Bounds;
        view.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        _contentArea.AddSubview(view);
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        if (_currentPageView != null)
        {
            _currentPageView.Frame = _contentArea.Bounds;
            OnContentLayout?.Invoke(_contentArea.Bounds);
        }
    }
}

public partial class TabbedPageHandler : TvOSViewHandler<ITabbedView, TabbedContainerView>
{
    public static readonly IPropertyMapper<ITabbedView, TabbedPageHandler> Mapper =
        new PropertyMapper<ITabbedView, TabbedPageHandler>(ViewMapper)
        {
            [nameof(TabbedPage.CurrentPage)] = MapCurrentPage,
        };

    int _shownIndex = -1;

    /// <summary>Lets the app switch tabs by setting TabbedPage.CurrentPage.</summary>
    public static void MapCurrentPage(TabbedPageHandler handler, ITabbedView view)
    {
        if (handler.TabbedPage is { CurrentPage: { } current } tabbedPage)
            handler.SelectPage(tabbedPage.Children.IndexOf(current));
    }

    public TabbedPageHandler() : base(Mapper)
    {
    }

    TabbedPage? TabbedPage => VirtualView as TabbedPage;

    protected override TabbedContainerView CreatePlatformView()
    {
        var view = new TabbedContainerView();
        view.OnTabSelected = OnTabSelected;
        view.OnContentLayout = OnContentLayout;
        return view;
    }

    protected override void ConnectHandler(TabbedContainerView platformView)
    {
        base.ConnectHandler(platformView);

        if (TabbedPage != null)
        {
            TabbedPage.PagesChanged += OnPagesChanged;
            SetupTabs();
        }
    }

    protected override void DisconnectHandler(TabbedContainerView platformView)
    {
        if (TabbedPage != null)
            TabbedPage.PagesChanged -= OnPagesChanged;

        base.DisconnectHandler(platformView);
    }

    void OnPagesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        SetupTabs();
    }

    void SetupTabs()
    {
        if (TabbedPage == null)
            return;

        var titles = new List<string>();
        foreach (var page in TabbedPage.Children)
            titles.Add(page.Title ?? "Tab");

        PlatformView.SetTabs(titles);

        _shownIndex = -1;
        if (TabbedPage.Children.Count > 0)
            SelectPage(Math.Max(0, TabbedPage.Children.IndexOf(TabbedPage.CurrentPage)));
    }

    void OnTabSelected(int index)
    {
        SelectPage(index);
    }

    void SelectPage(int index)
    {
        if (TabbedPage == null || index < 0 || index >= TabbedPage.Children.Count || MauiContext == null)
            return;
        if (index == _shownIndex)
            return;
        _shownIndex = index;

        TabbedPage.CurrentPage = TabbedPage.Children[index];
        PlatformView.SelectTab(index);

        var page = TabbedPage.Children[index];
        var platformView = ((IView)page).ToTvOSPlatform(MauiContext);
        PlatformView.ShowContent(platformView);
    }

    void OnContentLayout(CGRect bounds)
    {
        if (TabbedPage?.CurrentPage == null || bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var currentPage = (IView)TabbedPage.CurrentPage;
        currentPage.Measure((double)bounds.Width, (double)bounds.Height);
        currentPage.Arrange(new Rect(0, 0, (double)bounds.Width, (double)bounds.Height));
    }
}

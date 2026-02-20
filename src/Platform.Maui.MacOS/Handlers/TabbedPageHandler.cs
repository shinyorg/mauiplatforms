using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

public class TabbedContainerView : MacOSContainerView
{
    readonly NSSegmentedControl _tabBar;
    readonly NSView _contentArea;

    NSView? _currentPageView;

    public Action<nint>? OnTabSelected { get; set; }
    public Action<CGRect>? OnContentLayout { get; set; }

    public TabbedContainerView()
    {
        _tabBar = new NSSegmentedControl
        {
            SegmentStyle = NSSegmentStyle.Automatic,
            TranslatesAutoresizingMaskIntoConstraints = false,
            TrackingMode = NSSegmentSwitchTracking.SelectOne,
        };
        _tabBar.Activated += (s, e) => OnTabSelected?.Invoke(_tabBar.SelectedSegment);

        _contentArea = new NSView
        {
            TranslatesAutoresizingMaskIntoConstraints = false,
            WantsLayer = true,
        };

        AddSubview(_tabBar);
        AddSubview(_contentArea);

        NSLayoutConstraint.ActivateConstraints(new[]
        {
            _tabBar.TopAnchor.ConstraintEqualTo(TopAnchor, 10),
            _tabBar.CenterXAnchor.ConstraintEqualTo(CenterXAnchor),
            _tabBar.HeightAnchor.ConstraintEqualTo(30),

            _contentArea.TopAnchor.ConstraintEqualTo(_tabBar.BottomAnchor, 10),
            _contentArea.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
            _contentArea.TrailingAnchor.ConstraintEqualTo(TrailingAnchor),
            _contentArea.BottomAnchor.ConstraintEqualTo(BottomAnchor),
        });
    }

    public void SetTabs(IList<string> titles)
    {
        _tabBar.SegmentCount = titles.Count;
        for (int i = 0; i < titles.Count; i++)
        {
            _tabBar.SetLabel(titles[i], i);
            _tabBar.SetWidth(0, i); // auto-size
        }
    }

    public void SelectTab(int index)
    {
        _tabBar.SelectedSegment = index;
    }

    public void ShowContent(NSView view)
    {
        _currentPageView?.RemoveFromSuperview();
        _currentPageView = view;

        view.Frame = _contentArea.Bounds;
        view.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
        _contentArea.AddSubview(view);
    }

    public override void Layout()
    {
        base.Layout();
        if (_currentPageView != null)
        {
            _currentPageView.Frame = _contentArea.Bounds;
            OnContentLayout?.Invoke(_contentArea.Bounds);
        }
    }
}

public partial class TabbedPageHandler : MacOSViewHandler<ITabbedView, TabbedContainerView>
{
    public static readonly IPropertyMapper<ITabbedView, TabbedPageHandler> Mapper =
        new PropertyMapper<ITabbedView, TabbedPageHandler>(ViewMapper)
        {
            [nameof(TabbedPage.BarBackgroundColor)] = MapBarBackgroundColor,
            [nameof(TabbedPage.BarTextColor)] = MapBarTextColor,
            [nameof(TabbedPage.SelectedTabColor)] = MapSelectedTabColor,
            [nameof(TabbedPage.UnselectedTabColor)] = MapUnselectedTabColor,
        };

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

        if (TabbedPage.Children.Count > 0)
            SelectPage(0);
    }

    void OnTabSelected(nint index)
    {
        SelectPage((int)index);
    }

    void SelectPage(int index)
    {
        if (TabbedPage == null || index < 0 || index >= TabbedPage.Children.Count || MauiContext == null)
            return;

        TabbedPage.CurrentPage = TabbedPage.Children[index];
        PlatformView.SelectTab(index);

        var page = TabbedPage.Children[index];
        var platformView = ((IView)page).ToMacOSPlatform(MauiContext);
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

    public static void MapBarBackgroundColor(TabbedPageHandler handler, ITabbedView view)
    {
        // NSSegmentedControl uses system styling — no direct background color API
    }

    public static void MapBarTextColor(TabbedPageHandler handler, ITabbedView view)
    {
        // NSSegmentedControl uses system text styling
    }

    public static void MapSelectedTabColor(TabbedPageHandler handler, ITabbedView view)
    {
        // NSSegmentedControl uses system selection styling
    }

    public static void MapUnselectedTabColor(TabbedPageHandler handler, ITabbedView view)
    {
        // NSSegmentedControl uses system styling for unselected segments
    }
}

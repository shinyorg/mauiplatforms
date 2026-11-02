using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

public partial class SwitchHandler : TvOSViewHandler<ISwitch, TvOSButton>
{
    public static readonly IPropertyMapper<ISwitch, SwitchHandler> Mapper =
        new PropertyMapper<ISwitch, SwitchHandler>(ViewMapper)
        {
            [nameof(ISwitch.IsOn)] = MapIsOn,
            [nameof(ISwitch.TrackColor)] = MapTrackColor,
            [nameof(ISwitch.ThumbColor)] = MapThumbColor,
        };

    bool _updating;

    public SwitchHandler() : base(Mapper)
    {
    }

    // tvOS has no UISwitch: the switch is a toggle button that lifts on focus like any other.
    protected override TvOSButton CreatePlatformView()
    {
        var button = new TvOSButton();
#pragma warning disable CA1422 // ContentEdgeInsets: no UIButtonConfiguration
        button.ContentEdgeInsets = new UIEdgeInsets(16, 36, 16, 36);
#pragma warning restore CA1422
        button.TitleLabel!.Font = UIFont.SystemFontOfSize(29, UIFontWeight.Semibold);
        return button;
    }

    protected override void ConnectHandler(TvOSButton platformView)
    {
        base.ConnectHandler(platformView);
        platformView.PrimaryActionTriggered += OnToggled;
    }

    protected override void DisconnectHandler(TvOSButton platformView)
    {
        platformView.PrimaryActionTriggered -= OnToggled;
        base.DisconnectHandler(platformView);
    }

    void OnToggled(object? sender, EventArgs e)
    {
        if (_updating || VirtualView == null)
            return;

        _updating = true;
        try
        {
            VirtualView.IsOn = !VirtualView.IsOn;
        }
        finally
        {
            _updating = false;
        }
    }

    public static void MapIsOn(SwitchHandler handler, ISwitch view)
    {
        handler.PlatformView.SetTitle(view.IsOn ? "●  On" : "○  Off", UIControlState.Normal);
        UpdateColors(handler, view);
        view.InvalidateMeasure();
    }

    public static void MapTrackColor(SwitchHandler handler, ISwitch view) => UpdateColors(handler, view);

    public static void MapThumbColor(SwitchHandler handler, ISwitch view) => UpdateColors(handler, view);

    static void UpdateColors(SwitchHandler handler, ISwitch view)
    {
        // On: the track (OnColor) color, or the system green. Off: the standard translucent button.
        handler.PlatformView.CustomBackground = view.IsOn
            ? view.TrackColor?.ToPlatformColor() ?? UIColor.SystemGreen
            : null;
        handler.PlatformView.TextColor = view.ThumbColor?.ToPlatformColor() ?? UIColor.White;
    }
}

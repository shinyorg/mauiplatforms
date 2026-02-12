using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

public partial class SwitchHandler : TvOSViewHandler<ISwitch, UIButton>
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

    protected override UIButton CreatePlatformView()
    {
        var button = new UIButton(UIButtonType.System);
        button.SetTitle("OFF", UIControlState.Normal);
        return button;
    }

    protected override void ConnectHandler(UIButton platformView)
    {
        base.ConnectHandler(platformView);
        platformView.PrimaryActionTriggered += OnToggled;
    }

    protected override void DisconnectHandler(UIButton platformView)
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
        handler.PlatformView.SetTitle(view.IsOn ? "ON" : "OFF", UIControlState.Normal);
        handler.PlatformView.BackgroundColor = view.IsOn
            ? UIColor.SystemGreen
            : UIColor.SystemGray;
    }

    public static void MapTrackColor(SwitchHandler handler, ISwitch view)
    {
        if (!view.IsOn && view.TrackColor != null)
            handler.PlatformView.BackgroundColor = view.TrackColor.ToPlatformColor();
    }

    public static void MapThumbColor(SwitchHandler handler, ISwitch view)
    {
        // No thumb on button-based toggle
    }
}

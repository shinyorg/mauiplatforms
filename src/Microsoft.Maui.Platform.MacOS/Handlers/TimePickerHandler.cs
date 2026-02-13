using System;
using Microsoft.Maui.Handlers;
using AppKit;
using Foundation;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

public class TimePickerHandler : MacOSViewHandler<ITimePicker, NSDatePicker>
{
    public static readonly IPropertyMapper<ITimePicker, TimePickerHandler> Mapper =
        new PropertyMapper<ITimePicker, TimePickerHandler>(ViewMapper)
        {
            [nameof(ITimePicker.Time)] = MapTime,
            [nameof(ITimePicker.TextColor)] = MapTextColor,
            [nameof(ITimePicker.Format)] = MapFormat,
        };

    bool _updating;

    public TimePickerHandler() : base(Mapper) { }

    protected override NSDatePicker CreatePlatformView()
    {
        var picker = new NSDatePicker
        {
            DatePickerStyle = NSDatePickerStyle.TextFieldAndStepper,
            DatePickerElements = NSDatePickerElementFlags.HourMinuteSecond,
            DatePickerMode = NSDatePickerMode.Single,
            DateValue = (NSDate)DateTime.Today,
            Bezeled = true,
        };
        return picker;
    }

    protected override void ConnectHandler(NSDatePicker platformView)
    {
        base.ConnectHandler(platformView);
        platformView.Activated += OnTimeChanged;
    }

    protected override void DisconnectHandler(NSDatePicker platformView)
    {
        platformView.Activated -= OnTimeChanged;
        base.DisconnectHandler(platformView);
    }

    void OnTimeChanged(object? sender, EventArgs e)
    {
        if (_updating || VirtualView == null)
            return;

        _updating = true;
        try
        {
            var date = (DateTime)PlatformView.DateValue;
            VirtualView.Time = date.TimeOfDay;
        }
        finally
        {
            _updating = false;
        }
    }

    public static void MapTime(TimePickerHandler handler, ITimePicker timePicker)
    {
        if (handler._updating)
            return;

        if (timePicker.Time is TimeSpan time)
        {
            var baseDate = DateTime.Today.Add(time);
            handler.PlatformView.DateValue = (NSDate)baseDate;
        }
    }

    public static void MapTextColor(TimePickerHandler handler, ITimePicker timePicker)
    {
        if (timePicker.TextColor is not null)
            handler.PlatformView.TextColor = timePicker.TextColor.ToPlatformColor();
    }

    public static void MapFormat(TimePickerHandler handler, ITimePicker timePicker)
    {
        // NSDatePicker handles time format via its DatePickerElements configuration
    }
}

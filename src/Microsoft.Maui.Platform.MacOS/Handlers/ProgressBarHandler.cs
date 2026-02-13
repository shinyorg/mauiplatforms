using Microsoft.Maui.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

public class ProgressBarHandler : MacOSViewHandler<IProgress, NSProgressIndicator>
{
    public static readonly IPropertyMapper<IProgress, ProgressBarHandler> Mapper =
        new PropertyMapper<IProgress, ProgressBarHandler>(ViewMapper)
        {
            [nameof(IProgress.Progress)] = MapProgress,
            [nameof(IProgress.ProgressColor)] = MapProgressColor,
        };

    public ProgressBarHandler() : base(Mapper) { }

    protected override NSProgressIndicator CreatePlatformView()
    {
        return new NSProgressIndicator
        {
            Style = NSProgressIndicatorStyle.Bar,
            Indeterminate = false,
            MinValue = 0,
            MaxValue = 1,
            DoubleValue = 0,
        };
    }

    public static void MapProgress(ProgressBarHandler handler, IProgress progress)
    {
        handler.PlatformView.DoubleValue = Math.Clamp(progress.Progress, 0, 1);
    }

    public static void MapProgressColor(ProgressBarHandler handler, IProgress progress)
    {
        // NSProgressIndicator doesn't expose a direct tint color API.
        // The system accent color is used by default.
    }
}

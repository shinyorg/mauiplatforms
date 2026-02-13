using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

public class ProgressBarHandler : TvOSViewHandler<IProgress, UIProgressView>
{
    public static readonly IPropertyMapper<IProgress, ProgressBarHandler> Mapper =
        new PropertyMapper<IProgress, ProgressBarHandler>(ViewMapper)
        {
            [nameof(IProgress.Progress)] = MapProgress,
            [nameof(IProgress.ProgressColor)] = MapProgressColor,
        };

    public ProgressBarHandler() : base(Mapper) { }

    protected override UIProgressView CreatePlatformView()
    {
        return new UIProgressView(UIProgressViewStyle.Default);
    }

    public static void MapProgress(ProgressBarHandler handler, IProgress progress)
    {
        handler.PlatformView.Progress = (float)Math.Clamp(progress.Progress, 0, 1);
    }

    public static void MapProgressColor(ProgressBarHandler handler, IProgress progress)
    {
        handler.PlatformView.ProgressTintColor = progress.ProgressColor?.ToPlatformColor();
    }
}

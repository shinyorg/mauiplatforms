using CoreGraphics;
using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

public partial class ImageHandler : TvOSViewHandler<IImage, UIImageView>
{
    public static readonly IPropertyMapper<IImage, ImageHandler> Mapper =
        new PropertyMapper<IImage, ImageHandler>(ViewMapper)
        {
            [nameof(IImage.Aspect)] = MapAspect,
            [nameof(IImage.IsOpaque)] = MapIsOpaque,
            [nameof(IImageSourcePart.Source)] = MapSource,
        };

    public ImageHandler() : base(Mapper)
    {
    }

    protected override UIImageView CreatePlatformView()
    {
        return new UIImageView
        {
            ContentMode = UIViewContentMode.ScaleAspectFit,
            ClipsToBounds = true,
        };
    }

    public static void MapAspect(ImageHandler handler, IImage image)
    {
        handler.PlatformView.ContentMode = image.Aspect switch
        {
            Aspect.AspectFill => UIViewContentMode.ScaleAspectFill,
            Aspect.Fill => UIViewContentMode.ScaleToFill,
            Aspect.Center => UIViewContentMode.Center,
            _ => UIViewContentMode.ScaleAspectFit,
        };
    }

    public static void MapIsOpaque(ImageHandler handler, IImage image)
    {
        handler.PlatformView.Opaque = image.IsOpaque;
    }

    public static void MapSource(ImageHandler handler, IImage image)
    {
        if (image is IImageSourcePart imageSourcePart)
            handler.LoadImageSource(imageSourcePart);
    }

    static readonly HttpClient Http = new();
    static readonly NSCache Cache = new() { CountLimit = 200 };

    // Bumped on every source change, so a download that finishes after the view moved on
    // to another source (a recycled CollectionView cell) is dropped instead of shown.
    int _loadVersion;

    void LoadImageSource(IImageSourcePart imageSourcePart)
    {
        var version = ++_loadVersion;
        var source = imageSourcePart.Source;
        if (source == null)
        {
            PlatformView.Image = null;
            imageSourcePart.UpdateIsLoading(false);
            return;
        }

        imageSourcePart.UpdateIsLoading(true);

        switch (source)
        {
            case IUriImageSource { Uri: { } uri }:
                if (Cache.ObjectForKey(new NSString(uri.AbsoluteUri)) is UIImage cached)
                {
                    PlatformView.Image = cached;
                    imageSourcePart.UpdateIsLoading(false);
                }
                else
                {
                    // Clear the previous image so a recycled view doesn't show stale art.
                    PlatformView.Image = null;
                    LoadFromUri(uri, imageSourcePart, version);
                }
                break;

            case IFileImageSource { File: { Length: > 0 } fileName }:
                PlatformView.Image = UIImage.FromBundle(fileName) ?? UIImage.FromFile(fileName);
                imageSourcePart.UpdateIsLoading(false);
                break;

            default:
                PlatformView.Image = null;
                imageSourcePart.UpdateIsLoading(false);
                break;
        }
    }

    async void LoadFromUri(Uri uri, IImageSourcePart imageSourcePart, int version)
    {
        try
        {
            var data = await Http.GetByteArrayAsync(uri);
            var uiImage = UIImage.LoadFromData(NSData.FromArray(data));
            if (uiImage != null)
                Cache.SetObjectforKey(uiImage, new NSString(uri.AbsoluteUri));

            if (version != _loadVersion || PlatformView == null)
                return;

            PlatformView.Image = uiImage;
            if (uiImage != null)
            {
                // Fade in rather than pop.
                PlatformView.Alpha = 0;
                UIView.Animate(0.25, () => PlatformView.Alpha = (nfloat)VirtualView.Opacity);
            }
            // Only an image without a fixed size can change the layout around it.
            if (VirtualView is { } view && !(view.Width >= 0 && view.Height >= 0))
                view.InvalidateMeasure();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ImageHandler] Failed to load image from {uri}: {ex.Message}");
        }
        finally
        {
            if (version == _loadVersion)
                imageSourcePart.UpdateIsLoading(false);
        }
    }
}

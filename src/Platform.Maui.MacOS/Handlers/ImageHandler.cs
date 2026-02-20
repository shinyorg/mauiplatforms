using CoreGraphics;
using Foundation;
using Microsoft.Maui.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// NSImageView subclass that uses flipped coordinates and reports intrinsic content size.
/// </summary>
internal class FlippedNSImageView : NSImageView
{
    public FlippedNSImageView()
    {
        WantsLayer = true;
        ImageScaling = NSImageScale.ProportionallyUpOrDown;
    }

    public override bool IsFlipped => true;
}

public partial class ImageHandler : MacOSViewHandler<IImage, NSImageView>
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

    protected override NSImageView CreatePlatformView()
    {
        return new FlippedNSImageView();
    }

    public static void MapAspect(ImageHandler handler, IImage image)
    {
        handler.PlatformView.ImageScaling = image.Aspect switch
        {
            Aspect.AspectFill => NSImageScale.ProportionallyUpOrDown,
            Aspect.Fill => NSImageScale.AxesIndependently,
            Aspect.Center => NSImageScale.None,
            _ => NSImageScale.ProportionallyDown, // AspectFit
        };
    }

    public static void MapIsOpaque(ImageHandler handler, IImage image)
    {
        // NSImageView doesn't have a direct Opaque property like UIKit
        // WantsLayer + layer.Opaque is the closest equivalent
        if (handler.PlatformView.Layer != null)
            handler.PlatformView.Layer.Opaque = image.IsOpaque;
    }

    public static void MapSource(ImageHandler handler, IImage image)
    {
        if (image is not IImageSourcePart imageSourcePart)
            return;

        handler.LoadImageSource(imageSourcePart);
    }

    void LoadImageSource(IImageSourcePart imageSourcePart)
    {
        var source = imageSourcePart.Source;
        if (source == null)
        {
            PlatformView.Image = null;
            imageSourcePart.UpdateIsLoading(false);
            return;
        }

        imageSourcePart.UpdateIsLoading(true);

        if (source is IFileImageSource fileImageSource)
        {
            var fileName = fileImageSource.File;
            if (!string.IsNullOrEmpty(fileName))
            {
                // Try loading from app bundle, then from file path
                var nsImage = new NSImage(fileName);
                if (nsImage.Size.Width == 0 && nsImage.Size.Height == 0)
                {
                    // Try bundle resource
                    var bundlePath = NSBundle.MainBundle.PathForResource(
                        System.IO.Path.GetFileNameWithoutExtension(fileName),
                        System.IO.Path.GetExtension(fileName)?.TrimStart('.'));
                    if (bundlePath != null)
                        nsImage = new NSImage(bundlePath);
                }

                PlatformView.Image = nsImage;
            }

            imageSourcePart.UpdateIsLoading(false);
        }
        else if (source is IUriImageSource uriImageSource)
        {
            var uri = uriImageSource.Uri;
            if (uri != null)
            {
                LoadFromUri(uri, imageSourcePart);
            }
            else
            {
                PlatformView.Image = null;
                imageSourcePart.UpdateIsLoading(false);
            }
        }
        else if (source is IStreamImageSource streamImageSource)
        {
            LoadFromStream(streamImageSource, imageSourcePart);
        }
        else
        {
            PlatformView.Image = null;
            imageSourcePart.UpdateIsLoading(false);
        }
    }

    async void LoadFromUri(Uri uri, IImageSourcePart imageSourcePart)
    {
        try
        {
            using var client = new HttpClient();
            var data = await client.GetByteArrayAsync(uri);
            var nsData = NSData.FromArray(data);
            var nsImage = new NSImage(nsData);

            if (PlatformView != null)
                PlatformView.Image = nsImage;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load image from URI: {ex.Message}");
        }
        finally
        {
            imageSourcePart.UpdateIsLoading(false);
        }
    }

    async void LoadFromStream(IStreamImageSource streamSource, IImageSourcePart imageSourcePart)
    {
        try
        {
            var stream = await streamSource.GetStreamAsync(CancellationToken.None);
            if (stream != null)
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var nsData = NSData.FromArray(ms.ToArray());
                var nsImage = new NSImage(nsData);

                if (PlatformView != null)
                    PlatformView.Image = nsImage;

                stream.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load image from stream: {ex.Message}");
        }
        finally
        {
            imageSourcePart.UpdateIsLoading(false);
        }
    }
}

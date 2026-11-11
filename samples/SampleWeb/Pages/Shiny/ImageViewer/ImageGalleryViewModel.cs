using CommunityToolkit.Mvvm.ComponentModel;

namespace Sample.Features.ImageViewer;

public partial class GalleryImage : ObservableObject
{
    public required string Uri { get; init; }
    public required string Title { get; init; }

    [ObservableProperty]
    bool isOpen;
}

public partial class ImageGalleryViewModel : ObservableObject
{
    // Remote URIs on purpose: each cell shows its own loading ring, and opening one full-screen
    // resolves off the memory cache the thumbnail already filled rather than downloading again.
    public List<GalleryImage> Images { get; } = new()
    {
        new GalleryImage { Uri = "https://picsum.photos/seed/shiny-gallery-1/1200/800", Title = "Harbour" },
        new GalleryImage { Uri = "https://picsum.photos/seed/shiny-gallery-2/1200/800", Title = "Ridge Line" },
        new GalleryImage { Uri = "https://picsum.photos/seed/shiny-gallery-3/1200/800", Title = "Old Town" },
        new GalleryImage { Uri = "https://picsum.photos/seed/shiny-gallery-4/1200/800", Title = "Low Tide" },
        new GalleryImage { Uri = "https://picsum.photos/seed/shiny-gallery-5/1200/800", Title = "Switchback" },
        new GalleryImage { Uri = "https://picsum.photos/seed/shiny-gallery-6/1200/800", Title = "Long Exposure" }
    };
}

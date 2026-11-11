using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Shiny.Controls.Office.Skia;
using SkiaSharp.Views.Maui.Controls;

namespace SampleWeb.Pages.Shiny;

/// <summary>
/// Gives the Office pages real fonts. SkiaSharp on WebAssembly has no system fonts, so without this
/// every document draws in Skia's one monospace fallback. Carlito and Caladea are metric-compatible
/// with Calibri and Cambria (what Shiny.Blazor.Controls.Office bundles for the same reason), and the
/// Office layouts re-flow by themselves once the registry grows, so the fonts can arrive after the
/// first paint.
/// </summary>
public static class OfficeWebFonts
{
	static readonly string[] Files =
	[
		"Carlito-Regular.ttf", "Carlito-Bold.ttf", "Carlito-Italic.ttf", "Carlito-BoldItalic.ttf",
		"Caladea-Regular.ttf", "Caladea-Bold.ttf", "Caladea-Italic.ttf", "Caladea-BoldItalic.ttf",
	];

	static Task? _loading;

	/// <summary>Loads the bundled faces once (they are MauiAssets, served from the site root).</summary>
	public static Task EnsureLoadedAsync() => _loading ??= LoadAsync();

	static async Task LoadAsync()
	{
		foreach (var file in Files)
		{
			try
			{
				await using var stream = await FileSystem.OpenAppPackageFileAsync("OfficeFonts/" + file);
				using var buffer = new MemoryStream();
				await stream.CopyToAsync(buffer);
				OfficeFontRegistry.Default.Register(buffer.ToArray());
			}
			catch (Exception ex)
			{
				// A missing face falls back within its family; a missing family falls back to Skia's.
				Console.Error.WriteLine($"[SampleWeb] Office font {file} failed to load: {ex.Message}");
			}
		}
	}

	/// <summary>Wraps an Office page so its canvases repaint once the fonts are in.</summary>
	public static Page With(Page page)
	{
		_ = RepaintWhenLoadedAsync(page);
		return page;
	}

	static async Task RepaintWhenLoadedAsync(Page page)
	{
		await EnsureLoadedAsync();
		foreach (var canvas in page.GetVisualTreeDescendants().OfType<SKCanvasView>())
			canvas.InvalidateSurface();
	}
}

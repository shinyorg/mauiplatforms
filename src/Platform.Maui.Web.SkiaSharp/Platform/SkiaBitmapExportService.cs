using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;

namespace Microsoft.Maui.Platform.Web.Skia;

/// <summary>
/// MAUI's <see cref="IBitmapExportService"/> backed by Skia. Plain <c>net10.0</c> has no platform bitmap
/// export, so drawing to an image (a signature, an edited photo) needs one registered; Microsoft.Maui.Graphics.Skia
/// ships the context but no service around it.
/// </summary>
public sealed class SkiaBitmapExportService : IBitmapExportService
{
	public BitmapExportContext CreateContext(int width, int height, float displayScale = 1)
		=> new SkiaBitmapExportContext(width, height, displayScale);
}

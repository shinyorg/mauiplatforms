using System.Reflection;
using Microsoft.Maui.Platform.Web.Interop;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// Resolves MAUI image sources to URLs an &lt;img&gt; can display.
/// </summary>
public static class WebImageSource
{
	static HashSet<string>? _vectorImages;

	/// <summary>
	/// Resolves a source to a URL. Stream and font sources produce blob/data URLs; callers
	/// release blob URLs with <see cref="Release"/> when the image changes.
	/// </summary>
	public static async Task<string?> ResolveAsync(IImageSource? source, IFontManager? fontManager, CancellationToken token = default)
	{
		switch (source)
		{
			case null:
				return null;
			case IFileImageSource file:
				return ResolveFile(file.File);
			case IUriImageSource uri:
				return uri.Uri?.ToString();
			case IFontImageSource font:
				var family = (fontManager as WebFontManager)?.GetFontFamily(font.Font.Family) ?? "var(--maui-font)";
				var size = font.Font.Size > 0 ? font.Font.Size : 24;
				return await Dom.RenderGlyph(font.Glyph, family, size, WebCss.Color(font.Color) ?? "#000");
			case IStreamImageSource stream:
				await using (var s = await stream.GetStreamAsync(token))
				{
					if (s is null)
						return null;
					using var memory = new MemoryStream();
					await s.CopyToAsync(memory, token);
					return Dom.CreateObjectUrl(new ArraySegment<byte>(memory.GetBuffer(), 0, (int)memory.Length), Sniff(memory.GetBuffer()));
				}
			default:
				return null;
		}
	}

	public static void Release(string? url) => Dom.RevokeObjectUrl(url);

	/// <summary>
	/// MAUI apps reference "name.png" for an SVG MauiImage. The build targets deploy SVGs as-is
	/// (browsers render them natively) and list them in assembly metadata so the name can be mapped.
	/// </summary>
	public static string? ResolveFile(string? file)
	{
		if (string.IsNullOrWhiteSpace(file))
			return null;
		if (file.Contains("://") || file.StartsWith("data:", StringComparison.Ordinal) || file.StartsWith('/'))
			return file;

		var name = Path.GetFileNameWithoutExtension(file);
		if (!file.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) && VectorImages.Contains(name))
			return Path.ChangeExtension(file, ".svg");
		return file;
	}

	static HashSet<string> VectorImages => _vectorImages ??= LoadVectorImages();

	static HashSet<string> LoadVectorImages()
	{
		var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var assembly = Assembly.GetEntryAssembly();
		if (assembly is null)
			return set;
		foreach (var meta in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
		{
			if (meta.Key == "MauiWebVectorImage" && meta.Value is { Length: > 0 } value)
				set.Add(value);
		}
		return set;
	}

	static string? Sniff(byte[] data)
	{
		if (data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50)
			return "image/png";
		if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8)
			return "image/jpeg";
		if (data.Length >= 4 && data[0] == 0x47 && data[1] == 0x49)
			return "image/gif";
		if (data.Length >= 12 && data[8] == 0x57 && data[9] == 0x45)
			return "image/webp";
		if (data.Length >= 1 && data[0] == (byte)'<')
			return "image/svg+xml";
		return null;
	}
}

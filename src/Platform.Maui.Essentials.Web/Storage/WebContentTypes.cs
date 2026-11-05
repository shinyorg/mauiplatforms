using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Essentials.Web;

/// <summary>
/// Content types for files. MAUI's platform-neutral FileBase looks a missing content type up through
/// a method that throws NotImplementedInReferenceAssemblyException, so on the web a FileResult or
/// ReadOnlyFile created without one must never have ContentType read directly.
/// </summary>
internal static class WebContentTypes
{
	public const string Default = "application/octet-stream";

	static readonly Dictionary<string, string> ByExtension = new(StringComparer.OrdinalIgnoreCase)
	{
		[".png"] = "image/png",
		[".jpg"] = "image/jpeg",
		[".jpeg"] = "image/jpeg",
		[".gif"] = "image/gif",
		[".webp"] = "image/webp",
		[".avif"] = "image/avif",
		[".bmp"] = "image/bmp",
		[".svg"] = "image/svg+xml",
		[".heic"] = "image/heic",
		[".heif"] = "image/heif",
		[".ico"] = "image/x-icon",
		[".mp4"] = "video/mp4",
		[".m4v"] = "video/mp4",
		[".mov"] = "video/quicktime",
		[".webm"] = "video/webm",
		[".mp3"] = "audio/mpeg",
		[".m4a"] = "audio/mp4",
		[".wav"] = "audio/wav",
		[".ogg"] = "audio/ogg",
		[".pdf"] = "application/pdf",
		[".zip"] = "application/zip",
		[".json"] = "application/json",
		[".xml"] = "application/xml",
		[".txt"] = "text/plain",
		[".csv"] = "text/csv",
		[".html"] = "text/html",
		[".htm"] = "text/html",
		[".css"] = "text/css",
		[".js"] = "text/javascript",
		[".md"] = "text/markdown",
	};

	/// <summary>The content type for a file name, from its extension.</summary>
	public static string FromFileName(string? fileName)
		=> ByExtension.TryGetValue(Path.GetExtension(fileName ?? string.Empty), out var type) ? type : Default;

	/// <summary>The file's content type, falling back to its extension when MAUI cannot provide one.</summary>
	public static string Of(FileBase file)
	{
		try
		{
			var type = file.ContentType;
			if (!string.IsNullOrEmpty(type))
				return type;
		}
		catch (Exception ex) when (ex is NotImplementedException or NotSupportedException
			|| ex.GetType().Name == "NotImplementedInReferenceAssemblyException")
		{
		}
		return FromFileName(file.FileName);
	}
}

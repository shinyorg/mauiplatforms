using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Essentials.Web.Interop;
using Microsoft.Maui.Platform.Web;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Essentials.Web.Storage;

/// <summary>
/// Cache and app data live in the WebAssembly runtime's in-memory file system: they work with
/// System.IO but are discarded when the page reloads. App package files (MauiAsset) are deployed
/// next to index.html and fetched over HTTP.
/// </summary>
public class WebFileSystem : IFileSystem
{
	static readonly HttpClient Http = new();

	public string CacheDirectory => Ensure(Path.Combine(Path.GetTempPath(), "maui", "cache"));

	public string AppDataDirectory => Ensure(Path.Combine(Path.GetTempPath(), "maui", "data"));

	public async Task<Stream> OpenAppPackageFileAsync(string filename)
	{
		var response = await Http.GetAsync(PackageUri(filename), HttpCompletionOption.ResponseHeadersRead);
		if (!response.IsSuccessStatusCode)
		{
			response.Dispose();
			throw new FileNotFoundException($"App package file '{filename}' was not found ({(int)response.StatusCode}).", filename);
		}
		return await response.Content.ReadAsStreamAsync();
	}

	public async Task<bool> AppPackageFileExistsAsync(string filename)
	{
		using var request = new HttpRequestMessage(HttpMethod.Head, PackageUri(filename));
		using var response = await Http.SendAsync(request);
		return response.IsSuccessStatusCode;
	}

	static Uri PackageUri(string filename)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filename);
		var relative = filename.Replace('\\', '/').TrimStart('/');
		return new Uri(new Uri(WebMauiApplication.BaseUri), Uri.EscapeDataString(relative).Replace("%2F", "/"));
	}

	static string Ensure(string path)
	{
		Directory.CreateDirectory(path);
		return path;
	}
}

/// <summary>Common <see cref="FilePickerFileType"/>s with an entry for <see cref="Devices.WebDevicePlatform.Web"/>.</summary>
public static class WebFilePickerFileTypes
{
	public static FilePickerFileType Images { get; } = Create("image/*");

	public static FilePickerFileType Png { get; } = Create("image/png", ".png");

	public static FilePickerFileType Jpeg { get; } = Create("image/jpeg", ".jpg", ".jpeg");

	public static FilePickerFileType Videos { get; } = Create("video/*");

	public static FilePickerFileType Pdf { get; } = Create("application/pdf", ".pdf");

	/// <summary>Creates a file type from MIME types and/or extensions (the &lt;input accept&gt; syntax).</summary>
	public static FilePickerFileType Create(params string[] acceptTypes)
		=> new(new Dictionary<Microsoft.Maui.Devices.DevicePlatform, IEnumerable<string>> { [Devices.WebDevicePlatform.Web] = acceptTypes });
}

/// <summary>
/// File picking through a hidden &lt;input type="file"&gt;. Call from a user gesture (e.g. a
/// Clicked handler) or the browser blocks the dialog. Picked files are copied into the in-memory
/// file system because MAUI's FileResult can only open files by path.
/// </summary>
public class WebFilePicker : IFilePicker
{
	public async Task<FileResult?> PickAsync(PickOptions? options = null)
		=> (await PickFilesAsync(Accept(options), multiple: false, capture: null)).FirstOrDefault();

	public async Task<IEnumerable<FileResult>?> PickMultipleAsync(PickOptions? options = null)
		=> (await PickFilesAsync(Accept(options), multiple: true, capture: null)).OfType<FileResult>().ToList();

	internal static string? Accept(PickOptions? options)
	{
		if (options?.FileTypes is not { } fileTypes)
			return null;
		try
		{
			var types = fileTypes.Value?.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
			return types is { Length: > 0 } ? string.Join(',', types) : null;
		}
		catch (Exception ex) when (ex is PlatformNotSupportedException or NotImplementedException or FeatureNotSupportedException)
		{
			// The file type has no Web entry (MAUI's built-in types only know native platforms).
			return null;
		}
	}

	internal static async Task<List<FileResult?>> PickFilesAsync(string? accept, bool multiple, string? capture)
	{
		await EssentialsJs.EnsureLoadedAsync();
		var batch = await EssentialsJs.PickFiles(accept, multiple, capture);
		var results = new List<FileResult?>();
		if (batch == 0)
			return results;

		try
		{
			var folder = Path.Combine(Path.GetTempPath(), "maui", "cache", "picked", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			var count = EssentialsJs.BatchCount(batch);
			for (var i = 0; i < count; i++)
			{
				var name = Path.GetFileName(EssentialsJs.BatchFileName(batch, i));
				if (string.IsNullOrEmpty(name))
					name = $"file{i}";
				var type = EssentialsJs.BatchFileType(batch, i);
				var bytes = EssentialsJs.TakeBuffer(await EssentialsJs.BatchFileRead(batch, i));
				var path = Path.Combine(folder, name);
				await File.WriteAllBytesAsync(path, bytes);
				results.Add(string.IsNullOrEmpty(type) ? new FileResult(path) : new FileResult(path, type));
			}
		}
		finally
		{
			EssentialsJs.ReleaseBatch(batch);
		}
		return results;
	}
}

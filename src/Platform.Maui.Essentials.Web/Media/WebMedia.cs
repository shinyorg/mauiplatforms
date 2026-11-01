using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Microsoft.Maui.Essentials.Web.Interop;
using Microsoft.Maui.Essentials.Web.Storage;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Essentials.Web.Media;

/// <summary>Web Speech API synthesis.</summary>
public class WebTextToSpeech : ITextToSpeech
{
	public async Task<IEnumerable<Locale>> GetLocalesAsync()
	{
		await EssentialsJs.EnsureLoadedAsync();
		var records = await EssentialsJs.GetVoices();
		return records.Split('\u001e', StringSplitOptions.RemoveEmptyEntries)
			.Select(r => r.Split('\u001f'))
			.Where(f => f.Length == 3)
			.Select(f =>
			{
				var lang = f[0];
				var dash = lang.IndexOf('-');
				return CreateLocale(dash > 0 ? lang[..dash] : lang, dash > 0 ? lang[(dash + 1)..] : string.Empty, f[1], f[2]);
			})
			.ToList();
	}

	public async Task SpeakAsync(string text, SpeechOptions? options = null, CancellationToken cancelToken = default)
	{
		ArgumentNullException.ThrowIfNull(text);
		await EssentialsJs.EnsureLoadedAsync();

		var locale = options?.Locale;
		var lang = locale is null ? null : string.IsNullOrEmpty(locale.Country) ? locale.Language : $"{locale.Language}-{locale.Country}";
		using var registration = cancelToken.Register(EssentialsJs.CancelSpeech);
		await EssentialsJs.Speak(text, locale?.Id, lang, options?.Pitch ?? -1, options?.Volume ?? -1, options?.Rate ?? -1);
		cancelToken.ThrowIfCancellationRequested();
	}

	// Locale's constructor is internal; backends have no other way to create one.
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Locale's constructors are preserved by the DynamicDependency.")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(Locale))]
	static Locale CreateLocale(string language, string country, string name, string id)
		=> (Locale)Activator.CreateInstance(typeof(Locale), BindingFlags.Instance | BindingFlags.NonPublic, null,
			[language, country, name, id], null)!;
}

/// <summary>
/// Media picking through &lt;input type="file"&gt;. Capture uses the input's capture attribute,
/// which opens the camera on phones and tablets; desktop browsers fall back to a file dialog,
/// so capture is only reported as supported on touch-first devices.
/// </summary>
public class WebMediaPicker : IMediaPicker
{
	public bool IsCaptureSupported => BrowserJs.MatchMedia("(pointer: coarse)");

	public async Task<FileResult?> PickPhotoAsync(MediaPickerOptions? options = null)
		=> (await WebFilePicker.PickFilesAsync("image/*", multiple: false, capture: null)).FirstOrDefault();

	public async Task<List<FileResult>> PickPhotosAsync(MediaPickerOptions? options = null)
		=> Limit(await WebFilePicker.PickFilesAsync("image/*", multiple: true, capture: null), options);

	public Task<FileResult?> CapturePhotoAsync(MediaPickerOptions? options = null) => CaptureAsync("image/*");

	public async Task<FileResult?> PickVideoAsync(MediaPickerOptions? options = null)
		=> (await WebFilePicker.PickFilesAsync("video/*", multiple: false, capture: null)).FirstOrDefault();

	public async Task<List<FileResult>> PickVideosAsync(MediaPickerOptions? options = null)
		=> Limit(await WebFilePicker.PickFilesAsync("video/*", multiple: true, capture: null), options);

	public Task<FileResult?> CaptureVideoAsync(MediaPickerOptions? options = null) => CaptureAsync("video/*");

	async Task<FileResult?> CaptureAsync(string accept)
	{
		if (!IsCaptureSupported)
			throw new FeatureNotSupportedException("Camera capture is only available on touch devices in the browser.");
		return (await WebFilePicker.PickFilesAsync(accept, multiple: false, capture: "environment")).FirstOrDefault();
	}

	// The browser cannot cap a multi-select, so the limit is applied afterwards.
	static List<FileResult> Limit(List<FileResult?> results, MediaPickerOptions? options)
	{
		var files = results.OfType<FileResult>();
		return (options is { SelectionLimit: > 0 } ? files.Take(options.SelectionLimit) : files).ToList();
	}
}

/// <summary>Pages cannot capture their own pixels without user-mediated screen sharing.</summary>
public class WebScreenshot : IScreenshot
{
	public bool IsCaptureSupported => false;

	public Task<IScreenshotResult> CaptureAsync() => throw new FeatureNotSupportedException("Screenshots are not available in the browser.");
}

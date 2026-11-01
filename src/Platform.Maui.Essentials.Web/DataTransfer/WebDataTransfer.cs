using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Essentials.Web.Interop;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Essentials.Web.DataTransfer;

/// <summary>
/// Async Clipboard API. Reading needs permission (and focus); HasText cannot query the clipboard
/// synchronously, so it reflects what this page last copied or observed being copied.
/// </summary>
public class WebClipboard : IClipboard
{
	EventHandler<EventArgs>? _changed;
	bool _observing;
	bool _hasText;

	public bool HasText => _hasText;

	public async Task SetTextAsync(string? text)
	{
		await BrowserJs.ClipboardWrite(text ?? string.Empty);
		_hasText = !string.IsNullOrEmpty(text);
		_changed?.Invoke(this, EventArgs.Empty);
	}

	public async Task<string?> GetTextAsync()
	{
		var text = await BrowserJs.ClipboardRead();
		_hasText = !string.IsNullOrEmpty(text);
		return text;
	}

	public event EventHandler<EventArgs> ClipboardContentChanged
	{
		add
		{
			_changed += value;
			if (_observing)
				return;
			_observing = true;
			// Only copies and cuts inside this document are observable.
			EssentialsJs.WhenLoaded(() => EssentialsJs.ObserveClipboard(() =>
			{
				_hasText = true;
				_changed?.Invoke(this, EventArgs.Empty);
			}));
		}
		remove => _changed -= value;
	}
}

/// <summary>
/// Web Share API. Where sharing is unavailable (most desktop browsers for files), text falls back
/// to the clipboard and files fall back to downloads.
/// </summary>
public class WebShare(IClipboard clipboard) : IShare
{
	public async Task RequestAsync(ShareTextRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);
		await EssentialsJs.EnsureLoadedAsync();
		if (EssentialsJs.CanShare(withFiles: false))
		{
			await EssentialsJs.Share(request.Title ?? request.Subject, request.Text, request.Uri, withFiles: false);
			return;
		}

		var text = string.Join(Environment.NewLine, new[] { request.Text, request.Uri }.Where(s => !string.IsNullOrEmpty(s)));
		if (text.Length == 0)
			throw new FeatureNotSupportedException("Sharing is not supported in this browser.");
		await clipboard.SetTextAsync(text);
	}

	public Task RequestAsync(ShareFileRequest request)
	{
		ArgumentNullException.ThrowIfNull(request?.File);
		return ShareFilesAsync(request.Title, [request.File]);
	}

	public Task RequestAsync(ShareMultipleFilesRequest request)
	{
		ArgumentNullException.ThrowIfNull(request?.Files);
		return ShareFilesAsync(request.Title, request.Files);
	}

	static async Task ShareFilesAsync(string? title, IEnumerable<FileBase> files)
	{
		await EssentialsJs.EnsureLoadedAsync();
		var list = files.ToList();
		EssentialsJs.ClearShareFiles();
		foreach (var file in list)
		{
			var bytes = await File.ReadAllBytesAsync(file.FullPath);
			EssentialsJs.AddShareFile(new ArraySegment<byte>(bytes), file.FileName, file.ContentType);
		}

		if (EssentialsJs.CanShare(withFiles: true))
		{
			await EssentialsJs.Share(title, null, null, withFiles: true);
			return;
		}

		EssentialsJs.ClearShareFiles();
		foreach (var file in list)
		{
			var bytes = await File.ReadAllBytesAsync(file.FullPath);
			EssentialsJs.Download(new ArraySegment<byte>(bytes), file.FileName, file.ContentType);
		}
	}
}

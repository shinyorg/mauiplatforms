using System.Runtime.InteropServices.JavaScript;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Essentials.Web.Interop;

/// <summary>
/// [JSImport] bindings for wwwroot/maui-web-essentials.js. The module loads asynchronously, so
/// every caller awaits <see cref="EnsureLoadedAsync"/> first; synchronous reads use <see cref="BrowserJs"/>.
/// </summary>
internal static partial class EssentialsJs
{
	public const string Module = "maui-web-essentials";
	public const string ContentPath = "_content/Platform.Maui.Web.Essentials/";

	static Task? _load;

	public static bool IsLoaded => _load?.IsCompletedSuccessfully == true;

	public static Task EnsureLoadedAsync()
		=> _load ??= JSHost.ImportAsync(Module, new Uri(new Uri(WebMauiApplication.BaseUri), ContentPath + "maui-web-essentials.js").ToString());

	/// <summary>Runs <paramref name="action"/> once the module is loaded; failures are logged, not thrown.</summary>
	public static async void WhenLoaded(Action action)
	{
		try
		{
			await EnsureLoadedAsync();
			action();
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[maui-web-essentials] {ex.Message}");
		}
	}

	/// <summary>Copies a buffer parked by the module into managed memory.</summary>
	public static byte[] TakeBuffer(int id)
	{
		var bytes = new byte[BufferLength(id)];
		CopyBuffer(id, new ArraySegment<byte>(bytes));
		return bytes;
	}

	[JSImport("bufferLength", Module)]
	public static partial int BufferLength(int id);

	[JSImport("copyBuffer", Module)]
	public static partial void CopyBuffer(int id, [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> view);

	[JSImport("pickFiles", Module)]
	public static partial Task<int> PickFiles(string? accept, bool multiple, string? capture);

	[JSImport("batchCount", Module)]
	public static partial int BatchCount(int batch);

	[JSImport("batchFileName", Module)]
	public static partial string BatchFileName(int batch, int index);

	[JSImport("batchFileType", Module)]
	public static partial string BatchFileType(int batch, int index);

	[JSImport("batchFileRead", Module)]
	public static partial Task<int> BatchFileRead(int batch, int index);

	[JSImport("releaseBatch", Module)]
	public static partial void ReleaseBatch(int batch);

	[JSImport("openWindow", Module)]
	public static partial void OpenWindow(string url);

	[JSImport("navigate", Module)]
	public static partial void Navigate(string url);

	[JSImport("download", Module)]
	public static partial void Download([JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> bytes, string fileName, string? mime);

	[JSImport("addShareFile", Module)]
	public static partial void AddShareFile([JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> bytes, string fileName, string? mime);

	[JSImport("clearShareFiles", Module)]
	public static partial void ClearShareFiles();

	[JSImport("canShare", Module)]
	public static partial bool CanShare(bool withFiles);

	[JSImport("share", Module)]
	public static partial Task<bool> Share(string? title, string? text, string? url, bool withFiles);

	[JSImport("setKeepScreenOn", Module)]
	public static partial bool SetKeepScreenOn(bool on);

	[JSImport("observeDisplay", Module)]
	public static partial void ObserveDisplay([JSMarshalAs<JSType.Function>] Action callback);

	[JSImport("startBattery", Module)]
	public static partial Task<bool> StartBattery(
		[JSMarshalAs<JSType.Function<JSType.Number, JSType.Boolean, JSType.Number>>] Action<double, bool, double> callback);

	[JSImport("vibrate", Module)]
	public static partial bool Vibrate(int ms);

	[JSImport("observeConnectivity", Module)]
	public static partial void ObserveConnectivity([JSMarshalAs<JSType.Function>] Action callback);

	[JSImport("connectionType", Module)]
	public static partial string ConnectionType();

	[JSImport("observeClipboard", Module)]
	public static partial void ObserveClipboard([JSMarshalAs<JSType.Function>] Action callback);

	[JSImport("sensorSupported", Module)]
	public static partial bool SensorSupported(string kind);

	[JSImport("startSensor", Module)]
	public static partial Task<int> StartSensor(string kind, double intervalMs,
		[JSMarshalAs<JSType.Function<JSType.Object>>] Action<JSObject> callback);

	[JSImport("stopSensor", Module)]
	public static partial void StopSensor(int id);

	[JSImport("getLocation", Module)]
	public static partial Task<JSObject> GetLocation(bool highAccuracy, double timeoutMs);

	[JSImport("watchLocation", Module)]
	public static partial int WatchLocation(bool highAccuracy, [JSMarshalAs<JSType.Function<JSType.Object>>] Action<JSObject> callback);

	[JSImport("clearWatch", Module)]
	public static partial void ClearWatch(int id);

	[JSImport("getVoices", Module)]
	public static partial Task<string> GetVoices();

	[JSImport("speak", Module)]
	public static partial Task Speak(string text, string? voiceUri, string? lang, double pitch, double volume, double rate);

	[JSImport("cancelSpeech", Module)]
	public static partial void CancelSpeech();

	[JSImport("contactsSupported", Module)]
	public static partial bool ContactsSupported();

	[JSImport("pickContact", Module)]
	public static partial Task<string?> PickContact();
}

/// <summary>
/// Direct browser bindings that need no module, so synchronous Essentials APIs work from
/// the first page constructor. globalThis imports are bound to their owning object.
/// </summary>
internal static partial class BrowserJs
{
	[JSImport("globalThis.localStorage.getItem")]
	public static partial string? StorageGet(string key);

	[JSImport("globalThis.localStorage.setItem")]
	public static partial void StorageSet(string key, string value);

	[JSImport("globalThis.localStorage.removeItem")]
	public static partial void StorageRemove(string key);

	[JSImport("globalThis.localStorage.key")]
	public static partial string? StorageKey(int index);

	public static int StorageLength => Global("localStorage")?.GetPropertyAsInt32("length") ?? 0;

	[JSImport("globalThis.matchMedia")]
	private static partial JSObject MatchMediaQuery(string query);

	public static bool MatchMedia(string query) => MatchMediaQuery(query).GetPropertyAsBoolean("matches");

	[JSImport("globalThis.navigator.clipboard.writeText")]
	public static partial Task ClipboardWrite(string text);

	[JSImport("globalThis.navigator.clipboard.readText")]
	public static partial Task<string> ClipboardRead();

	public static JSObject? Global(string name) => JSHost.GlobalThis.GetPropertyAsJSObject(name);

	public static bool Has(JSObject? target, string property) => target?.HasProperty(property) == true;

	public static string? NavigatorString(string property)
		=> Global("navigator") is { } navigator && navigator.HasProperty(property) ? navigator.GetPropertyAsString(property) : null;

	public static double Number(JSObject? target, string property, double fallback = double.NaN)
	{
		if (target is null || !target.HasProperty(property))
			return fallback;
		return target.GetTypeOfProperty(property) == "number" ? target.GetPropertyAsDouble(property) : fallback;
	}
}

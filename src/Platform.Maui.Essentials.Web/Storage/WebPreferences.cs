using System.Globalization;
using Microsoft.Maui.Essentials.Web.Interop;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Essentials.Web.Storage;

/// <summary>
/// Preferences in localStorage. Keys are namespaced by shared name ("maui:{sharedName}:{key}")
/// and values are stored with invariant formatting; DateTime round-trips through ToBinary like
/// the native implementations so Kind is preserved.
/// </summary>
public class WebPreferences : IPreferences
{
	const string DefaultSharedName = "default";

	public bool ContainsKey(string key, string? sharedName = null) => BrowserJs.StorageGet(Key(key, sharedName)) is not null;

	public void Remove(string key, string? sharedName = null) => BrowserJs.StorageRemove(Key(key, sharedName));

	public void Clear(string? sharedName = null)
	{
		var prefix = Prefix(sharedName);
		var keys = new List<string>();
		for (var i = 0; i < BrowserJs.StorageLength; i++)
		{
			if (BrowserJs.StorageKey(i) is { } k && k.StartsWith(prefix, StringComparison.Ordinal))
				keys.Add(k);
		}
		foreach (var k in keys)
			BrowserJs.StorageRemove(k);
	}

	public void Set<T>(string key, T value, string? sharedName = null)
	{
		if (value is null)
		{
			Remove(key, sharedName);
			return;
		}

		var text = value switch
		{
			string s => s,
			bool b => b ? "true" : "false",
			int or long or short or byte => Convert.ToString(value, CultureInfo.InvariantCulture)!,
			double d => d.ToString("R", CultureInfo.InvariantCulture),
			float f => f.ToString("R", CultureInfo.InvariantCulture),
			decimal m => m.ToString(CultureInfo.InvariantCulture),
			DateTime dt => dt.ToBinary().ToString(CultureInfo.InvariantCulture),
			DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
			_ => throw new NotSupportedException($"Value type '{typeof(T)}' is not supported by Preferences."),
		};
		BrowserJs.StorageSet(Key(key, sharedName), text);
	}

	public T Get<T>(string key, T defaultValue, string? sharedName = null)
	{
		var text = BrowserJs.StorageGet(Key(key, sharedName));
		if (text is null)
			return defaultValue;

		try
		{
			object? value = defaultValue switch
			{
				string => text,
				bool => text == "true",
				int => int.Parse(text, CultureInfo.InvariantCulture),
				long => long.Parse(text, CultureInfo.InvariantCulture),
				short => short.Parse(text, CultureInfo.InvariantCulture),
				byte => byte.Parse(text, CultureInfo.InvariantCulture),
				double => double.Parse(text, CultureInfo.InvariantCulture),
				float => float.Parse(text, CultureInfo.InvariantCulture),
				decimal => decimal.Parse(text, CultureInfo.InvariantCulture),
				DateTime => DateTime.FromBinary(long.Parse(text, CultureInfo.InvariantCulture)),
				DateTimeOffset => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
				// A null default (string? or Nullable<T>) gives no runtime type; fall back to T.
				_ when typeof(T) == typeof(string) => text,
				_ => null,
			};
			return value is T typed ? typed : defaultValue;
		}
		catch (FormatException)
		{
			return defaultValue;
		}
		catch (OverflowException)
		{
			return defaultValue;
		}
	}

	static string Prefix(string? sharedName) => $"maui:{(string.IsNullOrEmpty(sharedName) ? DefaultSharedName : sharedName)}:";

	static string Key(string key, string? sharedName)
	{
		ArgumentNullException.ThrowIfNull(key);
		return Prefix(sharedName) + key;
	}
}

/// <summary>
/// Browsers have no OS-backed secret store; anything in localStorage or IndexedDB is readable by
/// any script on the origin. Rather than pretend, SecureStorage reports it is unsupported.
/// </summary>
public class WebSecureStorage : ISecureStorage
{
	const string Message = "SecureStorage is not available in the browser: there is no OS-protected secret store. " +
		"Keep secrets on a server, or use Preferences for non-sensitive data.";

	public Task<string?> GetAsync(string key) => throw new PlatformNotSupportedException(Message);

	public Task SetAsync(string key, string value) => throw new PlatformNotSupportedException(Message);

	public bool Remove(string key) => throw new PlatformNotSupportedException(Message);

	public void RemoveAll() => throw new PlatformNotSupportedException(Message);
}

using System.Reflection;
using Microsoft.Maui.Platform.Web.Handlers;
using Microsoft.Maui.Platform.Web.Interop;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// Registers MAUI fonts with the browser through the CSS Font Loading API. File fonts (MauiFont
/// items, deployed next to index.html by the build targets) load by URL; embedded fonts load from
/// their manifest resource bytes. Every registered font is addressable by alias and by file name.
/// </summary>
public class WebFontRegistrar : IFontRegistrar
{
	readonly Dictionary<string, string> _families = new(StringComparer.OrdinalIgnoreCase);

	public void Register(string filename, string? alias)
	{
		var family = AddFamily(filename, alias);
		Track(Dom.LoadFontUrl(family, filename));
	}

	public void Register(string filename, string? alias, Assembly assembly)
	{
		var family = AddFamily(filename, alias);
		var resource = assembly.GetManifestResourceNames()
			.FirstOrDefault(n => n.EndsWith(filename, StringComparison.OrdinalIgnoreCase));
		if (resource is null)
		{
			// ExportFont without an embedded resource: fall back to a deployed file.
			Track(Dom.LoadFontUrl(family, filename));
			return;
		}

		using var stream = assembly.GetManifestResourceStream(resource)!;
		using var memory = new MemoryStream();
		stream.CopyTo(memory);
		Track(Dom.LoadFontBytes(family, new ArraySegment<byte>(memory.GetBuffer(), 0, (int)memory.Length)));
	}

	/// <summary>Returns the CSS family registered for an alias or file name, or null.</summary>
	public string? GetFont(string font) => _families.TryGetValue(font, out var family) ? family : null;

	string AddFamily(string filename, string? alias)
	{
		var family = string.IsNullOrWhiteSpace(alias) ? Path.GetFileNameWithoutExtension(filename) : alias;
		_families[family] = family;
		_families[filename] = family;
		_families[Path.GetFileNameWithoutExtension(filename)] = family;
		return family;
	}

	static async void Track(Task load)
	{
		try
		{
			await load;
			// Text measured with a fallback font is now the wrong size.
			WebElementHandler.InvalidateAllMeasurements();
		}
		catch (Exception ex)
		{
			Dom.Log($"Font failed to load: {ex.Message}");
		}
	}
}

public class WebFontManager(IFontRegistrar registrar) : IFontManager
{
	public const double DefaultSize = 14;

	public double DefaultFontSize => DefaultSize;

	/// <summary>The CSS font-family list for a MAUI family name, or null to inherit.</summary>
	public string? GetFontFamily(string? family)
	{
		if (string.IsNullOrWhiteSpace(family))
			return null;

		// "File.ttf#Family" is the legacy MAUI/Forms form.
		var hash = family.IndexOf('#');
		var name = hash > 0 ? family[(hash + 1)..] : family;
		var resolved = registrar.GetFont(family) ?? registrar.GetFont(name) ?? name;
		return $"\"{WebCss.Escape(resolved)}\", var(--maui-font)";
	}

	public double GetFontSize(Font font) => font.Size > 0 && double.IsFinite(font.Size) ? font.Size : DefaultSize;

	/// <summary>Applies family, size, weight and slant to an element; unset values inherit.</summary>
	public void Apply(WebElement element, Font font)
	{
		element.SetStyle("font-family", GetFontFamily(font.Family));
		element.SetStyle("font-size", font.Size > 0 && double.IsFinite(font.Size) ? WebCss.FontSize(font.Size, font.AutoScalingEnabled) : null);
		element.SetStyle("font-weight", font.Weight == FontWeight.Regular ? null : WebCss.FontWeight(font.Weight));
		element.SetStyle("font-style", font.Slant == FontSlant.Default ? null : "italic");
	}
}

#pragma warning disable CS0612, CS0618 // IFontNamedSizeService is obsolete but Controls still resolves it for named sizes.
internal sealed class WebFontNamedSizeService : Microsoft.Maui.Controls.Internals.IFontNamedSizeService
{
	public double GetNamedSize(Microsoft.Maui.Controls.NamedSize size, Type targetElementType, bool useOldSizes) => size switch
	{
		Microsoft.Maui.Controls.NamedSize.Micro => 10,
		Microsoft.Maui.Controls.NamedSize.Small => 12,
		Microsoft.Maui.Controls.NamedSize.Medium => 16,
		Microsoft.Maui.Controls.NamedSize.Large => 20,
		Microsoft.Maui.Controls.NamedSize.Body => 14,
		Microsoft.Maui.Controls.NamedSize.Caption => 12,
		Microsoft.Maui.Controls.NamedSize.Header => 14,
		Microsoft.Maui.Controls.NamedSize.Subtitle => 18,
		Microsoft.Maui.Controls.NamedSize.Title => 24,
		_ => WebFontManager.DefaultSize,
	};
}
#pragma warning restore CS0612, CS0618

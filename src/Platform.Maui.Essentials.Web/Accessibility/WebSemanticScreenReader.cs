using Microsoft.Maui.Accessibility;
using Microsoft.Maui.Platform.Web.Interop;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Essentials.Web.Accessibility;

/// <summary>Announces through a visually hidden aria-live region.</summary>
public class WebSemanticScreenReader : ISemanticScreenReader
{
	WebElement? _region;
	bool _toggle;

	public void Announce(string text)
	{
		if (_region is null)
		{
			_region = new WebElement("div", "maui-sr-only");
			_region.SetAttribute("aria-live", "polite");
			_region.SetAttribute("role", "status");
			new WebElement(Dom.GetBody()).Append(_region);
		}

		// Screen readers ignore an unchanged live region; vary it so repeats are announced.
		_toggle = !_toggle;
		_region.SetText(_toggle ? text : text + " ");
	}
}

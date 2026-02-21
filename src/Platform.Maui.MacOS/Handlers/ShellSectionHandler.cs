using AppKit;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// Minimal ShellSection handler for macOS. Required for Shell's internal navigation
/// system (GoToAsync) to work. Must implement RequestNavigation command and call
/// NavigationFinished to complete the navigation pipeline.
/// </summary>
public partial class ShellSectionHandler : ElementHandler<ShellSection, NSView>
{
	public static readonly IPropertyMapper<ShellSection, ShellSectionHandler> Mapper =
		new PropertyMapper<ShellSection, ShellSectionHandler>(ElementMapper)
		{
			[nameof(ShellSection.CurrentItem)] = MapCurrentItem,
		};

	public static readonly CommandMapper<ShellSection, ShellSectionHandler> CommandMapper =
		new(ElementCommandMapper)
		{
			[nameof(IStackNavigation.RequestNavigation)] = RequestNavigation,
		};

	public ShellSectionHandler() : base(Mapper, CommandMapper) { }

	protected override NSView CreatePlatformElement()
	{
		return new NSView();
	}

	static void MapCurrentItem(ShellSectionHandler handler, ShellSection section)
	{
		// ShellHandler listens for Shell.CurrentItem changes and handles page switching
	}

	static void RequestNavigation(ShellSectionHandler handler, IStackNavigation view, object? arg)
	{
		if (arg is NavigationRequest request)
		{
			// Complete the navigation — the main ShellHandler handles rendering
			((IStackNavigation)view).NavigationFinished(request.NavigationStack);
		}
	}
}

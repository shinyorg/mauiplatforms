using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Graphics;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// Manages modal page presentation on macOS.
/// Modal pages are shown as overlay views on top of the window content,
/// with a semi-transparent backdrop. This matches MAUI's cross-platform
/// modal behavior while feeling native on macOS.
/// </summary>
internal class MacOSModalManager
{
	readonly FlippedNSView _contentContainer;
	readonly List<ModalEntry> _modalStack = new();

	record ModalEntry(Page Page, NSView BackdropView, NSView PageView, IMauiContext MauiContext);

	public MacOSModalManager(FlippedNSView contentContainer)
	{
		_contentContainer = contentContainer;
	}

	public int ModalCount => _modalStack.Count;
	public bool HasModals => _modalStack.Count > 0;

	public void PushModal(Page page, IMauiContext mauiContext, bool animated)
	{
		// Create a semi-transparent backdrop
		var backdrop = new NSView(_contentContainer.Bounds)
		{
			WantsLayer = true,
			AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable,
		};
		backdrop.Layer!.BackgroundColor = new CGColor(0, 0, 0, 0.4f);
		_contentContainer.AddSubview(backdrop);

		// Create the modal page view
		var platformView = ((IView)page).ToMacOSPlatform(mauiContext);

		// Modal pages are already positioned within safe bounds — skip safe area insets
		if (platformView is MacOSContainerView container)
			container.IgnorePlatformSafeArea = true;

		// Inset from window edges to create a "sheet" appearance
		var inset = GetModalInset();
		var pageFrame = new CGRect(
			inset, inset,
			_contentContainer.Bounds.Width - inset * 2,
			_contentContainer.Bounds.Height - inset * 2);

		platformView.Frame = pageFrame;
		platformView.WantsLayer = true;
		platformView.Layer!.CornerRadius = 10;
		platformView.Layer.MasksToBounds = true;
		platformView.Layer.BackgroundColor = NSColor.WindowBackground.CGColor;
		platformView.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
		_contentContainer.AddSubview(platformView);

		var entry = new ModalEntry(page, backdrop, platformView, mauiContext);
		_modalStack.Add(entry);

		// Measure and arrange the page
		LayoutModal(entry);
	}

	public Page? PopModal(bool animated)
	{
		if (_modalStack.Count == 0)
			return null;

		var entry = _modalStack[_modalStack.Count - 1];
		_modalStack.RemoveAt(_modalStack.Count - 1);
		RemoveModalViews(entry);

		return entry.Page;
	}

	void RemoveModalViews(ModalEntry entry)
	{
		entry.PageView.RemoveFromSuperview();
		entry.BackdropView.RemoveFromSuperview();
		entry.Page.Handler?.DisconnectHandler();
	}

	public void LayoutAllModals()
	{
		foreach (var entry in _modalStack)
			LayoutModal(entry);
	}

	void LayoutModal(ModalEntry entry)
	{
		var inset = GetModalInset();
		var bounds = _contentContainer.Bounds;
		var pageFrame = new CGRect(
			inset, inset,
			bounds.Width - inset * 2,
			bounds.Height - inset * 2);

		entry.BackdropView.Frame = bounds;
		entry.PageView.Frame = pageFrame;

		var page = entry.Page;
		page.Measure((double)pageFrame.Width, (double)pageFrame.Height);
		page.Arrange(new Rect(0, 0, (double)pageFrame.Width, (double)pageFrame.Height));
	}

	static nfloat GetModalInset() => 20;
}

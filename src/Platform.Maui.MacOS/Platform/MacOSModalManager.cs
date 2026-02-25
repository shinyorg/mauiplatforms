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
	readonly NSView _contentContainer;
	readonly List<ModalEntry> _modalStack = new();

	record ModalEntry(Page Page, NSView BackdropView, NSView EffectView, NSView PageView, IMauiContext MauiContext);

	public MacOSModalManager(NSView contentContainer)
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
		// Also prevent MAUI's PlatformArrange from overriding our frame
		if (platformView is MacOSContainerView container)
		{
			container.IgnorePlatformSafeArea = true;
			container.ExternalFrameManagement = true;
		}

		// Inset from visible area to create a "sheet" appearance
		var inset = GetModalInset();
		var safeRect = _contentContainer.SafeAreaRect;

		var pageFrame = new CGRect(
			safeRect.X + inset,
			safeRect.Y + inset,
			safeRect.Width - inset * 2,
			safeRect.Height - inset * 2);

		// Use NSVisualEffectView as the modal background container so it
		// automatically adapts to light/dark mode appearance changes.
		var effectView = new NSVisualEffectView(pageFrame)
		{
			Material = NSVisualEffectMaterial.WindowBackground,
			State = NSVisualEffectState.Active,
			BlendingMode = NSVisualEffectBlendingMode.BehindWindow,
			WantsLayer = true,
			AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable,
		};
		effectView.Layer!.CornerRadius = 10;
		effectView.Layer.MasksToBounds = true;

		platformView.Frame = new CGRect(0, 0, pageFrame.Width, pageFrame.Height);
		platformView.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
		effectView.AddSubview(platformView);

		_contentContainer.AddSubview(effectView);

		var entry = new ModalEntry(page, backdrop, effectView, platformView, mauiContext);
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
		entry.EffectView.RemoveFromSuperview();
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
		var safeRect = _contentContainer.SafeAreaRect;

		var pageFrame = new CGRect(
			safeRect.X + inset,
			safeRect.Y + inset,
			safeRect.Width - inset * 2,
			safeRect.Height - inset * 2);

		entry.BackdropView.Frame = bounds;
		entry.EffectView.Frame = pageFrame;
		entry.PageView.Frame = new CGRect(0, 0, pageFrame.Width, pageFrame.Height);

		// Trigger layout on the container — don't call page.Arrange directly
		// because PlatformArrange would reset our frame to (0,0)
		entry.PageView.NeedsLayout = true;
	}

	static nfloat GetModalInset() => 20;
}

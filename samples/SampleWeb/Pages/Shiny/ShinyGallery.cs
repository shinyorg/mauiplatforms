using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Sample.Features.AnimatedBackgrounds;
using Sample.Features.Avatars;
using Sample.Features.Badge;
using Sample.Features.BorderAnimations;
using Sample.Features.ButtonGroups;
using Sample.Features.Buttons;
using Sample.Features.Chips;
using Sample.Features.Confetti;
using Sample.Features.Dialogs;
using Sample.Features.Expanders;
using Sample.Features.Fab;
using Sample.Features.Gauges;
using Sample.Features.Marquee;
using Sample.Features.Media;
using Sample.Features.PasswordStrength;
using Sample.Features.Pills;
using Sample.Features.ProgressBar;
using Sample.Features.RangePickers;
using Sample.Features.Skeleton;
using Sample.Features.Slider;
using Sample.Features.Tags;
using Sample.Features.TextEntry;
using Sample.Features.Timeline;
using Sample.Features.Toast;

namespace SampleWeb.Pages.Shiny;

/// <summary>
/// The menu's Shiny Controls tab. Camera and MediaElement are web-specific demos of this repo's
/// Shiny.Maui.Controls.*.Web backends; the rest are copied from the Shiny controls sample
/// (shinyorg/controls, samples/Sample/Features) and run unchanged against the published packages.
/// </summary>
public static class ShinyGallery
{
	public static IReadOnlyList<GalleryItem> Items { get; } =
	[
		new("📷", "CameraView", () => new CameraWebPage()),
		new("🎬", "MediaElement", () => new MediaElementWebPage()),
		new("🔠", "VideoText", () => new VideoTextPage()),
		new("💊", "Pills", Bound<PillPage, PillViewModel>),
		new("🔴", "Badge", Bound<BadgePage, BadgeViewModel>),
		new("🧑", "Avatar", () => new AvatarPage()),
		new("🏷️", "Chips", Bound<ChipGroupPage, ChipGroupViewModel>),
		new("🔘", "ShinyButton", Bound<ButtonsPage, ButtonsViewModel>),
		new("🎚️", "ButtonGroup", Bound<ButtonGroupPage, ButtonGroupViewModel>),
		new("➕", "FAB & FabMenu", Bound<FabPage, FabViewModel>),
		new("⏱️", "Gauge", Bound<GaugePage, GaugeViewModel>),
		new("📶", "Progress Bar", Bound<ProgressBarPage, ProgressBarViewModel>),
		new("🎛️", "Slider", Bound<SliderPage, SliderViewModel>),
		new("↔️", "Range Slider", Bound<RangeSliderPage, RangeSliderViewModel>),
		new("📆", "Range Pickers", () => new RangePickersPage()),
		new("🔽", "Expander", Bound<ExpanderPage, ExpanderViewModel>),
		new("🕘", "Timeline", () => new TimelinePage()),
		new("⌨️", "Text Entry", Bound<TextEntryPage, TextEntryViewModel>),
		new("🔖", "TagEntry", Bound<TagEntryPage, TagEntryViewModel>),
		new("🔐", "Password Strength", Bound<PasswordStrengthPage, PasswordStrengthViewModel>),
		new("🦴", "Skeleton", Bound<SkeletonPage, SkeletonViewModel>),
		new("📜", "Marquee", () => new MarqueePage()),
		new("🎉", "Confetti", () => new ConfettiPage()),
		new("🌌", "Animated Backgrounds", () => new AnimatedBackgroundsPage()),
		new("✨", "Border Animations", () => new BorderAnimationsPage()),
		new("🍞", "Toast", Bound<ToastPage, ToastViewModel>),
		new("🗨️", "Dialogs", Bound<DialogsPage, DialogsViewModel>),
	];

	// In the controls sample, Shiny Shell creates each page's view model through DI and binds it;
	// this does the same for the gallery's plain FlyoutPage.
	static Page Bound<TPage, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>()
		where TPage : Page, new()
	{
		var services = IPlatformApplication.Current?.Services
			?? throw new InvalidOperationException("The app has not started.");
		return new TPage { BindingContext = ActivatorUtilities.CreateInstance<TViewModel>(services) };
	}
}

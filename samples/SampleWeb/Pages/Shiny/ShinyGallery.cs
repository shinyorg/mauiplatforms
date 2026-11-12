using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Sample.Features.Agents;
using Sample.Features.AnimatedBackgrounds;
using Sample.Features.Avatars;
using Sample.Features.Badge;
using Sample.Features.BorderAnimations;
using Sample.Features.ButtonGroups;
using Sample.Features.Buttons;
using Sample.Features.Chat;
using Sample.Features.Chips;
using Sample.Features.Confetti;
using Sample.Features.DataGrid;
using Sample.Features.Diagram;
using Sample.Features.Diagrams;
using Sample.Features.Dialogs;
using Sample.Features.Docking;
using Sample.Features.Expanders;
using Sample.Features.Fab;
using Sample.Features.FloatingPanel;
using Sample.Features.Gantt;
using Sample.Features.ImageViewer;
using Sample.Features.Kanban;
using Sample.Features.Keyframe;
using Sample.Features.Gauges;
using Sample.Features.Marquee;
using Sample.Features.Overlay;
using Sample.Features.Office;
using Sample.Features.Media;
using Sample.Features.PasswordStrength;
using Sample.Features.Pills;
using Sample.Features.ProgressBar;
using Sample.Features.RangePickers;
using Sample.Features.Scheduler;
using Sample.Features.SignaturePad;
using Sample.Features.TableView;
using Sample.Features.Skeleton;
using Sample.Features.Slider;
using Sample.Features.Tags;
using Sample.Features.TextEntry;
using Sample.Features.Timeline;
using Sample.Features.Toast;
using Sample.Features.TreeView;
using Sample.Features.Walkthrough;

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
		new("💬", "ChatView", Bound<ChatPage, ChatViewModel>),
		new("🤖", "Agent View", () => new AgentViewPage()),
		new("🧱", "A2UI Gallery", () => new A2UIGalleryPage()),
		new("🪟", "Floating Panel", Bound<SheetPage, SheetViewModel>),
		new("🔝", "Top Sheet", Bound<TopSheetPage, TopSheetViewModel>),
		new("🗂️", "Dual Panels", Bound<DualPanelPage, DualPanelViewModel>),
		new("🎵", "Minimized Sheet", Bound<MinimizedSheetStandalonePage, MinimizedSheetViewModel>),
		new("✍️", "Signature Pad", Bound<SignaturePadPage, SignaturePadViewModel>),
		new("📅", "Scheduler Calendar", Bound<CalendarPage, CalendarViewModel>),
		new("🗓️", "Scheduler Agenda", Bound<AgendaPage, AgendaViewModel>),
		new("📋", "Scheduler List", Bound<CalendarListPage, CalendarListViewModel>),
		new("📆", "Agenda + Calendar Picker", Bound<AgendaCalendarPickerPage, AgendaCalendarPickerViewModel>),
		new("📈", "Gantt", Bound<GanttPage, GanttViewModel>),
		new("🧭", "Diagram", Bound<DiagramPage, DiagramViewModel>),
		new("🧜", "Mermaid Flowchart", () => new BasicFlowchartPage()),
		new("↪️", "Mermaid Directions", () => new DirectionsPage()),
		new("🧩", "Mermaid Subgraphs", () => new SubgraphsPage()),
		new("🎨", "Mermaid Themes", () => new ThemesPage()),
		new("✏️", "Mermaid Editor", () => new InteractiveEditorPage()),
		new("🔍", "Image Viewer", Bound<ImageViewerPage, ImageViewerViewModel>),
		new("🖼️", "Image Gallery", Bound<ImageGalleryPage, ImageGalleryViewModel>),
		new("⚙️", "TableView Settings", Bound<BasicSettingsPage, BasicSettingsViewModel>),
		new("🎡", "TableView Pickers", Bound<PickerDemoPage, PickerDemoViewModel>),
		new("➕", "TableView Dynamic", Bound<DynamicSectionsPage, DynamicSectionsViewModel>),
		new("↕️", "TableView Drag Sort", Bound<DragSortPage, DragSortViewModel>),
		new("🎨", "TableView Styling", () => new StylingPage()),
		new("🌳", "Tree View", () => new TreeViewPage()),
		new("▦", "Data Grid", Bound<DataGridPage, DataGridViewModel>),
		new("🌲", "Tree Data Grid", Bound<TreeDataGridPage, TreeDataGridViewModel>),
		new("🧮", "Grid Grouping", Bound<DataGridGroupingPage, DataGridGroupingViewModel>),
		new("🎨", "Grid Formatting", Bound<DataGridFormattingPage, DataGridFormattingViewModel>),
		new("🪄", "Overlay", Bound<OverlayPage, OverlayViewModel>),
		new("🗃️", "Kanban", Bound<KanbanPage, KanbanViewModel>),
		new("▨", "Docking", () => new DockingPage()),
		new("🎞️", "Keyframe Animation", () => new KeyframePage()),
		new("👣", "Walkthrough", Bound<WalkthroughPage, WalkthroughViewModel>),
		new("💡", "Tooltip", Bound<TooltipPage, TooltipViewModel>),
		// Shiny.Maui.Controls.Office, drawn by Skia through Platform.Maui.Web.SkiaSharp. The editors
		// aren't here yet: building their ribbon touches MAUI's FilePickerFileType, whose type
		// initializer throws on plain net10.0. ImageEditor is left out too: it loads through MAUI's
		// net10.0 PlatformImage, which reads a PNG's size from its signature bytes.
		new("📊", "Spreadsheet", () => OfficeWebFonts.With(new SpreadsheetPage())),
		new("📄", "Document Viewer", () => OfficeWebFonts.With(new DocumentViewerPage())),
		new("🖼️", "Slide Viewer", () => OfficeWebFonts.With(new SlideViewerPage())),
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

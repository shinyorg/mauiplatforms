using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
#if MACAPP
using Microsoft.Maui.Platform.MacOS;
#endif

namespace Sample.Pages;

public class ToolbarPage : ContentPage
{
	readonly Label _statusLabel;
	readonly Label _countLabel;
	readonly VerticalStackLayout _itemsList;
	int _itemCount;

	public ToolbarPage()
	{
		Title = "Toolbar";

		_statusLabel = new Label
		{
			Text = "Use buttons below to add, remove, and configure toolbar items.",
			FontSize = 14,
		}.WithSecondaryText();

		_countLabel = new Label
		{
			Text = "Current toolbar items: 0",
			FontSize = 14,
		}.WithPrimaryText();

		_itemsList = new VerticalStackLayout { Spacing = 4 };

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(24),
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "Toolbar Demo",
						FontSize = 28,
						FontAttributes = FontAttributes.Bold,
					}.WithPrimaryText(),

					new Label
					{
						Text = "Test adding, removing, and configuring toolbar items at runtime. " +
							"Items can be placed in the content area (default) or the sidebar titlebar area " +
							"when using a native sidebar layout.",
						FontSize = 14,
					}.WithSecondaryText(),

					CreateInfoCard(),

					SectionHeader("Add Items"),
					CreateAddButtons(),

					SectionHeader("Add with SF Symbol Icons"),
					CreateIconButtons(),

#if MACAPP
					SectionHeader("Sidebar Placement (macOS)"),
					CreateSidebarPlacementButtons(),
#endif

					SectionHeader("Manage Items"),
					CreateManageButtons(),

					SectionHeader("Open in Sidebar Window"),
					CreateSidebarWindowButtons(),

					SectionHeader("Status"),
					_countLabel,
					_statusLabel,

					SectionHeader("Current Items"),
					_itemsList,
				}
			}
		};
	}

	static View CreateInfoCard() => new Border
	{
		Stroke = AppColors.AccentBlue,
		StrokeThickness = 1,
		StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
		Padding = new Thickness(16),
		Content = new VerticalStackLayout
		{
			Spacing = 6,
			Children =
			{
				new Label { Text = "How It Works", FontSize = 16, FontAttributes = FontAttributes.Bold }.WithPrimaryText(),
				new Label { Text = "• Page.ToolbarItems maps to NSToolbarItems in the window toolbar", FontSize = 13 }.WithSecondaryText(),
				new Label { Text = "• Items can show text, SF Symbol icons, or both", FontSize = 13 }.WithSecondaryText(),
				new Label { Text = "• MacOSToolbarItem.Placement controls sidebar vs content area placement", FontSize = 13 }.WithSecondaryText(),
				new Label { Text = "• NSTrackingSeparatorToolbarItem divides sidebar from content area", FontSize = 13 }.WithSecondaryText(),
				new Label { Text = "• Enabled/disabled state is reflected in real-time", FontSize = 13 }.WithSecondaryText(),
			}
		}
	};

	View CreateAddButtons()
	{
		var addText = MakeButton("Add Text Item", AppColors.AccentBlue, (s, e) =>
		{
			_itemCount++;
			var item = new ToolbarItem($"Item {_itemCount}", null, () =>
				SetStatus($"Clicked: Item {_itemCount}"));
			ToolbarItems.Add(item);
			RefreshDisplay();
			SetStatus($"Added text item: Item {_itemCount}");
		});

		var addDisabled = MakeButton("Add Disabled Item", AppColors.AccentPurple, (s, e) =>
		{
			_itemCount++;
			var item = new ToolbarItem
			{
				Text = $"Disabled {_itemCount}",
				Command = new Command(() => SetStatus($"Clicked: Disabled {_itemCount}"), () => false),
			};
			ToolbarItems.Add(item);
			RefreshDisplay();
			SetStatus($"Added disabled item: Disabled {_itemCount}");
		});

		return new VerticalStackLayout { Spacing = 8, Children = { addText, addDisabled } };
	}

	View CreateIconButtons()
	{
		var icons = new (string label, string symbol, Color color)[]
		{
			("plus.circle", "plus.circle", AppColors.AccentGreen),
			("trash", "trash", AppColors.AccentRed),
			("square.and.pencil", "square.and.pencil", AppColors.AccentOrange),
			("magnifyingglass", "magnifyingglass", AppColors.AccentTeal),
			("paperplane", "paperplane", AppColors.AccentBlue),
			("star.fill", "star.fill", AppColors.AccentPink),
		};

		var grid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star),
			},
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
			},
			ColumnSpacing = 8,
			RowSpacing = 8,
		};

		for (int i = 0; i < icons.Length; i++)
		{
			var (label, symbol, color) = icons[i];
			var captured = symbol;
			var btn = MakeButton(label, color, (s, e) =>
			{
				_itemCount++;
				var item = new ToolbarItem
				{
					Text = captured,
					IconImageSource = captured,
				};
				item.Clicked += (_, _) => SetStatus($"Clicked: {captured}");
				ToolbarItems.Add(item);
				RefreshDisplay();
				SetStatus($"Added icon item: {captured}");
			});
			btn.FontSize = 11;
			btn.HorizontalOptions = LayoutOptions.Fill;
			grid.Add(btn, i % 3, i / 3);
		}

		return grid;
	}

#if MACAPP
	View CreateSidebarPlacementButtons()
	{
		var addSidebar = MakeButton("Add Sidebar Item (plus)", AppColors.AccentGreen, (s, e) =>
		{
			_itemCount++;
			var item = new ToolbarItem
			{
				Text = $"Sidebar {_itemCount}",
				IconImageSource = "plus",
			};
			item.Clicked += (_, _) => SetStatus($"Clicked: Sidebar {_itemCount}");
			MacOSToolbarItem.SetPlacement(item, MacOSToolbarItemPlacement.Sidebar);
			ToolbarItems.Add(item);
			RefreshDisplay();
			SetStatus($"Added sidebar item: Sidebar {_itemCount} (will appear in sidebar titlebar when using native sidebar)");
		});

		var addSidebarFilter = MakeButton("Add Sidebar Item (line.3.horizontal.decrease)", AppColors.AccentTeal, (s, e) =>
		{
			_itemCount++;
			var item = new ToolbarItem
			{
				Text = $"Filter {_itemCount}",
				IconImageSource = "line.3.horizontal.decrease",
			};
			item.Clicked += (_, _) => SetStatus($"Clicked: Filter {_itemCount}");
			MacOSToolbarItem.SetPlacement(item, MacOSToolbarItemPlacement.Sidebar);
			ToolbarItems.Add(item);
			RefreshDisplay();
			SetStatus($"Added sidebar filter item");
		});

		var addMixed = MakeButton("Add Both: Sidebar + Content Items", AppColors.AccentPurple, (s, e) =>
		{
			_itemCount++;

			var sidebarItem = new ToolbarItem
			{
				Text = "New",
				IconImageSource = "plus",
			};
			sidebarItem.Clicked += (_, _) => SetStatus("Clicked: New (sidebar)");
			MacOSToolbarItem.SetPlacement(sidebarItem, MacOSToolbarItemPlacement.Sidebar);
			ToolbarItems.Add(sidebarItem);

			var contentItem = new ToolbarItem
			{
				Text = "Share",
				IconImageSource = "square.and.arrow.up",
			};
			contentItem.Clicked += (_, _) => SetStatus("Clicked: Share (content)");
			ToolbarItems.Add(contentItem);

			RefreshDisplay();
			SetStatus("Added sidebar 'New' + content 'Share' items");
		});

		return new VerticalStackLayout { Spacing = 8, Children = { addSidebar, addSidebarFilter, addMixed } };
	}
#endif

	View CreateManageButtons()
	{
		var removeLast = MakeButton("Remove Last Item", AppColors.AccentRed, (s, e) =>
		{
			if (ToolbarItems.Count > 0)
			{
				var last = ToolbarItems[ToolbarItems.Count - 1];
				ToolbarItems.Remove(last);
				RefreshDisplay();
				SetStatus($"Removed: {last.Text}");
			}
			else
			{
				SetStatus("No items to remove");
			}
		});

		var removeFirst = MakeButton("Remove First Item", AppColors.AccentOrange, (s, e) =>
		{
			if (ToolbarItems.Count > 0)
			{
				var first = ToolbarItems[0];
				ToolbarItems.RemoveAt(0);
				RefreshDisplay();
				SetStatus($"Removed: {first.Text}");
			}
			else
			{
				SetStatus("No items to remove");
			}
		});

		var clearAll = MakeButton("Clear All Items", AppColors.AccentRed, (s, e) =>
		{
			ToolbarItems.Clear();
			_itemCount = 0;
			RefreshDisplay();
			SetStatus("All toolbar items cleared");
		});

		var toggleFirst = MakeButton("Toggle First Item Enabled", AppColors.AccentTeal, (s, e) =>
		{
			if (ToolbarItems.Count > 0)
			{
				var first = ToolbarItems[0];
				// Toggle via command CanExecute by replacing the command
				if (first.Command is Command cmd)
				{
					var wasEnabled = cmd.CanExecute(null);
					first.Command = new Command(
						() => SetStatus($"Clicked: {first.Text}"),
						() => !wasEnabled);
					((Command)first.Command).ChangeCanExecute();
					RefreshDisplay();
					SetStatus($"Toggled {first.Text}: now {(!wasEnabled ? "enabled" : "disabled")}");
				}
				else
				{
					// Item has no command — give it a disabled one
					first.Command = new Command(
						() => SetStatus($"Clicked: {first.Text}"),
						() => false);
					((Command)first.Command).ChangeCanExecute();
					RefreshDisplay();
					SetStatus($"Disabled {first.Text}");
				}
			}
			else
			{
				SetStatus("No items to toggle");
			}
		});

		var renameFirst = MakeButton("Rename First Item", AppColors.AccentBlue, (s, e) =>
		{
			if (ToolbarItems.Count > 0)
			{
				var first = ToolbarItems[0];
				first.Text = $"Renamed {++_itemCount}";
				RefreshDisplay();
				SetStatus($"Renamed first item to: {first.Text}");
			}
			else
			{
				SetStatus("No items to rename");
			}
		});

		return new VerticalStackLayout
		{
			Spacing = 8,
			Children = { removeLast, removeFirst, clearAll, toggleFirst, renameFirst }
		};
	}

	View CreateSidebarWindowButtons()
	{
		var desc = new Label
		{
			Text = "Open this toolbar page inside a Shell with a native sidebar to test " +
				"the NSTrackingSeparatorToolbarItem behavior — sidebar-placed items appear " +
				"in the sidebar titlebar area.",
			FontSize = 13,
		}.WithSecondaryText();

		var openShell = MakeButton("Open in Shell (Native Sidebar)", AppColors.AccentPurple, (s, e) =>
		{
#if MACAPP
			var shell = new ToolbarDemoShell();
			Application.Current?.OpenWindow(new Window(shell));
			SetStatus("Opened toolbar demo in Shell with native sidebar");
#else
			SetStatus("Only available on macOS");
#endif
		});

		return new VerticalStackLayout { Spacing = 8, Children = { desc, openShell } };
	}

	void SetStatus(string text)
	{
		_statusLabel.Text = text;
	}

	void RefreshDisplay()
	{
		_countLabel.Text = $"Current toolbar items: {ToolbarItems.Count}";

		_itemsList.Children.Clear();
		for (int i = 0; i < ToolbarItems.Count; i++)
		{
			var item = ToolbarItems[i];
			var placement = "Content";
#if MACAPP
			if (MacOSToolbarItem.GetPlacement(item) == MacOSToolbarItemPlacement.Sidebar)
				placement = "Sidebar";
#endif
			var icon = item.IconImageSource is FileImageSource fs ? fs.File : "(none)";
			var enabled = item.Command == null || item.Command.CanExecute(null);

			var label = new Label
			{
				Text = $"  [{i}] \"{item.Text}\"  —  icon: {icon}  |  placement: {placement}  |  enabled: {enabled}",
				FontSize = 12,
				FontFamily = "Courier",
			}.WithSecondaryText();
			_itemsList.Children.Add(label);
		}

		if (ToolbarItems.Count == 0)
		{
			_itemsList.Children.Add(new Label
			{
				Text = "  (no toolbar items)",
				FontSize = 12,
				FontAttributes = FontAttributes.Italic,
			}.WithSecondaryText());
		}
	}

	static Button MakeButton(string text, Color bg, EventHandler handler)
	{
		var btn = new Button
		{
			Text = text,
			BackgroundColor = bg,
			TextColor = Colors.White,
			HeightRequest = 40,
			CornerRadius = 8,
			FontSize = 13,
			HorizontalOptions = LayoutOptions.Start,
			Padding = new Thickness(20, 0),
		};
		btn.Clicked += handler;
		return btn;
	}

	static Label SectionHeader(string text) => new Label
	{
		Text = text,
		FontSize = 22,
		FontAttributes = FontAttributes.Bold,
	}.WithSectionStyle();
}

#if MACAPP
/// <summary>
/// A Shell that hosts the ToolbarPage with native sidebar, to test
/// sidebar-placed toolbar items with NSTrackingSeparatorToolbarItem.
/// </summary>
class ToolbarDemoShell : Shell
{
	public ToolbarDemoShell()
	{
		Title = "Toolbar Demo — Native Sidebar";
		FlyoutBehavior = FlyoutBehavior.Locked;
		MacOSShell.SetUseNativeSidebar(this, true);

		var main = new FlyoutItem { Title = "Toolbar" };
		var toolbarContent = new ShellContent
		{
			Title = "Toolbar Test",
			Route = "toolbartest",
			ContentTemplate = new DataTemplate(() =>
			{
				var page = new ToolbarTestContentPage("Toolbar Test");
				return page;
			}),
		};
		MacOSShell.SetSystemImage(toolbarContent, "hammer");
		main.Items.Add(toolbarContent);

		var secondContent = new ShellContent
		{
			Title = "Another Page",
			Route = "anotherpage",
			ContentTemplate = new DataTemplate(() =>
			{
				var page = new ToolbarTestContentPage("Another Page");
				return page;
			}),
		};
		MacOSShell.SetSystemImage(secondContent, "doc.text");
		main.Items.Add(secondContent);

		Items.Add(main);
	}
}

/// <summary>
/// A content page with pre-configured sidebar and content toolbar items
/// and buttons to manipulate them at runtime.
/// </summary>
class ToolbarTestContentPage : ContentPage
{
	readonly Label _statusLabel;
	int _counter;

	public ToolbarTestContentPage(string title)
	{
		Title = title;

		_statusLabel = new Label
		{
			Text = "Toolbar items are configured. Try the buttons below.",
			FontSize = 14,
		}.WithSecondaryText();

		// Start with some default toolbar items
		AddDefaultItems();

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(24),
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = title,
						FontSize = 28,
						FontAttributes = FontAttributes.Bold,
					}.WithPrimaryText(),

					new Label
					{
						Text = "This page has toolbar items placed in both the sidebar titlebar area " +
							"and the content toolbar area. The tracking separator aligns with the sidebar divider.",
						FontSize = 14,
					}.WithSecondaryText(),

					SectionHeader("Sidebar Area Items"),
					MakeButton("Add Sidebar Item", AppColors.AccentGreen, OnAddSidebarItem),
					MakeButton("Remove Sidebar Items", AppColors.AccentRed, OnRemoveSidebarItems),

					SectionHeader("Content Area Items"),
					MakeButton("Add Content Item", AppColors.AccentBlue, OnAddContentItem),
					MakeButton("Remove Content Items", AppColors.AccentOrange, OnRemoveContentItems),

					SectionHeader("Bulk Operations"),
					MakeButton("Clear All", AppColors.AccentRed, OnClearAll),
					MakeButton("Reset to Defaults", AppColors.AccentPurple, OnResetDefaults),

					SectionHeader("Status"),
					_statusLabel,
				}
			}
		};
	}

	void AddDefaultItems()
	{
		// Sidebar toolbar items
		var newItem = new ToolbarItem { Text = "New", IconImageSource = "plus" };
		newItem.Clicked += (_, _) => _statusLabel.Text = "Clicked: New (sidebar)";
		MacOSToolbarItem.SetPlacement(newItem, MacOSToolbarItemPlacement.Sidebar);
		ToolbarItems.Add(newItem);

		var filterItem = new ToolbarItem { Text = "Filter", IconImageSource = "line.3.horizontal.decrease" };
		filterItem.Clicked += (_, _) => _statusLabel.Text = "Clicked: Filter (sidebar)";
		MacOSToolbarItem.SetPlacement(filterItem, MacOSToolbarItemPlacement.Sidebar);
		ToolbarItems.Add(filterItem);

		// Content toolbar items
		var shareItem = new ToolbarItem { Text = "Share", IconImageSource = "square.and.arrow.up" };
		shareItem.Clicked += (_, _) => _statusLabel.Text = "Clicked: Share (content)";
		ToolbarItems.Add(shareItem);

		var searchItem = new ToolbarItem { Text = "Search", IconImageSource = "magnifyingglass" };
		searchItem.Clicked += (_, _) => _statusLabel.Text = "Clicked: Search (content)";
		ToolbarItems.Add(searchItem);
	}

	void OnAddSidebarItem(object? s, EventArgs e)
	{
		_counter++;
		var item = new ToolbarItem
		{
			Text = $"Side {_counter}",
			IconImageSource = "folder",
		};
		item.Clicked += (_, _) => _statusLabel.Text = $"Clicked: Side {_counter}";
		MacOSToolbarItem.SetPlacement(item, MacOSToolbarItemPlacement.Sidebar);
		ToolbarItems.Add(item);
		_statusLabel.Text = $"Added sidebar item: Side {_counter}";
	}

	void OnAddContentItem(object? s, EventArgs e)
	{
		_counter++;
		var item = new ToolbarItem
		{
			Text = $"Action {_counter}",
			IconImageSource = "star",
		};
		item.Clicked += (_, _) => _statusLabel.Text = $"Clicked: Action {_counter}";
		ToolbarItems.Add(item);
		_statusLabel.Text = $"Added content item: Action {_counter}";
	}

	void OnRemoveSidebarItems(object? s, EventArgs e)
	{
		var toRemove = ToolbarItems
			.Where(i => MacOSToolbarItem.GetPlacement(i) == MacOSToolbarItemPlacement.Sidebar)
			.ToList();
		foreach (var item in toRemove)
			ToolbarItems.Remove(item);
		_statusLabel.Text = $"Removed {toRemove.Count} sidebar items";
	}

	void OnRemoveContentItems(object? s, EventArgs e)
	{
		var toRemove = ToolbarItems
			.Where(i => MacOSToolbarItem.GetPlacement(i) == MacOSToolbarItemPlacement.Content)
			.ToList();
		foreach (var item in toRemove)
			ToolbarItems.Remove(item);
		_statusLabel.Text = $"Removed {toRemove.Count} content items";
	}

	void OnClearAll(object? s, EventArgs e)
	{
		ToolbarItems.Clear();
		_counter = 0;
		_statusLabel.Text = "All toolbar items cleared";
	}

	void OnResetDefaults(object? s, EventArgs e)
	{
		ToolbarItems.Clear();
		_counter = 0;
		AddDefaultItems();
		_statusLabel.Text = "Reset to default toolbar items";
	}

	static Button MakeButton(string text, Color bg, EventHandler handler)
	{
		var btn = new Button
		{
			Text = text,
			BackgroundColor = bg,
			TextColor = Colors.White,
			HeightRequest = 40,
			CornerRadius = 8,
			FontSize = 13,
			HorizontalOptions = LayoutOptions.Start,
			Padding = new Thickness(20, 0),
		};
		btn.Clicked += handler;
		return btn;
	}

	static Label SectionHeader(string text) => new Label
	{
		Text = text,
		FontSize = 22,
		FontAttributes = FontAttributes.Bold,
	}.WithSectionStyle();
}
#endif

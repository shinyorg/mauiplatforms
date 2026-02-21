using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Sample.Pages;

public class CollectionViewPage : TabbedPage
{
	public CollectionViewPage()
	{
		Title = "CollectionView";

		Children.Add(new VerticalListTab());
		Children.Add(new HorizontalListTab());
		Children.Add(new VerticalGridTab());
		Children.Add(new HorizontalGridTab());
		Children.Add(new GroupedTab());
		Children.Add(new TemplateSelectorTab());
		Children.Add(new LargeListTab());
	}
}

#region Data Models

record SimpleItem(string Name, string Description, Color AccentColor);

record GroupedItem(string Name, string Detail);

class AnimalGroup : ObservableCollection<GroupedItem>
{
	public string GroupName { get; }
	public AnimalGroup(string name, IEnumerable<GroupedItem> items) : base(items)
	{
		GroupName = name;
	}
}

record MixedItem(string Title, string Subtitle, string ItemType, Color Color);

#endregion

#region Template Selector

class ItemTypeTemplateSelector : DataTemplateSelector
{
	public DataTemplate? CardTemplate { get; set; }
	public DataTemplate? CompactTemplate { get; set; }
	public DataTemplate? BannerTemplate { get; set; }

	protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
	{
		if (item is MixedItem mi)
		{
			return mi.ItemType switch
			{
				"banner" => BannerTemplate!,
				"compact" => CompactTemplate!,
				_ => CardTemplate!,
			};
		}
		return CardTemplate!;
	}
}

#endregion

#region Tab Pages

class VerticalListTab : ContentPage
{
	public VerticalListTab()
	{
		Title = "Vertical";
		var items = CollectionViewHelpers.GenerateSimpleItems(30);
		var statusLabel = new Label { Text = "Tap an item", FontSize = 12, TextColor = Colors.Gray, Margin = new Thickness(16, 8) };

		var cv = new CollectionView
		{
			ItemsSource = items,
			SelectionMode = SelectionMode.Single,
			ItemsLayout = LinearItemsLayout.Vertical,
			ItemTemplate = new DataTemplate(() =>
			{
				var nameLabel = new Label { FontSize = 15, FontAttributes = FontAttributes.Bold };
				nameLabel.SetBinding(Label.TextProperty, "Name");

				var descLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
				descLabel.SetBinding(Label.TextProperty, "Description");

				var accent = new BoxView { WidthRequest = 4, CornerRadius = 2 };
				accent.SetBinding(BoxView.ColorProperty, "AccentColor");

				return new Border
				{
					StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
					Stroke = Colors.Gray.WithAlpha(0.3f),
					StrokeThickness = 1,
					Margin = new Thickness(16, 4),
					Padding = 0,
					Content = new HorizontalStackLayout
					{
						Spacing = 12,
						Padding = new Thickness(0, 8, 12, 8),
						Children =
						{
							accent,
							new VerticalStackLayout
							{
								Spacing = 2,
								VerticalOptions = LayoutOptions.Center,
								Children = { nameLabel, descLabel }
							}
						}
					}
				};
			})
		};

		cv.SelectionChanged += (s, e) =>
		{
			if (e.CurrentSelection.FirstOrDefault() is SimpleItem si)
				statusLabel.Text = $"Selected: {si.Name}";
		};

		var grid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
			}
		};
		grid.Add(statusLabel, 0, 0);
		grid.Add(cv, 0, 1);
		Content = grid;
	}
}

class HorizontalListTab : ContentPage
{
	public HorizontalListTab()
	{
		Title = "Horizontal";
		var items = CollectionViewHelpers.GenerateSimpleItems(20);

		var cv = new CollectionView
		{
			ItemsSource = items,
			ItemsLayout = LinearItemsLayout.Horizontal,
			HeightRequest = 120,
			ItemTemplate = new DataTemplate(() =>
			{
				var nameLabel = new Label { FontSize = 13, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
				nameLabel.SetBinding(Label.TextProperty, "Name");

				var accent = new BoxView { HeightRequest = 60, WidthRequest = 60, CornerRadius = 30 };
				accent.SetBinding(BoxView.ColorProperty, "AccentColor");

				return new VerticalStackLayout
				{
					Spacing = 6,
					WidthRequest = 100,
					Padding = new Thickness(8),
					HorizontalOptions = LayoutOptions.Center,
					Children = { accent, nameLabel }
				};
			})
		};

		Content = new Grid
		{
			Padding = new Thickness(16),
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(new GridLength(120)),
				new RowDefinition(GridLength.Auto),
			},
			Children =
			{
				new Label { Text = "Horizontal List", FontSize = 18, FontAttributes = FontAttributes.Bold },
			}
		};
		var g = (Grid)Content;
		g.Add(new Label { Text = "Scroll horizontally to see more items", FontSize = 12, TextColor = Colors.Gray }, 0, 1);
		g.Add(cv, 0, 2);
		g.Add(new Label { Text = "Content below the horizontal list", FontSize = 14, TextColor = Colors.Gray }, 0, 3);
	}
}

class VerticalGridTab : ContentPage
{
	public VerticalGridTab()
	{
		Title = "Grid (V)";
		var items = CollectionViewHelpers.GenerateSimpleItems(24);

		var cv = new CollectionView
		{
			ItemsSource = items,
			ItemsLayout = new GridItemsLayout(3, ItemsLayoutOrientation.Vertical)
			{
				HorizontalItemSpacing = 8,
				VerticalItemSpacing = 8,
			},
			ItemTemplate = new DataTemplate(() =>
			{
				var nameLabel = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
				nameLabel.SetBinding(Label.TextProperty, "Name");

				var accent = new BoxView { HeightRequest = 50, CornerRadius = 8 };
				accent.SetBinding(BoxView.ColorProperty, "AccentColor");

				return new Border
				{
					StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
					Stroke = Colors.Gray.WithAlpha(0.3f),
					StrokeThickness = 1,
					Padding = new Thickness(8),
					Content = new VerticalStackLayout
					{
						Spacing = 6,
						Children = { accent, nameLabel }
					}
				};
			})
		};

		var grid = new Grid
		{
			Padding = new Thickness(16),
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
			}
		};
		grid.Add(new Label { Text = "3-Column Vertical Grid", FontSize = 18, FontAttributes = FontAttributes.Bold }, 0, 0);
		grid.Add(cv, 0, 1);
		Content = grid;
	}
}

class HorizontalGridTab : ContentPage
{
	public HorizontalGridTab()
	{
		Title = "Grid (H)";
		var items = CollectionViewHelpers.GenerateSimpleItems(20);

		var cv = new CollectionView
		{
			ItemsSource = items,
			HeightRequest = 200,
			ItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Horizontal)
			{
				HorizontalItemSpacing = 8,
				VerticalItemSpacing = 8,
			},
			ItemTemplate = new DataTemplate(() =>
			{
				var nameLabel = new Label { FontSize = 11, HorizontalTextAlignment = TextAlignment.Center };
				nameLabel.SetBinding(Label.TextProperty, "Name");

				var accent = new BoxView { HeightRequest = 40, WidthRequest = 80, CornerRadius = 6 };
				accent.SetBinding(BoxView.ColorProperty, "AccentColor");

				return new VerticalStackLayout
				{
					Spacing = 4,
					WidthRequest = 100,
					Padding = new Thickness(4),
					Children = { accent, nameLabel }
				};
			})
		};

		var grid = new Grid
		{
			Padding = new Thickness(16),
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(new GridLength(200)),
			}
		};
		grid.Add(new Label { Text = "2-Row Horizontal Grid", FontSize = 18, FontAttributes = FontAttributes.Bold }, 0, 0);
		grid.Add(new Label { Text = "Scroll horizontally — items fill 2 rows", FontSize = 12, TextColor = Colors.Gray }, 0, 1);
		grid.Add(cv, 0, 2);
		Content = grid;
	}
}

class GroupedTab : ContentPage
{
	public GroupedTab()
	{
		Title = "Grouped";
		var groups = new ObservableCollection<AnimalGroup>
		{
			new("🐾 Mammals", new[]
			{
				new GroupedItem("Dog", "Loyal companion"),
				new GroupedItem("Cat", "Independent feline"),
				new GroupedItem("Horse", "Majestic equine"),
				new GroupedItem("Dolphin", "Intelligent marine mammal"),
				new GroupedItem("Elephant", "Gentle giant"),
			}),
			new("🐦 Birds", new[]
			{
				new GroupedItem("Eagle", "Bird of prey"),
				new GroupedItem("Parrot", "Colorful talker"),
				new GroupedItem("Penguin", "Flightless swimmer"),
				new GroupedItem("Owl", "Nocturnal hunter"),
			}),
			new("🦎 Reptiles", new[]
			{
				new GroupedItem("Turtle", "Slow and steady"),
				new GroupedItem("Gecko", "Wall climber"),
				new GroupedItem("Iguana", "Tropical lizard"),
			}),
			new("🐟 Fish", new[]
			{
				new GroupedItem("Clownfish", "Reef dweller"),
				new GroupedItem("Salmon", "Upstream swimmer"),
				new GroupedItem("Shark", "Ocean predator"),
				new GroupedItem("Swordfish", "Fast swimmer"),
				new GroupedItem("Pufferfish", "Inflatable defense"),
			}),
		};

		var cv = new CollectionView
		{
			ItemsSource = groups,
			IsGrouped = true,
			GroupHeaderTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					FontSize = 16,
					FontAttributes = FontAttributes.Bold,
					Padding = new Thickness(16, 12, 16, 4),
					TextColor = Colors.CornflowerBlue,
				};
				label.SetBinding(Label.TextProperty, "GroupName");
				return label;
			}),
			GroupFooterTemplate = new DataTemplate(() =>
			{
				return new Border
				{
					HeightRequest = 1,
					BackgroundColor = Colors.Gray,
					Opacity = 0.3,
					StrokeThickness = 0,
					Margin = new Thickness(16, 4, 16, 8),
				};
			}),
			ItemTemplate = new DataTemplate(() =>
			{
				var nameLabel = new Label { FontSize = 14, FontAttributes = FontAttributes.Bold };
				nameLabel.SetBinding(Label.TextProperty, "Name");

				var detailLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
				detailLabel.SetBinding(Label.TextProperty, "Detail");

				return new HorizontalStackLayout
				{
					Spacing = 12,
					Padding = new Thickness(32, 6, 16, 6),
					Children =
					{
						new VerticalStackLayout
						{
							Spacing = 2,
							Children = { nameLabel, detailLabel }
						}
					}
				};
			})
		};

		var grid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
			}
		};
		grid.Add(new Label
		{
			Text = "Grouped CollectionView",
			FontSize = 18,
			FontAttributes = FontAttributes.Bold,
			Padding = new Thickness(16, 12),
		}, 0, 0);
		grid.Add(cv, 0, 1);
		Content = grid;
	}
}

class TemplateSelectorTab : ContentPage
{
	public TemplateSelectorTab()
	{
		Title = "Selector";

		var cardTemplate = new DataTemplate(() =>
		{
			var title = new Label { FontSize = 15, FontAttributes = FontAttributes.Bold };
			title.SetBinding(Label.TextProperty, "Title");
			var sub = new Label { FontSize = 12, TextColor = Colors.Gray };
			sub.SetBinding(Label.TextProperty, "Subtitle");
			var accent = new BoxView { WidthRequest = 4, CornerRadius = 2 };
			accent.SetBinding(BoxView.ColorProperty, "Color");

			return new Border
			{
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
				Stroke = Colors.Gray.WithAlpha(0.3f),
				StrokeThickness = 1,
				Margin = new Thickness(16, 4),
				Padding = 0,
				Content = new HorizontalStackLayout
				{
					Spacing = 12,
					Padding = new Thickness(0, 10, 12, 10),
					Children = { accent, new VerticalStackLayout { Spacing = 2, Children = { title, sub } } }
				}
			};
		});

		var compactTemplate = new DataTemplate(() =>
		{
			var title = new Label { FontSize = 13 };
			title.SetBinding(Label.TextProperty, "Title");
			var dot = new BoxView { WidthRequest = 8, HeightRequest = 8, CornerRadius = 4 };
			dot.SetBinding(BoxView.ColorProperty, "Color");

			return new HorizontalStackLayout
			{
				Spacing = 8,
				Padding = new Thickness(16, 4),
				Children = { dot, title }
			};
		});

		var bannerTemplate = new DataTemplate(() =>
		{
			var title = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center };
			title.SetBinding(Label.TextProperty, "Title");
			var sub = new Label { FontSize = 12, TextColor = Colors.White.WithAlpha(0.8f), HorizontalTextAlignment = TextAlignment.Center };
			sub.SetBinding(Label.TextProperty, "Subtitle");
			var bg = new Border
			{
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
				StrokeThickness = 0,
				Padding = new Thickness(20, 16),
				Margin = new Thickness(16, 6),
				Content = new VerticalStackLayout { Spacing = 4, Children = { title, sub } }
			};
			bg.SetBinding(Border.BackgroundColorProperty, "Color");
			return bg;
		});

		var selector = new ItemTypeTemplateSelector
		{
			CardTemplate = cardTemplate,
			CompactTemplate = compactTemplate,
			BannerTemplate = bannerTemplate,
		};

		var items = new List<MixedItem>
		{
			new("🎉 Welcome Banner", "Featured content at the top", "banner", Colors.CornflowerBlue),
			new("Project Alpha", "In development", "card", Colors.MediumSeaGreen),
			new("Bug fix #123", "Resolved", "compact", Colors.Gray),
			new("Bug fix #124", "Resolved", "compact", Colors.Gray),
			new("Bug fix #125", "In progress", "compact", Colors.Orange),
			new("🚀 Release 2.0", "Coming soon — new features inside", "banner", Colors.MediumOrchid),
			new("Project Beta", "Planning phase", "card", Colors.Coral),
			new("Project Gamma", "Testing", "card", Colors.Teal),
			new("Task: update docs", "Pending", "compact", Colors.SandyBrown),
			new("Task: review PR", "Pending", "compact", Colors.SandyBrown),
			new("🏆 Achievement", "100 commits this month!", "banner", Colors.Goldenrod),
			new("Project Delta", "Released", "card", Colors.SlateBlue),
		};

		var cv = new CollectionView
		{
			ItemsSource = items,
			ItemTemplate = selector,
		};

		var header = new VerticalStackLayout
		{
			Children =
			{
				new Label
				{
					Text = "DataTemplateSelector",
					FontSize = 18,
					FontAttributes = FontAttributes.Bold,
					Padding = new Thickness(16, 12),
				},
				new Label
				{
					Text = "Three template types: banner, card, compact",
					FontSize = 12,
					TextColor = Colors.Gray,
					Padding = new Thickness(16, 0, 16, 8),
				},
			}
		};

		var grid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
			}
		};
		grid.Add(header, 0, 0);
		grid.Add(cv, 0, 1);
		Content = grid;
	}
}

class LargeListTab : ContentPage
{
	public LargeListTab()
	{
		Title = "Large (500)";

		var items = new List<string>();
		for (int i = 0; i < 500; i++)
			items.Add($"Item {i + 1:N0}");

		var countLabel = new Label
		{
			Text = $"500 items loaded",
			FontSize = 12,
			TextColor = Colors.Gray,
			Padding = new Thickness(16, 8),
		};

		var cv = new CollectionView
		{
			ItemsSource = items,
			ItemsLayout = LinearItemsLayout.Vertical,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label { FontSize = 14, Padding = new Thickness(16, 8) };
				label.SetBinding(Label.TextProperty, ".");
				return label;
			})
		};

		var header = new VerticalStackLayout
		{
			Children =
			{
				countLabel,
				new Label
				{
					Text = "⚠️ No virtualization yet — may be slow to load",
					FontSize = 11,
					TextColor = Colors.Orange,
					Padding = new Thickness(16, 0, 16, 8),
				},
			}
		};

		var grid = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
			}
		};
		grid.Add(header, 0, 0);
		grid.Add(cv, 0, 1);
		Content = grid;
	}
}

#endregion

#region Helpers

static class CollectionViewHelpers
{
	static readonly Color[] AccentColors =
	{
		Colors.CornflowerBlue, Colors.Coral, Colors.MediumSeaGreen, Colors.MediumOrchid,
		Colors.SandyBrown, Colors.Teal, Colors.IndianRed, Colors.DodgerBlue,
		Colors.SlateBlue, Colors.OliveDrab, Colors.Crimson, Colors.DarkCyan,
	};

	public static List<SimpleItem> GenerateSimpleItems(int count) =>
		Enumerable.Range(1, count)
			.Select(i => new SimpleItem(
				$"Item {i}",
				$"Description for item {i}",
				AccentColors[(i - 1) % AccentColors.Length]))
			.ToList();
}

#endregion

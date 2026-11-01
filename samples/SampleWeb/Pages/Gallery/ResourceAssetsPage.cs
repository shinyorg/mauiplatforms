using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace SampleWeb.Pages;

public class ResourceAssetsPage : ContentPage
{
	readonly Label _imageStatus;
	readonly Label _fontStatus;
	readonly Label _appIconStatus;

	public ResourceAssetsPage()
	{
		Title = "Resource Image + Font + AppIcon";
		_imageStatus = new Label { FontSize = 12, TextColor = Colors.Gray };
		_fontStatus = new Label { FontSize = 12, TextColor = Colors.Gray };
		_appIconStatus = new Label { FontSize = 12, TextColor = Colors.Gray };

		var refreshButton = new Button { Text = "Refresh Resource Status" };
		refreshButton.Clicked += (s, e) => UpdateStatus();

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(24),
				Spacing = 12,
				Children =
				{
					new Label
					{
						Text = "🖼️ MAUI Resource Demo",
						FontSize = 28,
						FontAttributes = FontAttributes.Bold,
					},
					new Label
					{
						Text = "ImageSource and font alias below come from Resources/* items.",
						FontSize = 14,
						TextColor = Colors.Gray,
					},
					new Label
					{
						Text = "The app icon comes from the MauiIcon item and is served as the page favicon.",
						FontSize = 14,
						TextColor = Colors.Gray,
					},
					new Border
					{
						Stroke = Colors.LightGray,
						Padding = new Thickness(12),
						Content = new Image
						{
							Source = "dotnet_bot.png",
							HeightRequest = 170,
							Aspect = Aspect.AspectFit,
						},
					},
					_imageStatus,
					new Label { Text = "Default system font preview", FontSize = 20 },
					new Label
					{
						Text = "OpenSansRegular font alias preview",
						FontSize = 20,
						FontFamily = "OpenSansRegular",
					},
					new Label
					{
						Text = "OpenSans-Regular.ttf filename preview",
						FontSize = 20,
						FontFamily = "OpenSans-Regular.ttf",
					},
					new Label
					{
						Text = "OpenSansRegular bold + italic preview",
						FontSize = 20,
						FontFamily = "OpenSansRegular",
						FontAttributes = FontAttributes.Bold | FontAttributes.Italic,
					},
					new Entry
					{
						Text = "Entry control using OpenSansRegular",
						FontFamily = "OpenSansRegular",
					},
					new Editor
					{
						Text = "Editor control using OpenSansRegular.\nTry typing here to verify runtime text controls use the same font mapping.",
						FontFamily = "OpenSansRegular",
						AutoSize = EditorAutoSizeOption.TextChanges,
						HeightRequest = 90,
					},
					_fontStatus,
					_appIconStatus,
					refreshButton,
				}
			}
		};

		UpdateStatus();
	}

	// Resources are served from the site root, so ask over HTTP rather than the file system.
	void UpdateStatus() => UpdateStatusAsync().ContinueWith(_ => { }, TaskScheduler.Default);

	async Task UpdateStatusAsync()
	{
		var image = await FileSystem.Current.AppPackageFileExistsAsync("dotnet_bot.svg");
		var font = await FileSystem.Current.AppPackageFileExistsAsync("OpenSans-Regular.ttf");
		var icon = await FileSystem.Current.AppPackageFileExistsAsync("favicon.svg");

		_imageStatus.Text = $"dotnet_bot.svg served: {image} (requested as dotnet_bot.png, resolved to the SVG MauiImage)";
		_fontStatus.Text = $"OpenSans-Regular.ttf served: {font}. Compare alias/font-file labels plus Entry/Editor previews above.";
		_appIconStatus.Text = $"App icon served as favicon.svg: {icon}";
	}
}

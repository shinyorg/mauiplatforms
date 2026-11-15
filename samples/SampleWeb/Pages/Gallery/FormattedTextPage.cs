using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace SampleWeb.Pages;

class FormattedTextPage : ContentPage
{
	/// <summary>
	/// An underline colour for a <see cref="Span"/>, which MAUI doesn't model. MauiProgram applies it through
	/// <c>LabelHandler.SpanCreated</c> as CSS <c>text-decoration-color</c>.
	/// </summary>
	public static readonly BindableProperty UnderlineColorProperty =
		BindableProperty.CreateAttached("UnderlineColor", typeof(Color), typeof(FormattedTextPage), null);

	public static Color? GetUnderlineColor(BindableObject view) => (Color?)view.GetValue(UnderlineColorProperty);

	public static void SetUnderlineColor(BindableObject view, Color? value) => view.SetValue(UnderlineColorProperty, value);

	public FormattedTextPage()
	{
		Title = "FormattedText / Spans";

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Spacing = 20,
				Padding = new Thickness(24),
				Children =
				{
					new Label { Text = "FormattedText & Spans", FontSize = 22, FontAttributes = FontAttributes.Bold },

					// Basic styled spans
					BuildBasicSpans(),

					// Mixed font sizes
					BuildMixedSizes(),

					// Text decorations
					BuildDecorations(),

					// Colored spans
					BuildColoredSpans(),

					// CSS that Span doesn't model, through LabelHandler.SpanCreated
					BuildUnderlineColors(),

					// Paragraph-like formatted text
					BuildParagraph(),
				}
			}
		};
	}

	static Label BuildBasicSpans()
	{
		var label = new Label();
		label.FormattedText = new FormattedString
		{
			Spans =
			{
				new Span { Text = "Bold ", FontAttributes = FontAttributes.Bold },
				new Span { Text = "Italic ", FontAttributes = FontAttributes.Italic },
				new Span { Text = "Bold+Italic ", FontAttributes = FontAttributes.Bold | FontAttributes.Italic },
				new Span { Text = "Normal" },
			}
		};
		return label;
	}

	static Label BuildMixedSizes()
	{
		var label = new Label();
		label.FormattedText = new FormattedString
		{
			Spans =
			{
				new Span { Text = "Small ", FontSize = 10 },
				new Span { Text = "Medium ", FontSize = 16 },
				new Span { Text = "Large ", FontSize = 24 },
				new Span { Text = "Huge", FontSize = 32 },
			}
		};
		return label;
	}

	static Label BuildDecorations()
	{
		var label = new Label();
		label.FormattedText = new FormattedString
		{
			Spans =
			{
				new Span { Text = "Underlined ", TextDecorations = TextDecorations.Underline },
				new Span { Text = "Strikethrough ", TextDecorations = TextDecorations.Strikethrough },
				new Span { Text = "Both", TextDecorations = TextDecorations.Underline | TextDecorations.Strikethrough },
			}
		};
		return label;
	}

	static Label BuildColoredSpans()
	{
		var label = new Label();
		label.FormattedText = new FormattedString
		{
			Spans =
			{
				new Span { Text = "Red ", TextColor = Colors.Tomato, FontAttributes = FontAttributes.Bold },
				new Span { Text = "Blue ", TextColor = Colors.DodgerBlue, FontAttributes = FontAttributes.Bold },
				new Span { Text = "Green ", TextColor = Colors.MediumSeaGreen, FontAttributes = FontAttributes.Bold },
				new Span { Text = "Gold ", TextColor = Colors.Gold, BackgroundColor = Colors.DarkSlateGray, FontAttributes = FontAttributes.Bold },
				new Span { Text = "Purple", TextColor = Colors.Orchid, FontAttributes = FontAttributes.Bold },
			}
		};
		return label;
	}

	static Label BuildUnderlineColors()
	{
		var stabilizer = new Span { Text = "Stabilizer / CYA", TextDecorations = TextDecorations.Underline };
		SetUnderlineColor(stabilizer, Colors.DarkOrange);
		var chlorine = new Span { Text = "Free chlorine", TextDecorations = TextDecorations.Underline };
		SetUnderlineColor(chlorine, Colors.DodgerBlue);

		var label = new Label();
		label.FormattedText = new FormattedString
		{
			Spans =
			{
				stabilizer,
				new Span { Text = " is outside the recommended range, and " },
				chlorine,
				new Span { Text = " is low (underline colours set through LabelHandler.SpanCreated)." },
			}
		};
		return label;
	}

	static Label BuildParagraph()
	{
		var label = new Label();
		label.FormattedText = new FormattedString
		{
			Spans =
			{
				new Span { Text = "Microsoft.Maui.Platform.Web ", FontAttributes = FontAttributes.Bold, TextColor = Colors.DodgerBlue, FontSize = 16 },
				new Span { Text = "is a community backend for " },
				new Span { Text = ".NET MAUI", FontAttributes = FontAttributes.Bold },
				new Span { Text = " targeting " },
				new Span { Text = "WebAssembly", FontAttributes = FontAttributes.Italic, TextColor = Colors.MediumSeaGreen },
				new Span { Text = " using " },
				new Span { Text = "the DOM", TextDecorations = TextDecorations.Underline, FontAttributes = FontAttributes.Bold, TextColor = Colors.Orchid },
				new Span { Text = ". This label demonstrates rich text with " },
				new Span { Text = "multiple spans", BackgroundColor = Colors.Gold, TextColor = Colors.Black },
				new Span { Text = " rendered as styled HTML spans." },
			}
		};
		return label;
	}
}

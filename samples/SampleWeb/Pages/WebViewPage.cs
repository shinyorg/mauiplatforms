using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace SampleWeb.Pages;

public class WebViewPage : SpecimenPage
{
	public WebViewPage() : base("WebView", "A MAUI WebView is an iframe. HTML sources are same-origin, so EvaluateJavaScriptAsync can read and change them.")
	{
		var web = new WebView
		{
			HeightRequest = 260,
			Source = new HtmlWebViewSource
			{
				Html = """
					<html><body style="margin:0;padding:24px;font:16px/1.5 system-ui;color:#1B1530">
					<h2 style="margin:0 0 8px">Hello from HtmlWebViewSource</h2>
					<p id="message" style="margin:0;color:#6A6483">Press the button to change this text from C#.</p>
					</body></html>
					""",
			},
		};
		var run = new Button { Text = "Run JavaScript" };
		var readout = Specimen("HTML content", "The button calls EvaluateJavaScriptAsync and shows what the script returned.",
			new VerticalStackLayout { Spacing = 16, Children = { web, run } }, "Nothing evaluated yet.");
		var runs = 0;
		run.Clicked += async (_, _) =>
		{
			var result = await web.EvaluateJavaScriptAsync(
				$"document.getElementById('message').textContent = 'Updated from C# ({++runs})'; document.querySelector('h2').textContent.length");
			readout.Text = $"EvaluateJavaScriptAsync returned {result} (the heading's length)";
		};
	}
}

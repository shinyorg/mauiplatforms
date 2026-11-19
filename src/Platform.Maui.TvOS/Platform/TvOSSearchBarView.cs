using CoreGraphics;
using Foundation;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS;

/// <summary>
/// Custom focusable search bar for tvOS.
/// UISearchBar is not available on tvOS prior to 26.0, so this provides
/// equivalent functionality using a UITextField with a search icon.
/// </summary>
public class TvOSSearchBarView : UIView
{
	// UIKit can hand back this view after its managed wrapper is gone (a Superview lookup); recreate the wrapper
	// rather than throw
	protected TvOSSearchBarView(ObjCRuntime.NativeHandle handle) : base(handle) { }

	readonly UITextField _textField;
	readonly UILabel _searchIcon;

	public event EventHandler? TextChanged;
	public event EventHandler? SearchButtonPressed;

	public string? Text
	{
		get => _textField.Text;
		set => _textField.Text = value;
	}

	public string? Placeholder
	{
		get => _textField.Placeholder;
		set => _textField.Placeholder = value;
	}

	public UIColor? TextColor
	{
		get => _textField.TextColor;
		set => _textField.TextColor = value;
	}

	public bool IsReadOnly
	{
		get => !_textField.Enabled;
		set => _textField.Enabled = !value;
	}

	public TvOSSearchBarView()
	{
		_searchIcon = new UILabel
		{
			Text = "🔍",
			Font = UIFont.SystemFontOfSize(20),
			TextAlignment = UITextAlignment.Center,
		};
		AddSubview(_searchIcon);

		_textField = new UITextField
		{
			BorderStyle = UITextBorderStyle.RoundedRect,
			ReturnKeyType = UIReturnKeyType.Search,
		};
		_textField.EditingChanged += OnEditingChanged;
		_textField.ShouldReturn = OnShouldReturn;
		AddSubview(_textField);
	}

	public override CGSize SizeThatFits(CGSize size)
	{
		var textFieldSize = _textField.SizeThatFits(size);
		return new CGSize(
			double.IsInfinity((double)size.Width) ? 300 : (double)size.Width,
			Math.Max((double)textFieldSize.Height, 44));
	}

	public override void LayoutSubviews()
	{
		base.LayoutSubviews();

		var bounds = Bounds;
		if (bounds.Width <= 0 || bounds.Height <= 0)
			return;

		var iconWidth = 36.0;
		_searchIcon.Frame = new CGRect(0, 0, iconWidth, bounds.Height);
		_textField.Frame = new CGRect(iconWidth, 0, bounds.Width - iconWidth, bounds.Height);
	}

	void OnEditingChanged(object? sender, EventArgs e)
	{
		TextChanged?.Invoke(this, EventArgs.Empty);
	}

	bool OnShouldReturn(UITextField textField)
	{
		SearchButtonPressed?.Invoke(this, EventArgs.Empty);
		textField.ResignFirstResponder();
		return true;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_textField.EditingChanged -= OnEditingChanged;
			_textField.ShouldReturn = null;
		}
		base.Dispose(disposing);
	}
}

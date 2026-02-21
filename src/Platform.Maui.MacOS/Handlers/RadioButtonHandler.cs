using Microsoft.Maui.Handlers;
using AppKit;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

public partial class RadioButtonHandler : MacOSViewHandler<IRadioButton, NSButton>
{
	public static readonly IPropertyMapper<IRadioButton, RadioButtonHandler> Mapper =
		new PropertyMapper<IRadioButton, RadioButtonHandler>(ViewMapper)
		{
			[nameof(IRadioButton.IsChecked)] = MapIsChecked,
			[nameof(ITextStyle.TextColor)] = MapTextColor,
			[nameof(IText.Text)] = MapContent,
		};

	bool _updating;

	public RadioButtonHandler() : base(Mapper)
	{
	}

	protected override NSButton CreatePlatformView()
	{
		var button = new NSButton
		{
			Title = string.Empty,
		};
		button.SetButtonType(NSButtonType.Radio);
		return button;
	}

	protected override void ConnectHandler(NSButton platformView)
	{
		base.ConnectHandler(platformView);
		platformView.Activated += OnActivated;
	}

	protected override void DisconnectHandler(NSButton platformView)
	{
		platformView.Activated -= OnActivated;
		base.DisconnectHandler(platformView);
	}

	void OnActivated(object? sender, EventArgs e)
	{
		if (_updating || VirtualView == null)
			return;

		_updating = true;
		try
		{
			VirtualView.IsChecked = PlatformView.State == NSCellStateValue.On;
		}
		finally
		{
			_updating = false;
		}
	}

	public static void MapIsChecked(RadioButtonHandler handler, IRadioButton view)
	{
		if (handler._updating)
			return;

		handler.PlatformView.State = view.IsChecked ? NSCellStateValue.On : NSCellStateValue.Off;
	}

	public static void MapTextColor(RadioButtonHandler handler, IRadioButton view)
	{
		if (view is ITextStyle textStyle && textStyle.TextColor != null)
			handler.PlatformView.ContentTintColor = textStyle.TextColor.ToPlatformColor();
	}

	public static void MapContent(RadioButtonHandler handler, IRadioButton view)
	{
		// RadioButton.Content holds the display text (IText.Text is empty for string content)
		handler.PlatformView.Title = view.Content?.ToString() ?? string.Empty;
	}
}

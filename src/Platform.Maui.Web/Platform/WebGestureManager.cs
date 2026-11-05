using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// Maps a view's gesture recognizers to DOM pointer events, re-attaching when the collection changes.
/// </summary>
internal sealed class WebGestureManager : IDisposable
{
	const BindingFlags Internal = BindingFlags.Instance | BindingFlags.NonPublic;

	// MAUI raises gesture events through internal Send* methods; there is no public backend API.
	[DynamicDependency("SendTapped", typeof(TapGestureRecognizer))]
	static readonly MethodInfo? SendTapped = typeof(TapGestureRecognizer).GetMethod("SendTapped", Internal);

	static readonly Dictionary<string, MethodInfo?> PointerMethods = new();

	readonly View _view;
	readonly WebElement _element;
	readonly List<IDisposable> _subscriptions = new();
	bool _hasPan;

	public WebGestureManager(View view, WebElement element)
	{
		_view = view;
		_element = element;
		if (_view.GestureRecognizers is INotifyCollectionChanged observable)
			observable.CollectionChanged += OnGesturesChanged;
		Attach();
	}

	void OnGesturesChanged(object? sender, NotifyCollectionChangedEventArgs e) => Attach();

	void Attach()
	{
		Detach();
		foreach (var recognizer in _view.GestureRecognizers)
		{
			switch (recognizer)
			{
				case TapGestureRecognizer tap:
					AttachTap(tap);
					break;
				case PanGestureRecognizer pan:
					AttachPan(pan);
					break;
				case SwipeGestureRecognizer swipe:
					AttachSwipe(swipe);
					break;
				case PointerGestureRecognizer pointer:
					AttachPointer(pointer);
					break;
				case PinchGestureRecognizer pinch:
					AttachPinch(pinch);
					break;
			}
		}

		_element.SetStyle("touch-action", _hasPan ? "none" : null);
		if (_view.GestureRecognizers.OfType<TapGestureRecognizer>().Any())
			_element.SetStyle("cursor", "pointer");
	}

	void Detach()
	{
		foreach (var s in _subscriptions)
			s.Dispose();
		_subscriptions.Clear();
		_hasPan = false;
	}

	void AttachTap(TapGestureRecognizer tap)
	{
		var secondary = tap.Buttons.HasFlag(ButtonsMask.Secondary);
		var primary = tap.Buttons.HasFlag(ButtonsMask.Primary);

		if (primary)
		{
			_subscriptions.Add(_element.OnPointer("click", (x, y, flags) =>
			{
				if (PointerFlags.ClickCount(flags) % Math.Max(1, tap.NumberOfTapsRequired) == 0)
					RaiseTap(tap, new Point(x, y));
			}));
		}

		if (secondary)
			_subscriptions.Add(_element.On("contextmenu", () => RaiseTap(tap, null), preventDefault: true));
	}

	void RaiseTap(TapGestureRecognizer tap, Point? position)
	{
		if (!_view.IsEnabled)
			return;
		if (SendTapped is not null)
			SendTapped.Invoke(tap, [_view, (Func<IElement?, Point?>)(relativeTo => relativeTo is null || relativeTo == _view ? position : null)]);
		else if (tap.Command?.CanExecute(tap.CommandParameter) == true)
			tap.Command.Execute(tap.CommandParameter);
	}

	void AttachPan(PanGestureRecognizer pan)
	{
		_hasPan = true;
		var controller = (IPanGestureController)pan;
		var gestureId = 0;
		Point? start = null;

		_subscriptions.Add(_element.OnPointer("pointerdown", (x, y, _) =>
		{
			start = new Point(x, y);
			gestureId++;
			controller.SendPanStarted(_view, gestureId);
		}, capture: true));

		_subscriptions.Add(_element.OnPointer("pointermove", (x, y, _) =>
		{
			if (start is { } s)
				controller.SendPan(_view, x - s.X, y - s.Y, gestureId);
		}));

		_subscriptions.Add(_element.OnPointer("pointerup", (_, _, _) =>
		{
			if (start is null)
				return;
			start = null;
			controller.SendPanCompleted(_view, gestureId);
		}));

		_subscriptions.Add(_element.OnPointer("pointercancel", (_, _, _) =>
		{
			if (start is null)
				return;
			start = null;
			controller.SendPanCanceled(_view, gestureId);
		}));
	}

	void AttachSwipe(SwipeGestureRecognizer swipe)
	{
		Point? start = null;
		_subscriptions.Add(_element.OnPointer("pointerdown", (x, y, _) => start = new Point(x, y), capture: true));
		_subscriptions.Add(_element.OnPointer("pointerup", (x, y, _) =>
		{
			if (start is not { } s)
				return;
			start = null;
			var dx = x - s.X;
			var dy = y - s.Y;
			var threshold = swipe.Threshold;
			SwipeDirection direction;
			if (Math.Abs(dx) >= Math.Abs(dy) && Math.Abs(dx) >= threshold)
				direction = dx > 0 ? SwipeDirection.Right : SwipeDirection.Left;
			else if (Math.Abs(dy) >= threshold)
				direction = dy > 0 ? SwipeDirection.Down : SwipeDirection.Up;
			else
				return;

			if (swipe.Direction.HasFlag(direction))
				swipe.SendSwiped(_view, direction);
		}));
	}

	// Wheel events have no begin/end, so a pinch starts on the first ctrl+wheel and ends once they stop.
	const int PinchIdleMs = 250;

	void AttachPinch(PinchGestureRecognizer pinch)
	{
		var controller = (IPinchGestureController)pinch;
		var origin = new Point(0.5, 0.5);
		var active = false;
		var generation = 0;

		_subscriptions.Add(_element.OnZoomWheel(deltaY =>
		{
			if (!_view.IsEnabled)
				return;
			if (!active)
			{
				active = true;
				controller.SendPinchStarted(_view, origin);
			}

			// MAUI reports the scale change since the previous update, not a running total.
			controller.SendPinch(_view, Math.Exp(-deltaY * 0.01), origin);

			_ = EndWhenIdle(++generation);
		}));

		async Task EndWhenIdle(int current)
		{
			await Task.Delay(PinchIdleMs);
			if (current != generation || !active)
				return;
			active = false;
			try { controller.SendPinchEnded(_view); }
			catch (Exception ex) { Console.Error.WriteLine($"[maui-web] pinch end handler failed: {ex}"); }
		}
	}

	void AttachPointer(PointerGestureRecognizer pointer)
	{
		AddPointer(pointer, "pointerenter", "SendPointerEntered");
		AddPointer(pointer, "pointerleave", "SendPointerExited");
		AddPointer(pointer, "pointermove", "SendPointerMoved");
		AddPointer(pointer, "pointerdown", "SendPointerPressed");
		AddPointer(pointer, "pointerup", "SendPointerReleased");
	}

	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "PointerGestureRecognizer members are preserved by the DynamicDependency below.")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(PointerGestureRecognizer))]
	void AddPointer(PointerGestureRecognizer pointer, string domEvent, string method)
	{
		if (!PointerMethods.TryGetValue(method, out var send))
			PointerMethods[method] = send = typeof(PointerGestureRecognizer).GetMethod(method, Internal);
		if (send is null)
			return;

		_subscriptions.Add(_element.OnPointer(domEvent, (x, y, flags) =>
		{
			var position = new Point(x, y);
			var button = PointerFlags.Button(flags) == 2 ? ButtonsMask.Secondary : ButtonsMask.Primary;
			var args = send.GetParameters();
			object?[] values = new object?[args.Length];
			values[0] = _view;
			values[1] = (Func<IElement?, Point?>)(relativeTo => relativeTo is null || relativeTo == _view ? position : null);
			for (var i = 2; i < args.Length; i++)
				values[i] = args[i].ParameterType == typeof(ButtonsMask) ? button : null;
			send.Invoke(pointer, values);
		}));
	}

	public void Dispose()
	{
		if (_view.GestureRecognizers is INotifyCollectionChanged observable)
			observable.CollectionChanged -= OnGesturesChanged;
		Detach();
	}
}

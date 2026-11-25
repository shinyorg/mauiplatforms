using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// Maps a view's gesture recognizers to DOM pointer events, following changes to the collection.
/// </summary>
internal sealed class WebGestureManager : IDisposable
{
	// MAUI 11 made the Send* methods public; MAUI 10 has them internal.
	const BindingFlags Internal = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

	// MAUI raises gesture events through Send* methods; there is no backend API for them.
	[DynamicDependency("SendTapped", typeof(TapGestureRecognizer))]
	static readonly MethodInfo? SendTapped = typeof(TapGestureRecognizer).GetMethod("SendTapped", Internal);

	static readonly Dictionary<string, MethodInfo?> PointerMethods = new();

	readonly View _view;
	readonly WebElement _element;
	// Each recognizer's DOM subscriptions. A change to the collection only touches the recognizers
	// that came or went: re-attaching the rest would drop a gesture in progress (a control that adds
	// a recognizer when a drag starts would otherwise end that very drag).
	readonly Dictionary<IGestureRecognizer, List<IDisposable>> _attached = new();
	// The list the Attach* methods add to: the recognizer being attached.
	List<IDisposable> _subscriptions = new();

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
		var current = _view.GestureRecognizers.ToList();
		foreach (var removed in _attached.Keys.Where(r => !current.Contains(r)).ToList())
		{
			Dispose(_attached[removed]);
			_attached.Remove(removed);
		}

		foreach (var recognizer in current.Where(r => !_attached.ContainsKey(r)))
		{
			_subscriptions = new List<IDisposable>();
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
			}
			_attached[recognizer] = _subscriptions;
		}

		_element.SetStyle("touch-action", current.OfType<PanGestureRecognizer>().Any() ? "none" : null);
		// Only clear a cursor this manager set, never one the view's handler or styles chose.
		var tappable = current.OfType<TapGestureRecognizer>().Any();
		if (tappable != _pointerCursor)
			_element.SetStyle("cursor", tappable ? "pointer" : null);
		_pointerCursor = tappable;

		// A view you drag: a press held on it is the start of the drag, not the browser's long press, which on
		// iOS selects the text around it and opens the Copy/Look Up menu. Like the cursor, only clear what we set.
		var draggable = current.Any(r => r is PanGestureRecognizer or SwipeGestureRecognizer);
		if (draggable != _noSelect)
		{
			var value = draggable ? "none" : null;
			_element.SetStyle("user-select", value);
			_element.SetStyle("-webkit-user-select", value);
			_element.SetStyle("-webkit-touch-callout", value);
		}
		_noSelect = draggable;
	}

	bool _pointerCursor;
	bool _noSelect;

	static void Dispose(List<IDisposable> subscriptions)
	{
		foreach (var s in subscriptions)
			s.Dispose();
	}

	void Detach()
	{
		foreach (var subscriptions in _attached.Values)
			Dispose(subscriptions);
		_attached.Clear();
	}

	void AttachTap(TapGestureRecognizer tap)
	{
		var secondary = tap.Buttons.HasFlag(ButtonsMask.Secondary);
		var primary = tap.Buttons.HasFlag(ButtonsMask.Primary);

		if (primary)
		{
			_subscriptions.Add(_element.OnTap((x, y, count) =>
			{
				if (count % Math.Max(1, tap.NumberOfTapsRequired) != 0 || !_view.IsEnabled)
					return false;
				RaiseTap(tap, new Point(x, y));
				return true;
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
		var controller = (IPanGestureController)pan;
		var gestureId = 0;

		// The pan starts after a few pixels of movement, as on the native platforms: capturing the
		// pointer on press would retarget the click to this view and break taps on views inside it.
		_subscriptions.Add(_element.OnPan(PanSlop, (kind, dx, dy) =>
		{
			switch (kind)
			{
				case 1:
					gestureId++;
					controller.SendPanStarted(_view, gestureId);
					break;
				case 2:
					controller.SendPan(_view, dx, dy, gestureId);
					break;
				case 3:
					controller.SendPanCompleted(_view, gestureId);
					break;
				case 4:
					controller.SendPanCanceled(_view, gestureId);
					break;
			}
		}));
	}

	/// <summary>Pixels the pointer moves before a press becomes a pan.</summary>
	const double PanSlop = 6;

	void AttachSwipe(SwipeGestureRecognizer swipe)
	{
		_subscriptions.Add(_element.OnSwipe((dx, dy) =>
		{
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

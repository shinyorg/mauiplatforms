using CoreAnimation;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS;

/// <summary>
/// Custom focusable slider control for tvOS.
/// UISlider does not exist on tvOS, so this provides equivalent functionality
/// using CALayer rendering and Siri Remote gesture support.
/// </summary>
public class TvOSSliderView : UIControl
{
    // UIKit can hand back this view after its managed wrapper is gone (a Superview lookup); recreate the wrapper
    // rather than throw
    protected TvOSSliderView(ObjCRuntime.NativeHandle handle) : base(handle) { }

    const double DefaultTrackHeight = 4.0;
    const double ThumbDiameter = 30.0;
    const double FocusedThumbDiameter = 36.0;
    const double DefaultWidth = 300.0;
    const double DefaultHeight = 40.0;

    readonly CALayer _trackLayer;
    readonly CALayer _filledTrackLayer;
    readonly CALayer _thumbLayer;

    double _minimum;
    double _maximum = 1.0;
    double _value;
    bool _isFocused;

    public new event EventHandler? ValueChanged;
    public event EventHandler? DragStarted;
    public event EventHandler? DragCompleted;

    public double Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_value < _minimum)
                _value = _minimum;
            SetNeedsLayout();
        }
    }

    public double Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            if (_value > _maximum)
                _value = _maximum;
            SetNeedsLayout();
        }
    }

    public double Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, _minimum, _maximum);
            SetNeedsLayout();
        }
    }

    public UIColor? MinimumTrackTintColor
    {
        get => _filledTrackLayer.BackgroundColor is { } bg
            ? new UIColor(bg)
            : null;
        set
        {
            _filledTrackLayer.BackgroundColor = (value ?? UIColor.SystemBlue).CGColor;
        }
    }

    public UIColor? MaximumTrackTintColor
    {
        get => _trackLayer.BackgroundColor is { } bg
            ? new UIColor(bg)
            : null;
        set
        {
            _trackLayer.BackgroundColor = (value ?? UIColor.DarkGray).CGColor;
        }
    }

    public UIColor? ThumbTintColor
    {
        get => _thumbLayer.BackgroundColor is { } bg
            ? new UIColor(bg)
            : null;
        set
        {
            _thumbLayer.BackgroundColor = (value ?? UIColor.White).CGColor;
        }
    }

    public TvOSSliderView()
    {
        // Track background (full width)
        _trackLayer = new CALayer
        {
            BackgroundColor = UIColor.DarkGray.CGColor,
            CornerRadius = (nfloat)(DefaultTrackHeight / 2.0),
        };
        Layer.AddSublayer(_trackLayer);

        // Filled track (from left to current value)
        _filledTrackLayer = new CALayer
        {
            BackgroundColor = UIColor.SystemBlue.CGColor,
            CornerRadius = (nfloat)(DefaultTrackHeight / 2.0),
        };
        Layer.AddSublayer(_filledTrackLayer);

        // Thumb indicator
        _thumbLayer = new CALayer
        {
            BackgroundColor = UIColor.White.CGColor,
            CornerRadius = (nfloat)(ThumbDiameter / 2.0),
        };
        _thumbLayer.ShadowColor = UIColor.Black.CGColor;
        _thumbLayer.ShadowOffset = new CGSize(0, 1);
        _thumbLayer.ShadowRadius = 2;
        _thumbLayer.ShadowOpacity = 0.3f;
        Layer.AddSublayer(_thumbLayer);

        // Pan gesture for Siri Remote trackpad swipes
        var panGesture = new UIPanGestureRecognizer(OnPan);
        panGesture.AllowedTouchTypes = new NSNumber[]
        {
            new NSNumber((long)UITouchType.Indirect),
        };
        AddGestureRecognizer(panGesture);
    }

    public override bool CanBecomeFocused => true;

    public override CGSize SizeThatFits(CGSize size)
    {
        var width = double.IsInfinity((double)size.Width) || size.Width <= 0
            ? DefaultWidth
            : Math.Min((double)size.Width, DefaultWidth);

        return new CGSize(width, DefaultHeight);
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var trackY = (bounds.Height - DefaultTrackHeight) / 2.0;
        var thumbSize = _isFocused ? FocusedThumbDiameter : ThumbDiameter;

        // Inset track to account for thumb radius
        var trackInset = thumbSize / 2.0;
        var trackWidth = bounds.Width - thumbSize;

        // Track background
        _trackLayer.Frame = new CGRect(trackInset, trackY, trackWidth, DefaultTrackHeight);

        // Calculate fill percentage
        var range = _maximum - _minimum;
        var percentage = range > 0 ? (_value - _minimum) / range : 0;

        // Filled track
        var filledWidth = trackWidth * percentage;
        _filledTrackLayer.Frame = new CGRect(trackInset, trackY, filledWidth, DefaultTrackHeight);

        // Thumb position
        var thumbX = trackInset + (trackWidth * percentage) - (thumbSize / 2.0);
        var thumbY = (bounds.Height - thumbSize) / 2.0;
        _thumbLayer.Frame = new CGRect(thumbX, thumbY, thumbSize, thumbSize);
        _thumbLayer.CornerRadius = (nfloat)(thumbSize / 2.0);
    }

    public override void DidUpdateFocus(UIFocusUpdateContext context, UIFocusAnimationCoordinator coordinator)
    {
        base.DidUpdateFocus(context, coordinator);

        _isFocused = context.NextFocusedView == this;

        coordinator.AddCoordinatedAnimations(() =>
        {
            if (_isFocused)
            {
                Transform = CGAffineTransform.MakeScale(1.05f, 1.05f);
                _thumbLayer.ShadowOpacity = 0.5f;
            }
            else
            {
                Transform = CGAffineTransform.MakeIdentity();
                _thumbLayer.ShadowOpacity = 0.3f;
            }

            SetNeedsLayout();
            LayoutIfNeeded();
        }, null);
    }

    void OnPan(UIPanGestureRecognizer recognizer)
    {
        switch (recognizer.State)
        {
            case UIGestureRecognizerState.Began:
                DragStarted?.Invoke(this, EventArgs.Empty);
                break;

            case UIGestureRecognizerState.Changed:
                var translation = recognizer.TranslationInView(this);
                var range = _maximum - _minimum;
                if (range <= 0 || Bounds.Width <= 0)
                    break;

                // Scale translation to value range
                var delta = (double)translation.X / (double)Bounds.Width * range;
                Value = Math.Clamp(_value + delta, _minimum, _maximum);
                recognizer.SetTranslation(CGPoint.Empty, this);
                ValueChanged?.Invoke(this, EventArgs.Empty);
                break;

            case UIGestureRecognizerState.Ended:
            case UIGestureRecognizerState.Cancelled:
                DragCompleted?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    // Handle arrow key presses (game controllers, keyboard)
    public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        var handled = false;
        foreach (UIPress press in presses)
        {
            if (press.Type == UIPressType.LeftArrow)
            {
                AdjustValue(-GetStepSize());
                handled = true;
            }
            else if (press.Type == UIPressType.RightArrow)
            {
                AdjustValue(GetStepSize());
                handled = true;
            }
        }

        if (!handled)
            base.PressesBegan(presses, evt);
    }

    double GetStepSize()
    {
        var range = _maximum - _minimum;
        return range > 0 ? range / 20.0 : 0.05; // 5% steps
    }

    void AdjustValue(double delta)
    {
        DragStarted?.Invoke(this, EventArgs.Empty);
        Value = Math.Clamp(_value + delta, _minimum, _maximum);
        ValueChanged?.Invoke(this, EventArgs.Empty);
        DragCompleted?.Invoke(this, EventArgs.Empty);
    }
}

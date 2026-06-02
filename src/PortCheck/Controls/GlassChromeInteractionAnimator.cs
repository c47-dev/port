using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using PortCheck.Helpers;

namespace PortCheck.Controls;

/// <summary>
/// Shared hover/press liquid-glass motion for <see cref="GlassRoundButton"/> and <see cref="GlassPillButton"/>.
/// </summary>
public sealed class GlassChromeInteractionAnimator
{
    public const double PressScale = 0.96;
    public const double DeformSmoothing = 0.38;

    public static readonly TimeSpan HoverEnterDuration = TimeSpan.FromMilliseconds(220);
    public static readonly TimeSpan LensFadeDuration = TimeSpan.FromMilliseconds(160);
    public static readonly TimeSpan HoverLeaveDuration = FluidAnimation.TabPushDuration;
    public static readonly TimeSpan PressInDuration = TimeSpan.FromMilliseconds(100);
    public static readonly TimeSpan PressGlowDuration = TimeSpan.FromMilliseconds(80);

    private static readonly SolidColorBrush HoverFill = CreateFrozenBrush(0x00, 0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush IdleFill = CreateFrozenBrush(0x18, 0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush PressFill = CreateFrozenBrush(0x10, 0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush HoverStroke = CreateFrozenBrush(0xD8, 0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush IdleStroke = CreateFrozenBrush(0x55, 0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush ActiveStroke = CreateFrozenBrush(0xFF, 0xFF, 0xFF, 0xFF);

    private readonly UIElement _host;
    private readonly FrameworkElement _root;
    private readonly Border _shadowHost;
    private readonly ScaleTransform _scale;
    private readonly TranslateTransform _gelFollow;
    private readonly ScaleTransform _innerPinch;
    private readonly Border _lensPlate;
    private readonly Border _bd;
    private readonly Border _fresnelRim;
    private readonly Border _rimHighlight;
    private readonly Border _topSpecular;
    private readonly Border _bottomSpecular;
    private readonly Border _pressGlow;
    private ImageBrush? _lensBrush;

    private bool _pointerInside;
    private bool _pressed;
    private double _scaleX = 1;
    private double _scaleY = 1;
    private double _gelX;
    private double _gelY;
    private readonly GlassChromeInteractionOptions _options;

    public const double HoverEnterScale = 1.14;

    public GlassChromeInteractionAnimator(
        UIElement host,
        FrameworkElement root,
        Border shadowHost,
        ScaleTransform scale,
        TranslateTransform gelFollow,
        ScaleTransform innerPinch,
        Border lensPlate,
        Border bd,
        Border fresnelRim,
        Border rimHighlight,
        Border topSpecular,
        Border bottomSpecular,
        Border pressGlow,
        GlassChromeInteractionOptions? options = null)
    {
        _host = host;
        _root = root;
        _shadowHost = shadowHost;
        _scale = scale;
        _gelFollow = gelFollow;
        _innerPinch = innerPinch;
        _lensPlate = lensPlate;
        _bd = bd;
        _fresnelRim = fresnelRim;
        _rimHighlight = rimHighlight;
        _topSpecular = topSpecular;
        _bottomSpecular = bottomSpecular;
        _pressGlow = pressGlow;
        _options = options ?? GlassChromeInteractionOptions.Standard;
    }

    public static bool MotionEnabled => SystemParameters.ClientAreaAnimation;

    public void Attach()
    {
        Detach();
        if (_options.HoverOverlayOnly)
        {
            _bd.Opacity = 0;
            _bd.Background = Brushes.Transparent;
            _shadowHost.Effect = null;
        }
        else
        {
            ApplyIdleShadow();
        }

        _host.MouseEnter += OnMouseEnter;
        _host.MouseLeave += OnMouseLeave;
        _host.MouseMove += OnMouseMove;
        _host.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        _host.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
    }

    public void Detach()
    {
        _host.MouseEnter -= OnMouseEnter;
        _host.MouseLeave -= OnMouseLeave;
        _host.MouseMove -= OnMouseMove;
        _host.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        _host.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        _rimHighlight.BeginAnimation(UIElement.OpacityProperty, null);
        _fresnelRim.BeginAnimation(UIElement.OpacityProperty, null);
        _topSpecular.BeginAnimation(UIElement.OpacityProperty, null);
        _bottomSpecular.BeginAnimation(UIElement.OpacityProperty, null);
        _pressGlow.BeginAnimation(UIElement.OpacityProperty, null);
        _bd.BeginAnimation(UIElement.OpacityProperty, null);
        _lensPlate.BeginAnimation(UIElement.OpacityProperty, null);
        _gelFollow.BeginAnimation(TranslateTransform.XProperty, null);
        _gelFollow.BeginAnimation(TranslateTransform.YProperty, null);
        ClearLensBackdrop();
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        _pointerInside = true;
        ApplyGlassHoverVisual(true);

        if (!MotionEnabled)
        {
            ApplyScale(_options.HoverEnterScale, _options.HoverEnterScale);
            ApplyHoverShadow();
            EnterLiquidGlassLook();
            UpdateInwardDeform(e);
            return;
        }

        AnimateScale(_options.HoverEnterScale, _options.HoverEnterScale, HoverEnterDuration, FluidAnimation.SpringEase);
        ApplyHoverShadow();
        EnterLiquidGlassLook();
        UpdateInwardDeform(e);
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        _pointerInside = false;
        _pressed = false;
        ApplyGlassHoverVisual(false);

        if (!MotionEnabled)
        {
            ResetVisualState();
            return;
        }

        AnimateScale(1, 1, HoverLeaveDuration, FluidAnimation.SpringEase);
        _innerPinch.ScaleX = 1;
        _innerPinch.ScaleY = 1;
        ExitLiquidGlassLook();
        AnimateOpacity(_pressGlow, 0, PressGlowDuration);
        _gelFollow.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(_gelFollow.X, 0, HoverLeaveDuration)
        {
            EasingFunction = FluidAnimation.SpringEase
        });
        _gelFollow.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(_gelFollow.Y, 0, HoverLeaveDuration)
        {
            EasingFunction = FluidAnimation.SpringEase
        });
        if (!_options.HoverOverlayOnly)
            ApplyIdleShadow();
        else
            _shadowHost.Effect = null;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_pointerInside || _pressed)
            return;

        UpdateInwardDeform(e);
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _pressed = true;
        if (!_options.HoverOverlayOnly)
        {
            _bd.Background = PressFill;
            _bd.BorderBrush = ActiveStroke;
        }

        var (dx, dy) = GetNormalizedPointer(e);
        _pressGlow.Background = CreatePressGlowBrush(dx, dy);

        if (!MotionEnabled)
        {
            _pressGlow.Opacity = 0.62;
            UpdateInwardDeform(e, extraPinch: 0.022);
            return;
        }

        AnimateOpacity(_pressGlow, 0.65, PressGlowDuration);
        if (!_options.HoverOverlayOnly)
            AnimateScale(_options.PressScale, _options.PressScale, PressInDuration, new QuadraticEase { EasingMode = EasingMode.EaseOut });
        UpdateInwardDeform(e, extraPinch: 0.022);
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _pressed = false;
        if (!_pointerInside)
            return;

        if (!_options.HoverOverlayOnly)
        {
            _bd.Background = HoverFill;
            _bd.BorderBrush = HoverStroke;
        }

        if (!MotionEnabled)
        {
            _pressGlow.Opacity = 0;
            UpdateInwardDeform(e);
            return;
        }

        AnimateOpacity(_pressGlow, 0, PressGlowDuration);
        if (!_options.HoverOverlayOnly)
            AnimateScale(_options.HoverEnterScale, _options.HoverEnterScale, PressInDuration, FluidAnimation.SpringEase);
        UpdateInwardDeform(e);
    }

    private void UpdateInwardDeform(MouseEventArgs e, double extraPinch = 0)
    {
        if (_options.MaxInwardPinch <= 0 && _options.GelFollowPixels <= 0)
            return;

        var (dx, dy) = GetNormalizedPointer(e);
        var dist = Math.Min(1, Math.Sqrt(dx * dx + dy * dy));
        var pinch = (_options.MaxInwardPinch + extraPinch) * (0.4 + 0.6 * dist);

        var axisX = 0.25 + 0.75 * Math.Abs(dx);
        var axisY = 0.25 + 0.75 * Math.Abs(dy);
        var innerX = 1 - pinch * axisX;
        var innerY = 1 - pinch * axisY;

        var targetGelX = dx * _options.GelFollowPixels;
        var targetGelY = dy * _options.GelFollowPixels;

        _gelFollow.BeginAnimation(TranslateTransform.XProperty, null);
        _gelFollow.BeginAnimation(TranslateTransform.YProperty, null);

        if (!MotionEnabled)
        {
            if (_pressed)
                ApplyScale(_options.PressScale, _options.PressScale);
            _innerPinch.ScaleX = innerX;
            _innerPinch.ScaleY = innerY;
            _gelFollow.X = targetGelX;
            _gelFollow.Y = targetGelY;
            return;
        }

        var smoothInner = 0.42;
        _innerPinch.ScaleX += (innerX - _innerPinch.ScaleX) * smoothInner;
        _innerPinch.ScaleY += (innerY - _innerPinch.ScaleY) * smoothInner;

        _gelX += (targetGelX - _gelX) * DeformSmoothing;
        _gelY += (targetGelY - _gelY) * DeformSmoothing;
        _gelFollow.X = _gelX;
        _gelFollow.Y = _gelY;
    }

    private void EnterLiquidGlassLook()
    {
        if (_options.EnableLensPlate)
            ActivateLensBackdrop();

        if (!MotionEnabled)
        {
            if (_options.EnableLensPlate)
                _lensPlate.Opacity = _options.HoverLensPlateOpacity;
            if (_options.EnableFresnelRim)
                _fresnelRim.Opacity = _options.HoverFresnelOpacity;
            _rimHighlight.Opacity = _options.HoverRimHighlightOpacity;
            _topSpecular.Opacity = _options.HoverTopSpecularOpacity;
            _bottomSpecular.Opacity = _options.HoverBottomSpecularOpacity;
            if (!_options.HoverOverlayOnly)
            {
                _bd.Opacity = 0.05;
                _bd.BorderThickness = new Thickness(0);
            }

            return;
        }

        if (_options.EnableLensPlate)
            AnimateOpacity(_lensPlate, _options.HoverLensPlateOpacity, LensFadeDuration);
        if (_options.EnableFresnelRim)
            AnimateOpacity(_fresnelRim, _options.HoverFresnelOpacity, LensFadeDuration);
        AnimateOpacity(_rimHighlight, _options.HoverRimHighlightOpacity, LensFadeDuration);
        AnimateOpacity(_topSpecular, _options.HoverTopSpecularOpacity, LensFadeDuration);
        AnimateOpacity(_bottomSpecular, _options.HoverBottomSpecularOpacity, LensFadeDuration);
        if (_options.HoverOverlayOnly)
            return;

        AnimateOpacity(_bd, 0.05, LensFadeDuration);
        _bd.BorderThickness = new Thickness(0);
    }

    private void ExitLiquidGlassLook()
    {
        if (!MotionEnabled)
        {
            _lensPlate.Opacity = 0;
            _fresnelRim.Opacity = 0;
            _rimHighlight.Opacity = 0;
            _topSpecular.Opacity = 0;
            _bottomSpecular.Opacity = 0;
            if (!_options.HoverOverlayOnly)
            {
                _bd.Opacity = 0.88;
                _bd.BorderThickness = new Thickness(1);
            }

            ClearLensBackdrop();
            return;
        }

        if (_options.EnableLensPlate)
            AnimateOpacity(_lensPlate, 0, LensFadeDuration);
        if (_options.EnableFresnelRim)
            AnimateOpacity(_fresnelRim, 0, LensFadeDuration);
        AnimateOpacity(_rimHighlight, 0, LensFadeDuration);
        AnimateOpacity(_topSpecular, 0, LensFadeDuration);
        AnimateOpacity(_bottomSpecular, 0, LensFadeDuration);
        if (_options.HoverOverlayOnly)
        {
            ScheduleClearLensBackdrop();
            return;
        }

        AnimateOpacity(_bd, 0.88, LensFadeDuration);
        _bd.BorderThickness = new Thickness(1);
        ScheduleClearLensBackdrop();
    }

    private void ActivateLensBackdrop()
    {
        _lensPlate.Background = new SolidColorBrush(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF));
        _lensPlate.Effect = null;

        if (_options.UseBackdropLens)
        {
            _root.Dispatcher.BeginInvoke(
                LoadLensBackdropAsync,
                DispatcherPriority.Background);
        }
    }

    private void LoadLensBackdropAsync()
    {
        if (!_pointerInside || !_options.UseBackdropLens)
            return;

        var shell = FindAncestor<GlassPopupShell>(_root);
        if (shell != null && !shell.TryGetChromeLensBackdrop(out _, out _))
            shell.RefreshBackdrop();

        _lensBrush = GlassChromeLensBackdrop.TryCreateBrush(_root);
        if (_lensBrush == null || !_pointerInside)
            return;

        _lensPlate.Background = _lensBrush;
        _lensPlate.Effect = new BlurEffect { Radius = 0.6, RenderingBias = RenderingBias.Performance };
    }

    private void ScheduleClearLensBackdrop()
    {
        _root.Dispatcher.BeginInvoke(
            ClearLensBackdrop,
            DispatcherPriority.Background,
            LensFadeDuration + TimeSpan.FromMilliseconds(20));
    }

    private void ClearLensBackdrop()
    {
        _lensBrush = null;
        _lensPlate.Background = null;
        _lensPlate.Effect = null;
    }

    private (double dx, double dy) GetNormalizedPointer(MouseEventArgs e)
    {
        var w = _root.ActualWidth;
        var h = _root.ActualHeight;
        if (w <= 1 || h <= 1)
            return (0, 0);

        var pos = e.GetPosition(_root);
        var dx = Math.Clamp((pos.X - w * 0.5) / (w * 0.5), -1, 1);
        var dy = Math.Clamp((pos.Y - h * 0.5) / (h * 0.5), -1, 1);
        return (dx, dy);
    }

    private void ApplyGlassHoverVisual(bool hover)
    {
        if (_options.HoverOverlayOnly)
            return;

        if (hover)
        {
            _bd.Background = HoverFill;
            _bd.BorderBrush = HoverStroke;
            return;
        }

        _bd.Background = IdleFill;
        _bd.BorderBrush = IdleStroke;
    }

    private void ResetVisualState()
    {
        ApplyScale(1, 1);
        _innerPinch.ScaleX = 1;
        _innerPinch.ScaleY = 1;
        _pressGlow.Opacity = 0;
        ExitLiquidGlassLook();
        _gelFollow.X = 0;
        _gelFollow.Y = 0;
        _gelX = 0;
        _gelY = 0;
        if (_options.HoverOverlayOnly)
            _shadowHost.Effect = null;
        else
            ApplyIdleShadow();
    }

    private void AnimateScale(double toX, double toY, TimeSpan duration, IEasingFunction? easing = null)
    {
        _scaleX = toX;
        _scaleY = toY;
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(_scale.ScaleX, toX, duration)
        {
            EasingFunction = easing
        });
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(_scale.ScaleY, toY, duration)
        {
            EasingFunction = easing
        });
    }

    private void ApplyScale(double scaleX, double scaleY)
    {
        _scaleX = scaleX;
        _scaleY = scaleY;
        _scale.ScaleX = scaleX;
        _scale.ScaleY = scaleY;
    }

    private static void AnimateOpacity(UIElement target, double to, TimeSpan duration)
    {
        target.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(target.Opacity, to, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        });
    }

    private void ApplyHoverShadow() =>
        _shadowHost.Effect = new DropShadowEffect
        {
            Color = Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF),
            BlurRadius = 18,
            ShadowDepth = 0,
            Opacity = _options.HoverShadowOpacity
        };

    private void ApplyIdleShadow() =>
        _shadowHost.Effect = new DropShadowEffect
        {
            Color = Color.FromArgb(0x28, 0, 0, 0),
            BlurRadius = 8,
            ShadowDepth = 0,
            Opacity = 0.28
        };

    private static RadialGradientBrush CreatePressGlowBrush(double dx, double dy)
    {
        var center = new Point(0.5 + dx * 0.22, 0.5 + dy * 0.22);
        var brush = new RadialGradientBrush
        {
            GradientOrigin = center,
            Center = center,
            RadiusX = 0.40,
            RadiusY = 0.40
        };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x70, 0xFF, 0xFF, 0xFF), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 1));
        brush.Freeze();
        return brush;
    }

    private static SolidColorBrush CreateFrozenBrush(byte a, byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        brush.Freeze();
        return brush;
    }

    private static T? FindAncestor<T>(DependencyObject? node) where T : DependencyObject
    {
        while (node != null)
        {
            if (node is T match)
                return match;

            node = VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node);
        }

        return null;
    }
}


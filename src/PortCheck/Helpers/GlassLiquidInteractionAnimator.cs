using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace PortCheck.Helpers;

/// <summary>
/// Liquid-glass round chrome (refined from approved direction):
/// translucent lens, hover enlarge, inward pinch under cursor, gel follows pointer (+dx/+dy).
/// No outward stretch, no hover specular wedge.
/// </summary>
public sealed class GlassLiquidInteractionAnimator
{
    /// <summary>Approved ~1.14, refined.</summary>
    public const double HoverEnterScale = 1.11;
    public const double PressScale = 0.96;
    /// <summary>Approved ~12%, refined — scale decreases toward cursor (inward collapse).</summary>
    public const double MaxInwardPinch = 0.095;
    /// <summary>Approved ~5.5px, refined — gel follows cursor, not inverted.</summary>
    public const double GelFollowPixels = 3.2;
    public const double DeformSmoothing = 0.38;

    public static readonly TimeSpan HoverEnterDuration = TimeSpan.FromMilliseconds(220);
    public static readonly TimeSpan HoverLeaveDuration = FluidAnimation.TabPushDuration;
    public static readonly TimeSpan PressInDuration = TimeSpan.FromMilliseconds(100);
    public static readonly TimeSpan PressGlowDuration = TimeSpan.FromMilliseconds(80);

    private static readonly SolidColorBrush HoverFill = CreateFrozenBrush(0x06, 0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush IdleFill = CreateFrozenBrush(0x14, 0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush PressFill = CreateFrozenBrush(0x12, 0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush HoverStroke = CreateFrozenBrush(0xA0, 0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush IdleStroke = CreateFrozenBrush(0x55, 0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush ActiveStroke = CreateFrozenBrush(0xC0, 0xFF, 0xFF, 0xFF);

    private readonly UIElement _host;
    private readonly FrameworkElement _root;
    private readonly Border _shadowHost;
    private readonly ScaleTransform _scale;
    private readonly TranslateTransform _gelFollow;
    private readonly Border _bd;
    private readonly Border _collapseVignette;
    private readonly Border _rimHighlight;
    private readonly Border _pressGlow;

    private bool _pointerInside;
    private bool _pressed;
    private double _scaleX = 1;
    private double _scaleY = 1;
    private double _gelX;
    private double _gelY;

    public GlassLiquidInteractionAnimator(
        UIElement host,
        FrameworkElement root,
        Border shadowHost,
        ScaleTransform scale,
        TranslateTransform gelFollow,
        Border bd,
        Border collapseVignette,
        Border rimHighlight,
        Border pressGlow)
    {
        _host = host;
        _root = root;
        _shadowHost = shadowHost;
        _scale = scale;
        _gelFollow = gelFollow;
        _bd = bd;
        _collapseVignette = collapseVignette;
        _rimHighlight = rimHighlight;
        _pressGlow = pressGlow;
    }

    public static bool MotionEnabled => SystemParameters.ClientAreaAnimation;

    public void Attach()
    {
        Detach();
        ApplyIdleShadow();
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
        _pressGlow.BeginAnimation(UIElement.OpacityProperty, null);
        _collapseVignette.BeginAnimation(UIElement.OpacityProperty, null);
        _bd.BeginAnimation(UIElement.OpacityProperty, null);
        _gelFollow.BeginAnimation(TranslateTransform.XProperty, null);
        _gelFollow.BeginAnimation(TranslateTransform.YProperty, null);
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        _pointerInside = true;
        ApplyGlassHoverVisual(true);

        if (!MotionEnabled)
        {
            ApplyScale(HoverEnterScale, HoverEnterScale);
            SetHoverLayerOpacities();
            ApplyHoverShadow();
            UpdateInwardDeform(e);
            return;
        }

        AnimateScale(HoverEnterScale, HoverEnterScale, HoverEnterDuration, FluidAnimation.SpringEase);
        AnimateOpacity(_rimHighlight, 0.50, HoverEnterDuration);
        AnimateOpacity(_collapseVignette, 0.44, HoverEnterDuration);
        AnimateOpacity(_bd, 0.70, HoverEnterDuration);
        ApplyHoverShadow();
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
        AnimateOpacity(_rimHighlight, 0, HoverLeaveDuration);
        AnimateOpacity(_collapseVignette, 0, HoverLeaveDuration);
        AnimateOpacity(_pressGlow, 0, PressGlowDuration);
        AnimateOpacity(_bd, 0.88, HoverLeaveDuration);
        _gelFollow.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(_gelFollow.X, 0, HoverLeaveDuration)
        {
            EasingFunction = FluidAnimation.SpringEase
        });
        _gelFollow.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(_gelFollow.Y, 0, HoverLeaveDuration)
        {
            EasingFunction = FluidAnimation.SpringEase
        });
        ApplyIdleShadow();
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
        _bd.Background = PressFill;
        _bd.BorderBrush = ActiveStroke;

        var (dx, dy) = GetNormalizedPointer(e);
        _pressGlow.Background = CreatePressGlowBrush(dx, dy);

        if (!MotionEnabled)
        {
            ApplyScale(PressScale, PressScale);
            _pressGlow.Opacity = 0.62;
            _collapseVignette.Opacity = 0.52;
            UpdateInwardDeform(e, extraPinch: 0.022);
            return;
        }

        AnimateOpacity(_pressGlow, 0.65, PressGlowDuration);
        AnimateOpacity(_collapseVignette, 0.52, PressGlowDuration);
        AnimateScale(PressScale, PressScale, PressInDuration, new QuadraticEase { EasingMode = EasingMode.EaseOut });
        UpdateInwardDeform(e, extraPinch: 0.022);
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _pressed = false;
        if (!_pointerInside)
            return;

        _bd.Background = HoverFill;
        _bd.BorderBrush = HoverStroke;

        if (!MotionEnabled)
        {
            _pressGlow.Opacity = 0;
            UpdateInwardDeform(e);
            return;
        }

        AnimateOpacity(_pressGlow, 0, PressGlowDuration);
        AnimateOpacity(_collapseVignette, 0.44, PressGlowDuration);
        UpdateInwardDeform(e);
    }

    /// <summary>
    /// Envelope grows; axes under cursor pinch inward (1 - pinch), never (1 + stretch).
    /// Gel translation uses +dx/+dy so motion matches pointer.
    /// </summary>
    private void UpdateInwardDeform(MouseEventArgs e, double extraPinch = 0)
    {
        var (dx, dy) = GetNormalizedPointer(e);
        var dist = Math.Min(1, Math.Sqrt(dx * dx + dy * dy));
        var pinch = (MaxInwardPinch + extraPinch) * (0.4 + 0.6 * dist);

        var axisX = 0.25 + 0.75 * Math.Abs(dx);
        var axisY = 0.25 + 0.75 * Math.Abs(dy);
        var targetX = HoverEnterScale * (1 - pinch * axisX);
        var targetY = HoverEnterScale * (1 - pinch * axisY);

        if (_pressed)
        {
            targetX = PressScale;
            targetY = PressScale;
        }

        var targetGelX = dx * GelFollowPixels;
        var targetGelY = dy * GelFollowPixels;

        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        _gelFollow.BeginAnimation(TranslateTransform.XProperty, null);
        _gelFollow.BeginAnimation(TranslateTransform.YProperty, null);

        if (!MotionEnabled)
        {
            ApplyScale(targetX, targetY);
            _gelFollow.X = targetGelX;
            _gelFollow.Y = targetGelY;
            ApplyLensVisuals(dx, dy);
            return;
        }

        _scaleX += (targetX - _scaleX) * DeformSmoothing;
        _scaleY += (targetY - _scaleY) * DeformSmoothing;
        _scale.ScaleX = _scaleX;
        _scale.ScaleY = _scaleY;

        _gelX += (targetGelX - _gelX) * DeformSmoothing;
        _gelY += (targetGelY - _gelY) * DeformSmoothing;
        _gelFollow.X = _gelX;
        _gelFollow.Y = _gelY;

        ApplyLensVisuals(dx, dy);
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

    private void ApplyLensVisuals(double dx, double dy)
    {
        _collapseVignette.Background = CreateCollapseVignetteBrush(dx, dy);
    }

    private void ApplyGlassHoverVisual(bool hover)
    {
        if (hover)
        {
            _bd.Background = HoverFill;
            _bd.BorderBrush = HoverStroke;
            return;
        }

        _bd.Background = IdleFill;
        _bd.BorderBrush = IdleStroke;
    }

    private void SetHoverLayerOpacities()
    {
        _rimHighlight.Opacity = 0.50;
        _collapseVignette.Opacity = 0.44;
        _bd.Opacity = 0.70;
        _pressGlow.Opacity = 0;
    }

    private void ResetVisualState()
    {
        ApplyScale(1, 1);
        _rimHighlight.Opacity = 0;
        _collapseVignette.Opacity = 0;
        _pressGlow.Opacity = 0;
        _bd.Opacity = 0.88;
        _gelFollow.X = 0;
        _gelFollow.Y = 0;
        _gelX = 0;
        _gelY = 0;
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
            Color = Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF),
            BlurRadius = 14,
            ShadowDepth = 0,
            Opacity = 0.32
        };

    private void ApplyIdleShadow() =>
        _shadowHost.Effect = new DropShadowEffect
        {
            Color = Color.FromArgb(0x28, 0, 0, 0),
            BlurRadius = 8,
            ShadowDepth = 0,
            Opacity = 0.28
        };

    private static RadialGradientBrush CreateCollapseVignetteBrush(double dx, double dy)
    {
        var center = new Point(0.5 + dx * 0.16, 0.5 + dy * 0.16);
        var brush = new RadialGradientBrush
        {
            GradientOrigin = center,
            Center = center,
            RadiusX = 0.72,
            RadiusY = 0.72
        };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0, 0, 0), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x58, 0, 0, 0), 0.60));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0, 0, 0), 1));
        brush.Freeze();
        return brush;
    }

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
}

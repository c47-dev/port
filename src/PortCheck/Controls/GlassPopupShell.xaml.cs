using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PortCheck.Helpers;

namespace PortCheck.Controls;

public partial class GlassPopupShell : UserControl
{
    public static readonly DependencyProperty ShellContentProperty =
        DependencyProperty.Register(nameof(ShellContent), typeof(object), typeof(GlassPopupShell));

    public static readonly DependencyProperty ShellCornerRadiusProperty =
        DependencyProperty.Register(
            nameof(ShellCornerRadius),
            typeof(CornerRadius),
            typeof(GlassPopupShell),
            new PropertyMetadata(new CornerRadius(12)));

    public static readonly DependencyProperty UseMenuChromeProperty =
        DependencyProperty.Register(
            nameof(UseMenuChrome),
            typeof(bool),
            typeof(GlassPopupShell),
            new PropertyMetadata(true, OnUseMenuChromeChanged));

    private Rect? _cachedBackdropRect;
    private ImageSource? _cachedBackdropImage;

    /// <summary>
    /// Backdrop cache read for chrome controls (round/pill lens crop). Not global popup styling.
    /// </summary>
    internal bool TryGetChromeLensBackdrop(out ImageSource? source, out Rect deviceRect)
    {
        source = _cachedBackdropImage;
        if (_cachedBackdropRect is { } rect && source != null && rect.Width >= 1 && rect.Height >= 1)
        {
            deviceRect = rect;
            return true;
        }

        source = null;
        deviceRect = default;
        return false;
    }

    public GlassPopupShell()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ApplyChromeStyle();
            RefreshBackdrop();
        };
    }

    public object? ShellContent
    {
        get => GetValue(ShellContentProperty);
        set => SetValue(ShellContentProperty, value);
    }

    public CornerRadius ShellCornerRadius
    {
        get => (CornerRadius)GetValue(ShellCornerRadiusProperty);
        set => SetValue(ShellCornerRadiusProperty, value);
    }

    public bool UseMenuChrome
    {
        get => (bool)GetValue(UseMenuChromeProperty);
        set => SetValue(UseMenuChromeProperty, value);
    }

    private static void OnUseMenuChromeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GlassPopupShell shell && shell.IsLoaded)
            shell.ApplyChromeStyle();
    }

    private void ApplyChromeStyle()
    {
        if (UseMenuChrome)
        {
            Chrome.SetResourceReference(StyleProperty, "GlassSortMenuShell");
            SetMenuBackdropLowLight(true);
        }
        else
        {
            Chrome.ClearValue(StyleProperty);
            Chrome.Background = Brushes.Transparent;
            Chrome.BorderThickness = new Thickness(0);
            Chrome.Padding = new Thickness(0);
            Chrome.Effect = null;
            SetMenuBackdropLowLight(false);
        }
    }

    /// <summary>Sort menu uses port-list surface (blur + tint only), not popup rim/sheen stack.</summary>
    private void SetMenuBackdropLowLight(bool lowLight)
    {
        var visibility = lowLight ? Visibility.Collapsed : Visibility.Visible;
        InnerSheenLayer.Visibility = visibility;
        RimLightLayer.Visibility = visibility;
        TintLayer.Background = lowLight
            ? (Brush)FindResource("Glass.List.Surface.Fill")!
            : (Brush)FindResource("Glass.Tint")!;
    }

    /// <summary>
    /// Captures and caches blurred backdrop for this shell. Pass a window device rect when the shell fills a top-level popup.
    /// </summary>
    public void RefreshBackdrop(Rect? deviceRectOverride = null)
    {
        if (!IsLoaded)
            return;

        UpdateLayout();

        var rect = deviceRectOverride ?? BackdropBlurHelper.GetDeviceRect(Chrome);
        if (rect.Width < 1 || rect.Height < 1)
            return;

        if (_cachedBackdropRect.HasValue &&
            _cachedBackdropImage != null &&
            AreRectsEquivalent(_cachedBackdropRect.Value, rect))
        {
            BackdropBrush.ImageSource = _cachedBackdropImage;
            return;
        }

        _cachedBackdropRect = rect;
        var blurRadius = UseMenuChrome ? 28 : 32;
        var dimOpacity = UseMenuChrome ? 0.04 : 0.06;
        _cachedBackdropImage = BackdropBlurHelper.CaptureBlurredRegion(rect, blurRadius: blurRadius, dimOpacity: dimOpacity);
        BackdropBrush.ImageSource = _cachedBackdropImage;
    }

    private static bool AreRectsEquivalent(Rect a, Rect b) =>
        Math.Abs(a.X - b.X) < 0.5 &&
        Math.Abs(a.Y - b.Y) < 0.5 &&
        Math.Abs(a.Width - b.Width) < 0.5 &&
        Math.Abs(a.Height - b.Height) < 0.5;
}

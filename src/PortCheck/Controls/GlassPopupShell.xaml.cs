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
            Chrome.SetResourceReference(StyleProperty, "GlassPopupMenuShell");
        else
        {
            Chrome.ClearValue(StyleProperty);
            Chrome.Background = Brushes.Transparent;
            Chrome.BorderThickness = new Thickness(0);
            Chrome.Padding = new Thickness(0);
            Chrome.Effect = null;
        }
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
        _cachedBackdropImage = BackdropBlurHelper.CaptureBlurredRegion(rect, blurRadius: 32, dimOpacity: 0.06);
        BackdropBrush.ImageSource = _cachedBackdropImage;
    }

    private static bool AreRectsEquivalent(Rect a, Rect b) =>
        Math.Abs(a.X - b.X) < 0.5 &&
        Math.Abs(a.Y - b.Y) < 0.5 &&
        Math.Abs(a.Width - b.Width) < 0.5 &&
        Math.Abs(a.Height - b.Height) < 0.5;
}

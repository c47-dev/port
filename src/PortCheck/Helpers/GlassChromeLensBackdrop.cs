using System.Windows;

using System.Windows.Controls;

using System.Windows.Media;

using System.Windows.Media.Imaging;

using PortCheck.Controls;



namespace PortCheck.Helpers;



/// <summary>

/// Backdrop sampling for chrome controls (<see cref="GlassRoundButton"/>, <see cref="GlassPillButton"/>).

/// </summary>

public static class GlassChromeLensBackdrop

{

    private const double CaptureMarginDips = 10;



    public static ImageBrush? TryCreateBrush(FrameworkElement host)

    {

        if (host.ActualWidth < 1 || host.ActualHeight < 1)

            host.UpdateLayout();



        if (host.ActualWidth < 1 || host.ActualHeight < 1)

            return null;

        if (PresentationSource.FromVisual(host) == null)

            return null;



        var dpi = VisualTreeHelper.GetDpi(host);

        var pixelW = Math.Max(32, (int)Math.Ceiling(host.ActualWidth * dpi.DpiScaleX) + 20);

        var pixelH = Math.Max(32, (int)Math.Ceiling(host.ActualHeight * dpi.DpiScaleY) + 20);



        var hostRect = BackdropBlurHelper.GetDeviceRect(host);

        var captureRect = Inflate(hostRect, CaptureMarginDips, dpi.DpiScaleX);



        if (TryCropFromPopupShell(host, captureRect, out var shellBrush) && shellBrush != null)

            return RasterizeToImageBrush(shellBrush, pixelW, pixelH);



        var captured = BackdropBlurHelper.CaptureBlurredRegion(captureRect, blurRadius: 10, dimOpacity: 0);

        if (captured == null)

            return null;



        var direct = new ImageBrush(captured) { Stretch = Stretch.Fill };

        direct.Freeze();

        return direct;

    }



    private static bool TryCropFromPopupShell(FrameworkElement host, Rect captureRect, out ImageBrush? brush)

    {

        brush = null;

        var shell = FindAncestor<GlassPopupShell>(host);

        if (shell == null || !shell.TryGetChromeLensBackdrop(out var source, out var shellRect))

            return false;



        if (shellRect.Width < 1 || shellRect.Height < 1)

            return false;



        var relX = (captureRect.X - shellRect.X) / shellRect.Width;

        var relY = (captureRect.Y - shellRect.Y) / shellRect.Height;

        var relW = captureRect.Width / shellRect.Width;

        var relH = captureRect.Height / shellRect.Height;



        var viewbox = new Rect(relX, relY, relW, relH);

        if (viewbox.Width < 0.01 || viewbox.Height < 0.01)

            return false;



        viewbox.Intersect(new Rect(0, 0, 1, 1));

        if (viewbox.Width < 0.01 || viewbox.Height < 0.01)

            return false;



        brush = new ImageBrush(source)

        {

            Stretch = Stretch.Fill,

            Viewbox = viewbox,

            ViewboxUnits = BrushMappingMode.RelativeToBoundingBox

        };

        brush.Freeze();

        return true;

    }



    private static ImageBrush RasterizeToImageBrush(Brush source, int width, int height)

    {

        var surface = new Border

        {

            Width = width,

            Height = height,

            Background = source,

            SnapsToDevicePixels = true

        };

        surface.Measure(new Size(width, height));

        surface.Arrange(new Rect(0, 0, width, height));



        var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

        rtb.Render(surface);

        rtb.Freeze();



        var brush = new ImageBrush(rtb) { Stretch = Stretch.Fill };

        brush.Freeze();

        return brush;

    }



    private static Rect Inflate(Rect rect, double marginDips, double dpiScale)

    {

        var margin = marginDips * dpiScale;

        return new Rect(

            rect.X - margin,

            rect.Y - margin,

            rect.Width + margin * 2,

            rect.Height + margin * 2);

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



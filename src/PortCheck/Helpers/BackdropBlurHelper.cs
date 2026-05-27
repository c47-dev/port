using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace PortCheck.Helpers;

/// <summary>
/// Real backdrop blur without Windows Acrylic/DWM: captures the screen behind a region and applies Gaussian blur in WPF.
/// </summary>
public static class BackdropBlurHelper
{
    public static ImageSource? CaptureBlurredRegion(Rect deviceRect, double blurRadius = 28, double dimOpacity = 0.08)
    {
        if (deviceRect.Width < 1 || deviceRect.Height < 1)
            return null;

        try
        {
            var width = (int)Math.Ceiling(deviceRect.Width);
            var height = (int)Math.Ceiling(deviceRect.Height);
            var x = (int)Math.Floor(deviceRect.X);
            var y = (int)Math.Floor(deviceRect.Y);

            using var screen = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(screen))
            {
                g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
            }

            var source = ToBitmapSource(screen);
            if (source == null)
                return null;

            var blurred = ApplyBlur(source, blurRadius);
            if (blurred == null)
                return null;

            if (dimOpacity <= 0)
                return blurred;

            var group = new DrawingGroup();
            using (var ctx = group.Open())
            {
                ctx.DrawImage(blurred, new Rect(0, 0, width, height));
                ctx.DrawRectangle(new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)(dimOpacity * 255), 0, 0, 0)),
                    null, new Rect(0, 0, width, height));
            }

            return new DrawingImage(group);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Backdrop capture failed: {ex.Message}");
            return null;
        }
    }

    public static Rect GetDeviceRect(Window window)
    {
        var dpi = VisualTreeHelper.GetDpi(window);
        var w = window.ActualWidth > 0 ? window.ActualWidth : window.Width;
        var h = window.ActualHeight > 0 ? window.ActualHeight : window.Height;
        return new Rect(
            window.Left * dpi.DpiScaleX,
            window.Top * dpi.DpiScaleY,
            w * dpi.DpiScaleX,
            h * dpi.DpiScaleY);
    }

    private static BitmapSource? ApplyBlur(BitmapSource source, double radius)
    {
        var image = new System.Windows.Controls.Image
        {
            Source = source,
            Effect = new BlurEffect { Radius = radius, RenderingBias = RenderingBias.Quality }
        };

        image.Measure(new System.Windows.Size(source.PixelWidth, source.PixelHeight));
        image.Arrange(new Rect(0, 0, source.PixelWidth, source.PixelHeight));

        var rtb = new RenderTargetBitmap(source.PixelWidth, source.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(image);
        rtb.Freeze();
        return rtb;
    }

    private static BitmapSource? ToBitmapSource(Bitmap bitmap)
    {
        var data = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            var source = BitmapSource.Create(
                bitmap.Width,
                bitmap.Height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                data.Scan0,
                data.Stride * bitmap.Height,
                data.Stride);

            source.Freeze();
            return source;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}

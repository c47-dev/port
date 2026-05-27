using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace PortCheck.Helpers;

/// <summary>
/// Enables Windows 10/11 Acrylic blur effect on WPF windows using SetWindowCompositionAttribute
/// </summary>
public static class WindowBlurHelper
{
    [DllImport("user32.dll")]
    internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    internal enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4, // Real acrylic blur (Windows 10 RS4+)
        ACCENT_INVALID_STATE = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public uint AccentFlags;
        public uint GradientColor; // Format: 0xAABBGGRR (Alpha, Blue, Green, Red)
        public uint AnimationId;
    }

    private const uint AcrylicFlag = 0x2;

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19
    }

    /// <summary>
    /// Enables Acrylic blur effect on the window
    /// </summary>
    /// <param name="window">The WPF window</param>
    /// <param name="blurOpacity">Opacity of the blur (0-255). Default is 180 for good translucency</param>
    /// <param name="blurColor">Background tint color in BGR format (0xBBGGRR). Default is white (0xFFFFFF)</param>
    public static void EnableAcrylicBlur(Window window, byte blurOpacity = 180, uint blurColor = 0xFFFFFF)
    {
        try
        {
            var windowHelper = new WindowInteropHelper(window);
            
            if (windowHelper.Handle == IntPtr.Zero)
            {
                // Window not yet loaded, attach to SourceInitialized event
                window.SourceInitialized += (s, e) =>
                {
                    ApplyAcrylicBlur(window, blurOpacity, blurColor);
                };
            }
            else
            {
                ApplyAcrylicBlur(window, blurOpacity, blurColor);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to enable acrylic blur: {ex.Message}");
        }
    }

    private static void ApplyAcrylicBlur(Window window, byte blurOpacity, uint blurColor)
    {
        var windowHelper = new WindowInteropHelper(window);
        
        var accent = new AccentPolicy
        {
            AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            AccentFlags = AcrylicFlag,
            GradientColor = ((uint)blurOpacity << 24) | (blurColor & 0xFFFFFF)
        };

        var accentStructSize = Marshal.SizeOf(accent);
        var accentPtr = Marshal.AllocHGlobal(accentStructSize);
        
        try
        {
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(accentPtr);
        }
    }

    /// <summary>
    /// Enables blur with custom settings for a glass-like effect
    /// </summary>
    public static void EnableGlassBlur(Window window)
    {
        EnableAcrylicBlur(window, blurOpacity: 100, blurColor: 0xFFFFFF);
    }

    /// <summary>
    /// Light frost acrylic — lets desktop show through (Apple-style liquid glass).
    /// </summary>
    public static void EnableLiquidGlass(Window window)
    {
        EnableAcrylicBlur(window, blurOpacity: 42, blurColor: 0xFFFFFF);
    }

    public static void ApplyRoundedWindowCorners(Window window, int preference = DWMWCP_ROUND)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DWM round corners failed: {ex.Message}");
        }
    }

    public static void ApplyVisualRoundClip(Window window, double cornerRadius)
    {
        void Apply()
        {
            if (window.ActualWidth <= 0 || window.ActualHeight <= 0)
                return;

            window.Clip = new RectangleGeometry(
                new Rect(0, 0, window.ActualWidth, window.ActualHeight),
                cornerRadius,
                cornerRadius);
        }

        window.Loaded += (_, _) => Apply();
        window.SizeChanged += (_, _) => Apply();
    }

    /// <summary>
    /// Disables the blur effect
    /// </summary>
    public static void DisableBlur(Window window)
    {
        try
        {
            var windowHelper = new WindowInteropHelper(window);
            
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_DISABLED
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };

                SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to disable blur: {ex.Message}");
        }
    }
}

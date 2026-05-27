using System.Runtime.InteropServices;
using System.Windows;

namespace PortCheck.Helpers;

public static class TrayPositionHelper
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    public enum TaskbarEdge
    {
        Bottom,
        Top,
        Left,
        Right
    }

    public static TaskbarEdge DetectTaskbarEdge()
    {
        var work = SystemParameters.WorkArea;
        var screenW = SystemParameters.PrimaryScreenWidth;
        var screenH = SystemParameters.PrimaryScreenHeight;

        if (work.Top > 0)
            return TaskbarEdge.Top;
        if (work.Left > 0)
            return TaskbarEdge.Left;
        if (work.Bottom < screenH - 1)
            return TaskbarEdge.Bottom;
        return TaskbarEdge.Right;
    }

    public static void PositionNearTray(Window popup, double width, double height)
    {
        GetCursorPos(out var cursor);
        var work = SystemParameters.WorkArea;
        const double margin = 8;

        double left;
        double top;

        switch (DetectTaskbarEdge())
        {
            case TaskbarEdge.Top:
                left = cursor.X - width / 2;
                top = work.Top + margin;
                break;
            case TaskbarEdge.Left:
                left = work.Left + margin;
                top = cursor.Y - height / 2;
                break;
            case TaskbarEdge.Right:
                left = work.Right - width - margin;
                top = cursor.Y - height / 2;
                break;
            default:
                left = cursor.X - width / 2;
                top = work.Bottom - height - margin;
                break;
        }

        left = Math.Clamp(left, work.Left + margin, work.Right - width - margin);
        top = Math.Clamp(top, work.Top + margin, work.Bottom - height - margin);

        popup.Left = left;
        popup.Top = top;
    }
}

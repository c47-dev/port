using System.Windows.Threading;

namespace PortCheck.Validation;

internal static class ValidationAnimationPump
{
    public static void Wait(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = duration
        };

        timer.Tick += OnTick;
        timer.Start();
        Dispatcher.PushFrame(frame);
        return;

        void OnTick(object? sender, EventArgs e)
        {
            timer.Stop();
            timer.Tick -= OnTick;
            frame.Continue = false;
        }
    }
}

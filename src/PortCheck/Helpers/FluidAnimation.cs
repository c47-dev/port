using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PortCheck.Models;

namespace PortCheck.Helpers;

/// <summary>
/// iOS Mail–style spring / fade motion (response ~0.35, damping ~0.75).
/// </summary>
public static class FluidAnimation
{
    // TabIconSlot(32) + label gap(4) + label + trailing(8)
    public const double TabCollapsedWidth = 32;
    public const double TabLocalExpandedWidth = 106;
    public const double TabDockerExpandedWidth = 118;

    public static readonly TimeSpan PaneFadeDuration = TimeSpan.FromMilliseconds(220);
    public static readonly TimeSpan TabPushDuration = TimeSpan.FromMilliseconds(380);

    public static IEasingFunction SpringEase { get; } = new ElasticEase
    {
        Oscillations = 1,
        Springiness = 4,
        EasingMode = EasingMode.EaseOut
    };

    public static void RunPaneCrossfade(UIElement outgoing, UIElement incoming, Action? onCompleted = null)
    {
        outgoing.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0, PaneFadeDuration));
        incoming.BeginAnimation(UIElement.OpacityProperty, null);
        incoming.Opacity = 0;
        incoming.Visibility = Visibility.Visible;

        var fadeIn = new DoubleAnimation(0, 1, PaneFadeDuration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        fadeIn.Completed += (_, _) =>
        {
            outgoing.Visibility = Visibility.Collapsed;
            outgoing.ClearValue(UIElement.OpacityProperty);
            incoming.ClearValue(UIElement.OpacityProperty);
            if (outgoing is FrameworkElement outFe)
                outFe.RenderTransform = null;
            if (incoming is FrameworkElement inFe)
                inFe.RenderTransform = null;
            onCompleted?.Invoke();
        };
        incoming.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        if (incoming.RenderTransform is not TranslateTransform tt)
        {
            tt = new TranslateTransform(14, 0);
            incoming.RenderTransform = tt;
        }
        else
            tt.X = 14;

        tt.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(14, 0, PaneFadeDuration)
        {
            EasingFunction = SpringEase
        });
    }

    public static void SetPaneTabWidths(Button localTab, Button dockerTab, PortPane activePane)
    {
        localTab.BeginAnimation(FrameworkElement.WidthProperty, null);
        dockerTab.BeginAnimation(FrameworkElement.WidthProperty, null);
        localTab.Width = activePane == PortPane.Local ? TabLocalExpandedWidth : TabCollapsedWidth;
        dockerTab.Width = activePane == PortPane.Docker ? TabDockerExpandedWidth : TabCollapsedWidth;
    }

    /// <summary>
    /// Animate tab chip width so the expanding pill pushes its sibling (Mail Smart Categories).
    /// </summary>
    public static void RunTabPush(Button localTab, Button dockerTab, PortPane activePane)
    {
        AnimateTabWidth(localTab, activePane == PortPane.Local ? TabLocalExpandedWidth : TabCollapsedWidth);
        AnimateTabWidth(dockerTab, activePane == PortPane.Docker ? TabDockerExpandedWidth : TabCollapsedWidth);
    }

    private static void AnimateTabWidth(Button tab, double targetWidth)
    {
        tab.BeginAnimation(FrameworkElement.WidthProperty, null);
        var from = tab.ActualWidth;
        if (from <= 0 || double.IsNaN(from))
            from = tab.Width > 0 ? tab.Width : TabCollapsedWidth;

        var anim = new DoubleAnimation(from, targetWidth, TabPushDuration)
        {
            EasingFunction = SpringEase
        };
        tab.BeginAnimation(FrameworkElement.WidthProperty, anim);
    }

    public static void PopIcon(FrameworkElement target)
    {
        var scale = new ScaleTransform(1, 1);
        target.RenderTransform = scale;
        target.RenderTransformOrigin = new Point(0.5, 0.5);

        var anim = new DoubleAnimationUsingKeyFrames();
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(1.12, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120)))
        {
            EasingFunction = SpringEase
        });
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(280)))
        {
            EasingFunction = SpringEase
        });
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
    }
}

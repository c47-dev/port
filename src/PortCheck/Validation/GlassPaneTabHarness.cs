using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using PortCheck.Controls;

namespace PortCheck.Validation;

/// <summary>Headless checks for GlassPaneTabButton (--validate-glass-pane-tab).</summary>
public static class GlassPaneTabHarness
{
    public static int Run(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var reportPath = Path.Combine(outputDir, "glass-pane-tab-report.txt");

        try
        {
            var failures = new List<string>();
            failures.AddRange(ValidateStyle("GlassPaneTabLocal", "TabIcon"));
            failures.AddRange(ValidateStyle("GlassPaneTabDocker", "TabIconPopTarget"));

            if (failures.Count == 0)
            {
                File.WriteAllText(reportPath, "PASS\n");
                return 0;
            }

            File.WriteAllText(reportPath, "FAIL\n" + string.Join("\n", failures));
            return 1;
        }
        catch (Exception ex)
        {
            File.WriteAllText(reportPath, "FAIL\n" + ex);
            return 1;
        }
    }

    private static IEnumerable<string> ValidateStyle(string styleKey, string iconPartName)
    {
        var button = CreateMeasuredTab(styleKey);
        var prefix = $"{styleKey}: ";

        foreach (var msg in ValidateTemplateParts(button, iconPartName))
            yield return prefix + msg;

        if (button.Template?.FindName("PART_Chip", button) is not Grid chip
            || button.Template.FindName("PART_GlassStack", button) is not DependencyObject glassStack
            || button.Template.FindName("ContentHost", button) is not DependencyObject contentHost)
        {
            yield return prefix + "PART_Chip, PART_GlassStack, or ContentHost missing";
            yield break;
        }

        var glassIndex = chip.Children.IndexOf(glassStack as UIElement);
        var contentIndex = chip.Children.IndexOf(contentHost as UIElement);
        if (glassIndex < 0 || contentIndex < 0)
        {
            yield return prefix + "Glass stack or content host not in PART_Chip";
            yield break;
        }

        if (contentIndex <= glassIndex)
            yield return prefix + "ContentHost must render above PART_GlassStack (sharp glyphs)";

        if (button.Template.FindName(iconPartName, button) is DependencyObject icon
            && IsVisualDescendantOf(icon, glassStack))
            yield return prefix + $"{iconPartName} must not be inside PART_GlassStack";

        foreach (var msg in ValidateHoverChrome(button, prefix))
            yield return msg;
    }

    private static IEnumerable<string> ValidateHoverChrome(GlassPaneTabButton button, string prefix)
    {
        if (button.Template?.FindName("PART_Scale", button) is not ScaleTransform scale)
        {
            yield return prefix + "PART_Scale not resolved";
            yield break;
        }

        if (button.Template.FindName("PART_LensPlate", button) is not Border lens)
        {
            yield return prefix + "PART_LensPlate not resolved";
            yield break;
        }

        if (button.Template.FindName("TabIcon", button) is TextBlock localIcon)
        {
            if (localIcon.Opacity < 0.9)
                yield return prefix + $"TabIcon faded (opacity={localIcon.Opacity:F2})";
            if (localIcon.Foreground is SolidColorBrush fg && fg.Color.A < 128)
                yield return prefix + "TabIcon foreground too transparent";
        }

        button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent });
        button.UpdateLayout();

        if (scale.ScaleX < 1.05 && scale.ScaleY < 1.05)
            yield return prefix + $"Hover scale not applied (scale={scale.ScaleX:F3},{scale.ScaleY:F3})";

        var expectedLens = GlassChromeInteractionOptions.PaneTab.HoverLensPlateOpacity;
        if (lens.Opacity < expectedLens - 0.12 || lens.Opacity > expectedLens + 0.12)
            yield return prefix + $"Lens opacity out of range (opacity={lens.Opacity:F2}, expected~{expectedLens:F2})";

        if (lens.Effect is BlurEffect)
            yield return prefix + "Lens must not use BlurEffect on pane tabs (blurs labels)";

        button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseLeaveEvent });
        button.UpdateLayout();

        if (scale.ScaleX > 1.02 || scale.ScaleY > 1.02)
            yield return prefix + $"Hover scale not reset on leave (scale={scale.ScaleX:F3},{scale.ScaleY:F3})";

        if (lens.Opacity > 0.05)
            yield return prefix + $"Lens not cleared on leave (opacity={lens.Opacity:F2})";
    }

    private static GlassPaneTabButton CreateMeasuredTab(string styleKey)
    {
        if (Application.Current?.TryFindResource(styleKey) is not Style style)
            throw new InvalidOperationException($"Style not found: {styleKey}");

        var host = new Grid { Width = 220, Height = 48, ClipToBounds = false };
        var button = new GlassPaneTabButton
        {
            Style = style,
            Width = 32,
            Height = 32,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        host.Children.Add(button);
        host.Measure(new Size(220, 48));
        host.Arrange(new Rect(0, 0, 220, 48));
        button.UpdateLayout();
        return button;
    }

    private static IEnumerable<string> ValidateTemplateParts(GlassPaneTabButton button, string iconPartName)
    {
        string[] required =
        [
            "PART_Outer", "PART_Root", "PART_Scale", "PART_GelFollow", "PART_InnerPinch", "PART_Chip",
            "PART_GlassStack", "PART_LensPlate", "PART_FresnelRim", "PART_RimHighlight",
            "PART_TopSpecular", "PART_BottomSpecular", "PART_PressGlow", "Shell", "ContentHost",
            iconPartName, "Label", "PART_HitTarget"
        ];

        foreach (var name in required)
        {
            if (button.Template?.FindName(name, button) == null)
                yield return $"Template part missing: {name}";
        }
    }

    private static bool IsVisualDescendantOf(DependencyObject node, DependencyObject ancestor)
    {
        for (var current = VisualTreeHelper.GetParent(node); current != null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, ancestor))
                return true;
        }

        return false;
    }
}

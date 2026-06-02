using System.IO;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Input;

using System.Windows.Media;

using PortCheck.Controls;




namespace PortCheck.Validation;



/// <summary>Headless checks for GlassRoundButton (--validate-glass-round-button).</summary>

public static class GlassRoundButtonHarness

{

    public static int Run(string outputDir)

    {

        Directory.CreateDirectory(outputDir);

        var reportPath = Path.Combine(outputDir, "glass-round-button-report.txt");



        try

        {

            var failures = new List<string>();

            failures.AddRange(ValidateLayoutAndParts());

            failures.AddRange(ValidateHoverMotion());



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



    private static IEnumerable<string> ValidateLayoutAndParts()

    {

        var button = CreateMeasuredButton();

        return ValidateTemplateParts(button);

    }



    private static IEnumerable<string> ValidateHoverMotion()

    {

        var button = CreateMeasuredButton();

        var failures = ValidateTemplateParts(button).ToList();

        if (failures.Count > 0)

            return failures;



        if (button.Template?.FindName("PART_Scale", button) is not ScaleTransform scale)

            return ["PART_Scale not resolved after layout"];



        if (button.Template.FindName("PART_LensPlate", button) is not Border lens)

            return ["PART_LensPlate not resolved after layout"];



        button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0)

        {

            RoutedEvent = Mouse.MouseEnterEvent

        });

        button.UpdateLayout();



        var motionFailures = new List<string>();

        if (scale.ScaleX < 1.05 && scale.ScaleY < 1.05)

        {

            motionFailures.Add(

                $"Hover scale not applied (scale={scale.ScaleX:F3},{scale.ScaleY:F3}, expected>={GlassChromeInteractionAnimator.HoverEnterScale:F2})");

        }



        if (lens.Opacity < 0.5)

            motionFailures.Add($"Lens plate not visible on hover (opacity={lens.Opacity:F2})");



        button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0)

        {

            RoutedEvent = Mouse.MouseLeaveEvent

        });

        button.UpdateLayout();



        if (scale.ScaleX > 1.02 || scale.ScaleY > 1.02)

            motionFailures.Add($"Hover scale not reset on leave (scale={scale.ScaleX:F3},{scale.ScaleY:F3})");



        return motionFailures;

    }



    private static GlassRoundButton CreateMeasuredButton()

    {

        var host = new Grid { Width = 64, Height = 64, ClipToBounds = false };

        var button = new GlassRoundButton

        {

            Width = 32,

            Height = 32,

            HorizontalAlignment = HorizontalAlignment.Center,

            VerticalAlignment = VerticalAlignment.Center

        };

        host.Children.Add(button);

        host.Measure(new Size(64, 64));

        host.Arrange(new Rect(0, 0, 64, 64));

        button.UpdateLayout();

        return button;

    }



    private static IEnumerable<string> ValidateTemplateParts(GlassRoundButton button)

    {

        string[] required =

        [

            "PART_Outer", "PART_Root", "PART_Scale", "PART_GelFollow", "PART_InnerPinch",

            "PART_LensPlate", "PART_Bd", "PART_FresnelRim", "PART_RimHighlight",

            "PART_TopSpecular", "PART_BottomSpecular", "PART_PressGlow"

        ];



        foreach (var name in required)

        {

            if (button.Template?.FindName(name, button) == null)

                yield return $"Template part missing: {name}";

        }

    }

}


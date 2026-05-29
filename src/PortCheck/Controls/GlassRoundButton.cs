using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PortCheck.Helpers;

namespace PortCheck.Controls;

/// <summary>
/// 32×32 translucent lens chrome: hover enlarge + inward pinch, collapse vignette, rim refraction.
/// </summary>
public class GlassRoundButton : Button
{
    private const string PartRoot = "PART_Root";
    private const string PartScale = "PART_Scale";
    private const string PartGelFollow = "PART_GelFollow";
    private const string PartShadowHost = "PART_ShadowHost";
    private const string PartBd = "PART_Bd";
    private const string PartCollapseVignette = "PART_CollapseVignette";
    private const string PartRimHighlight = "PART_RimHighlight";
    private const string PartPressGlow = "PART_PressGlow";

    private GlassLiquidInteractionAnimator? _animator;

    static GlassRoundButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(GlassRoundButton),
            new FrameworkPropertyMetadata(typeof(GlassRoundButton)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _animator?.Detach();

        if (GetTemplateChild(PartRoot) is not FrameworkElement root
            || GetTemplateChild(PartScale) is not ScaleTransform scale
            || GetTemplateChild(PartGelFollow) is not TranslateTransform gelFollow
            || GetTemplateChild(PartShadowHost) is not Border shadowHost
            || GetTemplateChild(PartBd) is not Border bd
            || GetTemplateChild(PartCollapseVignette) is not Border collapseVignette
            || GetTemplateChild(PartRimHighlight) is not Border rimHighlight
            || GetTemplateChild(PartPressGlow) is not Border pressGlow)
        {
            _animator = null;
            return;
        }

        _animator = new GlassLiquidInteractionAnimator(
            this, root, shadowHost, scale, gelFollow, bd, collapseVignette, rimHighlight, pressGlow);
        _animator.Attach();
    }
}

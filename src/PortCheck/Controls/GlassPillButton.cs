using System.Windows;

using System.Windows.Controls;

using System.Windows.Media;



namespace PortCheck.Controls;



/// <summary>

/// Capsule liquid-glass chrome (e.g. Settings "Add"). Same motion stack as <see cref="GlassRoundButton"/>.

/// </summary>

public class GlassPillButton : Button

{

    private const string PartRoot = "PART_Root";

    private const string PartScale = "PART_Scale";

    private const string PartGelFollow = "PART_GelFollow";

    private const string PartInnerPinch = "PART_InnerPinch";

    private const string PartShadowHost = "PART_ShadowHost";

    private const string PartLensPlate = "PART_LensPlate";

    private const string PartBd = "PART_Bd";

    private const string PartFresnelRim = "PART_FresnelRim";

    private const string PartRimHighlight = "PART_RimHighlight";

    private const string PartTopSpecular = "PART_TopSpecular";

    private const string PartBottomSpecular = "PART_BottomSpecular";

    private const string PartPressGlow = "PART_PressGlow";



    private GlassChromeInteractionAnimator? _animator;



    static GlassPillButton()

    {

        DefaultStyleKeyProperty.OverrideMetadata(

            typeof(GlassPillButton),

            new FrameworkPropertyMetadata(typeof(GlassPillButton)));

    }



    public override void OnApplyTemplate()

    {

        base.OnApplyTemplate();

        _animator?.Detach();



        if (FindPart(PartRoot) is not FrameworkElement root

            || FindPart(PartScale) is not ScaleTransform scale

            || FindPart(PartGelFollow) is not TranslateTransform gelFollow

            || FindPart(PartInnerPinch) is not ScaleTransform innerPinch

            || FindPart(PartShadowHost) is not Border shadowHost

            || FindPart(PartLensPlate) is not Border lensPlate

            || FindPart(PartBd) is not Border bd

            || FindPart(PartFresnelRim) is not Border fresnelRim

            || FindPart(PartRimHighlight) is not Border rimHighlight

            || FindPart(PartTopSpecular) is not Border topSpecular

            || FindPart(PartBottomSpecular) is not Border bottomSpecular

            || FindPart(PartPressGlow) is not Border pressGlow)

        {

            _animator = null;

            return;

        }



        _animator = new GlassChromeInteractionAnimator(

            this,

            root,

            shadowHost,

            scale,

            gelFollow,

            innerPinch,

            lensPlate,

            bd,

            fresnelRim,

            rimHighlight,

            topSpecular,

            bottomSpecular,

            pressGlow);

        _animator.Attach();

    }



    private object? FindPart(string name) => Template?.FindName(name, this);

}



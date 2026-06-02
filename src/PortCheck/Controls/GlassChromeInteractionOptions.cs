namespace PortCheck.Controls;



/// <summary>Per-control tuning for <see cref="GlassChromeInteractionAnimator"/>.</summary>

public sealed class GlassChromeInteractionOptions

{

    public static GlassChromeInteractionOptions Standard { get; } = new();



    /// <summary>Full liquid-glass hover; Shell keeps idle/active; no screen-capture blur over labels.</summary>

    public static GlassChromeInteractionOptions PaneTab { get; } = new()

    {

        HoverOverlayOnly = true,

        UseBackdropLens = false,

        HoverLensPlateOpacity = 0.42,

        MaxInwardPinch = 0.07,

        GelFollowPixels = 2.8

    };



    public bool HoverOverlayOnly { get; init; }

    public bool UseBackdropLens { get; init; } = true;

    public double HoverEnterScale { get; init; } = 1.14;

    public double PressScale { get; init; } = 0.96;

    public double MaxInwardPinch { get; init; } = 0.095;

    public double GelFollowPixels { get; init; } = 3.2;

    public bool EnableLensPlate { get; init; } = true;

    public bool EnableFresnelRim { get; init; } = true;

    public double HoverLensPlateOpacity { get; init; } = 1;

    public double HoverTopSpecularOpacity { get; init; } = 0.72;

    public double HoverBottomSpecularOpacity { get; init; } = 0.42;

    public double HoverRimHighlightOpacity { get; init; } = 0.38;

    public double HoverFresnelOpacity { get; init; } = 0.92;

    public double HoverShadowOpacity { get; init; } = 0.45;

}



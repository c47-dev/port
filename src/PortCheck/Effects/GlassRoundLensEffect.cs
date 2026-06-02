using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PortCheck.Effects;

/// <summary>
/// WPF pixel-shader lens for 32×32 round chrome (adapted from WPF-Liquid-Glass-Effect / GlassyEffect.ps, MIT).
/// </summary>
public sealed class GlassRoundLensEffect : ShaderEffect
{
    public static readonly DependencyProperty InputProperty =
        RegisterPixelShaderSamplerProperty(nameof(Input), typeof(GlassRoundLensEffect), 0);

    public static readonly DependencyProperty TextureSizeProperty =
        DependencyProperty.Register(
            nameof(TextureSize),
            typeof(Point),
            typeof(GlassRoundLensEffect),
            new UIPropertyMetadata(new Point(32, 32), PixelShaderConstantCallback(0)));

    public static readonly DependencyProperty GlassCenterProperty =
        DependencyProperty.Register(
            nameof(GlassCenter),
            typeof(Point),
            typeof(GlassRoundLensEffect),
            new UIPropertyMetadata(new Point(16, 16), PixelShaderConstantCallback(1)));

    public static readonly DependencyProperty GlassSizeProperty =
        DependencyProperty.Register(
            nameof(GlassSize),
            typeof(Point),
            typeof(GlassRoundLensEffect),
            new UIPropertyMetadata(new Point(28, 28), PixelShaderConstantCallback(2)));

    public static readonly DependencyProperty BlurIntensityProperty =
        DependencyProperty.Register(
            nameof(BlurIntensity),
            typeof(float),
            typeof(GlassRoundLensEffect),
            new UIPropertyMetadata(0.45f, PixelShaderConstantCallback(3)));

    public GlassRoundLensEffect()
    {
        PixelShader = new PixelShader
        {
            UriSource = new Uri(
                "pack://application:,,,/PortCheck;component/Shaders/GlassRoundLensEffect.ps",
                UriKind.Absolute)
        };

        UpdateShaderValue(InputProperty);
        UpdateShaderValue(TextureSizeProperty);
        UpdateShaderValue(GlassCenterProperty);
        UpdateShaderValue(GlassSizeProperty);
        UpdateShaderValue(BlurIntensityProperty);
    }

    public Brush Input
    {
        get => (Brush)GetValue(InputProperty);
        set => SetValue(InputProperty, value);
    }

    public Point TextureSize
    {
        get => (Point)GetValue(TextureSizeProperty);
        set => SetValue(TextureSizeProperty, value);
    }

    public Point GlassCenter
    {
        get => (Point)GetValue(GlassCenterProperty);
        set => SetValue(GlassCenterProperty, value);
    }

    public Point GlassSize
    {
        get => (Point)GetValue(GlassSizeProperty);
        set => SetValue(GlassSizeProperty, value);
    }

    public float BlurIntensity
    {
        get => (float)GetValue(BlurIntensityProperty);
        set => SetValue(BlurIntensityProperty, value);
    }
}

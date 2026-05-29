using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PortCheck.Controls;

public partial class SettingsIconBadge : UserControl
{
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(SettingsIconBadge), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconBackgroundProperty =
        DependencyProperty.Register(nameof(IconBackground), typeof(Brush), typeof(SettingsIconBadge),
            new PropertyMetadata(Brushes.Transparent));

    public SettingsIconBadge()
    {
        InitializeComponent();
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public Brush IconBackground
    {
        get => (Brush)GetValue(IconBackgroundProperty);
        set => SetValue(IconBackgroundProperty, value);
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PortCheck.Controls;

public partial class PopupActionRow : UserControl
{
    public static readonly DependencyProperty IconTextProperty =
        DependencyProperty.Register(nameof(IconText), typeof(string), typeof(PopupActionRow), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register(nameof(LabelText), typeof(string), typeof(PopupActionRow), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ShortcutTextProperty =
        DependencyProperty.Register(nameof(ShortcutText), typeof(string), typeof(PopupActionRow), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconForegroundProperty =
        DependencyProperty.Register(nameof(IconForeground), typeof(Brush), typeof(PopupActionRow), new PropertyMetadata(Brushes.White));

    public static readonly DependencyProperty LabelForegroundProperty =
        DependencyProperty.Register(nameof(LabelForeground), typeof(Brush), typeof(PopupActionRow), new PropertyMetadata(Brushes.White));

    public PopupActionRow()
    {
        InitializeComponent();
    }

    public string IconText
    {
        get => (string)GetValue(IconTextProperty);
        set => SetValue(IconTextProperty, value);
    }

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public string ShortcutText
    {
        get => (string)GetValue(ShortcutTextProperty);
        set => SetValue(ShortcutTextProperty, value);
    }

    public Brush IconForeground
    {
        get => (Brush)GetValue(IconForegroundProperty);
        set => SetValue(IconForegroundProperty, value);
    }

    public Brush LabelForeground
    {
        get => (Brush)GetValue(LabelForegroundProperty);
        set => SetValue(LabelForegroundProperty, value);
    }
}

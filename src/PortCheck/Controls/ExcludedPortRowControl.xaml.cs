using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PortCheck.Controls;

public partial class ExcludedPortRowControl : UserControl
{
    public static readonly DependencyProperty PortProperty =
        DependencyProperty.Register(nameof(Port), typeof(int), typeof(ExcludedPortRowControl), new PropertyMetadata(0));

    public static readonly DependencyProperty RemoveCommandProperty =
        DependencyProperty.Register(nameof(RemoveCommand), typeof(ICommand), typeof(ExcludedPortRowControl));

    public ExcludedPortRowControl()
    {
        InitializeComponent();
    }

    public int Port
    {
        get => (int)GetValue(PortProperty);
        set => SetValue(PortProperty, value);
    }

    public ICommand? RemoveCommand
    {
        get => (ICommand?)GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }
}

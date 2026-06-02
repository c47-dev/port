using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using PortCheck.Models;

namespace PortCheck.Controls;

public partial class LocalPortRowControl : UserControl
{
    public static readonly DependencyProperty KillCommandProperty =
        DependencyProperty.Register(nameof(KillCommand), typeof(ICommand), typeof(LocalPortRowControl));
    public static readonly DependencyProperty ToggleFavouriteCommandProperty =
        DependencyProperty.Register(nameof(ToggleFavouriteCommand), typeof(ICommand), typeof(LocalPortRowControl));

    public LocalPortRowControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => SyncState();
        Unloaded += (_, _) => Detach(Row);
    }

    public ICommand? KillCommand
    {
        get => (ICommand?)GetValue(KillCommandProperty);
        set => SetValue(KillCommandProperty, value);
    }

    public ICommand? ToggleFavouriteCommand
    {
        get => (ICommand?)GetValue(ToggleFavouriteCommandProperty);
        set => SetValue(ToggleFavouriteCommandProperty, value);
    }

    private PortInfo? Row => DataContext as PortInfo;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Detach(e.OldValue as PortInfo);
        Attach(e.NewValue as PortInfo);
        SyncState();
    }

    private void BeginKill_Click(object sender, RoutedEventArgs e)
    {
        if (Row is not { } row)
            return;

        row.IsConfirmingKill = true;
        SyncState();
    }

    private void ConfirmKill_Click(object sender, RoutedEventArgs e)
    {
        if (Row is not { } row)
            return;

        row.IsConfirmingKill = false;
        SyncState();
        if (KillCommand?.CanExecute(row) == true)
            KillCommand.Execute(row);
    }

    private void CancelKill_Click(object sender, RoutedEventArgs e)
    {
        if (Row is not { } row)
            return;

        row.IsConfirmingKill = false;
        SyncState();
    }

    private void SyncState()
    {
        if (Row is not { } row)
            return;

        NormalRow.Visibility = row.IsConfirmingKill ? Visibility.Collapsed : Visibility.Visible;
        ConfirmRow.Visibility = row.IsConfirmingKill ? Visibility.Visible : Visibility.Collapsed;
        ProcessingText.Visibility = row.IsKilling ? Visibility.Visible : Visibility.Collapsed;

    }

    private void Attach(PortInfo? row)
    {
        if (row != null)
            row.PropertyChanged += OnRowPropertyChanged;
    }

    private void Detach(PortInfo? row)
    {
        if (row != null)
            row.PropertyChanged -= OnRowPropertyChanged;
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(SyncState);
    }
}

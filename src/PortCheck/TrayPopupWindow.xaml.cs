using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using PortCheck.Helpers;
using PortCheck.Models;
using PortCheck.ViewModels;

namespace PortCheck;

public partial class TrayPopupWindow : Window
{
    private readonly TrayViewModel _viewModel;
    private bool _isProcessingAction;
    private bool _isManualRefresh;

    public TrayPopupWindow()
        : this(App.Services.GetRequiredService<TrayViewModel>())
    {
    }

    public TrayPopupWindow(TrayViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            _isManualRefresh = true;
            await _viewModel.RefreshPortsCommand.ExecuteAsync(null);
            _isManualRefresh = false;
            UpdateEmptyState();
        };

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(TrayViewModel.FilteredPorts) or nameof(TrayViewModel.Ports))
                Dispatcher.Invoke(UpdateEmptyState);

            if (e.PropertyName == nameof(TrayViewModel.IsScanning))
            {
                Dispatcher.Invoke(() =>
                {
                    if (!_isManualRefresh)
                        return;

                    if (_viewModel.IsScanning)
                        RefreshStatusText.Text = "Refreshing...";
                    else
                    {
                        RefreshStatusText.Text = "Refreshed";
                        _isManualRefresh = false;
                    }
                });
            }
        };

        SetupInputBindings();
    }

    private void SetupInputBindings()
    {
        InputBindings.Add(new KeyBinding(_viewModel.RefreshPortsCommand, Key.R, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommandWrapper(() => KillAllWithConfirm()), Key.K, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommandWrapper(HideToTray), Key.Escape, ModifierKeys.None));
    }

    private void UpdateEmptyState()
    {
        EmptyStateText.Visibility = _viewModel.FilteredPorts.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public void ShowNearTray()
    {
        var work = SystemParameters.WorkArea;
        var maxWindowH = Math.Min(560, work.Height * 0.75);
        MaxHeight = maxWindowH;
        Height = maxWindowH;

        // Position before capture so CopyFromScreen sees the desktop behind this rect
        UpdateLayout();
        Width = 340;
        Height = maxWindowH;
        TrayPositionHelper.PositionNearTray(this, Width, Height);
        UpdateBackdropBlur();

        if (!IsLoaded)
        {
            Opacity = 0;
            Show();
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
        }
        else
        {
            Show();
        }

        Activate();
        SearchBox.Focus();
    }

    private void UpdateBackdropBlur()
    {
        var rect = BackdropBlurHelper.GetDeviceRect(this);
        BackdropImage.Source = BackdropBlurHelper.CaptureBlurredRegion(rect, blurRadius: 32, dimOpacity: 0.06);
    }

    private void PortsList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ListBox list)
            return;

        var scrollViewer = FindVisualChild<System.Windows.Controls.ScrollViewer>(list);
        if (scrollViewer == null)
            return;

        e.Handled = true;
        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3.0);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                return match;

            var nested = FindVisualChild<T>(child);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (_isProcessingAction)
            return;
        HideToTray();
    }

    private void KillSinglePort_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button { Tag: PortInfo port })
        {
            e.Handled = true;
            port.IsConfirmingKill = true;
        }
    }

    private async void ConfirmKill_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PortInfo port })
        {
            port.IsConfirmingKill = false;
            await _viewModel.KillProcessCommand.ExecuteAsync(port);
        }
    }

    private void CancelKill_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PortInfo port })
            port.IsConfirmingKill = false;
    }

    private async void KillAll_Click(object sender, RoutedEventArgs e) => await KillAllWithConfirm();

    private async Task KillAllWithConfirm()
    {
        if (!_viewModel.Ports.Any())
            return;

        _isProcessingAction = true;
        try
        {
            var dialog = new ConfirmDialog(
                $"Kill ALL {_viewModel.Ports.Count} active processes?",
                "This will terminate all processes currently using ports.",
                "Kill All")
            {
                Owner = this
            };

            dialog.ShowDialog();
            if (dialog.Result)
                await _viewModel.KillAllCommand.ExecuteAsync(null);
        }
        finally
        {
            _isProcessingAction = false;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _isManualRefresh = true;
        RefreshStatusText.Text = "Refreshing...";
        await _viewModel.RefreshPortsCommand.ExecuteAsync(null);
    }

    private void HideToTray_Click(object sender, RoutedEventArgs e) => HideToTray();

    private void HideToTray()
    {
        if (App.Services.GetService<TrayHost>() is TrayHost host)
            host.HidePopup();
        else
            Hide();
    }

    private sealed class RelayCommandWrapper : ICommand
    {
        private readonly Action _action;
        private readonly Func<Task>? _asyncAction;

        public RelayCommandWrapper(Action action) => _action = action;

        public RelayCommandWrapper(Func<Task> asyncAction)
        {
            _asyncAction = asyncAction;
            _action = () => _ = asyncAction();
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            if (_asyncAction != null)
                _ = _asyncAction();
            else
                _action();
        }
    }
}

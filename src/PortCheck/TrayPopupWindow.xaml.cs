using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
    private PortPane? _lastAnimatedPane;
    private Rect? _cachedBackdropRect;
    private ImageSource? _cachedBackdropImage;

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
            ApplyPaneVisibility(_viewModel.ActivePane);
            FluidAnimation.SetPaneTabWidths(LocalPaneTabButton, DockerPaneTabButton, _viewModel.ActivePane);
            _lastAnimatedPane = _viewModel.ActivePane;
            UpdateSearchPlaceholder();
            await Dispatcher.InvokeAsync(() => { });
        };

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TrayViewModel.ActivePane))
            {
                Dispatcher.Invoke(() =>
                {
                    AnimatePaneChange(_viewModel.ActivePane);
                    UpdateSearchPlaceholder();
                });
            }

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

    private void PaneTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
            FluidAnimation.PopIcon(element);
    }

    private void AnimatePaneChange(PortPane pane)
    {
        if (_lastAnimatedPane == null)
        {
            ApplyPaneVisibility(pane);
            FluidAnimation.SetPaneTabWidths(LocalPaneTabButton, DockerPaneTabButton, pane);
            _lastAnimatedPane = pane;
            return;
        }

        if (_lastAnimatedPane == pane)
            return;

        FluidAnimation.RunTabPush(LocalPaneTabButton, DockerPaneTabButton, pane);

        var outgoing = _lastAnimatedPane == PortPane.Local ? LocalPortsList : DockerPortsList;
        var incoming = pane == PortPane.Local ? LocalPortsList : DockerPortsList;
        outgoing.Visibility = Visibility.Visible;
        outgoing.Opacity = 1;
        incoming.Visibility = Visibility.Visible;
        incoming.Opacity = 0;
        FluidAnimation.RunPaneCrossfade(outgoing, incoming, () => ApplyPaneVisibility(pane));
        _lastAnimatedPane = pane;
    }

    private void ApplyPaneVisibility(PortPane pane)
    {
        var isLocal = pane == PortPane.Local;

        LocalPortsList.BeginAnimation(UIElement.OpacityProperty, null);
        DockerPortsList.BeginAnimation(UIElement.OpacityProperty, null);

        LocalPortsList.Visibility = isLocal ? Visibility.Visible : Visibility.Collapsed;
        DockerPortsList.Visibility = isLocal ? Visibility.Collapsed : Visibility.Visible;
        LocalPortsList.Opacity = isLocal ? 1 : 0;
        DockerPortsList.Opacity = isLocal ? 0 : 1;
        LocalPortsList.RenderTransform = null;
        DockerPortsList.RenderTransform = null;

        ListHostGrid.InvalidateMeasure();
    }

    private void UpdateSearchPlaceholder() =>
        SearchPlaceholder.Text = _viewModel.ActivePane == PortPane.Docker
            ? "Search Docker ports…"
            : "Search local ports…";

    private void SetupInputBindings()
    {
        InputBindings.Add(new KeyBinding(_viewModel.RefreshPortsCommand, Key.R, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommandWrapper(() => KillAllWithConfirm()), Key.K, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommandWrapper(HideToTray), Key.Escape, ModifierKeys.None));
    }

    public void ShowNearTray()
    {
        var work = SystemParameters.WorkArea;
        var maxWindowH = Math.Min(560, work.Height * 0.75);
        MaxHeight = maxWindowH;
        Height = maxWindowH;

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
        if (_cachedBackdropRect.HasValue &&
            _cachedBackdropImage != null &&
            AreRectsEquivalent(_cachedBackdropRect.Value, rect))
        {
            BackdropImage.Source = _cachedBackdropImage;
            return;
        }

        _cachedBackdropRect = rect;
        _cachedBackdropImage = BackdropBlurHelper.CaptureBlurredRegion(rect, blurRadius: 32, dimOpacity: 0.06);
        BackdropImage.Source = _cachedBackdropImage;
    }

    private void PortsList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ListBox list)
            return;

        var scrollViewer = FindVisualChild<ScrollViewer>(list);
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

    private void KillDockerPort_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button { Tag: DockerPortInfo row })
        {
            e.Handled = true;
            row.IsConfirmingKill = true;
        }
    }

    private async void ConfirmDockerKill_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DockerPortInfo row })
        {
            row.IsConfirmingKill = false;
            await _viewModel.KillContainerCommand.ExecuteAsync(row);
        }
    }

    private void CancelDockerKill_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DockerPortInfo row })
            row.IsConfirmingKill = false;
    }

    private async void KillAll_Click(object sender, RoutedEventArgs e) => await KillAllWithConfirm();

    private async Task KillAllWithConfirm()
    {
        if (_viewModel.ActivePane != PortPane.Local || !_viewModel.LocalPorts.Any(p => p.IsActive))
            return;

        _isProcessingAction = true;
        try
        {
            var count = _viewModel.LocalPorts.Count(p => p.IsActive);
            var dialog = new ConfirmDialog(
                $"Kill ALL {count} active processes?",
                "This will terminate all processes currently using ports in the Local Port list.",
                "Kill All")
            {
                Owner = this
            };

            dialog.ShowDialog();
            if (dialog.Result)
                await _viewModel.KillAllLocalCommand.ExecuteAsync(null);
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

    private static bool AreRectsEquivalent(Rect left, Rect right)
    {
        const double tolerance = 1;
        return Math.Abs(left.X - right.X) < tolerance &&
               Math.Abs(left.Y - right.Y) < tolerance &&
               Math.Abs(left.Width - right.Width) < tolerance &&
               Math.Abs(left.Height - right.Height) < tolerance;
    }
}

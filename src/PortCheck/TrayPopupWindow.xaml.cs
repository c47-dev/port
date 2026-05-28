using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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
    private Popup? _sortMenuPopup;
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
            ApplyRoundedClips();
            ApplyCaptureSurfaceOverride();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
            FocusPrimaryControl();
        };

        SizeChanged += (_, _) => ApplyRoundedClips();

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

            if (e.PropertyName == nameof(TrayViewModel.PopupSurface))
            {
                Dispatcher.Invoke(FocusPrimaryControl);
            }

            if (e.PropertyName == nameof(TrayViewModel.IsScanning))
            {
                Dispatcher.Invoke(() =>
                {
                    if (!_isManualRefresh)
                        return;

                    if (_viewModel.IsScanning)
                        RefreshActionRow.LabelText = "Refreshing...";
                    else
                    {
                        RefreshActionRow.LabelText = "Refreshed";
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

        ListHostGrid?.InvalidateMeasure();
    }

    private void UpdateSearchPlaceholder() =>
        SearchPlaceholder.Text = _viewModel.ActivePane == PortPane.Docker
            ? "Search Docker ports"
            : "Search local ports";

    private void ApplyRoundedClips()
    {
        ApplyRoundedClip(OuterChromeBorder, 20);
        ApplyRoundedClip(InnerChromeBorder, 18);
        ApplyRoundedClip(OuterGlassRoot, 20);
        ApplyRoundedClip(InnerContentRoot, 18);
    }

    private static void ApplyRoundedClip(FrameworkElement element, double radius)
    {
        if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
            return;

        element.Clip = new RectangleGeometry(
            new Rect(0, 0, element.ActualWidth, element.ActualHeight),
            radius,
            radius);
    }

    private void SetupInputBindings()
    {
        InputBindings.Add(new KeyBinding(_viewModel.RefreshPortsCommand, Key.R, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommandWrapper(() => KillAllWithConfirm()), Key.K, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommandWrapper(HandleEscapeAsync), Key.Escape, ModifierKeys.None));
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
        FocusPrimaryControl();
    }

    public void ShowForCapture()
    {
        Width = 340;
        Height = 520;
        MaxHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Show();
        Activate();
        UpdateLayout();
        UpdateBackdropBlur();
        FocusPrimaryControl();
    }

    private void FocusPrimaryControl()
    {
        if (_viewModel.PopupSurface == PopupSurface.Settings)
            ExcludedPortTextBox.Focus();
        else
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
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
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
        if (_isProcessingAction || IsCaptureSession)
            return;

        HideToTray();
    }

    private static bool IsCaptureSession =>
        Environment.GetCommandLineArgs().Any(arg =>
            arg.StartsWith("--capture-to=", StringComparison.OrdinalIgnoreCase));

    private void ApplyCaptureSurfaceOverride()
    {
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            const string prefix = "--capture-surface=";
            if (!arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var surface = arg[prefix.Length..];
            if (surface.Equals("settings", StringComparison.OrdinalIgnoreCase))
                _viewModel.OpenSettings();
        }
    }

    public async Task CaptureForValidationAsync()
    {
        ApplyCaptureSurfaceOverride();
        await CaptureScreenshotAndShutdownAsync();
    }

    private async Task CaptureScreenshotAndShutdownAsync()
    {
        await Task.Delay(600);
        UpdateLayout();

        var width = (int)Math.Ceiling(ActualWidth);
        var height = (int)Math.Ceiling(ActualHeight);
        if (width <= 0 || height <= 0)
            return;

        var capturePath = ResolveCaptureOutputPath();
        Directory.CreateDirectory(Path.GetDirectoryName(capturePath)!);

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(this);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        await using (var stream = File.Create(capturePath))
            encoder.Save(stream);

        Application.Current.Shutdown();
    }

    private static string ResolveCaptureOutputPath()
    {
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            const string prefix = "--capture-to=";
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return Path.GetFullPath(arg[prefix.Length..]);
        }

        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "artifacts",
            "popup-capture.png"));
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
        RefreshActionRow.LabelText = "Refreshing...";
        await _viewModel.RefreshPortsCommand.ExecuteAsync(null);
    }

    private void HideToTray_Click(object sender, RoutedEventArgs e) => HideToTray();

    private void SortFilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_sortMenuPopup is { IsOpen: true })
        {
            _sortMenuPopup.IsOpen = false;
            return;
        }

        var panel = new StackPanel();
        panel.Children.Add(CreatePopupMenuButton("Port", _viewModel.SortField == PortListSortField.Port,
            () => _viewModel.SortField = PortListSortField.Port, new CornerRadius(8, 8, 4, 4)));
        panel.Children.Add(CreatePopupMenuButton("Process name", _viewModel.SortField == PortListSortField.ProcessName,
            () => _viewModel.SortField = PortListSortField.ProcessName, new CornerRadius(4, 4, 4, 4)));
        panel.Children.Add(CreatePopupMenuButton("PID", _viewModel.SortField == PortListSortField.Pid,
            () => _viewModel.SortField = PortListSortField.Pid, new CornerRadius(4, 4, 4, 4)));
        panel.Children.Add(CreatePopupMenuDivider());
        panel.Children.Add(CreatePopupMenuButton("Ascending", !_viewModel.SortDescending,
            () => _viewModel.SortDescending = false, new CornerRadius(4, 4, 4, 4)));
        panel.Children.Add(CreatePopupMenuButton("Descending", _viewModel.SortDescending,
            () => _viewModel.SortDescending = true, new CornerRadius(4, 4, 8, 8)));

        var shell = new Border { Child = panel };
        shell.SetResourceReference(StyleProperty, "GlassPopupMenuShell");

        _sortMenuPopup = new Popup
        {
            Child = shell,
            PlacementTarget = SortFilterButton,
            Placement = PlacementMode.Bottom,
            VerticalOffset = 6,
            HorizontalOffset = -138,
            AllowsTransparency = true,
            StaysOpen = false,
            PopupAnimation = PopupAnimation.Fade
        };

        _sortMenuPopup.Opened += (_, _) => _isProcessingAction = true;
        _sortMenuPopup.Closed += (_, _) =>
        {
            _isProcessingAction = false;
            _sortMenuPopup = null;
        };

        _sortMenuPopup.IsOpen = true;
    }

    private async Task HandleEscapeAsync()
    {
        if (_viewModel.PopupSurface == PopupSurface.Settings)
            _viewModel.CloseSettings();
        else
            HideToTray();

        await Task.CompletedTask;
    }

    private async void ExcludedPortInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        await _viewModel.AddExcludedPortCommand.ExecuteAsync(null);
    }

    private async void RefreshIntervalInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        await _viewModel.CommitRefreshIntervalCommand.ExecuteAsync(null);
    }

    private async void RefreshIntervalInput_LostFocus(object sender, RoutedEventArgs e)
    {
        await _viewModel.CommitRefreshIntervalCommand.ExecuteAsync(null);
    }

    private Button CreatePopupMenuButton(string label, bool isSelected, Action onSelect, CornerRadius cornerRadius)
    {
        var button = new Button
        {
            Content = CreatePopupMenuItemContent(label, isSelected),
            BorderThickness = new Thickness(0),
            Tag = isSelected ? "Selected" : null
        };
        button.SetResourceReference(StyleProperty, "GlassPopupMenuButton");
        button.Loaded += (_, _) => ApplyPopupMenuItemCornerRadius(button, cornerRadius);
        button.Click += (_, _) =>
        {
            onSelect();
            _sortMenuPopup?.SetCurrentValue(Popup.IsOpenProperty, false);
        };
        return button;
    }

    private static void ApplyPopupMenuItemCornerRadius(Button button, CornerRadius cornerRadius)
    {
        button.ApplyTemplate();
        if (button.Template.FindName("Bd", button) is Border border)
            border.CornerRadius = cornerRadius;
    }

    private static Grid CreatePopupMenuItemContent(string label, bool isSelected)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var text = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center
        };
        text.SetResourceReference(TextBlock.ForegroundProperty, "Text.Primary");
        text.SetResourceReference(TextBlock.EffectProperty, "Text.Shadow");
        grid.Children.Add(text);

        if (isSelected)
        {
            var check = new TextBlock
            {
                Text = "\uE73E",
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 12,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            check.SetResourceReference(TextBlock.ForegroundProperty, "Text.Primary");
            check.SetResourceReference(TextBlock.EffectProperty, "Text.Shadow");
            Grid.SetColumn(check, 1);
            grid.Children.Add(check);
        }

        return grid;
    }

    private Border CreatePopupMenuDivider()
    {
        var divider = new Border();
        divider.SetResourceReference(StyleProperty, "GlassPopupMenuDivider");
        return divider;
    }

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

using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using PortCheck.Helpers;
using PortCheck.ViewModels;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;

namespace PortCheck;

public sealed class TrayHost : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly TrayViewModel _viewModel;
    private TaskbarIcon? _icon;
    private TrayPopupWindow? _popup;
    private Popup? _trayMenuPopup;
    private bool _isShuttingDown;

    public TrayHost(IServiceProvider services, TrayViewModel viewModel)
    {
        _services = services;
        _viewModel = viewModel;
    }

    public void Initialize()
    {
        _icon = new TaskbarIcon
        {
            ToolTipText = "PortCheck",
            Icon = LoadAppIcon(),
            Visibility = Visibility.Visible,
            ContextMenu = null
        };

        _trayMenuPopup = CreateTrayMenuPopup();
        _icon.TrayRightMouseUp += (_, _) => ShowTrayMenu();
        _icon.TrayLeftMouseDown += (_, _) => TogglePopup();

        if (!AdminHelper.IsRunningAsAdministrator())
        {
            _icon.ToolTipText = "PortCheck (not elevated — kill may fail)";
            _icon.ShowBalloonTip(
                "PortCheck",
                "Running without administrator rights. Port list works; killing processes may fail. Use Release build or run terminal as Administrator.",
                BalloonIcon.Warning);
        }
    }

    private Popup CreateTrayMenuPopup()
    {
        var panel = new StackPanel { MinWidth = 168 };

        panel.Children.Add(CreateTrayMenuButton("Refresh",
            () => _viewModel.RefreshPortsCommand.ExecuteAsync(null)));

        panel.Children.Add(CreateTrayMenuButton("Kill All", ShowKillAllFromTrayAsync,
            foreground: FindBrush("Danger")));

        panel.Children.Add(new Border
        {
            Height = 1,
            Margin = new Thickness(10, 4, 10, 4),
            Background = FindBrush("Glass.Stroke"),
            Opacity = 0.45
        });

        panel.Children.Add(CreateTrayMenuButton("Quit", () =>
        {
            Shutdown();
            return Task.CompletedTask;
        }));

        var shell = new Border
        {
            Background = new SolidColorBrush(WpfColor.FromArgb(245, 32, 32, 32)),
            BorderBrush = FindBrush("Glass.Stroke"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(4),
            Child = panel,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 0.35,
                BlurRadius = 16,
                ShadowDepth = 4
            }
        };

        return new Popup
        {
            Child = shell,
            StaysOpen = false,
            AllowsTransparency = true,
            Placement = PlacementMode.MousePoint,
            PopupAnimation = PopupAnimation.Fade
        };
    }

    private Button CreateTrayMenuButton(string label, Func<Task> onClick, WpfBrush? foreground = null)
    {
        var style = Application.Current.TryFindResource("GlassTrayMenuButton") as Style;
        var button = new Button
        {
            Content = label,
            BorderThickness = new Thickness(0)
        };

        if (style != null)
            button.Style = style;

        if (foreground != null)
            button.Foreground = foreground;

        button.Click += async (_, _) =>
        {
            _trayMenuPopup!.IsOpen = false;
            await onClick();
        };

        return button;
    }

    private static WpfBrush FindBrush(string key) =>
        Application.Current.FindResource(key) as WpfBrush ?? WpfBrushes.White;

    private void ShowTrayMenu()
    {
        if (_trayMenuPopup == null)
            return;

        _trayMenuPopup.IsOpen = true;
    }

    public void TogglePopup()
    {
        if (_popup is { IsVisible: true })
        {
            HidePopup();
            return;
        }

        ShowPopup();
    }

    public void ShowPopup()
    {
        _popup ??= _services.GetRequiredService<TrayPopupWindow>();
        _popup.ShowNearTray();
    }

    public void HidePopup() => _popup?.Hide();

    private async Task ShowKillAllFromTrayAsync()
    {
        if (!_viewModel.Ports.Any())
            return;

        ShowPopup();

        var dialog = new ConfirmDialog(
            $"Kill ALL {_viewModel.Ports.Count} active processes?",
            "This will terminate all processes currently using ports.",
            "Kill All")
        {
            Owner = _popup
        };

        dialog.ShowDialog();
        if (dialog.Result)
            await _viewModel.KillAllCommand.ExecuteAsync(null);
    }

    public void PrepareShutdown()
    {
        if (_isShuttingDown)
            return;

        _isShuttingDown = true;
        if (_trayMenuPopup != null)
            _trayMenuPopup.IsOpen = false;

        _popup?.Close();
        _popup = null;
        _icon?.Dispose();
        _icon = null;
    }

    private void Shutdown()
    {
        PrepareShutdown();
        _viewModel.StopAutoRefresh();
        Application.Current.Shutdown();
    }

    private static Icon LoadAppIcon()
    {
        var icoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(icoPath))
            return new Icon(icoPath);

        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
        {
            var extracted = Icon.ExtractAssociatedIcon(exePath);
            if (extracted != null)
                return extracted;
        }

        throw new FileNotFoundException("App icon not found. Expected Assets\\AppIcon.ico beside the executable.");
    }

    public void Dispose()
    {
        PrepareShutdown();
    }
}

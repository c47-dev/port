using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using PortCheck.Models;
using PortCheck.Services;

namespace PortCheck.ViewModels;

public partial class TrayViewModel : ObservableObject
{
    private readonly PortScannerService _scanner;
    private readonly ProcessKillerService _killer;
    private readonly DockerEngineClient _dockerEngine;
    private readonly DockerPortCatalogService _dockerCatalog;
    private readonly DockerContainerStopService _dockerStop;
    private readonly Dispatcher _dispatcher;
    private readonly bool _dockerCatalogEnabled;
    private readonly int _dockerProbeTimeoutMs;

    private CancellationTokenSource? _refreshCancellation;
    private CancellationTokenSource? _scanCts;
    private int _scanInFlight;

    [ObservableProperty]
    private ObservableCollection<PortInfo> _localPorts = new();

    [ObservableProperty]
    private ObservableCollection<PortInfo> _filteredLocalPorts = new();

    [ObservableProperty]
    private ObservableCollection<DockerPortInfo> _dockerPorts = new();

    [ObservableProperty]
    private ObservableCollection<DockerPortInfo> _filteredDockerPorts = new();

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private PortPane _activePane = PortPane.Local;

    [ObservableProperty]
    private bool _isDockerSurfaceVisible;

    public int RefreshIntervalSeconds { get; }

    public int LocalPortCount => LocalPorts.Count;
    public int DockerPortCount => DockerPorts.Count;
    public int ActivePanePortCount => ActivePane == PortPane.Docker ? DockerPortCount : LocalPortCount;

    public TrayViewModel(
        PortScannerService scanner,
        ProcessKillerService killer,
        DockerEngineClient dockerEngine,
        DockerPortCatalogService dockerCatalog,
        DockerContainerStopService dockerStop,
        IConfiguration configuration,
        Dispatcher dispatcher)
    {
        _scanner = scanner;
        _killer = killer;
        _dockerEngine = dockerEngine;
        _dockerCatalog = dockerCatalog;
        _dockerStop = dockerStop;
        _dispatcher = dispatcher;

        RefreshIntervalSeconds = configuration.GetValue("appSettings:refreshIntervalSeconds", 5);
        if (RefreshIntervalSeconds < 1)
            RefreshIntervalSeconds = 5;

        _dockerCatalogEnabled = configuration.GetValue("appSettings:dockerCatalogEnabled", true);
        _dockerProbeTimeoutMs = configuration.GetValue("appSettings:dockerEngineProbeTimeoutMs", 400);
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    partial void OnActivePaneChanged(PortPane value)
    {
        OnPropertyChanged(nameof(ActivePanePortCount));
        ApplyFilter();
        if (value == PortPane.Docker && IsDockerSurfaceVisible)
            _ = RefreshPortsAsync();
    }

    public async Task InitializeAsync()
    {
        await RefreshPortsAsync();
        StartAutoRefresh();
    }

    public void StopAutoRefresh() => _refreshCancellation?.Cancel();

    [RelayCommand]
    public async Task RefreshPortsAsync()
    {
        if (Interlocked.CompareExchange(ref _scanInFlight, 1, 0) != 0)
            return;

        IsScanning = true;
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        try
        {
            var localPorts = await _scanner.ScanPortsAsync();
            if (ct.IsCancellationRequested)
                return;

            var listen = HostListenSnapshot.FromPorts(localPorts);
            IReadOnlyList<DockerPortInfo> dockerRows = Array.Empty<DockerPortInfo>();
            var dockerVisible = false;

            if (_dockerCatalogEnabled &&
                await _dockerEngine.TryProbeAsync(_dockerProbeTimeoutMs, ct))
            {
                dockerRows = await _dockerCatalog.FetchPublishedTcpAsync(listen, ct);
                dockerVisible = dockerRows.Count > 0;
            }

            if (ct.IsCancellationRequested)
                return;

            var publishedHostPorts = dockerVisible
                ? dockerRows.Select(r => r.HostPort).ToHashSet()
                : new HashSet<int>();

            foreach (var port in localPorts)
                port.IsDockerPublished = publishedHostPorts.Contains(port.Port);

            var switchToLocal = ActivePane == PortPane.Docker && !dockerVisible;

            _dispatcher.Invoke(() =>
            {
                ReplaceCollection(LocalPorts, localPorts);
                ReplaceCollection(DockerPorts, dockerVisible ? dockerRows : Array.Empty<DockerPortInfo>());
                IsDockerSurfaceVisible = dockerVisible;
                if (switchToLocal)
                    ActivePane = PortPane.Local;
                ApplyFilter();
                OnPropertyChanged(nameof(LocalPortCount));
                OnPropertyChanged(nameof(DockerPortCount));
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error refreshing ports: {ex.Message}");
        }
        finally
        {
            IsScanning = false;
            Interlocked.Exchange(ref _scanInFlight, 0);
        }
    }

    [RelayCommand]
    public void SelectPane(PortPane pane)
    {
        if (pane == PortPane.Docker && !IsDockerSurfaceVisible)
            return;
        ActivePane = pane;
    }

    [RelayCommand]
    public async Task KillProcessAsync(PortInfo? port)
    {
        if (port is not { IsActive: true })
            return;

        try
        {
            port.IsKilling = true;
            var success = await _killer.KillProcessGracefullyAsync(port.Pid);
            await Task.Delay(500);
            await RefreshPortsAsync();
            if (!success)
            {
                if (LocalPorts.Contains(port))
                    port.IsKilling = false;

                if (!Helpers.AdminHelper.IsRunningAsAdministrator())
                {
                    System.Windows.MessageBox.Show(
                        "Could not kill this process. Run PortCheck as Administrator (Release build or elevated terminal).",
                        "PortCheck",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error killing process: {ex.Message}");
            port.IsKilling = false;
        }
    }

    [RelayCommand]
    public async Task KillContainerAsync(DockerPortInfo? row)
    {
        if (row == null || string.IsNullOrEmpty(row.ContainerId))
            return;

        try
        {
            row.IsKilling = true;
            var success = await _dockerStop.StopContainerAsync(row.ContainerId);
            await Task.Delay(500);
            await RefreshPortsAsync();
            if (!success && DockerPorts.Contains(row))
                row.IsKilling = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error stopping container: {ex.Message}");
            row.IsKilling = false;
        }
    }

    [RelayCommand]
    public async Task KillAllLocalAsync()
    {
        foreach (var port in LocalPorts.Where(p => p.IsActive).ToList())
            await KillProcessAsync(port);
    }

    private void StartAutoRefresh()
    {
        _refreshCancellation?.Cancel();
        _refreshCancellation = new CancellationTokenSource();
        var token = _refreshCancellation.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(RefreshIntervalSeconds), token);
                    if (!token.IsCancellationRequested)
                        await RefreshPortsAsync();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private void ApplyFilter()
    {
        var q = SearchQuery.Trim();

        if (ActivePane == PortPane.Docker)
        {
            var dockerSource = DockerPorts.AsEnumerable();
            if (!string.IsNullOrEmpty(q))
            {
                dockerSource = dockerSource.Where(p =>
                    p.HostPort.ToString().Contains(q, StringComparison.Ordinal) ||
                    p.ContainerPort.ToString().Contains(q, StringComparison.Ordinal) ||
                    (p.ContainerName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.ComposeProject?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.ComposeService?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    p.ContainerIdShort.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            ReplaceCollection(FilteredDockerPorts, dockerSource.OrderBy(p => p.HostPort).ToList());
            OnPropertyChanged(nameof(ActivePanePortCount));
            return;
        }

        IEnumerable<PortInfo> localSource = LocalPorts;
        if (!string.IsNullOrEmpty(q))
        {
            localSource = localSource.Where(p =>
                p.Port.ToString().Contains(q, StringComparison.Ordinal) ||
                (p.ProcessName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                p.Pid.ToString().Contains(q, StringComparison.Ordinal));
        }

        ReplaceCollection(FilteredLocalPorts, localSource.OrderBy(p => p.Port).ToList());
        OnPropertyChanged(nameof(LocalPortCount));
        OnPropertyChanged(nameof(ActivePanePortCount));
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
            target.Add(item);
    }
}

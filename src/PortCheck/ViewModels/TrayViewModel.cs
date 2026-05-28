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
    private readonly DockerPortCatalogService _dockerCatalog;
    private readonly DockerContainerStopService _dockerStop;
    private readonly SettingsService _settingsService;
    private readonly PortExclusionService _portExclusionService;
    private readonly Dispatcher _dispatcher;
    private readonly bool _dockerCatalogEnabled;

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
    private ObservableCollection<int> _userExcludedPorts = new();

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private PortPane _activePane = PortPane.Local;

    [ObservableProperty]
    private bool _isDockerSurfaceVisible;

    [ObservableProperty]
    private PortListSortField _sortField = PortListSortField.Port;

    [ObservableProperty]
    private bool _sortDescending;

    [ObservableProperty]
    private PopupSurface _popupSurface = PopupSurface.Ports;

    [ObservableProperty]
    private string _excludedPortInput = string.Empty;

    [ObservableProperty]
    private string _refreshIntervalInput = string.Empty;

    [ObservableProperty]
    private string _excludedPortValidationMessage = string.Empty;

    [ObservableProperty]
    private string _refreshIntervalValidationMessage = string.Empty;

    [ObservableProperty]
    private string _settingsStatusMessage = string.Empty;

    [ObservableProperty]
    private int _refreshIntervalSeconds;

    public TrayViewModel(
        PortScannerService scanner,
        ProcessKillerService killer,
        DockerPortCatalogService dockerCatalog,
        DockerContainerStopService dockerStop,
        SettingsService settingsService,
        PortExclusionService portExclusionService,
        IConfiguration configuration,
        Dispatcher dispatcher)
    {
        _scanner = scanner;
        _killer = killer;
        _dockerCatalog = dockerCatalog;
        _dockerStop = dockerStop;
        _settingsService = settingsService;
        _portExclusionService = portExclusionService;
        _dispatcher = dispatcher;
        _dockerCatalogEnabled = configuration.GetValue("appSettings:dockerCatalogEnabled", true);

        var settings = _settingsService.Load();
        _portExclusionService.SetUserExcludedPorts(settings.UserExcludedPorts);
        RefreshIntervalSeconds = Math.Clamp(settings.RefreshIntervalSeconds ?? configuration.GetValue("appSettings:refreshIntervalSeconds", 5), 3, 20);
        RefreshIntervalInput = RefreshIntervalSeconds.ToString();
        UserExcludedPorts = new ObservableCollection<int>(_portExclusionService.UserExcludedPorts);
    }

    public int LocalPortCount => LocalPorts.Count;
    public int DockerPortCount => DockerPorts.Count;
    public int ActivePanePortCount => ActivePane == PortPane.Docker ? DockerPortCount : LocalPortCount;
    public bool IsSettingsSurface => PopupSurface == PopupSurface.Settings;
    public bool IsPortsSurface => PopupSurface == PopupSurface.Ports;
    public bool HasExcludedPortValidationMessage => !string.IsNullOrWhiteSpace(ExcludedPortValidationMessage);
    public bool HasRefreshIntervalValidationMessage => !string.IsNullOrWhiteSpace(RefreshIntervalValidationMessage);
    public bool HasSettingsStatusMessage => !string.IsNullOrWhiteSpace(SettingsStatusMessage);
    public bool HasUserExcludedPorts => UserExcludedPorts.Count > 0;
    public bool CanAddExcludedPort => TryParseExcludedPortInput(ExcludedPortInput, out _, out _);
    public string HideShortcutText => PopupSurface == PopupSurface.Settings ? string.Empty : "Esc";
    public string SettingsShortcutText => PopupSurface == PopupSurface.Settings ? "Esc" : string.Empty;

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    partial void OnSortFieldChanged(PortListSortField value) => ApplyFilter();

    partial void OnSortDescendingChanged(bool value) => ApplyFilter();

    partial void OnActivePaneChanged(PortPane value)
    {
        OnPropertyChanged(nameof(ActivePanePortCount));
        ApplyFilter();
        if (value == PortPane.Docker && IsDockerSurfaceVisible)
            _ = RefreshPortsAsync();
    }

    partial void OnPopupSurfaceChanged(PopupSurface value)
    {
        OnPropertyChanged(nameof(IsSettingsSurface));
        OnPropertyChanged(nameof(IsPortsSurface));
        OnPropertyChanged(nameof(HideShortcutText));
        OnPropertyChanged(nameof(SettingsShortcutText));
        SettingsStatusMessage = string.Empty;
        if (value == PopupSurface.Ports)
            ExcludedPortValidationMessage = string.Empty;
    }

    partial void OnExcludedPortInputChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            ExcludedPortValidationMessage = string.Empty;
        else if (TryParseExcludedPortInput(value, out _, out var message))
            ExcludedPortValidationMessage = string.Empty;
        else
            ExcludedPortValidationMessage = message;

        AddExcludedPortCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanAddExcludedPort));
        OnPropertyChanged(nameof(HasExcludedPortValidationMessage));
    }

    partial void OnRefreshIntervalInputChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            RefreshIntervalValidationMessage = "Refresh interval must be between 3 and 20 seconds.";
        else if (TryParseRefreshInterval(value, out _, out var message))
            RefreshIntervalValidationMessage = string.Empty;
        else
            RefreshIntervalValidationMessage = message;

        OnPropertyChanged(nameof(HasRefreshIntervalValidationMessage));
    }

    partial void OnExcludedPortValidationMessageChanged(string value) => OnPropertyChanged(nameof(HasExcludedPortValidationMessage));

    partial void OnRefreshIntervalValidationMessageChanged(string value) => OnPropertyChanged(nameof(HasRefreshIntervalValidationMessage));

    partial void OnSettingsStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasSettingsStatusMessage));

    partial void OnUserExcludedPortsChanged(ObservableCollection<int> value)
    {
        OnPropertyChanged(nameof(HasUserExcludedPorts));
    }

    public async Task InitializeAsync()
    {
        await RefreshPortsAsync();
        StartAutoRefresh();
    }

    public void StopAutoRefresh() => _refreshCancellation?.Cancel();

    [RelayCommand]
    public void OpenSettings()
    {
        PopupSurface = PopupSurface.Settings;
    }

    [RelayCommand]
    public void CloseSettings()
    {
        PopupSurface = PopupSurface.Ports;
    }

    [RelayCommand(CanExecute = nameof(CanAddExcludedPort))]
    public async Task AddExcludedPortAsync()
    {
        if (!TryParseExcludedPortInput(ExcludedPortInput, out var port, out var message))
        {
            ExcludedPortValidationMessage = message;
            return;
        }

        _portExclusionService.SetUserExcludedPorts(UserExcludedPorts.Append(port));
        UserExcludedPorts = new ObservableCollection<int>(_portExclusionService.UserExcludedPorts);
        ExcludedPortInput = string.Empty;
        SettingsStatusMessage = string.Empty;

        await PersistSettingsAndRefreshAsync();
    }

    [RelayCommand]
    public async Task RemoveExcludedPortAsync(int port)
    {
        _portExclusionService.SetUserExcludedPorts(UserExcludedPorts.Where(existing => existing != port));
        UserExcludedPorts = new ObservableCollection<int>(_portExclusionService.UserExcludedPorts);
        SettingsStatusMessage = string.Empty;

        await PersistSettingsAndRefreshAsync();
    }

    [RelayCommand]
    public async Task CommitRefreshIntervalAsync()
    {
        if (!TryParseRefreshInterval(RefreshIntervalInput, out var interval, out var message))
        {
            RefreshIntervalValidationMessage = message;
            return;
        }

        RefreshIntervalSeconds = interval;
        RefreshIntervalInput = interval.ToString();
        RefreshIntervalValidationMessage = string.Empty;
        SettingsStatusMessage = string.Empty;

        try
        {
            PersistSettings();
            RestartAutoRefresh();
        }
        catch (Exception ex)
        {
            SettingsStatusMessage = $"Could not save settings: {ex.Message}";
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task RefreshPortsAsync()
    {
        if (Interlocked.CompareExchange(ref _scanInFlight, 1, 0) != 0)
            return;

        await _dispatcher.InvokeAsync(() => IsScanning = true);
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        try
        {
            var localPorts = await _scanner.ScanPortsAsync();
            if (ct.IsCancellationRequested)
                return;

            var listen = HostListenSnapshot.FromPorts(localPorts);
            IReadOnlyList<DockerPortInfo> catalogRows = Array.Empty<DockerPortInfo>();

            if (_dockerCatalogEnabled)
                catalogRows = await _dockerCatalog.FetchPublishedTcpAsync(listen, ct);

            var inferredRows = BuildInferredDockerRows(localPorts);
            var dockerRows = MergeDockerRows(catalogRows, inferredRows)
                .Where(row => !_portExclusionService.IsExcluded(row.HostPort))
                .ToList();
            var dockerVisible = dockerRows.Count > 0;

            var visibleLocalPorts = localPorts
                .Where(port => !_portExclusionService.IsExcluded(port.Port))
                .ToList();

            if (ct.IsCancellationRequested)
                return;

            var dockerHostPorts = dockerVisible
                ? dockerRows.Select(r => r.HostPort).ToHashSet()
                : new HashSet<int>();

            foreach (var port in visibleLocalPorts)
                port.IsDockerPublished = dockerHostPorts.Contains(port.Port);

            var switchToLocalPane = ActivePane == PortPane.Docker && !dockerVisible;

            await _dispatcher.InvokeAsync(() =>
            {
                ReconcileCollection(
                    LocalPorts,
                    visibleLocalPorts,
                    port => (port.Pid, port.Port, port.Address),
                    PreserveLocalRowState);
                ReconcileCollection(
                    DockerPorts,
                    dockerRows,
                    row => (row.ContainerId, row.HostPort, row.ContainerPort, row.HostAddress, row.Protocol),
                    PreserveDockerRowState);
                IsDockerSurfaceVisible = dockerVisible;
                if (switchToLocalPane)
                    ActivePane = PortPane.Local;
                ApplyFilter();
                OnPropertyChanged(nameof(LocalPortCount));
                OnPropertyChanged(nameof(DockerPortCount));
                OnPropertyChanged(nameof(HasUserExcludedPorts));
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error refreshing ports: {ex.Message}");
        }
        finally
        {
            await _dispatcher.InvokeAsync(() => IsScanning = false);
            Interlocked.Exchange(ref _scanInFlight, 0);
        }
    }

    [RelayCommand]
    public void SelectPane(PortPane pane)
    {
        if (pane == PortPane.Docker && !IsDockerSurfaceVisible)
            return;

        PopupSurface = PopupSurface.Ports;
        ActivePane = pane;
    }

    [RelayCommand]
    public async Task KillProcessAsync(PortInfo? port)
    {
        if (port is not { IsActive: true } || _portExclusionService.IsExcluded(port.Port))
            return;

        try
        {
            port.IsKilling = true;
            var success = await _killer.KillProcessGracefullyAsync(port.Pid);
            await Task.Delay(200);
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
        if (row == null || !row.IsKillSupported || _portExclusionService.IsExcluded(row.HostPort))
            return;

        try
        {
            row.IsKilling = true;
            var success = await _dockerStop.StopContainerAsync(row.ContainerId);
            await Task.Delay(200);
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
        var activePorts = LocalPorts
            .Where(p => p.IsActive && !_portExclusionService.IsExcluded(p.Port))
            .ToList();
        if (activePorts.Count == 0)
            return;

        foreach (var port in activePorts)
            port.IsKilling = true;

        foreach (var port in activePorts)
        {
            var success = await _killer.KillProcessGracefullyAsync(port.Pid);
            if (!success)
                port.IsKilling = false;
        }

        await Task.Delay(200);
        await RefreshPortsAsync();
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

    private void RestartAutoRefresh()
    {
        StopAutoRefresh();
        StartAutoRefresh();
    }

    private void PersistSettings()
    {
        _settingsService.Save(new UserSettings
        {
            RefreshIntervalSeconds = RefreshIntervalSeconds,
            UserExcludedPorts = UserExcludedPorts.ToArray()
        });
    }

    private async Task PersistSettingsAndRefreshAsync()
    {
        try
        {
            PersistSettings();
            await RefreshPortsAsync();
        }
        catch (Exception ex)
        {
            SettingsStatusMessage = $"Could not save settings: {ex.Message}";
        }
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
                    (p.HostAddress?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.ContainerName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.ComposeProject?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.ComposeService?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    p.ContainerIdShort.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            ReconcileCollection(
                FilteredDockerPorts,
                SortDockerRows(dockerSource).ToList(),
                row => (row.ContainerId, row.HostPort, row.ContainerPort, row.HostAddress, row.Protocol));
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

        ReconcileCollection(
            FilteredLocalPorts,
            SortLocalRows(localSource).ToList(),
            port => (port.Pid, port.Port, port.Address));
        OnPropertyChanged(nameof(LocalPortCount));
        OnPropertyChanged(nameof(ActivePanePortCount));
    }

    private static List<DockerPortInfo> BuildInferredDockerRows(IReadOnlyList<PortInfo> localPorts)
    {
        return localPorts
            .Where(port => PortScannerService.IsDockerRelatedProcess(port.ProcessName))
            .Select(port => new DockerPortInfo
            {
                ContainerId = $"inferred:{port.Pid}:{port.Port}:{port.Address}",
                ContainerName = port.ProcessName,
                HostPort = port.Port,
                ContainerPort = port.Port,
                Protocol = "tcp",
                HostAddress = port.Address,
                IsHostListening = true,
                IsInferred = true,
                SourcePid = port.Pid
            })
            .OrderBy(row => row.HostPort)
            .ThenBy(row => row.HostAddress, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<DockerPortInfo> MergeDockerRows(
        IReadOnlyList<DockerPortInfo> catalogRows,
        IReadOnlyList<DockerPortInfo> inferredRows)
    {
        if (catalogRows.Count == 0)
            return inferredRows;
        if (inferredRows.Count == 0)
            return catalogRows;

        var merged = new Dictionary<(int HostPort, string HostAddress, string Protocol), DockerPortInfo>();
        foreach (var row in inferredRows)
            merged[(row.HostPort, row.HostAddress, row.Protocol)] = row;
        foreach (var row in catalogRows)
            merged[(row.HostPort, row.HostAddress, row.Protocol)] = row;

        return merged.Values
            .OrderBy(row => row.HostPort)
            .ThenBy(row => row.HostAddress, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IEnumerable<PortInfo> SortLocalRows(IEnumerable<PortInfo> source) =>
        SortField switch
        {
            PortListSortField.ProcessName => SortDescending
                ? source.OrderByDescending(p => p.ProcessName, StringComparer.OrdinalIgnoreCase).ThenByDescending(p => p.Port)
                : source.OrderBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Port),
            PortListSortField.Pid => SortDescending
                ? source.OrderByDescending(p => p.Pid).ThenByDescending(p => p.Port)
                : source.OrderBy(p => p.Pid).ThenBy(p => p.Port),
            _ => SortDescending
                ? source.OrderByDescending(p => p.Port).ThenByDescending(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
                : source.OrderBy(p => p.Port).ThenBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
        };

    private IEnumerable<DockerPortInfo> SortDockerRows(IEnumerable<DockerPortInfo> source) =>
        SortField switch
        {
            PortListSortField.ProcessName => SortDescending
                ? source.OrderByDescending(p => p.ContainerName, StringComparer.OrdinalIgnoreCase).ThenByDescending(p => p.HostPort)
                : source.OrderBy(p => p.ContainerName, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.HostPort),
            PortListSortField.Pid => SortDescending
                ? source.OrderByDescending(p => p.SourcePid ?? 0).ThenByDescending(p => p.HostPort)
                : source.OrderBy(p => p.SourcePid ?? 0).ThenBy(p => p.HostPort),
            _ => SortDescending
                ? source.OrderByDescending(p => p.HostPort).ThenBy(p => p.HostAddress, StringComparer.OrdinalIgnoreCase)
                : source.OrderBy(p => p.HostPort).ThenBy(p => p.HostAddress, StringComparer.OrdinalIgnoreCase)
        };

    private bool TryParseExcludedPortInput(string value, out int port, out string message)
    {
        if (!int.TryParse(value.Trim(), out port))
        {
            message = "Port must be a whole number between 1 and 65535.";
            return false;
        }

        if (port is < 1 or > 65535)
        {
            message = "Port must be between 1 and 65535.";
            return false;
        }

        if (_portExclusionService.ProtectedPorts.Contains(port))
        {
            message = "This port is already protected by Windows defaults.";
            return false;
        }

        if (UserExcludedPorts.Contains(port))
        {
            message = "This port is already excluded.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool TryParseRefreshInterval(string value, out int interval, out string message)
    {
        if (!int.TryParse(value.Trim(), out interval))
        {
            message = "Refresh interval must be a whole number between 3 and 20 seconds.";
            return false;
        }

        if (interval is < 3 or > 20)
        {
            message = "Refresh interval must be between 3 and 20 seconds.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static void ReconcileCollection<TItem, TKey>(
        ObservableCollection<TItem> target,
        IReadOnlyList<TItem> items,
        Func<TItem, TKey> keySelector,
        Action<TItem, TItem>? preserveState = null)
        where TKey : notnull
    {
        var existingByKey = target.ToDictionary(keySelector);
        var reconciled = new List<TItem>(items.Count);

        foreach (var item in items)
        {
            if (existingByKey.TryGetValue(keySelector(item), out var existing))
                preserveState?.Invoke(existing, item);

            reconciled.Add(item);
        }

        var commonCount = Math.Min(target.Count, reconciled.Count);
        for (var i = 0; i < commonCount; i++)
            target[i] = reconciled[i];

        while (target.Count > reconciled.Count)
            target.RemoveAt(target.Count - 1);

        for (var i = commonCount; i < reconciled.Count; i++)
            target.Add(reconciled[i]);
    }

    private static void PreserveLocalRowState(PortInfo existing, PortInfo next)
    {
        next.IsKilling = existing.IsKilling;
        next.IsConfirmingKill = existing.IsConfirmingKill;
    }

    private static void PreserveDockerRowState(DockerPortInfo existing, DockerPortInfo next)
    {
        next.IsKilling = existing.IsKilling;
        next.IsConfirmingKill = existing.IsConfirmingKill;
    }
}

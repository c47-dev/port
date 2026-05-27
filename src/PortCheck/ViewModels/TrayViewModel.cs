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
    private readonly Dispatcher _dispatcher;
    private CancellationTokenSource? _refreshCancellation;

    [ObservableProperty]
    private ObservableCollection<PortInfo> _ports = new();

    [ObservableProperty]
    private ObservableCollection<PortInfo> _filteredPorts = new();

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public int RefreshIntervalSeconds { get; }

    public TrayViewModel(
        PortScannerService scanner,
        ProcessKillerService killer,
        IConfiguration configuration,
        Dispatcher dispatcher)
    {
        _scanner = scanner;
        _killer = killer;
        _dispatcher = dispatcher;
        RefreshIntervalSeconds = configuration.GetValue("appSettings:refreshIntervalSeconds", 5);
        if (RefreshIntervalSeconds < 1)
            RefreshIntervalSeconds = 5;
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    public async Task InitializeAsync()
    {
        await RefreshPortsAsync();
        StartAutoRefresh();
    }

    public void StopAutoRefresh() => _refreshCancellation?.Cancel();

    [RelayCommand]
    public async Task RefreshPortsAsync()
    {
        if (IsScanning)
            return;

        IsScanning = true;
        try
        {
            var scannedPorts = await _scanner.ScanPortsAsync();
            _dispatcher.Invoke(() =>
            {
                Ports.Clear();
                foreach (var port in scannedPorts)
                    Ports.Add(port);
                ApplyFilter();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error refreshing ports: {ex.Message}");
        }
        finally
        {
            IsScanning = false;
        }
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
                if (Ports.Contains(port))
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
    public async Task KillAllAsync()
    {
        foreach (var port in Ports.ToList())
            await KillProcessAsync(port);
    }

    private void ApplyFilter()
    {
        var q = SearchQuery.Trim().ToLowerInvariant();
        IEnumerable<PortInfo> source = Ports;

        if (!string.IsNullOrEmpty(q))
        {
            source = source.Where(p =>
                p.Port.ToString().Contains(q, StringComparison.Ordinal) ||
                (p.ProcessName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                p.Pid.ToString().Contains(q, StringComparison.Ordinal));
        }

        var ordered = source.OrderBy(p => p.Port).ToList();
        FilteredPorts.Clear();
        foreach (var port in ordered)
            FilteredPorts.Add(port);

        OnPropertyChanged(nameof(TotalPortCount));
        OnPropertyChanged(nameof(FilteredPorts));
    }

    public int TotalPortCount => Ports.Count;
}

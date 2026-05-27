using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PortCheck.Models;

public class DockerPortInfo : INotifyPropertyChanged
{
    private bool _isConfirmingKill;
    private bool _isKilling;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; init; } = Guid.NewGuid();
    public string ContainerId { get; init; } = string.Empty;
    public string ContainerName { get; init; } = string.Empty;
    public string? ComposeProject { get; init; }
    public string? ComposeService { get; init; }
    public int HostPort { get; init; }
    public int ContainerPort { get; init; }
    public string Protocol { get; init; } = "tcp";
    public string HostAddress { get; init; } = string.Empty;
    public bool IsHostListening { get; init; }
    public bool IsInferred { get; init; }
    public int? SourcePid { get; init; }

    public bool IsKillSupported => !IsInferred && !string.IsNullOrEmpty(ContainerId);

    public string ContainerIdShort =>
        ContainerId.Length > 12 ? ContainerId[..12] : ContainerId;

    public string DisplayHostPort => $":{HostPort}";

    public string DisplayMapping => $"{HostPort} -> {ContainerPort}/{Protocol}";

    public string DisplayPortDetail
    {
        get
        {
            if (IsInferred)
            {
                var source = SourcePid is int pid ? $"{ContainerName} PID {pid}" : ContainerName;
                return $"{DisplayMapping} | {HostAddress} | inferred Docker listener | {source}";
            }

            var compose = !string.IsNullOrEmpty(ComposeService)
                ? string.IsNullOrEmpty(ComposeProject)
                    ? ComposeService
                    : $"{ComposeProject}/{ComposeService}"
                : null;
            var tail = compose != null ? $"{ContainerName} | {compose}" : ContainerName;
            return $"{DisplayMapping} | {HostAddress} | {tail}";
        }
    }

    public bool IsConfirmingKill
    {
        get => _isConfirmingKill;
        set
        {
            if (_isConfirmingKill == value)
                return;
            _isConfirmingKill = value;
            OnPropertyChanged();
        }
    }

    public bool IsKilling
    {
        get => _isKilling;
        set
        {
            if (_isKilling == value)
                return;
            _isKilling = value;
            OnPropertyChanged();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

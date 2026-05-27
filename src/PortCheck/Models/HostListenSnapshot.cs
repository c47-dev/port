namespace PortCheck.Models;

public readonly struct HostListenSnapshot
{
    private readonly HashSet<int> _ports;

    public HostListenSnapshot(IEnumerable<int> listeningPorts)
    {
        _ports = listeningPorts.ToHashSet();
    }

    public bool IsTcpListening(int port) => _ports.Contains(port);

    public static HostListenSnapshot FromPorts(IEnumerable<PortInfo> ports) =>
        new(ports.Where(p => p.IsActive).Select(p => p.Port));
}

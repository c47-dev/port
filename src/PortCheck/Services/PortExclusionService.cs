namespace PortCheck.Services;

public sealed class PortExclusionService
{
    private readonly IReadOnlySet<int> _protectedPorts;
    private HashSet<int> _userExcludedPorts = [];

    public PortExclusionService(ProtectedPortCatalogService protectedPortCatalog)
    {
        _protectedPorts = protectedPortCatalog.ProtectedPorts;
    }

    public IReadOnlySet<int> ProtectedPorts => _protectedPorts;

    public IReadOnlyList<int> UserExcludedPorts => _userExcludedPorts.OrderBy(port => port).ToArray();

    public void SetUserExcludedPorts(IEnumerable<int> ports)
    {
        _userExcludedPorts = ports
            .Where(port => port is >= 1 and <= 65535)
            .Where(port => !_protectedPorts.Contains(port))
            .Distinct()
            .ToHashSet();
    }

    public bool IsExcluded(int port) => _protectedPorts.Contains(port) || _userExcludedPorts.Contains(port);
}

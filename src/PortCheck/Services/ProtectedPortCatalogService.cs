using System.IO;
using System.Text.Json;

namespace PortCheck.Services;

public sealed class ProtectedPortCatalogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IReadOnlySet<int> _protectedPorts;

    public ProtectedPortCatalogService()
    {
        _protectedPorts = LoadProtectedPorts();
    }

    public IReadOnlySet<int> ProtectedPorts => _protectedPorts;

    private static IReadOnlySet<int> LoadProtectedPorts()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Config", "protected-ports.json");
        if (!File.Exists(path))
            throw new InvalidOperationException($"Missing protected port catalog: {path}");

        using var stream = File.OpenRead(path);
        var document = JsonSerializer.Deserialize<ProtectedPortsDocument>(stream, JsonOptions);
        if (document?.Ports == null || document.Ports.Count == 0)
            throw new InvalidOperationException("Protected port catalog must contain at least one port.");

        var ports = new HashSet<int>();
        foreach (var port in document.Ports)
        {
            if (port is < 1 or > 65535)
                throw new InvalidOperationException($"Protected port catalog contains invalid port: {port}");

            ports.Add(port);
        }

        return ports;
    }

    private sealed class ProtectedPortsDocument
    {
        public List<int> Ports { get; init; } = [];
    }
}

using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using PortCheck.Models;

namespace PortCheck.Services;

[SupportedOSPlatform("windows")]
public sealed class DockerPortCatalogService
{
    private readonly DockerEngineClient _client;
    private readonly DockerWslCliClient _wslCli;
    private readonly int _timeoutMs;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DockerPortCatalogService(DockerEngineClient client, DockerWslCliClient wslCli, int timeoutMs)
    {
        _client = client;
        _wslCli = wslCli;
        _timeoutMs = timeoutMs;
    }

    public async Task<IReadOnlyList<DockerPortInfo>> FetchPublishedTcpAsync(
        HostListenSnapshot listen,
        CancellationToken cancellationToken)
    {
        string? json;
        try
        {
            json = await _client.GetAsync("/containers/json?all=false", _timeoutMs, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return await _wslCli.FetchPublishedTcpAsync(listen, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(json))
            return await _wslCli.FetchPublishedTcpAsync(listen, cancellationToken);

        DockerContainerDto[]? containers;
        try
        {
            containers = JsonSerializer.Deserialize<DockerContainerDto[]>(json, JsonOptions);
        }
        catch
        {
            return await _wslCli.FetchPublishedTcpAsync(listen, cancellationToken);
        }

        if (containers == null || containers.Length == 0)
            return await _wslCli.FetchPublishedTcpAsync(listen, cancellationToken);

        var rows = new List<DockerPortInfo>();
        foreach (var container in containers)
        {
            if (string.IsNullOrEmpty(container.Id))
                continue;

            var name = container.Names?.FirstOrDefault()?.TrimStart('/') ?? container.Id[..Math.Min(12, container.Id.Length)];
            string? project = null;
            string? service = null;
            if (container.Labels != null)
            {
                container.Labels.TryGetValue("com.docker.compose.project", out project);
                container.Labels.TryGetValue("com.docker.compose.service", out service);
            }

            if (container.Ports == null)
                continue;

            foreach (var port in container.Ports)
            {
                if (port.PublicPort is not > 0)
                    continue;
                var type = port.Type ?? "tcp";
                if (!string.Equals(type, "tcp", StringComparison.OrdinalIgnoreCase))
                    continue;

                rows.Add(CreateRow(container.Id, name, project, service, port.PublicPort.Value,
                    port.PrivatePort, type, string.IsNullOrEmpty(port.Ip) ? "0.0.0.0" : port.Ip, listen));
            }
        }

        if (rows.Count == 0)
            return await _wslCli.FetchPublishedTcpAsync(listen, cancellationToken);

        return rows
            .OrderBy(r => r.HostPort)
            .ThenBy(r => r.ContainerName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static DockerPortInfo CreateRow(
        string containerId,
        string name,
        string? project,
        string? service,
        int hostPort,
        int containerPort,
        string protocol,
        string hostAddress,
        HostListenSnapshot listen) => new()
    {
        ContainerId = containerId,
        ContainerName = name,
        ComposeProject = project,
        ComposeService = service,
        HostPort = hostPort,
        ContainerPort = containerPort,
        Protocol = protocol,
        HostAddress = hostAddress,
        IsHostListening = listen.IsTcpListening(hostPort)
    };

    private sealed class DockerContainerDto
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("Names")]
        public string[]? Names { get; set; }

        [JsonPropertyName("Labels")]
        public Dictionary<string, string>? Labels { get; set; }

        [JsonPropertyName("Ports")]
        public DockerPortBindingDto[]? Ports { get; set; }
    }

    private sealed class DockerPortBindingDto
    {
        [JsonPropertyName("IP")]
        public string? Ip { get; set; }

        [JsonPropertyName("PrivatePort")]
        public int PrivatePort { get; set; }

        [JsonPropertyName("PublicPort")]
        public int? PublicPort { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }
    }
}

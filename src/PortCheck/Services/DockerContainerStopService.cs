using System.Runtime.Versioning;

namespace PortCheck.Services;

[SupportedOSPlatform("windows")]
public sealed class DockerContainerStopService
{
    private readonly DockerEngineClient _client;
    private readonly int _timeoutMs;

    public DockerContainerStopService(DockerEngineClient client, int timeoutMs)
    {
        _client = client;
        _timeoutMs = timeoutMs;
    }

    public Task<bool> StopContainerAsync(string containerId, CancellationToken cancellationToken = default) =>
        _client.PostAsync($"/containers/{containerId}/stop?t=10", _timeoutMs, cancellationToken);
}

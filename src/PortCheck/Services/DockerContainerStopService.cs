using System.Runtime.Versioning;

namespace PortCheck.Services;

[SupportedOSPlatform("windows")]
public sealed class DockerContainerStopService
{
    private readonly DockerEngineClient _client;
    private readonly DockerWslCliClient _wslCli;
    private readonly int _timeoutMs;

    public DockerContainerStopService(DockerEngineClient client, DockerWslCliClient wslCli, int timeoutMs)
    {
        _client = client;
        _wslCli = wslCli;
        _timeoutMs = timeoutMs;
    }

    public async Task<bool> StopContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        var stopped = await _client.PostAsync($"/containers/{containerId}/stop?t=10", _timeoutMs, cancellationToken);
        if (stopped)
            return true;

        return await _wslCli.StopContainerAsync(containerId, cancellationToken);
    }
}

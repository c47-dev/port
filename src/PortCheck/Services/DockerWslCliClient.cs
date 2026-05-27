using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using PortCheck.Models;

namespace PortCheck.Services;

[SupportedOSPlatform("windows")]
public sealed class DockerWslCliClient
{
    private static readonly Regex PublishedPortPattern = new(
        @"(?<host>\[[^\]]+\]|[^:\s,]+):(?<hostPort>\d+)->(?<containerPort>\d+)\/(?<protocol>tcp)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string? _configuredDistribution;
    private readonly int _timeoutMs;
    private string? _resolvedDistribution;

    public DockerWslCliClient(string? configuredDistribution, int timeoutMs)
    {
        _configuredDistribution = string.IsNullOrWhiteSpace(configuredDistribution)
            ? null
            : configuredDistribution.Trim();
        _timeoutMs = timeoutMs;
    }

    public async Task<IReadOnlyList<DockerPortInfo>> FetchPublishedTcpAsync(
        HostListenSnapshot listen,
        CancellationToken cancellationToken)
    {
        var distribution = await ResolveDistributionAsync(cancellationToken);
        if (distribution == null)
            return Array.Empty<DockerPortInfo>();

        var result = await RunWslAsync(
            distribution,
            "docker ps --format '{{.ID}}\\t{{.Names}}\\t{{.Ports}}'",
            cancellationToken);
        if (!result.Success)
            return Array.Empty<DockerPortInfo>();

        var rows = new List<DockerPortInfo>();
        foreach (var line in SplitLines(result.StdOut))
        {
            var parts = line.Split('\t');
            if (parts.Length < 3)
                continue;

            var containerId = parts[0].Trim();
            var containerName = parts[1].Trim();
            var ports = parts[2].Trim();
            if (string.IsNullOrEmpty(containerId) || string.IsNullOrEmpty(ports))
                continue;

            foreach (Match match in PublishedPortPattern.Matches(ports))
            {
                if (!int.TryParse(match.Groups["hostPort"].Value, out var hostPort) ||
                    !int.TryParse(match.Groups["containerPort"].Value, out var containerPort))
                {
                    continue;
                }

                var protocol = match.Groups["protocol"].Value;
                var hostAddress = NormalizeHost(match.Groups["host"].Value);
                rows.Add(new DockerPortInfo
                {
                    ContainerId = containerId,
                    ContainerName = containerName,
                    HostPort = hostPort,
                    ContainerPort = containerPort,
                    Protocol = protocol,
                    HostAddress = hostAddress,
                    IsHostListening = listen.IsTcpListening(hostPort)
                });
            }
        }

        return rows
            .GroupBy(row => (row.ContainerId, row.HostPort, row.ContainerPort, row.HostAddress, row.Protocol))
            .Select(group => group.First())
            .OrderBy(row => row.HostPort)
            .ThenBy(row => row.ContainerName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<bool> StopContainerAsync(string containerId, CancellationToken cancellationToken)
    {
        var distribution = await ResolveDistributionAsync(cancellationToken);
        if (distribution == null)
            return false;

        var escapedContainerId = containerId.Replace("'", "'\"'\"'");
        var result = await RunWslAsync(
            distribution,
            $"docker stop --time 10 '{escapedContainerId}'",
            cancellationToken);
        return result.Success;
    }

    private async Task<string?> ResolveDistributionAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_resolvedDistribution))
            return _resolvedDistribution;

        if (!string.IsNullOrEmpty(_configuredDistribution))
        {
            var probe = await RunWslAsync(_configuredDistribution, "docker ps --format '{{.ID}}'", cancellationToken);
            if (probe.Success)
            {
                _resolvedDistribution = _configuredDistribution;
                return _resolvedDistribution;
            }

            return null;
        }

        var list = await RunProcessAsync(
            "wsl.exe",
            "-l -q",
            cancellationToken);
        if (!list.Success)
            return null;

        foreach (var distribution in SplitLines(list.StdOut))
        {
            var name = distribution.Replace("\0", string.Empty).Trim();
            if (string.IsNullOrEmpty(name) ||
                name.Contains("docker-desktop", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var probe = await RunWslAsync(name, "docker ps --format '{{.ID}}'", cancellationToken);
            if (!probe.Success)
                continue;

            _resolvedDistribution = name;
            return _resolvedDistribution;
        }

        return null;
    }

    private Task<ProcessResult> RunWslAsync(
        string distribution,
        string shellCommand,
        CancellationToken cancellationToken)
    {
        var escaped = shellCommand.Replace("\"", "\\\"");
        return RunProcessAsync(
            "wsl.exe",
            $"-d {QuoteArgument(distribution)} sh -lc \"{escaped}\"",
            cancellationToken);
    }

    private async Task<ProcessResult> RunProcessAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeoutMs);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return ProcessResult.Timeout;
        }

        var stdOut = await stdOutTask;
        var stdErr = await stdErrTask;
        return new ProcessResult(process.ExitCode == 0, stdOut, stdErr);
    }

    private static string NormalizeHost(string value) =>
        value switch
        {
            "::" => "::",
            "[::]" => "::",
            _ => value
        };

    private static string QuoteArgument(string value) =>
        $"\"{value.Replace("\"", "\\\"")}\"";

    private static IEnumerable<string> SplitLines(string value) =>
        value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
        }
    }

    private readonly record struct ProcessResult(bool Success, string StdOut, string StdErr)
    {
        public static ProcessResult Timeout => new(false, string.Empty, "timeout");
    }
}

using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Runtime.Versioning;

namespace PortCheck.Services;

[SupportedOSPlatform("windows")]
public sealed class DockerEngineClient
{
    private readonly string _pipeName;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public DockerEngineClient(string pipeName) => _pipeName = pipeName;

    public async Task<bool> TryProbeAsync(int timeoutMs, CancellationToken cancellationToken)
    {
        try
        {
            await using var pipe = new NamedPipeClientStream(
                ".",
                _pipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);
            await pipe.ConnectAsync(cts.Token);
            return pipe.IsConnected;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetAsync(string path, int timeoutMs, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var (ok, body) = await SendCoreAsync("GET", path, null, timeoutMs, cancellationToken);
            return ok ? body : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> PostAsync(string path, int timeoutMs, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var (ok, _) = await SendCoreAsync("POST", path, null, timeoutMs, cancellationToken);
            return ok;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<(bool Ok, string? Body)> SendCoreAsync(
        string method,
        string path,
        string? requestBody,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        await using var pipe = new NamedPipeClientStream(
            ".",
            _pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);
        await pipe.ConnectAsync(cts.Token);

        var requestBuilder = new StringBuilder();
        requestBuilder.Append(method).Append(' ').Append(path).Append(" HTTP/1.1\r\n");
        requestBuilder.Append("Host: localhost\r\n");
        requestBuilder.Append("Connection: close\r\n");
        if (requestBody != null)
        {
            var bytes = Encoding.UTF8.GetBytes(requestBody);
            requestBuilder.Append("Content-Type: application/json\r\n");
            requestBuilder.Append("Content-Length: ").Append(bytes.Length).Append("\r\n");
        }

        requestBuilder.Append("\r\n");
        if (requestBody != null)
            requestBuilder.Append(requestBody);

        var requestBytes = Encoding.UTF8.GetBytes(requestBuilder.ToString());
        await pipe.WriteAsync(requestBytes, cts.Token);
        await pipe.FlushAsync(cts.Token);

        using var ms = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            int read;
            try
            {
                read = await pipe.ReadAsync(buffer, cts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return (false, null);
            }

            if (read == 0)
                break;
            if (ms.Length + read > 4 * 1024 * 1024)
                return (false, null);
            ms.Write(buffer, 0, read);
        }

        var raw = Encoding.UTF8.GetString(ms.ToArray());
        var responseBody = ExtractBody(raw, out var statusCode);
        var ok = statusCode is >= 200 and < 300;
        return (ok, responseBody);
    }

    private static string? ExtractBody(string raw, out int statusCode)
    {
        statusCode = 0;
        var headerEnd = raw.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (headerEnd < 0)
            return null;

        var statusLine = raw.AsSpan(0, raw.IndexOf('\r'));
        if (statusLine.Length < 12 || !statusLine.StartsWith("HTTP/"))
            return null;

        var codeSlice = statusLine[9..];
        var space = codeSlice.IndexOf(' ');
        if (space > 0)
            int.TryParse(codeSlice[..space], out statusCode);

        var headers = raw[..headerEnd];
        var body = raw[(headerEnd + 4)..];

        if (headers.Contains("Transfer-Encoding: chunked", StringComparison.OrdinalIgnoreCase))
            return DecodeChunked(body);

        var contentLengthMarker = "Content-Length:";
        var idx = headers.IndexOf(contentLengthMarker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var lineEnd = headers.IndexOf('\r', idx);
            var lenStr = headers[(idx + contentLengthMarker.Length)..lineEnd].Trim();
            if (int.TryParse(lenStr, out var len) && body.Length >= len)
                return body[..len];
        }

        return body;
    }

    private static string DecodeChunked(string body)
    {
        var sb = new StringBuilder();
        var offset = 0;
        while (offset < body.Length)
        {
            var lineEnd = body.IndexOf('\r', offset);
            if (lineEnd < 0)
                break;
            if (!int.TryParse(body.AsSpan(offset, lineEnd - offset), System.Globalization.NumberStyles.HexNumber, null, out var chunkSize))
                break;
            offset = lineEnd + 2;
            if (chunkSize == 0)
                break;
            sb.Append(body, offset, chunkSize);
            offset += chunkSize + 2;
        }

        return sb.ToString();
    }
}

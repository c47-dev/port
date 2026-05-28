using System.IO;
using System.Text.Json;
using PortCheck.Models;

namespace PortCheck.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly int _defaultRefreshIntervalSeconds;

    public SettingsService(int defaultRefreshIntervalSeconds)
    {
        _defaultRefreshIntervalSeconds = ClampRefreshInterval(defaultRefreshIntervalSeconds);
    }

    public string SettingsFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PortCheck",
        "settings.json");

    public UserSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
        {
            return new UserSettings
            {
                RefreshIntervalSeconds = _defaultRefreshIntervalSeconds,
                UserExcludedPorts = Array.Empty<int>()
            };
        }

        try
        {
            using var stream = File.OpenRead(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<UserSettingsDocument>(stream, JsonOptions);

            return new UserSettings
            {
                RefreshIntervalSeconds = ClampRefreshInterval(settings?.RefreshIntervalSeconds ?? _defaultRefreshIntervalSeconds),
                UserExcludedPorts = NormalizePorts(settings?.UserExcludedPorts)
            };
        }
        catch
        {
            return new UserSettings
            {
                RefreshIntervalSeconds = _defaultRefreshIntervalSeconds,
                UserExcludedPorts = Array.Empty<int>()
            };
        }
    }

    public void Save(UserSettings settings)
    {
        var path = SettingsFilePath;
        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Settings directory is unavailable.");
        Directory.CreateDirectory(directory);

        var payload = new UserSettingsDocument
        {
            RefreshIntervalSeconds = ClampRefreshInterval(settings.RefreshIntervalSeconds ?? _defaultRefreshIntervalSeconds),
            UserExcludedPorts = NormalizePorts(settings.UserExcludedPorts).ToList()
        };

        var tempPath = $"{path}.tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(payload, JsonOptions));

        if (File.Exists(path))
            File.Replace(tempPath, path, null);
        else
            File.Move(tempPath, path);
    }

    private static int ClampRefreshInterval(int value) => Math.Clamp(value, 3, 20);

    private static IReadOnlyList<int> NormalizePorts(IEnumerable<int>? ports) =>
        (ports ?? Array.Empty<int>())
            .Where(port => port is >= 1 and <= 65535)
            .Distinct()
            .OrderBy(port => port)
            .ToArray();

    private sealed class UserSettingsDocument
    {
        public int? RefreshIntervalSeconds { get; init; }

        public List<int>? UserExcludedPorts { get; init; }
    }
}

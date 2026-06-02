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
    private readonly string? _settingsFilePath;

    public SettingsService(int defaultRefreshIntervalSeconds, string? settingsFilePath = null)
    {
        _defaultRefreshIntervalSeconds = ClampRefreshInterval(defaultRefreshIntervalSeconds);
        _settingsFilePath = settingsFilePath;
    }

    public string SettingsFilePath => _settingsFilePath ?? Path.Combine(
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
                UserExcludedPorts = Array.Empty<int>(),
                FavouritePorts = Array.Empty<int>()
            };
        }

        try
        {
            using var stream = File.OpenRead(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<UserSettingsDocument>(stream, JsonOptions);

            return new UserSettings
            {
                RefreshIntervalSeconds = ClampRefreshInterval(settings?.RefreshIntervalSeconds ?? _defaultRefreshIntervalSeconds),
                UserExcludedPorts = NormalizePorts(settings?.UserExcludedPorts),
                FavouritePorts = NormalizePorts(settings?.FavouritePorts)
            };
        }
        catch
        {
            return new UserSettings
            {
                RefreshIntervalSeconds = _defaultRefreshIntervalSeconds,
                UserExcludedPorts = Array.Empty<int>(),
                FavouritePorts = Array.Empty<int>()
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
            UserExcludedPorts = NormalizePorts(settings.UserExcludedPorts).ToList(),
            FavouritePorts = NormalizePorts(settings.FavouritePorts).ToList()
        };

        var tempPath = $"{path}.tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(payload, JsonOptions));
        File.Move(tempPath, path, overwrite: true);
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

        public List<int>? FavouritePorts { get; init; }
    }
}

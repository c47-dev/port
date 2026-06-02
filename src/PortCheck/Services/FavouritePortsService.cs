using PortCheck.Models;

namespace PortCheck.Services;

public sealed class FavouritePortsService
{
    public const int MaxFavouritePorts = 32;

    private readonly SettingsService _settingsService;
    private readonly PortExclusionService _portExclusionService;

    public FavouritePortsService(SettingsService settingsService, PortExclusionService portExclusionService)
    {
        _settingsService = settingsService;
        _portExclusionService = portExclusionService;
    }

    public IReadOnlyList<int> LoadFavouritePorts()
    {
        var settings = _settingsService.Load();
        var pruned = PruneExcludedPorts(settings.FavouritePorts);
        if (pruned.SequenceEqual(settings.FavouritePorts))
            return pruned;

        SaveFavouritePorts(pruned);
        return pruned;
    }

    public IReadOnlyList<int> ToggleFavourite(int port)
    {
        var existing = LoadFavouritePorts().ToList();

        if (_portExclusionService.IsExcluded(port))
            return existing;

        if (existing.Remove(port))
        {
            SaveFavouritePorts(existing);
            return existing;
        }

        if (existing.Count >= MaxFavouritePorts)
            return existing;

        existing.Add(port);
        SaveFavouritePorts(existing);
        return existing;
    }

    public IReadOnlyList<int> PruneExcludedPorts(IEnumerable<int> favouritePorts) =>
        NormalizePorts(favouritePorts)
            .Where(port => !_portExclusionService.IsExcluded(port))
            .Take(MaxFavouritePorts)
            .ToArray();

    public IReadOnlyList<PortInfo> BuildFavouriteDisplayRows(
        IEnumerable<int> favouritePorts,
        IEnumerable<PortInfo> localPorts)
    {
        var activeByPort = localPorts
            .Where(port => port.IsActive)
            .GroupBy(port => port.Port)
            .ToDictionary(group => group.Key, group => group.First());

        var rows = new List<PortInfo>();
        foreach (var port in PruneExcludedPorts(favouritePorts))
        {
            if (activeByPort.TryGetValue(port, out var active))
            {
                rows.Add(active);
                continue;
            }

            rows.Add(PortInfo.Inactive(port));
        }

        return rows;
    }

    public void SaveFavouritePorts(IEnumerable<int> favouritePorts)
    {
        var settings = _settingsService.Load();
        _settingsService.Save(new UserSettings
        {
            RefreshIntervalSeconds = settings.RefreshIntervalSeconds,
            UserExcludedPorts = settings.UserExcludedPorts,
            FavouritePorts = PruneExcludedPorts(favouritePorts)
        });
    }

    private static IEnumerable<int> NormalizePorts(IEnumerable<int> ports) =>
        ports
            .Where(port => port is >= 1 and <= 65535)
            .Distinct()
            .OrderBy(port => port);
}

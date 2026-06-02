using System.IO;
using PortCheck.Models;
using PortCheck.Services;

namespace PortCheck.Validation;

public static class FavouritePortsHarness
{
    public static int Run(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var reportPath = Path.Combine(outputDir, "favourite-ports-report.txt");

        try
        {
            var failures = new List<string>();
            failures.AddRange(ValidatePersistenceAndPrune(outputDir));
            failures.AddRange(ValidateMergeRows(outputDir));

            if (failures.Count == 0)
            {
                File.WriteAllText(reportPath, "PASS\n");
                return 0;
            }

            File.WriteAllText(reportPath, "FAIL\n" + string.Join("\n", failures));
            return 1;
        }
        catch (Exception ex)
        {
            File.WriteAllText(reportPath, "FAIL\n" + ex);
            return 1;
        }
    }

    private static IEnumerable<string> ValidatePersistenceAndPrune(string outputDir)
    {
        var settingsPath = Path.Combine(outputDir, "settings.json");
        var settingsService = new SettingsService(5, settingsPath);
        var exclusionService = new PortExclusionService(new HashSet<int> { 135, 445 });
        exclusionService.SetUserExcludedPorts([3000]);
        var favouriteService = new FavouritePortsService(settingsService, exclusionService);

        settingsService.Save(new UserSettings
        {
            RefreshIntervalSeconds = 7,
            UserExcludedPorts = exclusionService.UserExcludedPorts,
            FavouritePorts = [3000, 8080, 135, 8080, 65536, 5432]
        });

        var loaded = favouriteService.LoadFavouritePorts();
        var expected = new[] { 5432, 8080 };
        if (!loaded.SequenceEqual(expected))
            yield return $"LoadFavouritePorts prune mismatch. expected=[{string.Join(",", expected)}] actual=[{string.Join(",", loaded)}]";

        var reloadedSettings = settingsService.Load();
        if (!reloadedSettings.FavouritePorts.SequenceEqual(expected))
            yield return $"Pruned favourites were not persisted. actual=[{string.Join(",", reloadedSettings.FavouritePorts)}]";

        var afterToggleRemove = favouriteService.ToggleFavourite(8080);
        if (!afterToggleRemove.SequenceEqual([5432]))
            yield return $"Toggle remove mismatch. actual=[{string.Join(",", afterToggleRemove)}]";

        var afterToggleExcluded = favouriteService.ToggleFavourite(3000);
        if (!afterToggleExcluded.SequenceEqual([5432]))
            yield return $"Excluded port toggle should no-op. actual=[{string.Join(",", afterToggleExcluded)}]";
    }

    private static IEnumerable<string> ValidateMergeRows(string outputDir)
    {
        var settingsPath = Path.Combine(outputDir, "merge-settings.json");
        var settingsService = new SettingsService(5, settingsPath);
        var exclusionService = new PortExclusionService(new HashSet<int> { 135 });
        exclusionService.SetUserExcludedPorts([5000]);
        var favouriteService = new FavouritePortsService(settingsService, exclusionService);

        var localPorts = new[]
        {
            PortInfo.Active(3000, 111, "node", "127.0.0.1", "user", "npm run dev"),
            PortInfo.Active(5432, 222, "postgres", "127.0.0.1", "user", "postgres")
        };

        var rows = favouriteService.BuildFavouriteDisplayRows([5432, 3000, 5000, 7000, 135], localPorts);
        var rowList = rows.ToList();

        if (rowList.Count != 3)
            yield return $"Expected 3 favourite rows after prune, got {rowList.Count}";

        if (rowList.Count >= 1 && (!rowList[0].IsActive || rowList[0].Port != 3000 || rowList[0].Pid != 111))
            yield return $"First favourite row mismatch for active port 3000.";

        if (rowList.Count >= 2 && (!rowList[1].IsActive || rowList[1].Port != 5432 || rowList[1].Pid != 222))
            yield return $"Second favourite row mismatch for active port 5432.";

        if (rowList.Count >= 3)
        {
            var inactive = rowList[2];
            if (inactive.Port != 7000 || inactive.IsActive || inactive.ProcessName != "Not running")
                yield return $"Inactive favourite row mismatch for port 7000.";
        }
    }
}

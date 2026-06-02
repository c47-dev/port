namespace PortCheck.Models;

public sealed class UserSettings
{
    public int? RefreshIntervalSeconds { get; init; }

    public IReadOnlyList<int> UserExcludedPorts { get; init; } = Array.Empty<int>();

    public IReadOnlyList<int> FavouritePorts { get; init; } = Array.Empty<int>();
}

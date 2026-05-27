using System.Windows.Media;

namespace PortCheck.Helpers;

/// <summary>
/// Official Docker mark from docker-icon-seeklogo.svg (viewBox 0 0 756.3 596.9, fill #1d63ed).
/// </summary>
public static class DockerIconGeometry
{
    public static Geometry Logo { get; }

    public const string BrandColor = "#FF1D63ED";

    static DockerIconGeometry()
    {
        const string path = "M744,245.2c-18.5-12.5-67.3-17.8-102.7-8.3-1.9-35.3-20.1-65-53.4-90.9l-12.3-8.3-8.2,12.4c-16.1,24.5-22.9,57.1-20.5,86.8,1.9,18.3,8.3,38.8,20.5,53.7-46.1,26.7-88.6,20.7-276.8,20.7H0c-.9,42.5,6,124.2,58,190.8,5.7,7.4,12,14.5,18.9,21.3,42.3,42.3,106.1,73.3,201.6,73.4,145.7.1,270.5-78.6,346.4-269,25,.4,90.9,4.5,123.2-57.9.8-1,8.2-16.5,8.2-16.5l-12.3-8.3h0ZM189.7,206.4h-81.7v81.7h81.7v-81.7ZM295.2,206.4h-81.7v81.7h81.7v-81.7ZM400.8,206.4h-81.7v81.7h81.7v-81.7h0ZM506.3,206.4h-81.7v81.7h81.7v-81.7ZM84.1,206.4H2.4v81.7h81.7s0-81.7,0-81.7ZM189.7,103.2h-81.7v81.7h81.7v-81.7ZM295.2,103.2h-81.7v81.7h81.7v-81.7ZM400.8,103.2h-81.7v81.7h81.7v-81.7h0ZM400.8,0h-81.7v81.7h81.7V0h0Z";

        var geometry = Geometry.Parse(path);
        geometry.Freeze();
        Logo = geometry;
    }
}

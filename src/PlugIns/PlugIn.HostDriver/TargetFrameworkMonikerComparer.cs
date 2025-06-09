namespace PlugIn.HostDriver;

public class TargetFrameworkMonikerComparer : IComparer<string>
{
    public static readonly TargetFrameworkMonikerComparer Instance = new();

    private TargetFrameworkMonikerComparer()
    {
    }

    public int Compare(string? x, string? y)
    {
        string? xOs = null;
        string? xOsVersion = null;
        string? yOs = null;
        string? yOsVersion = null;
        int xMajor, yMajor, xMinor, yMinor;

        bool xIsNetFx = TargetFrameworkMonikerParser.TryParseNetFxMoniker(x ?? string.Empty, out xMajor, out xMinor);
        bool xIsDotNet = !xIsNetFx && TargetFrameworkMonikerParser.TryParseNetMoniker(x ?? string.Empty, out xMajor, out xMinor, out xOs, out xOsVersion);
        bool yIsNetFx = TargetFrameworkMonikerParser.TryParseNetFxMoniker(y ?? string.Empty, out yMajor, out yMinor);
        bool yIsDotNet = !yIsNetFx && TargetFrameworkMonikerParser.TryParseNetMoniker(y ?? string.Empty, out yMajor, out yMinor, out yOs, out yOsVersion);

        // Note: currently ignoring netcoreapp, netstandard, and other variations.

        if (!(xIsNetFx || xIsDotNet))
        {
            throw new ArgumentException($"Unrecognized tfm: {x}", nameof(x));
        }

        if (!(yIsNetFx || yIsDotNet))
        {
            throw new ArgumentException($"Unrecognized tfm: {y}", nameof(y));
        }

        if (xIsNetFx)
        {
            if (!yIsNetFx)
            {
                return -1; // x is .NET Framework, y is not. This comparer ranks .NET as higher than .NET Framework.
            }

        }
        else
        {
            if (yIsNetFx)
            {
                return 1; // x is not .NET Framework, y is. This comparer ranks .NET as higher than .NET Framework.
            }
        }

        // At this point, both x and y are either .NET Framework or .NET Core/Standard/5+/6+.
        int majorResult = xMajor.CompareTo(yMajor);
        int versionMatch = majorResult != 0
            ? majorResult
            : xMinor.CompareTo(yMinor);

        if (versionMatch != 0 || !xIsDotNet)
        {
            // If the major versions are different, we can return the result immediately.
            // Also, if this is not a .NET style moniker (e.g., net8.0), then we don't expect any OS specifiers,
            // so we are done.
            return versionMatch;
        }

        // The major and minor versions are the same and it's a .NET moniker, so we need to check for OS specifiers.
        // .NET style moniker, so it might have an OS specifier, e.g., net8.0-windows or net8.0-windows10.0.19041.
        if (xOs == null && yOs == null)
        {
            return 0; // Both are the same .NET version with no OS specifier.
        }

        // If just one has an OS specifier, we rank it higher.
        if (xOs == null)
        {
            return -1;
        }

        if (yOs == null)
        {
            return 1;
        }

        int osCompare = string.Compare(xOs, yOs, StringComparison.OrdinalIgnoreCase);
        if (osCompare != 0)
        {
            return osCompare; // If OS names are different, we use their lexicographical order.
        }

        if (xOsVersion == null && yOsVersion == null)
        {
            return 0; // Both have the same OS name with no version specifier.
        }

        // We have the same .NET version and OS name. If just one has an OS version, we rank it higher.
        if (xOsVersion == null)
        {
            return -1;
        }

        if (yOsVersion == null)
        {
            return 1;
        }

        // Same .NET version, OS name, and both have OS version, e.g.:
        //  net8.0-windows10.0.19041 vs net8.0-windows10.0.22621


        if (xOsVersion == yOsVersion)
        {
            return 0; // Both have the same OS version.
        }

        var xParts = xOsVersion.Split('.');
        var yParts = yOsVersion.Split('.');

        int maxParts = Math.Max(xParts.Length, yParts.Length);
        for (int i = 0; i < maxParts; i++)
        {
            int xPart = i < xParts.Length ? int.Parse(xParts[i]) : -1;
            int yPart = i < yParts.Length ? int.Parse(yParts[i]) : -1;
            int cmp = xPart.CompareTo(yPart);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        // Normally we wouldn't expect to reach here because we already compared the full OS versions.
        // But it could happen if comparing:
        //  net8.0-windows10.0
        //  net8.0-windows10.00

        return 0;
    }
}

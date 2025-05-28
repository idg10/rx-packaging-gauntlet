namespace PlugIn.HostDriver;

public class TargetFrameworkMonikerComparer : IComparer<string>
{
    public static readonly TargetFrameworkMonikerComparer Instance = new();

    private TargetFrameworkMonikerComparer()
    {
    }

    public int Compare(string? x, string? y)
    {
        bool xIsNetFx = TargetFrameworkMonikerParser.TryParseNetFxMoniker(x ?? string.Empty, out int xMajor, out int xMinor);
        bool xIsDotNet = !xIsNetFx && TargetFrameworkMonikerParser.TryParseNetMoniker(x ?? string.Empty, out int xMajorNet, out int xMinorNet);
        bool yIsNetFx = TargetFrameworkMonikerParser.TryParseNetFxMoniker(y ?? string.Empty, out int yMajor, out int yMinor);
        bool yIsDotNet = !yIsNetFx && TargetFrameworkMonikerParser.TryParseNetMoniker(y ?? string.Empty, out int yMajorNet, out int yMinorNet);

        // Note: currently ignoring netcoreapp, netstandard, and other variations.

        if (!(xIsNetFx || xIsDotNet))
        {
            throw new ArgumentException($"Unrecognized tfm: {x}", nameof(x));
        }

        if (!(yIsNetFx || yIsDotNet))
        {
            throw new ArgumentException($"Unrecognized tfm: {x}", nameof(y));
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
        return majorResult != 0
            ? majorResult
            : xMinor.CompareTo(yMinor);
    }
}

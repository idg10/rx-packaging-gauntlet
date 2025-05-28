namespace PlugIn.HostDriver;

internal class TargetFrameworkMonikerParser
{
    public static bool TryParseNetFxMoniker(
        string targetFrameworkMoniker, out int majorVersion, out int minorVersionAsTwoDigitNumber)
    {
        ReadOnlySpan<char> tfm = targetFrameworkMoniker;
        int length = targetFrameworkMoniker.Length;
        if (!tfm.StartsWith("net") ||
            (length < 5) || (length > 6) ||
            !char.IsDigit(tfm[3]) ||
            !int.TryParse(tfm[4..], out int minorVersion))
        {
            majorVersion = minorVersionAsTwoDigitNumber = 0;
            return false;
        }

        majorVersion = tfm[3] - '0';
        minorVersionAsTwoDigitNumber = length == 5
            ? minorVersion * 10
            : minorVersion;

        return true;
    }

    public static bool TryParseNetMoniker(
        string targetFrameworkMoniker, out int majorVersion, out int minorVersionAsTwoDigitNumber)
    {
        ReadOnlySpan<char> tfm = targetFrameworkMoniker;
        int length = targetFrameworkMoniker.Length;
        int indexOfDot = tfm.IndexOf('.');
        if (!tfm.StartsWith("net") ||
            indexOfDot < 0 ||
            (length < indexOfDot + 1) ||
            !int.TryParse(tfm[3..indexOfDot], out majorVersion) ||
            !int.TryParse(tfm[(indexOfDot + 1)..], out int minorVersion))
        {
            majorVersion = minorVersionAsTwoDigitNumber = 0;
            return false;
        }

        // Currently this is for consistency with TryParseNetFxMoniker, but if there is ever, say, a net12.11 then a
        // net12.2 or similar, this ensures that the second comes out looking higher than the first (20 > 11, instead
        // of 2 < 11).
        // In practice, everything recent has a zero minor version, so the fact that we reserve two digits makes no
        // difference.
        minorVersionAsTwoDigitNumber = minorVersion * 10;
        return true;
    }
}

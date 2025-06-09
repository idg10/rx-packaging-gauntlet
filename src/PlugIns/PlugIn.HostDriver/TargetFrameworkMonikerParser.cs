using System.Text.RegularExpressions;

namespace PlugIn.HostDriver;

public class TargetFrameworkMonikerParser
{
    private static readonly Regex DotnetTfmRegex = new(@"^net(?<major>\d+)\.(?<minor>\d+)(?<os>-(?<osname>[^\d]+)(?<osversion>[\d.]*))?$");

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
        string targetFrameworkMoniker, out int majorVersion, out int minorVersionAsTwoDigitNumber, out string? os, out string? osVersion)
    {
        os = osVersion = null;
        Match match = DotnetTfmRegex.Match(targetFrameworkMoniker);
        if (!match.Success ||
            !int.TryParse(match.Groups["major"].Value, out majorVersion) ||
            !int.TryParse(match.Groups["minor"].Value, out int minorVersion))
        {
            majorVersion = minorVersionAsTwoDigitNumber = 0;
            return false;
        }

        if (match.Groups["os"].Success)
        {
            os = match.Groups["osname"].Value;
            osVersion = match.Groups["osversion"].Value;
            if (osVersion.Length == 0)
            {
                osVersion = null; // If the OS version is empty, we treat it as not specified.
            }
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


// Dimensions:
//  RX version (published or proposed)
//  TFM
//  Whether project uses WPF and/or Windows Forms
//  Whether the transitive frameworks workaround is in use.
// We should also check an application that doesn't actually use Rx, as a baseline for whether WPF and/or
// Windows Forms are included in the output.

using RxGauntlet;

using System.Diagnostics;
using System.Xml;

string[] baseNetTfms =
[
    "net6.0",
    "net8.0",
    "net9.0"
];

string?[] windowsVersions =
[
    //null,
    "windows10.0.18362.0",
    "windows10.0.19041.0",
    "windows10.0.22000.0"
];

RxVersions[] rxVersions =
[
    RxVersions.Rx30,
    RxVersions.Rx31,
    RxVersions.Rx44,
    RxVersions.Rx50,
    RxVersions.Rx60,
];

bool?[] boolsWithNull = [null, true, false];
bool[] bools = [true, false];

IEnumerable<Scenario> scenarios =
    from rxVersion in rxVersions
    from baseNetTfm in baseNetTfms
    from windowsVersion in windowsVersions
    from useWpf in boolsWithNull
    from useWindowsForms in boolsWithNull
    from useTransitiveFrameworksWorkaround in bools
    select new Scenario(baseNetTfm, windowsVersion, useWpf, useWindowsForms, useTransitiveFrameworksWorkaround, rxVersion);

DirectoryInfo templateProjectFolder = new(
    Path.Combine(AppContext.BaseDirectory, "../../../../Bloat/Bloat.ConsoleWinRtTemplate/"));

foreach(Scenario scenario in scenarios)
{
    try
    {
        await RunScenario(scenario);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error running scenario {scenario}: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}

async Task RunScenario(Scenario scenario)
{
    Console.WriteLine(scenario);
    string tfm = scenario.WindowsVersion is string windowsVersion
        ? $"{scenario.BaseNetTfm}-{windowsVersion}"
        : scenario.BaseNetTfm;

    (string rxPackage, string rxVersion) = scenario.RxVersion switch
    {
        RxVersions.Rx30 => ("System.Reactive.Linq", "3.0.0"),
        RxVersions.Rx31 => ("System.Reactive.Linq", "3.1.0"),
        RxVersions.Rx44 => ("System.Reactive", "4.4.1"),
        RxVersions.Rx50 => ("System.Reactive", "5.0.0"),
        RxVersions.Rx60 => ("System.Reactive", "6.0.1"),
        _ => throw new ArgumentOutOfRangeException(nameof(scenario.RxVersion), scenario.RxVersion, null)
    };

    string copyPath = Path.Combine(
        Path.GetTempPath(),
        "RxGauntlet",
        "CheckIssue1745",
        DateTime.Now.ToString("yyyyMMdd-HHmmss"));

    Directory.CreateDirectory(copyPath);
    try
    {
        foreach (string file in Directory.GetFiles(templateProjectFolder.FullName))
        {
            string extension = Path.GetExtension(file).ToLowerInvariant();
            string relativePath = Path.GetRelativePath(templateProjectFolder.FullName, file);
            string destinationPath = Path.Combine(copyPath, relativePath);

            switch (extension)
            {
                case ".cs":
                    File.Copy(file, destinationPath, true);
                    break;

                case ".csproj":
                    //RewriteProjectFile(file, destinationPath, "net6.0-windows10.0.19041.0", "System.Reactive", "6.0.0");
                    //RewriteProjectFile(file, destinationPath, "net6.0-windows10.0.19041.0", "System.Reactive.Linq", "3.0.0");
                    RewriteProjectFile(
                        file,
                        destinationPath,
                        tfm,
                        rxPackage,
                        rxVersion,
                        scenario.UseWpf,
                        scenario.UseWindowsForms,
                        scenario.EmitDisableTransitiveFrameworkReferences);
                    break;
            }
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,

            // Comment this out to see the output in the console window
            CreateNoWindow = true,
            Arguments = $"publish -c Release Bloat.ConsoleWinRtTemplate.csproj",
            WorkingDirectory = copyPath,
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync();

        string binFolder = Path.Combine(copyPath, "bin");

        bool includesWpf = false;
        bool includesWindowsForms = false;
        foreach (string file in Directory.GetFiles(binFolder, "*", new EnumerationOptions { RecurseSubdirectories = true }))
        {
            string filename = Path.GetFileName(file);
            if (filename.Equals("PresentationFramework.dll", StringComparison.InvariantCultureIgnoreCase))
            {
                includesWpf = true;
            }

            if (filename.Equals("System.Windows.Forms.dll", StringComparison.InvariantCultureIgnoreCase))
            {
                includesWindowsForms = true;
            }
        }

        Console.WriteLine($"WPF: {includesWpf}");
        Console.WriteLine($"Windows Forms: {includesWindowsForms}");
        Console.WriteLine();
    }
    finally
    {
        Directory.Delete(copyPath, true);
    }
}

void RewriteProjectFile(
    string file,
    string destinationPath,
    string tfm,
    string rxPackageName,
    string rxPackageVersion,
    bool? useWpf,
    bool? useWindowsForms,
    bool emitDisableTransitiveFrameworkReferences)
{
    XmlDocument document = new();
    document.Load(file);
    

    XmlNode targetFrameworkNode = document.SelectSingleNode("/Project/PropertyGroup/TargetFramework")
        ?? throw new InvalidOperationException($"Did not find <TargetFramework> in {file}");
    targetFrameworkNode.InnerText = tfm;

    XmlNode rxPackageRefNode = document.SelectSingleNode("/Project/ItemGroup/PackageReference[@Include='System.Reactive']")
        ?? throw new InvalidOperationException($"Did not find <PackageReference> in {file}");
    XmlAttributeCollection packageRefAttributes = rxPackageRefNode.Attributes!;

    if (packageRefAttributes["Include"] is not XmlAttribute includeAttribute)
    {
        includeAttribute = document.CreateAttribute("Include");
        packageRefAttributes.Append(includeAttribute);
    }

    if (packageRefAttributes["Version"] is not XmlAttribute versionAttribute)
    {
        versionAttribute = document.CreateAttribute("Version");
        packageRefAttributes.Append(versionAttribute);
    }

    includeAttribute.Value = rxPackageName;
    versionAttribute.Value = rxPackageVersion;

    if (useWpf.HasValue || useWindowsForms.HasValue)
    {
        XmlElement uiFrameworksPropertyGroup = document.CreateElement("PropertyGroup");

        if (useWpf.HasValue)
        {
            XmlElement useWpfElement = document.CreateElement("UseWPF");
            useWpfElement.InnerText = useWpf.Value.ToString();
            uiFrameworksPropertyGroup.AppendChild(useWpfElement);
        }

        if (useWindowsForms.HasValue)
        {
            XmlElement useWindowsFormsElement = document.CreateElement("UseWindowsForms");
            useWindowsFormsElement.InnerText = useWindowsForms.Value.ToString();
            uiFrameworksPropertyGroup.AppendChild(useWindowsFormsElement);
        }

        document.SelectSingleNode("/Project")!.AppendChild(uiFrameworksPropertyGroup);
    }

    if (emitDisableTransitiveFrameworkReferences)
    {
        // <PropertyGroup>
        //   <DisableTransitiveFrameworkReferences>true</DisableTransitiveFrameworkReferences>
        // </PropertyGroup>
        XmlElement transitiveWorkaroundPropertyGroup = document.CreateElement("PropertyGroup");
        XmlElement disableTransitiveFrameworkReferencesElement = document.CreateElement("DisableTransitiveFrameworkReferences");
        disableTransitiveFrameworkReferencesElement.InnerText = "True";
        transitiveWorkaroundPropertyGroup.AppendChild(disableTransitiveFrameworkReferencesElement);
        document.SelectSingleNode("/Project")!.AppendChild(transitiveWorkaroundPropertyGroup);
    }

    document.Save(destinationPath);
}

internal record Scenario(
    string BaseNetTfm,
    string? WindowsVersion,
    bool? UseWpf,
    bool? UseWindowsForms,
    bool EmitDisableTransitiveFrameworkReferences,
    RxVersions RxVersion);

// Dimensions:
//  RX version (published or proposed)
//  TFM
//  Whether project uses WPF and/or Windows Forms
//  Whether the transitive frameworks workaround is in use.
// We should also check an application that doesn't actually use Rx, as a baseline for whether WPF and/or
// Windows Forms are included in the output.

using NodaTime;

using RxGauntlet;
using RxGauntlet.Build;
using RxGauntlet.LogModel;
using RxGauntlet.Xml;

using System.Diagnostics;
using System.Xml;

string[] baseNetTfms =
[
    "net6.0",
    "net8.0",
    "net9.0"
];

string[] windowsVersions =
[
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

using (var output = new FileStream("CheckIssue1745.json", FileMode.Create, FileAccess.Write, FileShare.Read))
using (var jsonWriter = new System.Text.Json.Utf8JsonWriter(output))
{
    jsonWriter.WriteStartArray();
    foreach (Scenario scenario in scenarios)
    {
        try
        {
            Issue1745TestRun result = await RunScenario(scenario);
            result.WriteTo(jsonWriter);
            jsonWriter.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running scenario {scenario}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
    jsonWriter.WriteEndArray();
}

async Task<Issue1745TestRun> RunScenario(Scenario scenario)
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

    using (var projectClone = ModifiedProjectClone.Create(
        templateProjectFolder.FullName,
        "CheckIssue1745",
        (projectFilePath, xmlDoc) => RewriteProjectXmlDocument(
            projectFilePath,
            tfm,
            rxPackage,
            rxVersion,
            scenario.UseWpf,
            scenario.UseWindowsForms,
            scenario.EmitDisableTransitiveFrameworkReferences,
            xmlDoc)))
    {
        await projectClone.RunDotnetPublish("Bloat.ConsoleWinRtTemplate.csproj");
        string binFolder = Path.Combine(projectClone.ClonedProjectFolderPath, "bin");

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

        // Note: currently this test run has no specialized config so the schema generation
        // doesn't create a type to represent issue1745TestRunConfig. That's why we use
        // the common TestRunConfig here.
        var config = TestRunConfig.Create(
            baseNetTfm: scenario.BaseNetTfm,
            emitDisableTransitiveFrameworkReferences: scenario.EmitDisableTransitiveFrameworkReferences,
            rxVersion: NuGetPackage.Create(id: rxPackage, version: rxVersion),
            useWindowsForms: scenario.UseWindowsForms,
            windowsVersion: scenario.WindowsVersion,
            useWpf: scenario.UseWpf);
        if (scenario.WindowsVersion is string wv)
        {
            config = config.WithWindowsVersion(wv);
        }

        return Issue1745TestRun.Create(
            config: config,
            deployedWindowsForms:includesWindowsForms,
            deployedWpf:includesWpf,
            testRunDateTime: OffsetDateTime.FromDateTimeOffset(DateTimeOffset.UtcNow),
            testRunId: Guid.NewGuid());
    }
}

static void RewriteProjectXmlDocument(string file, string tfm, string rxPackageName, string rxPackageVersion, bool? useWpf, bool? useWindowsForms, bool emitDisableTransitiveFrameworkReferences, XmlDocument document)
{
    XmlNode targetFrameworkNode = document.GetRequiredNode("/Project/PropertyGroup/TargetFramework");
    targetFrameworkNode.InnerText = tfm;

    XmlNode rxPackageRefNode = document.GetRequiredNode("/Project/ItemGroup/PackageReference[@Include='System.Reactive']");

    rxPackageRefNode.SetAttribute("Include", rxPackageName);
    rxPackageRefNode.SetAttribute("Version", rxPackageVersion);

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
}

internal record Scenario(
    string BaseNetTfm,
    string WindowsVersion,
    bool? UseWpf,
    bool? UseWindowsForms,
    bool EmitDisableTransitiveFrameworkReferences,
    RxVersions RxVersion);
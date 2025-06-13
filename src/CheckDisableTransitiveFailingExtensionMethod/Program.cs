

// Expecting build failures in some configurations.

using RxGauntlet;
using RxGauntlet.Build;
using RxGauntlet.LogModel;
using RxGauntlet.Xml;

using System.Reflection.Metadata;
using System.Xml;
using System.Xml.Linq;

string templateProjectFolder =
    Path.Combine(AppContext.BaseDirectory, "../../../../ExtensionMethods/ExtensionMethods.DisableTransitiveWorkaroundFail/");

RxVersions[] rxVersions = [ RxVersions.Rx60 ];
string[] baseNetTfms = [ "net8.0" ];
string?[] windowsVersions = [ null, "windows10.0.19041.0" ];
bool?[] boolsWithNull = [null, true, false];
bool[] bools = [true, false];

IEnumerable<Scenario> scenarios =
    from rxVersion in rxVersions
    from baseNetTfm in baseNetTfms
    from windowsVersion in windowsVersions
    from useWpf in (windowsVersion is null ? [false] : boolsWithNull)
    from useWindowsForms in (windowsVersion is null ? [false] : boolsWithNull)
    from useTransitiveFrameworksWorkaround in bools
    select new Scenario(baseNetTfm, windowsVersion, useWpf, useWindowsForms, useTransitiveFrameworksWorkaround, rxVersion);

foreach (Scenario scenario in scenarios)
{
    await RunScenario(scenario);
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

    using (var projectClone = ModifiedProjectClone.Create(
        templateProjectFolder,
        "CheckDisableTransitiveFailingExtensionMethod",
        (projectFilePath, xmlDoc) =>
        {
            XmlNode targetFrameworkNode = xmlDoc.GetRequiredNode("/Project/PropertyGroup/TargetFramework");
            targetFrameworkNode.InnerText = tfm;

            XmlNode rxPackageRefNode = xmlDoc.GetRequiredNode("/Project/ItemGroup/PackageReference[@Include='System.Reactive']");

            rxPackageRefNode.SetAttribute("Include", rxPackage);
            rxPackageRefNode.SetAttribute("Version", rxVersion);

            if (scenario.UseWpf.HasValue || scenario.UseWindowsForms.HasValue)
            {
                XmlElement uiFrameworksPropertyGroup = xmlDoc.CreateElement("PropertyGroup");

                if (scenario.UseWpf.HasValue)
                {
                    XmlElement useWpfElement = xmlDoc.CreateElement("UseWPF");
                    useWpfElement.InnerText = scenario.UseWpf.Value.ToString();
                    uiFrameworksPropertyGroup.AppendChild(useWpfElement);
                }

                if (scenario.UseWindowsForms.HasValue)
                {
                    XmlElement useWindowsFormsElement = xmlDoc.CreateElement("UseWindowsForms");
                    useWindowsFormsElement.InnerText = scenario.UseWindowsForms.Value.ToString();
                    uiFrameworksPropertyGroup.AppendChild(useWindowsFormsElement);
                }

                xmlDoc.SelectSingleNode("/Project")!.AppendChild(uiFrameworksPropertyGroup);
            }


            if (scenario.EmitDisableTransitiveFrameworkReferences)
            {
                // <PropertyGroup>
                //   <DisableTransitiveFrameworkReferences>true</DisableTransitiveFrameworkReferences>
                // </PropertyGroup>
                XmlElement transitiveWorkaroundPropertyGroup = xmlDoc.CreateElement("PropertyGroup");
                XmlElement disableTransitiveFrameworkReferencesElement = xmlDoc.CreateElement("DisableTransitiveFrameworkReferences");
                disableTransitiveFrameworkReferencesElement.InnerText = "True";
                transitiveWorkaroundPropertyGroup.AppendChild(disableTransitiveFrameworkReferencesElement);
                xmlDoc.SelectSingleNode("/Project")!.AppendChild(transitiveWorkaroundPropertyGroup);
            }
        }))
    {
        await projectClone.RunDotnetBuild("ExtensionMethods.DisableTransitiveWorkaroundFail.csproj");
    }
}

internal record Scenario(
    string BaseNetTfm,
    string? WindowsVersion,
    bool? UseWpf,
    bool? UseWindowsForms,
    bool EmitDisableTransitiveFrameworkReferences,
    RxVersions RxVersion);
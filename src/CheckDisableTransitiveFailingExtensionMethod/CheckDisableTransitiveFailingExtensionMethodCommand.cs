// Expecting build failures in some configurations.

using RxGauntlet;
using RxGauntlet.Build;
using RxGauntlet.CommandLine;
using RxGauntlet.Xml;

using Spectre.Console.Cli;

using System.Xml;

namespace CheckDisableTransitiveFailingExtensionMethod;

internal sealed class CheckDisableTransitiveFailingExtensionMethodCommand : AsyncCommand<RxSourceSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RxSourceSettings settings)
    {
        PackageIdAndVersion[]? replaceSystemReactiveWith = settings.RxPackagesParsed;
        if (replaceSystemReactiveWith is [])
        {
            replaceSystemReactiveWith = null;
        }

        string templateProjectFolder =
            Path.Combine(AppContext.BaseDirectory, "../../../../ExtensionMethods/ExtensionMethods.DisableTransitiveWorkaroundFail/");

        RxVersions[] rxVersions = [replaceSystemReactiveWith is null ? RxVersions.Rx60 : RxVersions.SpecifiedByArguments];
        string[] baseNetTfms = ["net8.0"];
        string?[] windowsVersions = [null, "windows10.0.19041.0"];
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

        return 0;

        async Task RunScenario(Scenario scenario)
        {
            Console.WriteLine(scenario);
            string tfm = scenario.WindowsVersion is string windowsVersion
                ? $"{scenario.BaseNetTfm}-{windowsVersion}"
                : scenario.BaseNetTfm;

            using (var projectClone = ModifiedProjectClone.Create(
                templateProjectFolder,
                "CheckDisableTransitiveFailingExtensionMethod",
                (projectFilePath, xmlDoc) =>
                {
                    if (settings.PackageSource is string packageSource)
                    {
                        // We need to emit a NuGet.config file, because the arguments specified a custom package source
                        string nuGetConfigContent = $"""
                            <?xml version="1.0" encoding="utf-8"?>
                            <configuration>
                              <packageSources>
                                <clear />
                                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                                <add key="AdditionalSource" value="{packageSource}" />
                              </packageSources>
                            </configuration>
                            """;

                        string projectFolderPath = Path.GetDirectoryName(projectFilePath) ??
                            throw new InvalidOperationException("Project file path does not have a directory component.");
                        File.WriteAllText(
                            Path.Combine(projectFolderPath, "NuGet.config"),
                            nuGetConfigContent);
                    }

                    XmlNode targetFrameworkNode = xmlDoc.GetRequiredNode("/Project/PropertyGroup/TargetFramework");
                    targetFrameworkNode.InnerText = tfm;

                    XmlNode rxPackageRefNode = xmlDoc.GetRequiredNode("/Project/ItemGroup/PackageReference[@Include='System.Reactive']");
                    if (replaceSystemReactiveWith is not null)
                    {
                        if (replaceSystemReactiveWith is [PackageIdAndVersion singleReplacement])
                        {
                            rxPackageRefNode.SetAttribute("Include", singleReplacement.PackageId);
                            rxPackageRefNode.SetAttribute("Version", singleReplacement.Version);
                        }
                        else
                        {
                            // The command line arguments specified multiple packages to replace System.Reactive with,
                            // so we remove the original PackageReference and add new ones.
                            XmlNode packageRefItemGroup = rxPackageRefNode.ParentNode!;
                            packageRefItemGroup.RemoveChild(rxPackageRefNode);

                            foreach (PackageIdAndVersion packageIdAndVersion in replaceSystemReactiveWith)
                            {
                                XmlNode rxNewPackageRefNode = packageRefItemGroup.OwnerDocument!.CreateElement("PackageReference");
                                rxNewPackageRefNode.SetAttribute("Include", packageIdAndVersion.PackageId);
                                rxNewPackageRefNode.SetAttribute("Version", packageIdAndVersion.Version);
                                packageRefItemGroup.AppendChild(rxNewPackageRefNode);
                            }
                        }
                    }
                    else
                    {
                        (string rxPackage, string rxVersion) = scenario.RxVersion switch
                        {
                            RxVersions.Rx30 => ("System.Reactive.Linq", "3.0.0"),
                            RxVersions.Rx31 => ("System.Reactive.Linq", "3.1.0"),
                            RxVersions.Rx44 => ("System.Reactive", "4.4.1"),
                            RxVersions.Rx50 => ("System.Reactive", "5.0.0"),
                            RxVersions.Rx60 => ("System.Reactive", "6.0.1"),
                            _ => throw new ArgumentOutOfRangeException(nameof(scenario.RxVersion), scenario.RxVersion, null)
                        };
                        rxPackageRefNode.SetAttribute("Include", rxPackage);
                        rxPackageRefNode.SetAttribute("Version", rxVersion);
                    }



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
                int result = await projectClone.RunDotnetBuild("ExtensionMethods.DisableTransitiveWorkaroundFail.csproj");

                Console.WriteLine($"{scenario}: {result}");
            }
        }
    }
    internal record Scenario(
        string BaseNetTfm,
        string? WindowsVersion,
        bool? UseWpf,
        bool? UseWindowsForms,
        bool EmitDisableTransitiveFrameworkReferences,
        RxVersions RxVersion);
}

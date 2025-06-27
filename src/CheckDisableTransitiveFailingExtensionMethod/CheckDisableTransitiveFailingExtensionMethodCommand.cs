// Expecting build failures in some configurations.

using Corvus.Json;

using RxGauntlet;
using RxGauntlet.Build;
using RxGauntlet.CommandLine;
using RxGauntlet.LogModel;
using RxGauntlet.Xml;

using Spectre.Console.Cli;

using System;
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

        using (var output = new FileStream("CheckExtensionMethodsWorkaround.json", FileMode.Create, FileAccess.Write, FileShare.Read))
        using (var jsonWriter = new System.Text.Json.Utf8JsonWriter(output))
        {
            jsonWriter.WriteStartArray();
            foreach (Scenario scenario in scenarios)
            {
                ExtensionMethodsWorkaroundTestRun result = await RunScenario(scenario);
                result.WriteTo(jsonWriter);
                jsonWriter.Flush();
            }
            jsonWriter.WriteEndArray();
        }

        return 0;

        async Task<ExtensionMethodsWorkaroundTestRun> RunScenario(Scenario scenario)
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


                var config = ExtensionMethodsWorkaroundTestRunConfig.Create(
                    baseNetTfm: scenario.BaseNetTfm,
                    emitDisableTransitiveFrameworkReferences: scenario.EmitDisableTransitiveFrameworkReferences,
                    rxVersion: scenario.RxVersion.ToString(),
                    useWindowsForms: scenario.UseWindowsForms,
                    windowsVersion: scenario.WindowsVersion.AsNullableJsonString(),
                    useWpf: scenario.UseWpf);
                if (scenario.WindowsVersion is string wv)
                {
                    config = config.WithWindowsVersion(wv);
                }
                return ExtensionMethodsWorkaroundTestRun.Create(
                    config: config,
                    deployedWindowsForms: includesWindowsForms,
                    deployedWpf: includesWpf);
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

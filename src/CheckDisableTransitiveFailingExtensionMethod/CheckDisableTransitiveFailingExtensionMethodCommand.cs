// Expecting build failures in some configurations.

using Corvus.Json;

using RxGauntlet.Build;
using RxGauntlet.CommandLine;
using RxGauntlet.LogModel;

using Spectre.Console.Cli;

using System.Diagnostics;
using System.Text.Json;

namespace CheckDisableTransitiveFailingExtensionMethod;

internal sealed class CheckDisableTransitiveFailingExtensionMethodCommand : TestCommandBase<TestSettings>
{
    protected override string DefaultOutputFilename => "CheckExtensionMethodsWorkaround.json";

    protected override async Task<int> ExecuteTestAsync(
        TestDetails testDetails, CommandContext context, TestSettings settings, Utf8JsonWriter jsonWriter)
    {
        // TODO: check that using only the main package is the right thing to do here.
        PackageIdAndVersion[] replaceSystemReactiveWith = [settings.RxMainPackageParsed];

        string templateProjectFolder =
            Path.Combine(AppContext.BaseDirectory, "../../../../ExtensionMethods/ExtensionMethods.DisableTransitiveWorkaroundFail/");

        string[] baseNetTfms = ["net8.0"];
        string?[] windowsVersions = [null, "windows10.0.19041.0"];
        bool?[] boolsWithNull = [null, true, false];
        bool[] bools = [true, false];

        IEnumerable<Scenario> scenarios =
            from baseNetTfm in baseNetTfms
            from windowsVersion in windowsVersions
            from useWpf in (windowsVersion is null ? [false] : boolsWithNull)
            from useWindowsForms in (windowsVersion is null ? [false] : boolsWithNull)
            from useTransitiveFrameworksWorkaround in bools
            select new Scenario(baseNetTfm, windowsVersion, useWpf, useWindowsForms, useTransitiveFrameworksWorkaround);

        jsonWriter.WriteStartArray();
        foreach (Scenario scenario in scenarios)
        {
            ExtensionMethodsWorkaroundTestRun result = await RunScenario(scenario);
            result.WriteTo(jsonWriter);
            jsonWriter.Flush();
        }
        jsonWriter.WriteEndArray();

        return 0;

        async Task<ExtensionMethodsWorkaroundTestRun> RunScenario(Scenario scenario)
        {
            Console.WriteLine(scenario);
            string tfm = scenario.WindowsVersion is string windowsVersion
                ? $"{scenario.BaseNetTfm}-{windowsVersion}"
                : scenario.BaseNetTfm;

            string rxPackage, rxVersion;
            rxPackage = rxVersion = string.Empty;
            using (var projectClone = ModifiedProjectClone.Create(
                templateProjectFolder,
                "CheckDisableTransitiveFailingExtensionMethod",
                (project) =>
                {
                    project.SetTargetFramework(tfm);

                    if (replaceSystemReactiveWith is not null)
                    {
                        project.ReplacePackageReference("System.Reactive", replaceSystemReactiveWith);
                        (rxPackage, rxVersion) = (replaceSystemReactiveWith[0].PackageId, replaceSystemReactiveWith[0].Version);
                    }

                    project.AddUseUiFrameworksIfRequired(scenario.UseWpf, scenario.UseWindowsForms);

                    if (scenario.EmitDisableTransitiveFrameworkReferences)
                    {
                        project.AddPropertyGroup([new("DisableTransitiveFrameworkReferences", "True")]);
                    }
                },
                settings.PackageSource is string packageSource ? [("loc", packageSource)] : null))
            {
                BuildOutput buildResult = await projectClone.RunDotnetBuild("ExtensionMethods.DisableTransitiveWorkaroundFail.csproj");

                Console.WriteLine($"{scenario}: {buildResult}");

                bool includesWpf = false;
                bool includesWindowsForms = false;
                foreach (string file in Directory.GetFiles(buildResult.OutputFolder, "*", new EnumerationOptions { RecurseSubdirectories = true }))
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

                Debug.Assert(!string.IsNullOrWhiteSpace(rxPackage), "rxPackage should not be null or empty.");
                Debug.Assert(!string.IsNullOrWhiteSpace(rxVersion), "rxVersion should not be null or empty.");
                NuGetPackage rxVersionPackage = NuGetPackage.Create(
                    id: rxPackage,
                    version: rxVersion,
                    packageSource: settings.PackageSource.AsNullableJsonString());
                var config = TestRunConfigWithUiFrameworkSettings.Create(
                    baseNetTfm: scenario.BaseNetTfm,
                    emitDisableTransitiveFrameworkReferences: scenario.EmitDisableTransitiveFrameworkReferences,
                    rxVersion: rxVersionPackage,
                    useWindowsForms: scenario.UseWindowsForms,
                    windowsVersion: scenario.WindowsVersion.AsNullableJsonString(),
                    useWpf: scenario.UseWpf);
                if (scenario.WindowsVersion is string wv)
                {
                    config = config.WithWindowsVersion(wv);
                }
                return ExtensionMethodsWorkaroundTestRun.Create(
                    config: config,
                    buildSucceeded: buildResult.Succeeded,
                    deployedWindowsForms: includesWindowsForms,
                    deployedWpf: includesWpf,
                    testRunDateTime: testDetails.TestRunDateTime,
                    testRunId: testDetails.TestRunId);
            }
        }
    }
    internal record Scenario(
        string BaseNetTfm,
        string? WindowsVersion,
        bool? UseWpf,
        bool? UseWindowsForms,
        bool EmitDisableTransitiveFrameworkReferences);
}

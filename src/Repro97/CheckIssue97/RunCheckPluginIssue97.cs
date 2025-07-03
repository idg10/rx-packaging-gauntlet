using Corvus.Json;

using NodaTime;

using NuGet.Common;

using PlugIn.HostDriver;
using PlugIn.HostSerialization;

using RxGauntlet.Build;
using RxGauntlet.LogModel;

using System.Text.Json;

namespace CheckIssue97;

internal class RunCheckPluginIssue97
{
    ////Dictionary<RxVersions, PlugInDescriptor[]> netFxPluginsByRxVersion = new()
    ////{
    ////    { RxVersions.Rx30, [PlugInDescriptor.Net45Rx30, PlugInDescriptor.Net46Rx30]},
    ////    { RxVersions.Rx31, [PlugInDescriptor.Net45Rx31, PlugInDescriptor.Net46Rx31]},
    ////    { RxVersions.Rx44, [PlugInDescriptor.Net46Rx44, PlugInDescriptor.Net462Rx44]},
    ////    { RxVersions.Rx50, [PlugInDescriptor.Net462Rx50, PlugInDescriptor.Net472Rx50]},
    ////    { RxVersions.Rx60, [PlugInDescriptor.Net462Rx60, PlugInDescriptor.Net472Rx60] }
    ////};

    private class TempNuGetLogger : ILogger
    {
        public void Log(LogLevel level, string data)
        {
            Console.WriteLine(data);
        }

        public void Log(ILogMessage message)
        {
            Console.WriteLine(message.FormatWithCode());
        }

        public Task LogAsync(LogLevel level, string data)
        {
            Console.WriteLine(data);
            return Task.CompletedTask;
        }

        public Task LogAsync(ILogMessage message)
        {
            Console.WriteLine(message.FormatWithCode());
            return Task.CompletedTask;
        }

        public void LogDebug(string data)
        {
            Console.WriteLine(data);
        }

        public void LogError(string data)
        {
            Console.WriteLine(data);
        }

        public void LogInformation(string data)
        {
            Console.WriteLine(data);
        }

        public void LogInformationSummary(string data)
        {
            Console.WriteLine(data);
        }

        public void LogMinimal(string data)
        {
            Console.WriteLine(data);
        }

        public void LogVerbose(string data)
        {
            Console.WriteLine(data);
        }

        public void LogWarning(string data)
        {
            Console.WriteLine(data);
        }
    }

    public static async Task RunAsync(
        string testRunId, OffsetDateTime testRunDateTime, Utf8JsonWriter jsonWriter, PackageIdAndVersion[] packages, string? packageSource)
    {
        using PlugInHost plugInHost = new();
        foreach (Scenario scenario in await PlugInTargetSelection.GetPlugInTfmPairingsAsync(packages, packageSource))
        {
            Console.WriteLine(scenario);

            string hostRuntimeTfm = scenario.HostTfm;
            PlugInDescriptor firstPlugIn = scenario.firstPlugIn;
            PlugInDescriptor secondPlugIn = scenario.secondPlugIn;

            HostOutput output = await plugInHost.Run(
                hostRuntimeTfm,
                firstPlugIn,
                secondPlugIn,
                async stdout =>
                {
                    MemoryStream stdOutCopy = new();
                    await stdout.CopyToAsync(stdOutCopy);
                    stdOutCopy.Seek(0, SeekOrigin.Begin);
                    try
                    {
                        return (await JsonSerializer.DeserializeAsync<HostOutput>(stdOutCopy))!;
                    }
                    catch (JsonException x)
                    {
                        stdOutCopy.Seek(0, SeekOrigin.Begin);
                        string stdOutContent = await new StreamReader(stdOutCopy).ReadToEndAsync();
                        Console.Error.WriteLine($"Error deserializing output for {hostRuntimeTfm} {firstPlugIn.RxPackages[0]} {firstPlugIn.TargetFrameworkMoniker} and {secondPlugIn.TargetFrameworkMoniker} {secondPlugIn.TargetFrameworkMoniker}: {x.Message}");
                        Console.Error.WriteLine("Output:");
                        Console.Error.WriteLine(stdOutContent);
                        throw;
                    }
                });

            // NEXT: we need to incorporate the other information from HostOutput:
            //  PlugInLocation
            //  RxFullAssemblyName
            //  RxLocation
            //  RxTargetFramework
            //  FlowsCancellationTokenToOperationCancelledException
            //  SupportsWindowsForms
            // Also, remove all the now-unneeded variations on the PlugIn project, since they are
            // all generated from the one template.
            // Can remove the Common folder too, because the source is now in place in that PlugIn project.
            // Issue97TestRunConfig seems to have useWPF/windowsforms properties. Should it?
            var result = Issue97TestRun.Create(
                Issue97TestRunConfig.Create(
                    hostTfm: scenario.HostTfm,
                    plugIn1: PlugInDescriptorToDetails(scenario.firstPlugIn),
                    plugIn2: PlugInDescriptorToDetails(scenario.secondPlugIn),
                    rxVersion: NuGetPackage.Create(packages[0].PackageId, packages[0].Version)),
                testRunDateTime: testRunDateTime,
                testRunId: testRunId,
                plugIn1: HostPlugInResultToOutputFormat(output.FirstPlugIn),
                plugIn2: HostPlugInResultToOutputFormat(output.SecondPlugIn));

            result.WriteTo(jsonWriter);
        }
    }

    private static PlugInConfig PlugInDescriptorToDetails(PlugInDescriptor descriptor)
    {
        NuGetPackage Package(PackageIdAndVersion p) =>
            NuGetPackage.Create(p.PackageId, p.Version, descriptor.PackageSource.AsNullableJsonString());

        return PlugInConfig.Create(
            tfm: descriptor.TargetFrameworkMoniker,
            rxVersion: RxVersion.Create(
                mainRxPackage: Package(descriptor.RxPackages[0]),
                allPackages: RxVersion.AllPackagesArray.Create(descriptor.RxPackages.Select(Package).ToArray())));
    }

    private static PlugInResult HostPlugInResultToOutputFormat(HostOutput.PlugInResult result)
    {
        return PlugInResult.Create(
            rxFullAssemblyName: result.RxFullAssemblyName,
            rxLocation: result.RxLocation,
            rxTargetFramework: result.RxTargetFramework,
            plugInLocation: result.PlugInLocation,
            flowsCancellationTokenToOperationCancelledException: result.FlowsCancellationTokenToOperationCancelledException,
            supportsWindowsForms: result.SupportsWindowsForms);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

using Corvus.Json;

using NodaTime;

using PlugIn.HostDriver;
using PlugIn.HostSerialization;

using RxGauntlet.Build;
using RxGauntlet.LogModel;

using System.Text.Json;

namespace CheckIssue97;

internal class RunCheckPluginIssue97
{
    public static async Task RunAsync(
        string testRunId, OffsetDateTime testRunDateTime, Utf8JsonWriter jsonWriter, PackageIdAndVersion[] packages, string? packageSource)
    {
        using PlugInHost plugInHost = new();
        foreach (Scenario scenario in await PlugInTargetSelection.GetPlugInTfmPairingsAsync(packages, packageSource))
        {
            Console.WriteLine(scenario);

            string hostRuntimeTfm = scenario.HostTfm;
            PlugInDescriptor firstPlugIn = scenario.FirstPlugIn;
            PlugInDescriptor secondPlugIn = scenario.SecondPlugIn;

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

            var result = Issue97TestRun.Create(
                Issue97TestRunConfig.Create(
                    hostTfm: scenario.HostTfm,
                    plugIn1: PlugInDescriptorToDetails(scenario.FirstPlugIn),
                    plugIn2: PlugInDescriptorToDetails(scenario.SecondPlugIn),
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

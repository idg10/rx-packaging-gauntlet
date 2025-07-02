using NodaTime;

using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

using PlugIn.HostDriver;

using RxGauntlet;
using RxGauntlet.LogModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CheckIssue97;

internal class RunCheckPluginIssue97
{
    private static readonly string[] hostRuntimes =
    [
        //"net462",
        "net472",
        "net481",
        "net8.0"
    ];

    private static readonly string[] everyTfmWeAreConsidering =
        [
            "net11",
            "net20",
            "net35",
            "net40",
            "net403",
            "net45",
            "net451",
            "net452",
            "net46",
            "net461",
            "net462",
            "net47",
            "net471",
            "net472",
            "net48",
            "net481",
            "netcoreapp1.0",
            "netcoreapp1.1",
            "netcoreapp2.0",
            "netcoreapp2.1",
            "netcoreapp2.2",
            "netcoreapp3.0",
            "netcoreapp3.1",
            "net5.0",
            "net6.0",
            "net7.0",
            "net8.0",
            "net9.0",
        ];

    Dictionary<RxVersions, PlugInDescriptor[]> netFxPluginsByRxVersion = new()
    {
        { RxVersions.Rx30, [PlugInDescriptor.Net45Rx30, PlugInDescriptor.Net46Rx30]},
        { RxVersions.Rx31, [PlugInDescriptor.Net45Rx31, PlugInDescriptor.Net46Rx31]},
        { RxVersions.Rx44, [PlugInDescriptor.Net46Rx44, PlugInDescriptor.Net462Rx44]},
        { RxVersions.Rx50, [PlugInDescriptor.Net462Rx50, PlugInDescriptor.Net472Rx50]},
        { RxVersions.Rx60, [PlugInDescriptor.Net462Rx60, PlugInDescriptor.Net472Rx60] }
    };

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

    public static async Task<Issue97TestRun> RunAsync(
        string testRunId, OffsetDateTime testRunDateTime, Scenario scenario, string? packageSource)
    {
        // For the selected Rx version, we want to determine a list of TFMs of interest. The goal here is to
        // get a list of plug-ins all built against the same Rx version, but where each plug-in ends up selecting
        // a different target from the Rx NuGet Package.
        //
        // For example the Rx3.0 NuGet package has netstandard1.0, netstandard1.1, netstandard1.3, net45, and
        // net46 targets. If we build plug-ins against Rx3.0, where one targets net45, and another targets net46,
        // then that has covered every possible .NET Framework TFM for which Rx3.0 offers a distinct target.
        // As for the .NET Standard targets, the only runtimes that do not support .NET Standard 1.3 are the
        // ancient UAP8.x runtimes (which we don't have a way of testing, and which are very ancient, so we
        // will ignore them) and .NET Framework 4.5.2 and earlier. However, for any .NET Framework version that
        // can use Rx 3.0, the build will select a .NET FX target of Rx. The only situation in which we can make
        // a plug-in choose a .NET Standard target is to use .NET (non-FX), and in all cases, this will always select
        // the netstandard1.3 target. So in practice, a single plug-in targeting .net 8.0 will cover all possibilities.
        //
        // So in the case of Rx3.0, if our host TFM is .NET Framework, then since plug-ins with net45 and net46
        // TFMs will cover all availabile options when it comes to selecting the available targets, we need just
        // (net45, net46) and (net46, net45) as the pairs of plug-in TFMs to test. If the host TFM is .NET, then
        // there are no pairings because we needed only the net8.0 plug-in, and since we never pair a plug-in with
        // itself, there are no pairings.

        //string packageId = "System.Reactive.Linq";
        //string version = "3.0.0";
        string packageId = "System.Reactive";
        string version = "6.0.1";

        //var source = packageSource ?? "https://api.nuget.org/v3/index.json";
        string source = "https://api.nuget.org/v3/index.json";
        //var logger = NullLogger.Instance;
        ILogger logger = new TempNuGetLogger();
        var cache = new SourceCacheContext();

        var repository = Repository.Factory.GetCoreV3(source);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

        using var packageStream = new MemoryStream();
        if (!await resource.CopyNupkgToStreamAsync(
            packageId, new NuGetVersion(version), packageStream, cache, logger, CancellationToken.None))
        {
            throw new InvalidOperationException($"Could not download {packageId} {version} from {source}");
        }

        packageStream.Position = 0;
        using var reader = new PackageArchiveReader(packageStream);
        var files = reader.GetFiles().ToList();

        // Some packages (e.g. System.Reactive.Linq 3.0.0) report file entries for what should be folders.
        // For example, we get lib/netstandard1.0/, which is a zero-length file. I think something went
        // wrong when the package was created back in 2016, because these are not files. (There are files
        // inside these folders, such as lib/netstandard1.0/System.Reactive.Linq.dll, but the parent folder
        // itself really shouldn't be reported as a file.) It does not report any such bogus entries for
        // the other target frameworks - this just seems to afflict the netstandard targets.
        // These cause PackageArchiveReader to report these as belonging to an 'Any' framework, which
        // does not accurately represent what the package actually offers, so we strip these out.
        IEnumerable<FrameworkSpecificGroup> libItemsExcludingBogusFolderEntries = reader.GetLibItems()
            .Where(x => x.Items.All(item => item.Split('/') is [.., string final] && final.Length > 0));
        List<NuGetFramework> frameworks = libItemsExcludingBogusFolderEntries            
            .Select(x => x.TargetFramework)
            .Where(fx => fx is not null)
            .Distinct()
            .ToList();

        var reducer = new FrameworkReducer();

        // At this point, as Carmel has pointed out, if we have a netstandard target, we know
        // we've got a potentially tricky case, because it might not be obvious which target
        // a plug-in needs to specify to get the netstandardX.X target. But for everything
        // else, maybe it is as straightforward as just using the TFM in this list. (So if the
        // list includes `net45`, we build a `net45`-targetting plug-in.)
        //
        // If it is a netstandard target, we only need to consider versions of .NET older
        // than the oldest .NET Framework target offered by the version of Rx we're testing.
        //
        // net45, netstandard2.0
        // net45, net462

        foreach (string hostRuntimeTfm in hostRuntimes)
        {
            // Assuming we're running on a supported version of Windows 11 or later. This ensures that when we get to
            // plugins with OS-specific TFMs, the host runtime version comes out as higher than or equal to the plugin TFM
            // in cases where they match on major and minor versions.
            string effectiveHostTfm = TargetFrameworkMonikerParser.TryParseNetFxMoniker(hostRuntimeTfm, out _, out _)
                ? hostRuntimeTfm
                : $"{hostRuntimeTfm}-windows10.0.22631";

            //bool isNetFx = Regex.IsMatch(hostRuntimeTfm, @"^net4[0-9]{2}$");
            var hostFramework = NuGetFramework.Parse(effectiveHostTfm);
            NuGetFramework? nearest = reducer.GetNearest(hostFramework, frameworks);

            // This filters out Rx targets where none of the TFMs we could use in the plug-ins to select
            // that target are compatible with the host runtime.
            var plugInTfmsCompatibleWithHostRuntime = everyTfmWeAreConsidering
                .Select(NuGetFramework.Parse)
                .Where(item => reducer.GetNearest(hostFramework, [item]) is not null);
            var plugInTfmsWithNearestRxMatch = plugInTfmsCompatibleWithHostRuntime
                .SelectMany(plugInFramework =>
                {
                    // If the plug-in TFM is compatible with the host runtime, we can determine which Rx target it will select.
                    (NuGetFramework PluginTfm, string RxTfm)[] result = reducer.GetNearest(plugInFramework, frameworks) is NuGetFramework selectedRxTarget
                        ? [(plugInFramework, selectedRxTarget.GetShortFolderName())]
                        : [];

                    return result;
                });

            Dictionary<string, List<NuGetFramework>> plugInTfmsBySelectedRxTarget = plugInTfmsWithNearestRxMatch
                .GroupBy(item => item.RxTfm)
                .ToDictionary(                    
                    g => g.Key,
                    g => g.Select(p => p.PluginTfm).OrderBy(x => x.Version).ToList());

            Console.WriteLine(JsonSerializer.Serialize(plugInTfmsBySelectedRxTarget, JsonSerializerOptions.Default));

            foreach((string key, List<NuGetFramework> plugInTfms) in plugInTfmsBySelectedRxTarget)
            {
                var candidate = reducer.GetNearest(hostFramework, plugInTfms);
                Console.WriteLine(candidate is not null);
            }


            //PlugInDescriptor[] pluginsByVersion = isNetFx
            //    ? scenario.firstPlugIn.GetNetFxPluginsByRxVersion(scenario.secondPlugIn)
            //    : scenario.firstPlugIn.GetDotnetPluginsByRxVersion(scenario.secondPlugIn);




            //
            // Next we filter this list of plug-ins to those that are compatible with the host runtime. Finally,
            // we produce a list of every possible ordered pairs of plug-in TFMs. For 
            // For the host runtime
            // under consideration, we want to identify a set of plug-in TFMs all of which will build a plug-in
            // capable of running on the host runtime, and then to reduce this to a minimal set where each TFM will
            // cause a different Rx target to be selected.
            // We're only considering host runtime versions still in support, the oldest being net462, which is newer
            // than the newest target in Rx30. But for plug-ins we want to consider a wider range of TFMs, because we
            // need to emulate scenarios in which an old plug-in gets loaded.
            // So in theory we are considering potentially any net4XX TFM for the plug-in. In practice we can exploit
            // knowledge about what Rx versions actually exist, to know that we only need to consider a few candidate
            // plug-in TFMs.
            // Specifically, for .NET Framework we only need to consider net45, net46, net462, and net472. It so happens
            // that with the versions of Rx published to date, adding any more TFMs to the list will not add any new
            // outcomes to the specific version of
            // which of the available Rx targets will the minimal set of possible where a plug-in  that will select different targets, and we want those grouped by 
        }

        return Issue97TestRun.Create(
            TestRunConfig.Create(
                baseNetTfm: "net472",
                emitDisableTransitiveFrameworkReferences: false,
                rxVersion: NuGetPackage.Create("System.Reactive", "6.0.1")),
            plugIn1: PlugInDetails.Create(tfm: "foo"),
            plugIn2: PlugInDetails.Create(tfm: "bar"),
            testRunDateTime: testRunDateTime,
            testRunId: testRunId);
    }
}

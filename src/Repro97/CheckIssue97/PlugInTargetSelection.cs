using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

using PlugIn.HostDriver;

using RxGauntlet.Build;

using System.Text.Json;

namespace CheckIssue97;

internal class PlugInTargetSelection
{
    private static readonly string[] hostRuntimes =
    [
        "net462",
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
            //"net461",
            "net462",
            "net47",
            //"net471",
            "net472",
            "net48",
            "net481",
            //"netcoreapp1.0",
            //"netcoreapp1.1",
            //"netcoreapp2.0",
            //"netcoreapp2.1",
            //"netcoreapp2.2",
            "netcoreapp3.0",
            "netcoreapp3.1",
            "net5.0",
            "net6.0",
            "net7.0",
            "net8.0",
            "net9.0",
            "net5.0-windows10.0.19041",
            "net6.0-windows10.0.19041",
            "net7.0-windows10.0.19041",
            "net8.0-windows10.0.19041",
            "net9.0-windows10.0.19041",
        ];

    public static async Task<List<Scenario>> GetPlugInTfmPairingsAsync(
        PackageIdAndVersion[] packages, string? packageSource)
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

        string mainPackageId = packages[0].PackageId;
        string mainPackageVersion = packages[0].Version;

        var source = packageSource ?? "https://api.nuget.org/v3/index.json";
        var logger = NullLogger.Instance;
        var cache = new SourceCacheContext();

        var repository = Repository.Factory.GetCoreV3(source);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

        using var packageStream = new MemoryStream();
        if (!await resource.CopyNupkgToStreamAsync(
            mainPackageId, new NuGetVersion(mainPackageVersion), packageStream, cache, logger, CancellationToken.None))
        {
            throw new InvalidOperationException($"Could not download {mainPackageId} {mainPackageVersion} from {source}");
        }

        packageStream.Position = 0;
        using var reader = new PackageArchiveReader(packageStream);
        var files = reader.GetFiles().ToList();

        // Some packages (e.g. System.Reactive.Linq 3.0.0) report file entries for what should be folders.
        // For example, we get lib/netstandard1.0/, which is a zero-length file. I think something went
        // wrong when the package was created back in 2016, because these should not be files. (There are files
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
        // However the brute-force approach, in which we just try every TFM we are considering
        // and ask the NuGet library which Rx target it would select, seems to work well enough.

        List<Scenario> results = new();
        foreach (string hostRuntimeTfm in hostRuntimes)
        {
            // Assuming we're running on a supported version of Windows 11 or later. This ensures that when we get to
            // plugins with OS-specific TFMs, the host runtime version comes out as higher than or equal to the plugin TFM
            // in cases where they match on major and minor versions.
            string effectiveHostTfm = TargetFrameworkMonikerParser.TryParseNetFxMoniker(hostRuntimeTfm, out _, out _)
                ? hostRuntimeTfm
                : $"{hostRuntimeTfm}-windows10.0.22631";

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

            List<string> selectedPlugInTfms = new();
            foreach ((string key, List<NuGetFramework> plugInTfms) in plugInTfmsBySelectedRxTarget)
            {
                // We could just do this to let NuGet pick which it thinks is the best of the available TFMs for
                // the host runtime:
                // NuGetFramework? candidate = reducer.GetNearest(hostFramework, plugInTfms);
                // However, that tends to pick the highest possible version. E.g., for Rx 6.0.1 in the net472 host,
                // its pick for the netstandard2.0 NuGet target is a plug-in TFM of net471.
                // Back when we did all this by hand, we chose net462 as the plug-in TFM that resolved to netstandard2.0.
                // More generally, we prefer the oldest TFM that works. (This whole test scenario is essentially recreating
                // legacy setups so the older TFMs usually better reflect the real-life scenarios these tests represent.)
                NuGetFramework? candidate = plugInTfms
                    .Select(plugInTfm => reducer.GetNearest(hostFramework, [plugInTfm]))
                    .FirstOrDefault(framework => framework is not null);
                if (candidate is not null)
                {
                    selectedPlugInTfms.Add(candidate.GetShortFolderName());
                }
                else
                {
                    // If we cannot find a compatible TFM, then we cannot test this Rx target.
                    Console.WriteLine($"No compatible plug-in TFM found for Rx target {key} with host runtime {hostRuntimeTfm}");
                }
            }

            results.AddRange(
                from firstPlugIn in selectedPlugInTfms
                from secondPlugIn in selectedPlugInTfms
                where firstPlugIn != secondPlugIn
                select new Scenario(hostRuntimeTfm, new PlugInDescriptor(firstPlugIn, packages, packageSource), new PlugInDescriptor(secondPlugIn, packages, packageSource)));
        }

        return results;
    }
}


// Rx Version   Targets
//  3.0         net45; net46; netcore451 (aka win81); netcoreapp1.0; netstandard1.0; netstandard1.1; netstandard1.3; uap10.0; wpa81
// Host Version
//  .NET FX 4.6
// Plug-in target   Task flow   WindowsForms
//  .NET 4.5        Not flowed  Available
//  .NET 4.6        Flowed      Available
//
// 20 versions to check.

// Things to check:
// 

using CheckIssue97;

using PlugIn.HostDriver;
using PlugIn.HostSerialization;

using RxGauntlet;

using Spectre.Console.Cli;

using System.Text.Json;
using System.Text.Json.Serialization;


var app = new CommandApp<CheckPluginIssue97Command>();

app.Configure(config =>
{
    //config.AddCommand<CheckDeploymentBloatCommand>("check");
});

return await app.RunAsync(args);

//string[] netFxHostRuntimes = [
//    "net462",
//    "net472",
//    "net481"];
//Dictionary<RxVersions, PlugInDescriptor[]> netFxPluginsByRxVersion = new()
//{
//    { RxVersions.Rx30, [PlugInDescriptor.Net45Rx30, PlugInDescriptor.Net46Rx30]},
//    { RxVersions.Rx31, [PlugInDescriptor.Net45Rx31, PlugInDescriptor.Net46Rx31]},
//    { RxVersions.Rx44, [PlugInDescriptor.Net46Rx44, PlugInDescriptor.Net462Rx44]},
//    { RxVersions.Rx50, [PlugInDescriptor.Net462Rx50, PlugInDescriptor.Net472Rx50]},
//    { RxVersions.Rx60, [PlugInDescriptor.Net462Rx60, PlugInDescriptor.Net472Rx60] }
//};

//string[] dotnetHostRuntimes = ["net8.0"];
//Dictionary<RxVersions, PlugInDescriptor[]> dotnetPluginsByRxVersion = new()
//{
//    { 
//        RxVersions.Rx44,
//        [
//            PlugInDescriptor.Dotnet50Rx44, PlugInDescriptor.Dotnet50WindowsRx44,
//            PlugInDescriptor.Dotnet60Rx44, PlugInDescriptor.Dotnet60WindowsRx44, PlugInDescriptor.Dotnet60Windows10019041Rx44,
//            PlugInDescriptor.Dotnet80Rx44, PlugInDescriptor.Dotnet80WindowsRx44, PlugInDescriptor.Dotnet80Windows10019041Rx44,
//            PlugInDescriptor.Dotnet90Rx44, PlugInDescriptor.Dotnet90WindowsRx44, PlugInDescriptor.Dotnet90Windows10019041Rx44
//        ]
//    },
//    {
//        RxVersions.Rx50,
//        [
//            PlugInDescriptor.Dotnet50Rx50, PlugInDescriptor.Dotnet50Windows10019041Rx50,
//            PlugInDescriptor.Dotnet60Rx50, PlugInDescriptor.Dotnet60Windows10019041Rx50,
//            PlugInDescriptor.Dotnet80Rx50, PlugInDescriptor.Dotnet80Windows10019041Rx50,
//            PlugInDescriptor.Dotnet90Rx50, PlugInDescriptor.Dotnet90Windows10019041Rx50
//        ]
//    },
//};

//JsonSerializerOptions jsonOptions = new()
//{
//    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//    WriteIndented = true,
//    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
//    //DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
//};

//bool isFirstRow = true;
//Console.WriteLine("[");
//await RunTests(netFxHostRuntimes, netFxPluginsByRxVersion);
//await RunTests(dotnetHostRuntimes, dotnetPluginsByRxVersion);
//Console.WriteLine("]");

//async Task RunTests(string[] hostRuntimes, Dictionary<RxVersions, PlugInDescriptor[]> pluginsByRxVersion)
//{
//    foreach (string hostRuntimeTfm in hostRuntimes)
//    {
//        // Assuming we're running on a supported version of Windows 11 or later. This ensures that when we get to
//        // plugins with OS-specific TFMs, the host runtime version comes out as higher than or equal to the plugin TFM
//        // in cases where they match on major and minor versions.
//        string effectiveHostTfm = TargetFrameworkMonikerParser.TryParseNetFxMoniker(hostRuntimeTfm, out _, out _)
//            ? hostRuntimeTfm
//            : $"{hostRuntimeTfm}-windows10.0.22631";
//        foreach ((RxVersions rxVersion, PlugInDescriptor[] plugins) in pluginsByRxVersion)
//        {
//            PlugInDescriptor[] hostablePlugins = plugins.Where(p =>
//                TargetFrameworkMonikerComparer.Instance.Compare(effectiveHostTfm, p.TargetFrameworkMoniker) >= 0).ToArray();

//            bool expectedToShowIssue97 = rxVersion < RxVersions.Rx31 || rxVersion >= RxVersions.Rx50;

//            IEnumerable<(PlugInDescriptor FirstPlugIn, PlugInDescriptor SecondPlugIn)> pairs =
//                from firstPlugIn in hostablePlugins
//                from secondPlugIn in hostablePlugins
//                where firstPlugIn != secondPlugIn
//                select (firstPlugIn, secondPlugIn);

//            foreach ((PlugInDescriptor firstPlugIn, PlugInDescriptor secondPlugIn) in pairs)
//            {
//                HostOutput output = await PlugInHost.Run(
//                    hostRuntimeTfm,
//                    firstPlugIn,
//                    secondPlugIn,
//                    async stdout =>
//                    {
//                        MemoryStream stdOutCopy = new();
//                        await stdout.CopyToAsync(stdOutCopy);
//                        stdOutCopy.Seek(0, SeekOrigin.Begin);
//                        try
//                        {
//                            return (await JsonSerializer.DeserializeAsync<HostOutput>(stdOutCopy))!;
//                        }
//                        catch (JsonException x)
//                        {
//                            stdOutCopy.Seek(0, SeekOrigin.Begin);
//                            string stdOutContent = await new StreamReader(stdOutCopy).ReadToEndAsync();
//                            Console.Error.WriteLine($"Error deserializing output for {hostRuntimeTfm} {firstPlugIn.RxVersion} {firstPlugIn.TargetFrameworkMoniker} and {secondPlugIn.RxVersion} {secondPlugIn.TargetFrameworkMoniker}: {x.Message}");
//                            Console.Error.WriteLine("Output:");
//                            Console.Error.WriteLine(stdOutContent);
//                            throw;
//                        }
//                    });
//                Console.WriteLine();

//                PlugInTestResult testResult = new(
//                    hostRuntimeTfm,
//                    firstPlugIn,
//                    secondPlugIn,
//                    output);

//                if (isFirstRow)
//                {
//                    isFirstRow = false;
//                }
//                else
//                {
//                    Console.WriteLine(",");
//                }
//                Console.WriteLine(JsonSerializer.Serialize(testResult, jsonOptions));
//            }
//        }
//    }
//}
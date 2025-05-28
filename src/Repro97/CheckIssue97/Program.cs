
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

using System.Text.Json;
using System.Text.Json.Serialization;

string[] hostRuntimes = [
    "net462",
    "net472",
    "net481"];
Dictionary<RxVersions, PlugInDescriptor[]> pluginsByRxVersion = new()
{
    { RxVersions.Rx30, [PlugInDescriptor.Net45Rx30, PlugInDescriptor.Net46Rx30 ]},
    { RxVersions.Rx31, [PlugInDescriptor.Net45Rx31, PlugInDescriptor.Net46Rx31 ]},
    { RxVersions.Rx44, [PlugInDescriptor.Net46Rx44, PlugInDescriptor.Net462Rx44 ]},
    { RxVersions.Rx50, [PlugInDescriptor.Net462Rx50, PlugInDescriptor.Net472Rx50 ]},
    { RxVersions.Rx60, [PlugInDescriptor.Net462Rx60, PlugInDescriptor.Net472Rx60] }
};

JsonSerializerOptions jsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    //DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
};

foreach (string hostRuntimeTfm in hostRuntimes)
{
    foreach ((RxVersions rxVersion, PlugInDescriptor[] plugins) in pluginsByRxVersion)
    {
        if (plugins.Any(p =>
            TargetFrameworkMonikerComparer.Instance.Compare(hostRuntimeTfm, p.TargetFrameworkMoniker) < 0))
        {
            // Skip this Rx version if the host runtime TFM is lower than the plugin TFM.
            continue;
        }

        bool expectedToShowIssue97 = rxVersion < RxVersions.Rx31 || rxVersion >= RxVersions.Rx50;

        IEnumerable<(PlugInDescriptor FirstPlugIn, PlugInDescriptor SecondPlugIn)> pairs =
            from firstPlugIn in plugins
            from secondPlugIn in plugins
            where firstPlugIn != secondPlugIn
            select (firstPlugIn, secondPlugIn);

        foreach ((PlugInDescriptor firstPlugIn, PlugInDescriptor secondPlugIn) in pairs)
        {
            HostOutput output = await PlugInHost.Run(
                hostRuntimeTfm,
                firstPlugIn,
                secondPlugIn,
                async stdout =>
                {
                    return (await JsonSerializer.DeserializeAsync<HostOutput>(stdout))!;
                    //string json = await new StreamReader(stdout).ReadToEndAsync();
                    //HostOutput result = JsonSerializer.Deserialize<HostOutput>(json)!;
                    //Console.WriteLine(JsonSerializer.Serialize(result));
                    //return result;
                });
            Console.WriteLine();

            PlugInTestResult testResult = new(
                hostRuntimeTfm,
                firstPlugIn,
                secondPlugIn,
                output);

            Console.WriteLine(JsonSerializer.Serialize(testResult, jsonOptions));
        }
    }
}
using RxGauntlet.CommandLine;
using RxGauntlet.LogModel;

using Spectre.Console.Cli;

using System.Text.Json;

namespace CheckIssue1745;

internal class CheckDeploymentBloatCommand : TestCommandBase<TestSettings>
{
    protected override string DefaultOutputFilename => "CheckIssue1745.json";

    private static readonly string[] baseNetTfms =
    [
        //"net6.0",
        "net8.0",
        "net9.0"
    ];

    private static readonly string[] windowsVersions =
    [
        "windows10.0.18362.0",
        "windows10.0.19041.0",
        "windows10.0.22000.0"
    ];

    private static readonly bool?[] boolsWithNull = [null, true, false];
    private static readonly bool[] bools = [true, false];

    protected override async Task<int> ExecuteTestAsync(
        TestDetails testDetails, CommandContext context, TestSettings settings, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteStartArray();

        IEnumerable<Scenario> scenarios =
            from baseNetTfm in baseNetTfms
            from windowsVersion in windowsVersions
            from useWpf in boolsWithNull
            from useWindowsForms in boolsWithNull
            from useTransitiveFrameworksWorkaround in bools
            select new Scenario(baseNetTfm, windowsVersion, useWpf, useWindowsForms, useTransitiveFrameworksWorkaround, settings.RxPackagesParsed, settings.PackageSource);

        foreach (Scenario scenario in scenarios)
        {
            try
            {
                Issue1745TestRun result = await RunDeploymentBloatCheck.RunAsync(
                    testDetails.TestRunId, testDetails.TestRunDateTime, scenario, settings.PackageSource);
                result.WriteTo(jsonWriter);
                jsonWriter.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running scenario {scenario}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        jsonWriter.WriteEndArray();

        return 0;
    }
}

using RxGauntlet.CommandLine;

using Spectre.Console.Cli;

using System.Text.Json;

namespace CheckIssue97;

internal class CheckPluginIssue97Command : TestCommandBase<TestSettings>
{
    private static readonly string[] netFxHostRuntimes = ["net462", "net472", "net481"];

    protected override string DefaultOutputFilename => "CheckPluginIssue97.json";

    protected override async Task<int> ExecuteTestAsync(
        TestDetails testDetails,
        CommandContext context,
        TestSettings settings,
        Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteStartArray();

        await RunCheckPluginIssue97.RunAsync(
            testDetails.TestRunId,
            testDetails.TestRunDateTime,
            jsonWriter,
            settings.RxPackagesParsed,
            settings.PackageSource);

        jsonWriter.WriteEndArray();

        return 0;
    }
}

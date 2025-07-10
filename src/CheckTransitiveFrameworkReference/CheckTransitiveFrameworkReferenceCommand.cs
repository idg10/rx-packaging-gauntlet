using RxGauntlet.CommandLine;

using Spectre.Console.Cli;

using System.Text.Json;

namespace CheckTransitiveFrameworkReference;

/// <summary>
/// Main command.
/// </summary>
/// <remarks>
/// 
/// </remarks>
internal class CheckTransitiveFrameworkReferenceCommand : TestCommandBase<TestSettings>
{
    protected override string DefaultOutputFilename => "CheckTransitiveFrameworkReference.json";

    protected override async Task<int> ExecuteTestAsync(
        TestDetails testDetails,
        CommandContext context,
        TestSettings settings,
        Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteStartArray();

        using RunTransitiveFrameworkReferenceCheck runCheck = new(
            testDetails.TestRunId,
            testDetails.TestRunDateTime,
            settings.RxMainPackageParsed,
            settings.RxLegacyPackageParsed,
            settings.RxUiFrameworkPackagesParsed,
            settings.PackageSource is string packageSource ? [("loc", packageSource)] : null);
        foreach (var scenario in Scenario.GetScenarios())
        {
            Console.WriteLine(scenario);
            RxGauntlet.LogModel.TransitiveFrameworkReferenceTestRun testResult = await runCheck.RunScenarioAsync(scenario);

            testResult.WriteTo(jsonWriter);
            await jsonWriter.FlushAsync();
        }

        jsonWriter.WriteEndArray();

        return 0;
    }
}

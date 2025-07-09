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
            await runCheck.RunScenarioAsync(scenario);
        }

        return 0;
    }
}

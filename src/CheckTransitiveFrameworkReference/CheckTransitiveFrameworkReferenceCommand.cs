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

    protected override Task<int> ExecuteTestAsync(
        TestDetails testDetails,
        CommandContext context,
        TestSettings settings,
        Utf8JsonWriter jsonWriter)
    {
        using RunTransitiveFrameworkReferenceCheck runCheck = new(settings.PackageSource is string packageSource ? [("loc", packageSource)] : null);
        foreach (var scenario in Scenario.GetScenarios())
        {
            
            Console.WriteLine(scenario);
        }

        return Task.FromResult(0);
    }
}

using RxGauntlet.CommandLine;

using Spectre.Console.Cli;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
        foreach (var scenario in Scenario.GetScenarios())
        {
            Console.WriteLine(scenario);
        }

        return Task.FromResult(0);
    }
}

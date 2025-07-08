using RxGauntlet.CommandLine;

using Spectre.Console.Cli;

using System.ComponentModel;

namespace RxGauntlet.Cli;

/// <summary>
/// Settings for the <c>test-candidate</c> command.
/// </summary>
/// <remarks>
/// <para>
/// Annoyingly, we end up having to duplicate the <see cref="OutputDirectory"/> and <see cref="TestId"/>
/// that also appear in <see cref="RxGauntletAllPublishedRxCommandSettings"/>. See the remarks for
/// <see cref="IOrchestrationCommandSettings"/> for more information.
/// </para>
internal class RxGauntletCandidateCommandSettings : RxSourceSettings, IOrchestrationCommandSettings
{
    [CommandOption("-o|--output")]
    [Description("The output directory for the RxGauntlet results. Defaults to a subfolder of the directory named for the date and time.")]
    public string? OutputDirectory { get; init; }

    [Description("A unique id to be written into all test result output files, enabling them all to be identified as part of the same test run. Defaults to a value based on the current date and time.")]
    [CommandOption("--test-id")]
    public string? TestId { get; init; }
}

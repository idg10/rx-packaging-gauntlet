using RxGauntlet.CommandLine;

using Spectre.Console;
using Spectre.Console.Cli;

using System.ComponentModel;

namespace RxGauntlet;

/// <summary>
/// Defines the command line arguments for RxGauntlet.
/// </summary>
/// <remarks>
/// Since we accept <c>--rx-package</c> and <c>--package-source</c> arguments in the same form as all the individual
/// repro projects, this derives from the same <see cref="RxSourceSettings"/> class as them.
/// </remarks>
internal class RxGauntletCommandSettings : RxSourceSettings
{
    [CommandOption("--all-published-rx")]
    public bool AllPublishedRx { get; init; } = false;

    [CommandOption("-o|--output")]
    [Description("The output directory for the RxGauntlet results. Defaults to a subfolder of the directory named for the date and time.")]
    public string? OutputDirectory { get; init; }

    [Description("A unique id to be written into all test result output files, enabling them all to be identified as part of the same test run. Defaults to a value based on the current date and time.")]
    [CommandOption("--test-id")]
    public string? TestId { get; init; }

    public override ValidationResult Validate()
    {
        ValidationResult result = base.ValidateRxPackages(packageRequired: false);
        if (result.Successful)
        {
            if (AllPublishedRx)
            {
                if (this.RxPackages.Length > 0)
                {
                    result = ValidationResult.Error("Cannot specify both --all-published-rx and --rx-package");
                }
            }

            if (this.RxPackages.Length == 0 && !AllPublishedRx)
            {
                result = ValidationResult.Error("Must specify either --all-published-rx or --rx-package");
            }
        }

        return result;
    }
}

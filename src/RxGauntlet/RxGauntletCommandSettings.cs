using RxGauntlet.CommandLine;

using Spectre.Console;
using Spectre.Console.Cli;

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

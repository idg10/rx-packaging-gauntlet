using Spectre.Console.Cli;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxGauntlet;

internal sealed class RxGauntletCommand : AsyncCommand<RxGauntletCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RxGauntletCommandSettings settings)
    {
        TestRunPackageSelection[] packageSelections;
        if (settings.AllPublishedRx)
        {
            packageSelections =
            [
                new([new("System.Reactive.Linq", "3.0.0")], null),
                new([new("System.Reactive.Linq", "3.1.0")], null),
                new([new("System.Reactive", "4.4.1")], null),
                new([new("System.Reactive", "5.0.0")], null),
                new([new("System.Reactive", "6.0.1")], null),
            ];
        }
        else
        {
            packageSelections = [new TestRunPackageSelection(settings.RxPackagesParsed, settings.PackageSource)];
        }

        TestType[] testTypes = TestType.All;

        RunGauntlet runner = new();
        await runner.RunAsync(
            testTypes,
            packageSelections);

        return 0;
    }
}

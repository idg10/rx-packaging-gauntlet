// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

namespace RxGauntlet.Cli;

/// <summary>
/// Handles the CLI's <c>test-all-published-rx</c> command.
/// </summary>
internal class RxGauntletAllPublishedRxCommand : RxGauntletCommandBase<RxGauntletAllPublishedRxCommandSettings>
{
    protected override TestRunPackageSelection[] GetPackageSelection(RxGauntletAllPublishedRxCommandSettings settings)
    {
        return
            [
                // TODO: we could actually supply UI-framework-specific ids for earlier versions. Do we need to?
                new(new("System.Reactive.Linq", "3.0.0"), null, [], null),
                new(new("System.Reactive.Linq", "3.1.0"), null, [], null),
                new(new("System.Reactive", "4.4.1"), null, [], null),
                new(new("System.Reactive", "5.0.0"), null, [], null),
                new(new("System.Reactive", "6.0.1"), null, [], null),
            ];
    }
}

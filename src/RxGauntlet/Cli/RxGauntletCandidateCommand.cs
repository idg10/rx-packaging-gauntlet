// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

namespace RxGauntlet.Cli;

internal class RxGauntletCandidateCommand : RxGauntletCommandBase<RxGauntletCandidateCommandSettings>
{
    protected override TestRunPackageSelection[] GetPackageSelection(RxGauntletCandidateCommandSettings settings)
    {
        return [new TestRunPackageSelection(
            settings.RxMainPackageParsed,
            settings.RxLegacyPackageParsed,
            settings.RxUiFrameworkPackagesParsed,
            settings.PackageSource)];
    }
}

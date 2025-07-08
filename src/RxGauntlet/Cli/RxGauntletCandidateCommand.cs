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

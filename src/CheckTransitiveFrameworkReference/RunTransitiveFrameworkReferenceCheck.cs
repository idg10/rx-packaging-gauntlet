using RxGauntlet.Build;
using RxGauntlet.LogModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckTransitiveFrameworkReference;

internal class RunTransitiveFrameworkReferenceCheck(
    (string FeedName, string FeedLocation)[]? additionalPackageSources) : IDisposable
{
    private const string AppTempFolderName = "TransitiveFrameworkReference";
    private static readonly string AppTemplateProjectFolder =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../TransitiveReferences/Transitive.App"));
    private static readonly string LibTemplateProjectFolder =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../TransitiveReferences/Transitive.Lib.UsesRx"));

    private readonly ComponentBuilder _componentBuilder = new(AppTempFolderName);

    public void Dispose()
    {
        _componentBuilder.Dispose();
    }

    public async Task<TransitiveFrameworkReferenceTestRun> RunScenarioAsync(
        Scenario scenario)
    {
        // TODO: do we need to distinguish between this and whether we get our Rx reference transitively or directly? Or are
        // we already handling that in the scenario variations?
        //scenario.AppHasCodeUsingNonUiFrameworkSpecificRxDirectly

        await _componentBuilder.BuildLocalNuGetPackageAsync(
            LibTemplateProjectFolder,
            projectRewriter =>
            {

            },
            additionalPackageSources);


        return TransitiveFrameworkReferenceTestRun.Create(;
    }
}
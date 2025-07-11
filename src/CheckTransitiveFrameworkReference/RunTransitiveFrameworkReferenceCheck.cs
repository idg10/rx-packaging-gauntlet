using NodaTime;

using RxGauntlet.Build;
using RxGauntlet.LogModel;

using System.Collections.Generic;
using System.Diagnostics;

namespace CheckTransitiveFrameworkReference;

internal class RunTransitiveFrameworkReferenceCheck(
    string testRunId,
    OffsetDateTime testRunDateTime,
    PackageIdAndVersion rxMainPackage,
    PackageIdAndVersion? rxLegacyPackage,
    PackageIdAndVersion[] rxUiPackages,
    (string FeedName, string FeedLocation)[]? additionalPackageSources) : IDisposable
{
    private const string AppTempFolderName = "TransitiveFrameworkReference";
    private static readonly string TemplateProjectsParentFolder =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../TransitiveReferences"));
    private static readonly string AppTemplateProject =
        Path.GetFullPath(Path.Combine(TemplateProjectsParentFolder, "Transitive.App/Transitive.App.csproj"));
    private static readonly string LibTemplateProject =
        Path.GetFullPath(Path.Combine(TemplateProjectsParentFolder, "Transitive.Lib.UsesRx/Transitive.Lib.UsesRx.csproj"));

    private static readonly PackageIdAndVersion OldRx = new("System.Reactive", "6.0.1");

    private readonly ComponentBuilder _componentBuilder = new(AppTempFolderName);
    private readonly Dictionary<TransitiveRxReferenceViaLibrary, PackageIdAndVersion> _builtLibPackages = new();

    private int _scenarioCounter = 0;

    public void Dispose()
    {
        _componentBuilder.Dispose();
    }

    public async Task<TransitiveFrameworkReferenceTestRun> RunScenarioAsync(
        Scenario scenario)
    {
        Console.WriteLine(++_scenarioCounter);
        // TODO: do we need to distinguish between this and whether we get our Rx reference transitively or directly? Or are
        // we already handling that in the scenario variations?
        //scenario.AppHasCodeUsingNonUiFrameworkSpecificRxDirectly

        PackageIdAndVersion[] newRxMainAndIfRequiredLegacyPackage =
            rxLegacyPackage is not null
                ? [rxMainPackage, rxLegacyPackage]
                : [rxMainPackage];


        // Build all the NuGet packages we'll need, and work out what their package IDs and versions are.
        async Task ProcessLib(TransitiveRxReferenceViaLibrary ld)
        {
            if (!_builtLibPackages.ContainsKey(ld))
            {
                Console.WriteLine(ld);
                // Note: we put the timestamp in so that we get a different package ID each time we run the test.
                // NuGet copies the package into its local cache (.nuget) and doesn't overwrite it if the ID and version
                // match, because it presumes nobody would ever try to publish a new version of a package without
                // changing the name.
                // TODO: we don't currently have a direct way of simulating two different versions of the same package.
                string packageVersion = $"1.0.0-preview{DateTime.UtcNow.ToString("yyyyddMMHHmmssff")}";
                string rxVersionPart = ld.ReferencesNewRxVersion ? "Old" : "New";
                string tfmsNamePart = string.Join(".", ld.Tfms).Replace(";", ".");
                bool hasWindowsTarget = ld.Tfms.Contains("-windows");
                string hasUiRxPart = hasWindowsTarget
                    ? (ld.HasWindowsTargetUsingUiFrameworkSpecificRxFeature ? ".Ui" : ".NoUi")
                    : "";
                string assemblyName = $"Transitive.Lib.UsesRx.{rxVersionPart}{hasUiRxPart}.{tfmsNamePart}";

                BuildOutput packageBuildResult = await _componentBuilder.BuildLocalNuGetPackageAsync(
                    LibTemplateProject,
                    project =>
                    {
                        project.SetTargetFrameworks(ld.Tfms);
                        project.AddAssemblyNameProperty(assemblyName);
                        project.AddPropertyGroup([new("Version", packageVersion)]);

                        if (ld.ReferencesNewRxVersion)
                        {
                            // TODO: do we need to consider scenarios in which future Rx.NET relegates System.Reactive
                            // to a legacy facade, but a component has a reference to System.Reactive v7. I don't think
                            // that's a thing a library should ever do but perhaps we need to test it.

                            PackageIdAndVersion[] replaceSystemReactiveWith = ld.HasWindowsTargetUsingUiFrameworkSpecificRxFeature
                                ? [..newRxMainAndIfRequiredLegacyPackage, ..rxUiPackages]
                                : newRxMainAndIfRequiredLegacyPackage;
                            project.ReplacePackageReference("System.Reactive", replaceSystemReactiveWith);
                        }

                        if (!ld.HasWindowsTargetUsingUiFrameworkSpecificRxFeature)
                        {
                            project.ReplaceProperty("_ScenarioWindowsDefineConstants", "");
                        }
                    },
                    additionalPackageSources);

                if (!packageBuildResult.Succeeded)
                {
                    throw new InvalidOperationException("Unexpected failure when building NuGet package to be consumed by test app");
                }

                _builtLibPackages.Add(ld, new PackageIdAndVersion(assemblyName, packageVersion));
            }
        }

        //(LibraryDetails? Details, RxAcquiredVia Aquisition)[] libs =
        RxDependency[] libs =
            [
                ..scenario.RxDependenciesBefore,
                ..scenario.RxDependenciesAfter,
                //(scenario.BeforeLibrary, scenario.RxBefore),
                //(scenario.AfterLibrary, scenario.RxUpgrade),
                //(scenario.BeforeAndAfterLibrary, scenario.RxBeforeAndAfter)
            ];
        foreach (RxDependency dependency in libs)
        {
            await dependency.Match(
                (DirectRxPackageReference _) => Task.CompletedTask,
                ProcessLib);
        }

        bool appExpectingToUseRxUiFeatures = scenario.AppHasCodeUsingNonUiFrameworkSpecificRxDirectly
            || scenario.AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature;
        //PackageIdAndVersion[] GetPackage(LibraryDetails? ld, RxAcquiredVia acq, bool packageRefsIncludeLegacyPackageIfAvailable)
        PackageIdAndVersion[] GetPackage(RxDependency rxDependency)
        {
            //return acq switch
            //{
            //    RxAcquiredVia.NoReference => [],
            //    RxAcquiredVia.PackageTransitiveDependency => ld is not null
            //        ? [builtLibPackages[ld]]
            //        throw new InvalidOperationException($"{acq} requires library details"),
            //    RxAcquiredVia.PackageReferenceInProject => ld is null
            //    ? (isNew
            //        ? (appExpectingToUseRxUiFeatures
            //            ? [newRxMainAndIfRequiredLegacyPackage, .. rxUiPackages] : [newRxMainAndIfRequiredLegacyPackage])
            //        : [OldRx])
            //    : [builtLibPackages[ld]],
            //        _ => throw new ArgumentException($"Unknown enum entry {acq}", nameof(acq))
            //};

            return rxDependency.Match(GetPackageDirect, GetPackageTransitive);

            PackageIdAndVersion[] GetPackageTransitive(TransitiveRxReferenceViaLibrary libRef)
            {
                return [_builtLibPackages[libRef]];
            }

            PackageIdAndVersion[] GetPackageDirect(DirectRxPackageReference rxDependency)
            {
                return rxDependency.Match(
                    (OldRx _) => [OldRx],
                    (NewRx newRx) =>
                    {
                        PackageIdAndVersion[] rxMainRef = newRx.IncludeLegacyPackageWhereAvailable
                            ? newRxMainAndIfRequiredLegacyPackage
                            : [rxMainPackage];
                        return newRx.IncludeUiPackages
                            ? [.. rxMainRef, .. rxUiPackages]
                            : rxMainRef;
                    });
            }
        }

        PackageIdAndVersion[] beforeLibraries = scenario.RxDependenciesBefore.SelectMany(dep => GetPackage(dep)).ToArray();
        //[
        //    ..GetPackage(scenario.BeforeLibrary, scenario.RxBefore, packageRefsIncludeLegacyPackageIfAvailable: false),
        //    ..GetPackage(scenario.BeforeAndAfterLibrary, scenario.RxBeforeAndAfter, packageRefsIncludeLegacyPackageIfAvailable: false),
        //];

        PackageIdAndVersion[] afterLibraries = scenario.RxDependenciesAfter.SelectMany(dep => GetPackage(dep)).ToArray();
        //[
        //        ..GetPackage(scenario.BeforeAndAfterLibrary, scenario.RxBeforeAndAfter, packageRefsIncludeLegacyPackageIfAvailable: true),
        //        ..GetPackage(scenario.AfterLibrary, scenario.RxUpgrade, packageRefsIncludeLegacyPackageIfAvailable: true),
        //    ];

        async Task<BuildOutput> BuildApp(PackageIdAndVersion[] packageRefs, bool isAfter)
        {
            // Note that the ComponentBuilder automatically adds the dynamically created package source
            // (which contains any packages just built with _componentBuilder.BuildLocalNuGetPackageAsync)
            // to the list of available feeds, combining that with and feeds specified in this call.
            BuildOutput r = await _componentBuilder.BuildAppAsync(
                AppTemplateProject,
                project =>
                {
                    project.SetTargetFramework(scenario.ApplicationTfm);
                    project.ReplaceProjectReferenceWithPackageReference(
                        "Transitive.Lib.UsesRx.csproj",
                        packageRefs);
                    if (isAfter && scenario.DisableTransitiveFrameworkReferencesAfter)
                    {
                        project.AddPropertyGroup([new("DisableTransitiveFrameworkReferences", "True")]);
                    }

                    // Currently this is the only flag using _ScenarioDefineConstants,
                    // so we don't need to accumulate the values.
                    List<string> allTargetsDefineConstants = [];
                    if (scenario.AppHasCodeUsingNonUiFrameworkSpecificRxDirectly)
                    {
                        allTargetsDefineConstants.Add("UseNonUiFrameworkSpecificRxDirectly");
                    }
                    if (scenario.AppInvokesLibraryMethodThatUsesNonUiFrameworkSpecificRxFeature)
                    {
                        allTargetsDefineConstants.Add("InvokeLibraryMethodThatUsesNonFrameworkSpecificRxFeature");
                    }
                    project.ReplaceProperty(
                        "_ScenarioDefineConstants",
                        string.Join(";", allTargetsDefineConstants));

                    List<string> windowsDefineConstants = [];
                    if (scenario.AppHasCodeUsingUiFrameworkSpecificRxDirectly)
                    {
                        windowsDefineConstants.Add("UseUiFrameworkSpecificRxDirectly");
                    }
                    if (scenario.AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature)
                    {
                        windowsDefineConstants.Add("InvokeLibraryMethodThatUsesUiFrameworkSpecificRxFeature");
                    }

                    project.ReplaceProperty(
                        "_ScenarioWindowsDefineConstants",
                        string.Join(";", windowsDefineConstants));
                },
                additionalPackageSources);

            if (r.Succeeded)
            {
                // TODO: do we actually want to run the app?
                ProcessStartInfo startInfo = new()
                {
                    FileName = Path.Combine(r.OutputFolder, scenario.ApplicationTfm, "win-x64", "Transitive.App.exe"),
                    UseShellExecute = false
                };
                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    await process.WaitForExitAsync();
                }
            }
            else
            {
                Debugger.Break();
            }
            return r;
        }

        // TODO: we will actually build two apps: before and after upgrade.
        BuildOutput beforeBuildResult = await BuildApp(beforeLibraries, isAfter: false);
        BuildOutput afterBuildResult = await BuildApp(afterLibraries, isAfter: true);

        (bool deployedWindowsForms, bool deployedWpf) = beforeBuildResult.CheckForUiComponentsInOutput();
        var beforeResult = TransitiveFrameworkReferenceTestPartResult.Create(
            buildSucceeded: beforeBuildResult.Succeeded,
            deployedWindowsForms: deployedWindowsForms,
            deployedWpf: deployedWpf);
        (deployedWindowsForms, deployedWpf) = afterBuildResult.CheckForUiComponentsInOutput();
        var afterResult = TransitiveFrameworkReferenceTestPartResult.Create(
            buildSucceeded: afterBuildResult.Succeeded,
            deployedWindowsForms: deployedWindowsForms,
            deployedWpf: deployedWpf);
        var config = TransitiveFrameworkReferenceTestRunConfig.Create(
            rxVersion: NuGetPackage.Create(rxMainPackage.PackageId, rxMainPackage.Version),
            appUsesRxNonUiDirectly: scenario.AppHasCodeUsingNonUiFrameworkSpecificRxDirectly,
            appUsesRxUiDirectly: scenario.AppHasCodeUsingUiFrameworkSpecificRxDirectly,
            appUsesRxNonUiViaLibrary: scenario.AppInvokesLibraryMethodThatUsesNonUiFrameworkSpecificRxFeature,
            appUsesRxUiViaLibrary: scenario.AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature,
            before: MakeConfig(scenario.RxDependenciesBefore, false),
            after: MakeConfig(scenario.RxDependenciesAfter, scenario.DisableTransitiveFrameworkReferencesAfter));
        return TransitiveFrameworkReferenceTestRun.Create(
            testRunId: testRunId,
            testRunDateTime: testRunDateTime,
            config: config,
            resultsBefore: beforeResult,
            resultsAfter: afterResult);
    }

    private static TestRunPartConfig MakeConfig(RxDependency[] deps, bool disableTransitiveFrameworkReferences)
    {
        return TestRunPartConfig.Create(
            directRefToOldRx: deps.Any(d => d.IsOldRx),
            directRefToNewRxMain: deps.Any(d => d.IsNewRx),
            directRefToNewRxLegacyFacade: deps.Any(d => d.TryGetNewRx(out NewRx n) && n.IncludeLegacyPackageWhereAvailable),
            directRefToNewRxUiPackages: deps.Any(d => d.TryGetNewRx(out NewRx n) && n.IncludeUiPackages),

            transitiveRefToOldRx: deps.Any(d => d.TryGetTransitiveRxReferenceViaLibrary(
                out TransitiveRxReferenceViaLibrary tr) && !tr.ReferencesNewRxVersion),
            transitiveRefToNewRxMain: deps.Any(d => d.TryGetTransitiveRxReferenceViaLibrary(
                out TransitiveRxReferenceViaLibrary tr) && tr.ReferencesNewRxVersion),
            transitiveRefToNewRxLegacyFacade: false, // Currently we don't have a way to make this happen.

            // Currently these next two properties are always the same.
            transitiveRefToNewRxUiPackages: deps.Any(d => d.TryGetTransitiveRxReferenceViaLibrary(
                out TransitiveRxReferenceViaLibrary tr) && tr.HasWindowsTargetUsingUiFrameworkSpecificRxFeature),
            transitiveRefUsesRxUiFeatures: deps.Any(d => d.TryGetTransitiveRxReferenceViaLibrary(
                out TransitiveRxReferenceViaLibrary tr) && tr.HasWindowsTargetUsingUiFrameworkSpecificRxFeature),

            disableTransitiveFrameworkReferences: disableTransitiveFrameworkReferences);
    }
}
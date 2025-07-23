
#pragma warning disable IDE0350 // Use implicitly typed lambda - in OneOf matches, it's typically easier to understand with explicit parameter types

using NodaTime;

using RxGauntlet.Build;
using RxGauntlet.LogModel;

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

        PackageIdAndVersion[] newRxMainAndIfRequiredLegacyPackage =
            rxLegacyPackage is not null
                ? [rxMainPackage, rxLegacyPackage]
                : [rxMainPackage];
        PackageIdAndVersion[] newRxLegacyPackageIfAvailableElseMain =
            rxLegacyPackage is not null
                ? [rxLegacyPackage]
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
                string packageVersion = $"1.0.0-preview{DateTime.UtcNow:yyyyddMMHHmmssff}";
                string rxVersionPart = ld.ReferencesNewRxVersion ? "New" : "Old";
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

                if (!packageBuildResult.BuildSucceeded)
                {
                    throw new InvalidOperationException("Unexpected failure when building NuGet package to be consumed by test app");
                }

                _builtLibPackages.Add(ld, new PackageIdAndVersion(assemblyName, packageVersion));
            }
        }

        RxDependency[] libs =
            [
                ..scenario.RxDependenciesBefore,
                ..scenario.RxDependenciesAfter,
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
                        PackageIdAndVersion[] rxMainRef = newRx.LegacyPackageChoice switch
                        {
                            NewRxLegacyOptions.JustMain => [rxMainPackage],
                            NewRxLegacyOptions.JustLegacy => newRxLegacyPackageIfAvailableElseMain,
                            NewRxLegacyOptions.MainAndLegacy => newRxMainAndIfRequiredLegacyPackage,
                            _ => throw new NotImplementedException(),
                        };
                        return newRx.IncludeUiPackages
                            ? [.. rxMainRef, .. rxUiPackages]
                            : rxMainRef;
                    });
            }
        }

        PackageIdAndVersion[] beforeLibraries = scenario.RxDependenciesBefore.SelectMany(GetPackage).ToArray();
        PackageIdAndVersion[] afterLibraries = scenario.RxDependenciesAfter.SelectMany(GetPackage).ToArray();

        async Task<BuildAndRunOutput> BuildApp(PackageIdAndVersion[] packageRefs, bool isAfter)
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

                    if ((!isAfter && scenario.UseWpfAndWindowsFormsBefore) ||
                        (isAfter && scenario.UseWpfAndWindowsFormsAfter))
                    {
                        project.AddPropertyGroup([new("UseWPF", "True"), new("UseWindowsForms", "True")]);
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

            int? runExitCode = null;
            string? runStdOut = null;
            string? runStdErr = null;
            if (r.BuildSucceeded)
            {
                // TODO: do we actually want to run the app?
                ProcessStartInfo startInfo = new()
                {
                    FileName = Path.Combine(r.OutputFolder, scenario.ApplicationTfm, "win-x64", "Transitive.App.exe"),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    // TODO: we have 3 versions of this stdout handling now. 1: centralise. 2: work out
                    // if we really need this extra time to complete stdout handling.
                    Task<string> stdOutTask = Task.Run(process.StandardOutput.ReadToEndAsync);
                    Task<string> stdErrTask = Task.Run(process.StandardError.ReadToEndAsync);
                    Task processTask = process.WaitForExitAsync();
                    Task firstToFinish = await Task.WhenAny(processTask, stdOutTask, stdErrTask);

                    if (!stdOutTask.IsCompleted)
                    {
                        // The process finished, but the standard output task is still running. It's possible that
                        // it is nearly done, so give it some time.
                        await Task.WhenAny(stdOutTask, Task.Delay(2000));
                    }
                    if (!stdErrTask.IsCompleted)
                    {
                        await Task.WhenAny(stdErrTask, Task.Delay(2000));
                    }

                    if (!stdOutTask.IsCompleted)
                    {
                        throw new InvalidOperationException("Did not get output from program");
                    }

                    if (!stdErrTask.IsCompleted)
                    {
                        throw new InvalidOperationException("Did not get error output from program");
                    }
                    runStdOut = await stdOutTask;
                    runStdErr = await stdErrTask;

                    await processTask;
                    runExitCode = process.ExitCode;
                }
            }
            else
            {
                Debugger.Break();
            }
            return new(r.BuildProcessExitCode, r.OutputFolder, r.BuildStdOut, runExitCode, runStdOut, runStdErr);
        }

        // TODO: we will actually build two apps: before and after upgrade.
        BuildAndRunOutput beforeBuildResult = await BuildApp(beforeLibraries, isAfter: false);
        BuildAndRunOutput afterBuildResult = await BuildApp(afterLibraries, isAfter: true);

        static TransitiveFrameworkReferenceTestPartResult MakePartResult(BuildAndRunOutput buildAndRunOutput)
        {
            (bool deployedWindowsForms, bool deployedWpf) = buildAndRunOutput.CheckForUiComponentsInOutput();
            var r = TransitiveFrameworkReferenceTestPartResult.Create(
                buildSucceeded: buildAndRunOutput.BuildSucceeded,
                executionExitCode: buildAndRunOutput.ExecuteExitCode,
                deployedWindowsForms: deployedWindowsForms,
                deployedWpf: deployedWpf);

            if (!buildAndRunOutput.BuildSucceeded)
            {
                r = r.WithBuildStdOut(buildAndRunOutput.BuildStdOut);
            }

            if (buildAndRunOutput.ExecuteExitCode is int exit && exit != 0)
            {
                if (buildAndRunOutput.ExecuteStdOut is string execStdOut)
                {
                    r = r.WithExecutionStdOut(execStdOut);
                }
                if (buildAndRunOutput.ExecuteStdErr is string execStdErr)
                {
                    r = r.WithExecutionStdOut(execStdErr);
                }
            }

            return r;
        }

        var beforeResult = MakePartResult(beforeBuildResult);
        var afterResult = MakePartResult(afterBuildResult);

        // Can't do this before now because MakePartResult accesses the output folder.
        await _componentBuilder.DeleteBuiltAppNowAsync(beforeBuildResult);
        await _componentBuilder.DeleteBuiltAppNowAsync(afterBuildResult);

        var config = TransitiveFrameworkReferenceTestRunConfig.Create(
            rxVersion: NuGetPackage.Create(rxMainPackage.PackageId, rxMainPackage.Version),
            appUsesRxNonUiDirectly: scenario.AppHasCodeUsingNonUiFrameworkSpecificRxDirectly,
            appUsesRxUiDirectly: scenario.AppHasCodeUsingUiFrameworkSpecificRxDirectly,
            appUsesRxNonUiViaLibrary: scenario.AppInvokesLibraryMethodThatUsesNonUiFrameworkSpecificRxFeature,
            appUsesRxUiViaLibrary: scenario.AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature,
            before: MakeConfig(scenario.RxDependenciesBefore, false, scenario.UseWpfAndWindowsFormsBefore),
            after: MakeConfig(scenario.RxDependenciesAfter, scenario.DisableTransitiveFrameworkReferencesAfter, scenario.UseWpfAndWindowsFormsAfter));
        return TransitiveFrameworkReferenceTestRun.Create(
            testRunId: testRunId,
            testRunDateTime: testRunDateTime,
            config: config,
            resultsBefore: beforeResult,
            resultsAfter: afterResult);
    }

    private static TestRunPartConfig MakeConfig(RxDependency[] deps, bool disableTransitiveFrameworkReferences, bool useWpfAndWindowsForms)
    {
        var result = TestRunPartConfig.Create(
            directRefToOldRx: deps.Any(d => d.IsOldRx),
            directRefToNewRxMain: deps.Any(d => d.TryGetNewRx(out NewRx n) && n.LegacyPackageChoice is not NewRxLegacyOptions.JustLegacy),
            directRefToNewRxLegacyFacade: deps.Any(d => d.TryGetNewRx(out NewRx n) && n.LegacyPackageChoice is not NewRxLegacyOptions.JustMain),
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

            useWpf: useWpfAndWindowsForms,
            useWindowsForms: useWpfAndWindowsForms,
            disableTransitiveFrameworkReferences: disableTransitiveFrameworkReferences);

        if (deps.FirstOrDefault(d => d.TryGetTransitiveRxReferenceViaLibrary(out _)) is RxDependency trd)
        {
            TransitiveRxReferenceViaLibrary tr = (TransitiveRxReferenceViaLibrary)trd;
            result = result.WithTransitiveLibraryTfms(tr.Tfms);
        }

        return result;
    }
}
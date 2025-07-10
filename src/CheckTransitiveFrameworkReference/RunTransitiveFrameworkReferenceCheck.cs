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

        PackageIdAndVersion[] newRxMainAndIfRequiredLegacyPackage =
            rxLegacyPackage is not null
                ? [rxMainPackage, rxLegacyPackage]
                : [rxMainPackage];


        // Build all the NuGet packages we'll need, and work out what their package IDs and versions are.
        Dictionary<LibraryDetails, PackageIdAndVersion> builtLibPackages = new();
        async Task ProcessLib(LibraryDetails ld)
        {
            if (!builtLibPackages.ContainsKey(ld))
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
                    projectRewriter =>
                    {
                        projectRewriter.SetTargetFrameworks(ld.Tfms);
                        projectRewriter.AddAssemblyNameProperty(assemblyName);
                        projectRewriter.AddPropertyGroup([new("Version", packageVersion)]);

                        if (ld.ReferencesNewRxVersion)
                        {
                            // TODO: do we need to consider scenarios in which future Rx.NET relegates System.Reactive
                            // to a legacy facade, but a component has a reference to System.Reactive v7. I don't think
                            // that's a thing a library should ever do but perhaps we need to test it.

                            PackageIdAndVersion[] replaceSystemReactiveWith = ld.HasWindowsTargetUsingUiFrameworkSpecificRxFeature
                                ? [..newRxMainAndIfRequiredLegacyPackage, ..rxUiPackages]
                                : newRxMainAndIfRequiredLegacyPackage;
                            projectRewriter.ReplacePackageReference("System.Reactive", replaceSystemReactiveWith);
                        }

                        if (!ld.HasWindowsTargetUsingUiFrameworkSpecificRxFeature
                         || !scenario.AppHasCodeUsingNonUiFrameworkSpecificRxDirectly)
                        {
                            List<string> windowsDefineConstants = [];
                            if (ld.HasWindowsTargetUsingUiFrameworkSpecificRxFeature)
                            {
                                windowsDefineConstants.Add("InvokeLibraryMethodThatUsesUiFrameworkSpecificRxFeature");
                            }

                            if (scenario.AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature)
                            {
                                windowsDefineConstants.Add("UseUiFrameworkSpecificRxDirectly");
                            }

                            projectRewriter.ReplaceProperty(
                                "_ScenarioWindowsDefineConstants",
                                string.Join(";", windowsDefineConstants));
                        }
                    },
                    additionalPackageSources);

                if (!packageBuildResult.Succeeded)
                {
                    throw new InvalidOperationException("Unexpected failure when building NuGet package to be consumed by test app");
                }

                builtLibPackages.Add(ld, new PackageIdAndVersion(assemblyName, packageVersion));
            }
        }

        (LibraryDetails? Details, RxAcquiredVia Aquisition)[] libs =
            [
                (scenario.BeforeLibrary, scenario.RxBefore),
                (scenario.AfterLibrary, scenario.RxUpgrade),
                (scenario.BeforeAndAfterLibrary, scenario.RxBeforeAndAfter)
            ];
        foreach ((LibraryDetails? ld, RxAcquiredVia aq) in libs)
        {
            if (aq == RxAcquiredVia.PackageTransitiveDependency)
            {
                Debug.Assert(ld is not null, $"When {aq} specified, LibraryDetails are required");
                await ProcessLib(ld);
            }
            else if (aq == RxAcquiredVia.NoReference)
            {
                Debug.Assert(ld is null, $"When {aq} specified, there should be no LibraryDetails");
            }
        }

        bool appExpectingToUseRxUiFeatures = scenario.AppHasCodeUsingNonUiFrameworkSpecificRxDirectly
            || scenario.AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature;
        PackageIdAndVersion[] GetPackage(LibraryDetails? ld, RxAcquiredVia acq, bool isNew)
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

            if (ld is null)
            {
                return acq switch
                {
                    RxAcquiredVia.NoReference => [],
                    RxAcquiredVia.PackageTransitiveDependency => throw new InvalidOperationException($"{acq} requires library details"),
                    RxAcquiredVia.PackageReferenceInProject => isNew
                        ? (appExpectingToUseRxUiFeatures
                            ? [..newRxMainAndIfRequiredLegacyPackage, ..rxUiPackages] : newRxMainAndIfRequiredLegacyPackage)
                        : [OldRx],
                    _ => throw new ArgumentException($"Unknown enum entry {acq}", nameof(acq))
                };
            }

            Debug.Assert(acq == RxAcquiredVia.PackageTransitiveDependency,
                $"When LibraryDetails are specified, RxAcquiredVia must be {RxAcquiredVia.PackageTransitiveDependency}");
            return [builtLibPackages[ld]];
        }

        PackageIdAndVersion[] beforeLibraries =
            [
                ..GetPackage(scenario.BeforeLibrary, scenario.RxBefore, isNew: false),
                ..GetPackage(scenario.BeforeAndAfterLibrary, scenario.RxBeforeAndAfter, isNew: false),
            ];

        PackageIdAndVersion[] afterLibraries =
            [
                ..GetPackage(scenario.BeforeAndAfterLibrary, scenario.RxBeforeAndAfter, isNew: false),
                ..GetPackage(scenario.AfterLibrary, scenario.RxUpgrade, isNew: true),
            ];

        async Task<BuildOutput> BuildApp(PackageIdAndVersion[] packageRefs)
        {
            // Note that the ComponentBuilder automatically adds the dynamically created package source
            // (which contains any packages just built with _componentBuilder.BuildLocalNuGetPackageAsync)
            // to the list of available feeds, combining that with and feeds specified in this call.
            BuildOutput r = await _componentBuilder.BuildAppAsync(
                AppTemplateProject,
                projectRewriter =>
                {
                    projectRewriter.SetTargetFrameworks(scenario.ApplicationTfm);
                    projectRewriter.ReplaceProjectReferenceWithPackageReference(
                        "Transitive.Lib.UsesRx.csproj",
                        packageRefs);

                    if (!scenario.AppHasCodeUsingNonUiFrameworkSpecificRxDirectly)
                    {
                        projectRewriter.ReplaceProperty("_ScenarioDefineConstants", "");
                    }
                    if (!scenario.AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature)
                    {
                        projectRewriter.ReplaceProperty("_ScenarioWindowsDefineConstants", "");
                    }
                },
                additionalPackageSources);

            if (r.Succeeded)
            {
                // TODO: do we actually want to run the app?
                ProcessStartInfo startInfo = new()
                {
                    FileName = Path.Combine(r.OutputFolder, scenario.ApplicationTfm, "Transitive.App.exe"),
                    UseShellExecute = false
                };
                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    await process.WaitForExitAsync();
                }

            }
            return r;
        }

        // TODO: we will actually build two apps: before and after upgrade.
        await BuildApp(beforeLibraries);
        await BuildApp(afterLibraries);

        // NEXT TIME 2025/07/09: we're using Transitive.Lib.UsesRx for both refs here, and one of them should be
        // Rx.next, or possibly a completely different package.



        var placeholder = TransitiveFrameworkReferenceTestPartResult.Create(
            buildSucceeded: true,
            deployedWindowsForms: false,
            deployedWpf: false);
        var config = TransitiveFrameworkReferenceTestRunConfig.Create(
            appTfm: scenario.ApplicationTfm,
            rxVersion: new NuGetPackage(),
            oldRxVersion: new RxVersion(),
            newRxVersion: new RxVersion());
        return TransitiveFrameworkReferenceTestRun.Create(
            testRunId: testRunId,
            testRunDateTime: testRunDateTime,
            config: config,

            // TODO: make real!
            // TODO: why are these deployedWindowsForms/Wpf here and also in the per test part section?
            deployedWindowsForms: false,
            deployedWpf: false,
            beforeRxUpgrade: placeholder,
            afterRxUpgrade : placeholder);
    }
}
namespace CheckTransitiveFrameworkReference;

internal enum RxAcquiredVia
{
    NoReference,
    PackageReferenceInProject,
    PackageTransitiveDependency
}

/// <summary>
/// 
/// </summary>
/// <param name="RxBefore">
/// For the Rx reference that changes in the before and after scenarios, this describes how this reference is acquired
/// in the before scenario.
/// </param>
/// <param name="RxUpgrade">
/// For the Rx reference that changes in the before and after scenarios, this describes how this reference is acquired
/// in the before scenario.
/// </param>
/// <param name="RxBeforeAndAfter">
/// For the Rx reference that is the same in the before and after scenarios, this describes how this reference is
/// acquired.
/// </param>
/// <param name="AppHasCodeUsingNonUiFrameworkSpecificRxDirectly"></param>
internal record Scenario(
    string ApplicationTfm,
    string TfmsOfBeforeAndAfterLibrary,
    RxAcquiredVia RxBefore,
    RxAcquiredVia RxUpgrade,
    RxAcquiredVia RxBeforeAndAfter,
    LibraryDetails? BeforeLibrary,
    LibraryDetails? BeforeAndAfterLibrary,
    LibraryDetails? AfterLibrary,
    bool AppHasCodeUsingNonUiFrameworkSpecificRxDirectly,
    bool AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature) // TODO: do we need before/after/both flavours of this?
{
    public static IEnumerable<Scenario> GetScenarios()
    {
        // Application dimensions.
        // There are essentially three of these.
        //
        // App dimension 1: Rx initially referenced directly by app csproj vs only referenced transitively
        // App dimension 2: latest version via csproj vs reference
        (AcquisitionAndIsOld RxBeforeAndAfter, AcquisitionAndIsOld? RxBefore, AcquisitionAndIsOld RxAfter)[] rxRefDirectFromAppVsTransitive =
        [
            // Dim 1: initially transitive reference to old
            // Dim 2: latest acquired by adding package ref to project
            (
                RxBeforeAndAfter: new(RxAcquiredVia.PackageTransitiveDependency, ReferencesNewRxVersion: false),
                RxBefore: null,
                RxAfter: new(RxAcquiredVia.PackageReferenceInProject, ReferencesNewRxVersion: true)             
            ),

            // Dim 1: initially transitive AND project reference to old
            // Dim 2: latest acquired by updating package ref in project
            (
                RxBeforeAndAfter: new(RxAcquiredVia.PackageTransitiveDependency, ReferencesNewRxVersion: false),
                RxBefore:  new(RxAcquiredVia.PackageReferenceInProject, ReferencesNewRxVersion: false),
                RxAfter: new(RxAcquiredVia.PackageReferenceInProject, ReferencesNewRxVersion: true)
            ),

            // Dim 1: initially package reference to new, then we add a transitive ref to the old
            // Dim 2: direct ref to latest package in csproj (both before and after)
            (
                RxBeforeAndAfter: new(RxAcquiredVia.PackageReferenceInProject, ReferencesNewRxVersion: true),
                RxBefore: null,
                RxAfter: new(RxAcquiredVia.PackageTransitiveDependency, ReferencesNewRxVersion: false)
            ),

            // Dim 1: initially transitive reference to new, then we add a transitive ref to the old
            // Dim 2: transitive ref to latest package in csproj (both before and after)
            (
                RxBeforeAndAfter: new(RxAcquiredVia.PackageTransitiveDependency, ReferencesNewRxVersion: true),
                RxBefore: null,
                RxAfter: new(RxAcquiredVia.PackageTransitiveDependency, ReferencesNewRxVersion: false)
            ),
        ];

        // App dimension 3: use of RX UI features
        //
        // This itself has three dimensions:
        //  Does the library's Windows-specific target contain code that uses UI-framework-specific Rx functionality?
        //  Does the app use the code in the library that uses UI-framework-specific Rx functionality?
        //  Does the app uses UI-framework-specific Rx functionality directly?
        // (The first two aren't quite independent, because the app can't use code that the library doesn't have.)
        (bool LibraryWindowsTargetUsesRxUiFeatures, bool AppInvokesLibraryCodePathsUsingRxUiFeatures, bool AppUseRxUiFeaturesDirectly)[] rxUiUsages =
        [
            (LibraryWindowsTargetUsesRxUiFeatures: false, AppInvokesLibraryCodePathsUsingRxUiFeatures: false, AppUseRxUiFeaturesDirectly: false),
            (LibraryWindowsTargetUsesRxUiFeatures: true, AppInvokesLibraryCodePathsUsingRxUiFeatures: false, AppUseRxUiFeaturesDirectly: false),
            (LibraryWindowsTargetUsesRxUiFeatures: true, AppInvokesLibraryCodePathsUsingRxUiFeatures: true, AppUseRxUiFeaturesDirectly: false),

            (LibraryWindowsTargetUsesRxUiFeatures: false, AppInvokesLibraryCodePathsUsingRxUiFeatures: false, AppUseRxUiFeaturesDirectly: true),
            (LibraryWindowsTargetUsesRxUiFeatures: true, AppInvokesLibraryCodePathsUsingRxUiFeatures: false, AppUseRxUiFeaturesDirectly: true),
            (LibraryWindowsTargetUsesRxUiFeatures: true, AppInvokesLibraryCodePathsUsingRxUiFeatures: true, AppUseRxUiFeaturesDirectly: true),
        ];


        return
            from rxRef in rxRefDirectFromAppVsTransitive
            from rxUiUsage in rxUiUsages
            let oldLibrary = new LibraryDetails("net8.0;net8.0-windows10.0.19041", false, rxUiUsage.LibraryWindowsTargetUsesRxUiFeatures)
            let newLibrary = new LibraryDetails("net8.0;net8.0-windows10.0.19041", true, rxUiUsage.LibraryWindowsTargetUsesRxUiFeatures)
            select new Scenario(
                ApplicationTfm: "net8.0-windows10.0.19041",
                TfmsOfBeforeAndAfterLibrary: "net8.0;net8.0-windows10.0.19041",
                RxBefore: rxRef.RxBefore?.Acquisition ?? RxAcquiredVia.NoReference,
                RxUpgrade: rxRef.RxAfter.Acquisition,
                RxBeforeAndAfter: rxRef.RxBeforeAndAfter.Acquisition,
                BeforeLibrary: rxRef.RxBefore is AcquisitionAndIsOld rxBefore
                    ? (rxBefore.ReferencesNewRxVersion ? newLibrary : oldLibrary)
                    : null,
                BeforeAndAfterLibrary: rxRef.RxBeforeAndAfter.ReferencesNewRxVersion ? newLibrary : oldLibrary,
                AfterLibrary: rxRef.RxAfter.ReferencesNewRxVersion ? newLibrary : oldLibrary,
                rxUiUsage.AppUseRxUiFeaturesDirectly,
                rxUiUsage.AppInvokesLibraryCodePathsUsingRxUiFeatures);
    }
}

internal record AcquisitionAndIsOld(RxAcquiredVia Acquisition, bool ReferencesNewRxVersion);

internal record LibraryDetails(
    string Tfms,
    bool ReferencesNewRxVersion,
    bool HasWindowsTargetUsingUiFrameworkSpecificRxFeature);
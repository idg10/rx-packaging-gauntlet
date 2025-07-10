namespace CheckTransitiveFrameworkReference;

internal enum RxAcquiredVia
{
    NoReference,
    PackageReferenceInProjectOldRx,
    PackageReferenceInProjectNewRx,
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
    bool AppHasCodeUsingUiFrameworkSpecificRxDirectly,
    bool AppInvokesLibraryMethodThatUsesNonUiFrameworkSpecificRxFeature,
    bool AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature) // TODO: do we need before/after/both flavours of this?
{
    //private readonly static bool[] boolValues = [false, true];
    //private readonly static bool[] boolJustFalse = [false];
    private readonly static IEnumerable<bool> boolValues = [false, true];
    private readonly static IEnumerable<bool> boolJustFalse = [false];

    public static IEnumerable<Scenario> GetScenarios()
    {
        // Application dimensions.
        // There are essentially three of these.
        //
        // App dimension 1: Rx initially referenced directly by app csproj vs only referenced transitively
        // App dimension 2: latest version via csproj vs reference
        AppChoice[] rxRefDirectFromAppVsTransitive =
        [
            //// Commented out because we've tested these. Restore once finished testing later ones.
            ////// Dim 1: initially transitive reference to old
            ////// Dim 2: latest acquired by adding package ref to project
            ////new(
            ////    RxBeforeAndAfter: new(RxAcquiredVia.PackageTransitiveDependency, ReferencesNewRxVersion: false),
            ////    RxBefore: null,
            ////    RxAfter: new(RxAcquiredVia.PackageReferenceInProjectNewRx, ReferencesNewRxVersion: true)             
            ////),

            ////// Dim 1: initially transitive AND package reference to old
            ////// Dim 2: latest acquired by updating package ref in project
            ////new(
            ////    RxBeforeAndAfter: new(RxAcquiredVia.PackageTransitiveDependency, ReferencesNewRxVersion: false),
            ////    RxBefore:  new(RxAcquiredVia.PackageReferenceInProjectOldRx, ReferencesNewRxVersion: false),
            ////    RxAfter: new(RxAcquiredVia.PackageReferenceInProjectNewRx, ReferencesNewRxVersion: true)
            ////),

            // Dim 1: initially package reference to new, then we add a transitive ref to the old
            // Dim 2: direct ref to latest package in csproj (both before and after)
            // TODO: in cases where we have a new main (e.g., System.Reactive.Net) what do we do about
            // the legacy System.Reactive package here? In the Before case, where we were just using Rx.next
            // there would be no reason to reference the System.Reactive, so we shouldn't do that. We should
            // then look at what happens in after scenarios where we've done nothing besides adding the
            // package that gave us the legacy ref, and also what happens when we include the v.next
            // System.Reactive legacy package. (Would we also want to check whether the build issues a
            // warning advising you to add a reference to a newer version of the legacy package?)
            //
            // I'm starting to think that this AppChoice needs to include whether we have UseWpf/UseWindowsForms,
            // and also a more flexible before/after spec: it might need to be able to provide a list of what to
            // do there, and when we specify an Rx package reference direct from the app, that needs to be able
            // to say whether it's old or new, and whether it should include the legacy System.Reactive where
            // that's appropriate.
            new(
                RxBeforeAndAfter: new(RxAcquiredVia.PackageReferenceInProjectNewRx, ReferencesNewRxVersion: true),
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
        RxUsageChoices[] GetRxUsages(AppChoice appChoice)
        {
            IEnumerable<RxUsageChoices> ForAppRxUsage(bool appUseRxNonUiFeatures, bool appUseRxUiFeatures)
            {
                bool libAvailableBeforeAndAfter =
                    appChoice.RxBeforeAndAfter.Acquisition == RxAcquiredVia.PackageTransitiveDependency ||
                    ((appChoice.RxBefore?.Acquisition ?? RxAcquiredVia.NoReference) == RxAcquiredVia.PackageTransitiveDependency &&
                     appChoice.RxAfter.Acquisition == RxAcquiredVia.PackageTransitiveDependency);
                return
                    from libOffersUi in libAvailableBeforeAndAfter ? boolValues : boolJustFalse
                    from appInvokesLibUi in (libOffersUi ? boolValues : boolJustFalse)
                    select new RxUsageChoices(
                        LibraryWindowsTargetUsesRxUiFeatures: libOffersUi,
                        AppInvokesLibraryCodePathsUsingRxNonUiFeatures: libAvailableBeforeAndAfter,
                        AppInvokesLibraryCodePathsUsingRxUiFeatures: appInvokesLibUi,
                        AppUseRxNonUiFeaturesDirectly: appUseRxNonUiFeatures,
                        AppUseRxUiFeaturesDirectly: appUseRxUiFeatures);
            }

            return
                (from appUsesNonUiRxDirectly in boolValues
                 from appUseRxUiFeatures in boolValues
                 from usageChoice in ForAppRxUsage(appUsesNonUiRxDirectly, appUseRxUiFeatures)
                 select usageChoice)
                 .ToArray();
            ////[
            ////    new(LibraryWindowsTargetUsesRxUiFeatures: false, AppInvokesLibraryCodePathsUsingRxUiFeatures: false, AppUseRxUiFeaturesDirectly: false),
            ////    new(LibraryWindowsTargetUsesRxUiFeatures: true, AppInvokesLibraryCodePathsUsingRxUiFeatures: false, AppUseRxUiFeaturesDirectly: false),
            ////    new(LibraryWindowsTargetUsesRxUiFeatures: true, AppInvokesLibraryCodePathsUsingRxUiFeatures: true, AppUseRxUiFeaturesDirectly: false),

            ////    new(LibraryWindowsTargetUsesRxUiFeatures: false, AppInvokesLibraryCodePathsUsingRxUiFeatures: false, AppUseRxUiFeaturesDirectly: true),
            ////    new(LibraryWindowsTargetUsesRxUiFeatures: true, AppInvokesLibraryCodePathsUsingRxUiFeatures: false, AppUseRxUiFeaturesDirectly: true),
            ////    new(LibraryWindowsTargetUsesRxUiFeatures: true, AppInvokesLibraryCodePathsUsingRxUiFeatures: true, AppUseRxUiFeaturesDirectly: true),
            ////];
        }

        return
            from rxRef in rxRefDirectFromAppVsTransitive
            //from appUsesNonUiRxDirectly in boolValues
            //from rxUiUsage in rxUiUsages
            from rxUsage in GetRxUsages(rxRef)
            let oldLibrary = new LibraryDetails("net8.0;net8.0-windows10.0.19041", false, rxUsage.LibraryWindowsTargetUsesRxUiFeatures)
            let newLibrary = new LibraryDetails("net8.0;net8.0-windows10.0.19041", true, rxUsage.LibraryWindowsTargetUsesRxUiFeatures)
            select new Scenario(
                ApplicationTfm: "net8.0-windows10.0.19041",
                TfmsOfBeforeAndAfterLibrary: "net8.0;net8.0-windows10.0.19041",
                RxBefore: rxRef.RxBefore?.Acquisition ?? RxAcquiredVia.NoReference,
                RxUpgrade: rxRef.RxAfter.Acquisition,
                RxBeforeAndAfter: rxRef.RxBeforeAndAfter.Acquisition,
                BeforeLibrary: rxRef.RxBefore is AcquisitionAndIsOld rxBefore && rxBefore.Acquisition == RxAcquiredVia.PackageTransitiveDependency
                    ? (rxBefore.ReferencesNewRxVersion ? newLibrary : oldLibrary)
                    : null,
                BeforeAndAfterLibrary: rxRef.RxBeforeAndAfter.Acquisition == RxAcquiredVia.PackageTransitiveDependency
                    ? rxRef.RxBeforeAndAfter.ReferencesNewRxVersion ? newLibrary : oldLibrary
                    : null,
                AfterLibrary: rxRef.RxAfter.Acquisition == RxAcquiredVia.PackageTransitiveDependency
                    ? rxRef.RxAfter.ReferencesNewRxVersion ? newLibrary : oldLibrary
                    : null,
                AppHasCodeUsingNonUiFrameworkSpecificRxDirectly: rxUsage.AppUseRxNonUiFeaturesDirectly,
                AppHasCodeUsingUiFrameworkSpecificRxDirectly: rxUsage.AppUseRxUiFeaturesDirectly,
                AppInvokesLibraryMethodThatUsesNonUiFrameworkSpecificRxFeature: rxUsage.AppInvokesLibraryCodePathsUsingRxNonUiFeatures,
                AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature: rxUsage.AppInvokesLibraryCodePathsUsingRxUiFeatures);
    }

    private record AppChoice(
        AcquisitionAndIsOld RxBeforeAndAfter,
        AcquisitionAndIsOld? RxBefore,
        AcquisitionAndIsOld RxAfter);

    private record RxUsageChoices(
        bool LibraryWindowsTargetUsesRxUiFeatures,
        bool AppInvokesLibraryCodePathsUsingRxNonUiFeatures,
        bool AppInvokesLibraryCodePathsUsingRxUiFeatures,
        bool AppUseRxNonUiFeaturesDirectly,
        bool AppUseRxUiFeaturesDirectly);
}

internal record AcquisitionAndIsOld(RxAcquiredVia Acquisition, bool ReferencesNewRxVersion);

internal record LibraryDetails(
    string Tfms,
    bool ReferencesNewRxVersion,
    bool HasWindowsTargetUsingUiFrameworkSpecificRxFeature);
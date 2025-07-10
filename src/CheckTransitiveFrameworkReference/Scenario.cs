using OneOf;

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
    RxDependency[] RxDependenciesBefore,
    RxDependency[] RxDependenciesAfter,
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
                RxBefore:
                [
                    new NewRx(IncludeLegacyPackageWhereAvailable: false, IncludeUiPackages: false)
                ],
                RxAfter:
                [
                    new NewRx(IncludeLegacyPackageWhereAvailable: true, IncludeUiPackages: false),
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false)
                ]
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
                bool ReferencesLib(RxDependency[] deps) => deps
                    .Any(ac => ac.Match((DirectRxPackageReference _) => false, (TransitiveRxReferenceViaLibrary _) => true));

                bool libAvailableBefore = ReferencesLib(appChoice.RxBefore);
                bool libAvailableAfter = ReferencesLib(appChoice.RxAfter);
                bool libAvailableBeforeAndAfter = libAvailableBefore && libAvailableAfter;

                    //appChoice.RxBeforeAndAfter.Acquisition == RxAcquiredVia.PackageTransitiveDependency ||
                    //((appChoice.RxBefore?.Acquisition ?? RxAcquiredVia.NoReference) == RxAcquiredVia.PackageTransitiveDependency &&
                    // appChoice.RxAfter.Acquisition == RxAcquiredVia.PackageTransitiveDependency);
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
                RxDependenciesBefore: rxRef.RxBefore,
                RxDependenciesAfter: rxRef.RxAfter,
                AppHasCodeUsingNonUiFrameworkSpecificRxDirectly: rxUsage.AppUseRxNonUiFeaturesDirectly,
                AppHasCodeUsingUiFrameworkSpecificRxDirectly: rxUsage.AppUseRxUiFeaturesDirectly,
                AppInvokesLibraryMethodThatUsesNonUiFrameworkSpecificRxFeature: rxUsage.AppInvokesLibraryCodePathsUsingRxNonUiFeatures,
                AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature: rxUsage.AppInvokesLibraryCodePathsUsingRxUiFeatures);
    }

    private record AppChoice(
        RxDependency[] RxBefore,
        RxDependency[] RxAfter);

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

internal readonly record struct OldRx();
internal readonly record struct NewRx(
    bool IncludeLegacyPackageWhereAvailable,
    bool IncludeUiPackages);

internal readonly record struct TransitiveRxReferenceViaLibrary(
    string Tfms,
    bool ReferencesNewRxVersion,
    bool HasWindowsTargetUsingUiFrameworkSpecificRxFeature);

[GenerateOneOf]
internal partial class DirectRxPackageReference : OneOfBase<OldRx, NewRx>;

[GenerateOneOf]
internal partial class RxDependency : OneOfBase<DirectRxPackageReference, TransitiveRxReferenceViaLibrary>
{
    // Although the source generator generates conversions for each of the types we specify, it does
    // not appear to handle nesting. And although DirectRxPackageReference in turn has conversions to
    // and from OldRx and NewRx, C# only allows a single level of implicit conversion.
    //
    // I want to be able to use the constituent types of DirectRxPackageReference (OldRx and NewRx)
    // anywhere a RxDependency is required (just like I can use a TransitiveRxReferenceViaLibrary
    // anywhere a RxDependency is required, or like I can use either OldRx or NewRx anywhere a
    // DirectRxPackageReference is required). We enable this by defining conversions for those types
    // here.
    public static implicit operator RxDependency(OldRx _) => new DirectRxPackageReference(_);
    public static explicit operator OldRx(RxDependency _) => _.AsT0.AsT0;

    public static implicit operator RxDependency(NewRx _) => new DirectRxPackageReference(_);
    public static explicit operator NewRx(RxDependency _) => _.AsT0.AsT1;
}
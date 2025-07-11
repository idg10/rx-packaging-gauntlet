using OneOf;

using System.Diagnostics;

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
    bool DisableTransitiveFrameworkReferencesAfter,
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
        AppChoice[] appChoices =
        [
            //// Commented out because we've tested these. Restore once finished testing later ones.
            // Dim 1: initially transitive reference to old
            // Dim 2: latest acquired by adding package ref to project
            new(
                RxBefore:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false)
                ],
                RxAfter:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false),
                    new NewRx(IncludeLegacyPackageWhereAvailable: false, IncludeUiPackages: false),
                ]
            ),

            ////// Dim 1: initially transitive AND package reference to old
            ////// Dim 2: latest acquired by updating package ref in project
            ////new(
            ////    RxBeforeAndAfter: new(RxAcquiredVia.PackageTransitiveDependency, ReferencesNewRxVersion: false),
            ////    RxBefore:  new(RxAcquiredVia.PackageReferenceInProjectOldRx, ReferencesNewRxVersion: false),
            ////    RxAfter: new(RxAcquiredVia.PackageReferenceInProjectNewRx, ReferencesNewRxVersion: true)
            ////),

            ////// Dim 1: initially package reference to new, then we add a transitive ref to the old
            ////// Dim 2: direct ref to latest package in csproj (both before and after)
            //////
            ////// TODO: I'm starting to think that this AppChoice needs to include whether we have UseWpf/UseWindowsForms,
            ////// and also a more flexible before/after spec: it might need to be able to provide a list of what to
            ////// do there, and when we specify an Rx package reference direct from the app, that needs to be able
            ////// to say whether it's old or new, and whether it should include the legacy System.Reactive where
            ////// that's appropriate.

            ////// This models a scenario a developer will typically stumble into:
            //////  * already using new Rx
            //////  * adds reference to a library that uses old Rx
            ////// In designs where System.Reactive is no longer the main package, we expect compiler errors
            ////// if the main app code itself use using Rx, because we will now effectively have two
            ////// versions of Rx in scope. If the main app itself doesn't use Rx directly, then we don't
            ////// expect compiler errors—having two versions of Rx around is fine in that case, because
            ////// nobody anywhere is trying to compiler Rx code in the scopes where both versions are
            ////// available.
            ////// TODO: In either case, would we also want to check whether the build issues a warning advising
            ////// you to add a reference to a newer version of the legacy package?
            ////new(
            ////    RxBefore:
            ////    [
            ////        new NewRx(IncludeLegacyPackageWhereAvailable: false, IncludeUiPackages: false)
            ////    ],
            ////    RxAfter:
            ////    [
            ////        new NewRx(IncludeLegacyPackageWhereAvailable: false, IncludeUiPackages: false),
            ////        new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false)
            ////    ]
            ////),

            ////// This models the case where the developer stumbled into the preceding scenario, and got
            ////// compiler errors (which only happens when we're looking at a future Rx design that relegates
            ////// System.Reactive to a legacy package) but they then added a reference to the new version of
            ////// the legacy System.Reactive package to fix the compiler errors.
            ////// This fixes the compiler errors, but if the legacy System.Reactive package continues to cause
            ////// a framework reference to Microsoft.WindowsDesktop.App, we now get bloat in self-contained
            ////// deployments.
            ////new(
            ////    RxBefore:
            ////    [
            ////        new NewRx(IncludeLegacyPackageWhereAvailable: false, IncludeUiPackages: false)
            ////    ],
            ////    RxAfter:
            ////    [
            ////        new NewRx(IncludeLegacyPackageWhereAvailable: true, IncludeUiPackages: false),
            ////        new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false)
            ////    ]
            ////),
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
            // Work out whether the app has a reference to the library that uses Rx. (This determines
            // whether we emit code in the app that calls into that library.)
            bool ReferencesLib(RxDependency[] deps) => deps
                .Any(ac => ac.Match((DirectRxPackageReference _) => false, (TransitiveRxReferenceViaLibrary _) => true));

            bool libAvailableBefore = ReferencesLib(appChoice.RxBefore);
            bool libAvailableAfter = ReferencesLib(appChoice.RxAfter);
            bool libAvailableBeforeAndAfter = libAvailableBefore && libAvailableAfter;

            // Determine whether Rx UI features are available to the library that gives us a transitive
            // Rx reference (if such a library is referenced at all).
            bool ReferencesLibThatHasCouldUseRxUi(RxDependency[] deps) => deps
                .Any(ac => ac.Match(
                    (DirectRxPackageReference rx) => false,
                    (TransitiveRxReferenceViaLibrary t) =>
                    // Rx UI always available to old version.
                    (t.Tfms.Contains("-windows") && !t.ReferencesNewRxVersion)
                    // Currently, this check is set up so that when using the new Rx, we only build the library
                    // with references to the Rx UI framework packages if the library is going to use them.
                    // In principle, it would be possible for the library to refer to, say, System.Reactive.For.Wpf,
                    // but not use it. Since this doesn't seem like a useful scenario, we don't model it.
                    || t.HasWindowsTargetUsingUiFrameworkSpecificRxFeature));
            bool uiAvailableToLibInBefore = ReferencesLibThatHasCouldUseRxUi(appChoice.RxBefore);
            bool uiAvailableToLibInAfter = ReferencesLibThatHasCouldUseRxUi(appChoice.RxAfter);
            bool uiAvailableToLibInBeforeAndAfter = uiAvailableToLibInBefore && uiAvailableToLibInAfter;

            // This determines whether Rx's UI features are available as a result of the references we
            // have. (Note that even when Rx's UI features are available to the app as a result of an
            // Rx dependency acquired transitively via a library, that library won't necessarily be making
            // use of those Rx UI features.)
            bool ReferencesMakeRxUiAvailable(RxDependency[] deps) => deps
                .Any(ac => ac.Match(
                    (DirectRxPackageReference rx) => rx.Match((OldRx _) => true, (NewRx n) => n.IncludeUiPackages),
                    (TransitiveRxReferenceViaLibrary t) => t.HasWindowsTargetUsingUiFrameworkSpecificRxFeature));
            bool uiAvailableViaReferencesBefore = ReferencesMakeRxUiAvailable(appChoice.RxBefore);
            bool uiAvailableViaReferencesAfter = ReferencesMakeRxUiAvailable(appChoice.RxAfter);
            bool uiAvailableBeforeAndAfter = uiAvailableViaReferencesBefore && uiAvailableViaReferencesAfter;

            IEnumerable<RxUsageChoices> ForAppRxUsage(bool appUseRxNonUiFeatures, bool appUseRxUiFeatures)
            {
                return
                    from libOffersUi in uiAvailableToLibInBefore ? boolValues : boolJustFalse
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
                 from appUseRxUiFeatures in uiAvailableBeforeAndAfter ? boolValues : [false]
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

        bool ShouldTestDisableTransitiveFrameworksWorkaround(AppChoice appChoice)
        {
            // If the 'after' app continues to have a reference to the old System.Reactive (which happens in
            // some scenarios we test - perhaps the app doesn't use Rx directly, and ends up with transitive
            // references to both old and new Rx, and we don't want that always to mean that the app author
            // now has to take remedial action), then self-contained deployments will include the desktop
            // framework. We'd like to know if disabling transitive frameworks (the 'workaround') is effective
            // in this case. (I.e., does it stop bloat, and not cause any new problems.)
            // If we have a reference to the new System.Reactive, that typically means that the app author
            // found it necessary to do that to fix problems (or they're using some library that did that).
            // We haven't yet decided whether a new System.Reactive-as-legacy-package should still cause
            // an automatic dependency on the desktop framework, but since that's one design option, we
            // need to know if disabling transitive framework references can be used to prevent bloat.
            bool DependencyMayCauseImplicitDesktopFrameworkReference(RxDependency d) =>
                d.Match(
                    (DirectRxPackageReference rx) => rx.Match(
                        (OldRx _) => true,
                        (NewRx n) => n.IncludeLegacyPackageWhereAvailable),
                    (TransitiveRxReferenceViaLibrary t) => !t.ReferencesNewRxVersion);

            // Note that we are ignoring IncludeUiPackages because that means an explicit choice to do UI things,
            // at which point a self-contained deployment has to include the desktop framework.
            return appChoice.RxAfter.Any(DependencyMayCauseImplicitDesktopFrameworkReference);
        }

        return
            from appChoice in appChoices
                //from appUsesNonUiRxDirectly in boolValues
                //from rxUiUsage in rxUiUsages
            from rxUsage in GetRxUsages(appChoice)
            from disableTransitiveFrameworkReferences in (ShouldTestDisableTransitiveFrameworksWorkaround(appChoice) ? boolValues : [false])
            let oldLibrary = new LibraryDetails("net8.0;net8.0-windows10.0.19041", false, rxUsage.LibraryWindowsTargetUsesRxUiFeatures)
            let newLibrary = new LibraryDetails("net8.0;net8.0-windows10.0.19041", true, rxUsage.LibraryWindowsTargetUsesRxUiFeatures)
            select new Scenario(
                ApplicationTfm: "net8.0-windows10.0.19041",
                TfmsOfBeforeAndAfterLibrary: "net8.0;net8.0-windows10.0.19041",
                RxDependenciesBefore: appChoice.RxBefore,
                RxDependenciesAfter: appChoice.RxAfter,
                DisableTransitiveFrameworkReferencesAfter: disableTransitiveFrameworkReferences,
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
internal partial class DirectRxPackageReference : OneOfBase<OldRx, NewRx>
{
    // Would be nice if the code generator generated these, but it doesn't today.
    public bool IsOldRx => Match((OldRx _) => true, (NewRx _) => false);
    public bool IsNewRx => Match((OldRx _) => false, (NewRx _) => true);

    public bool TryGetOldRx(out OldRx oldRx)
    {
        return TryPickT0(out oldRx, out _);
    }

    public bool TryGetNewRx(out NewRx newRx)
    {
        return TryPickT1(out newRx, out _);
    }
}

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

    public bool IsTransitiveRxReferenceViaLibrary => Match((DirectRxPackageReference _) => false, (TransitiveRxReferenceViaLibrary _) => true);

    public bool IsOldRx => Match((DirectRxPackageReference d) => d.IsOldRx, (TransitiveRxReferenceViaLibrary _) => false);
    public bool IsNewRx => Match((DirectRxPackageReference d) => d.IsNewRx, (TransitiveRxReferenceViaLibrary _) => false);

    public bool TryGetNewRx(out NewRx newRx)
    {
        if (TryGetDirectRxPackageReference(out DirectRxPackageReference pr))
        {
            return pr.TryGetNewRx(out newRx);
        }
        newRx = default;
        return false;
    }

    public bool TryGetOldRx(out OldRx oldRx)
    {
        if (TryGetDirectRxPackageReference(out DirectRxPackageReference pr))
        {
            return pr.TryGetOldRx(out oldRx);
        }
        oldRx = default;
        return false;
    }

    public bool TryGetDirectRxPackageReference(out DirectRxPackageReference packageReference)
    {
        return TryPickT0(out packageReference, out _);
    }

    public bool TryGetTransitiveRxReferenceViaLibrary(out TransitiveRxReferenceViaLibrary transitive)
    {
        return TryPickT1(out transitive, out _);
    }
}
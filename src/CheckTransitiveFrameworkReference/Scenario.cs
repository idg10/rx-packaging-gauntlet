
#pragma warning disable IDE0350 // Use implicitly typed lambda - in OneOf matches, it's typically easier to understand with explicit parameter types

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
    bool DisableTransitiveFrameworkReferencesAfter,
    bool AppHasCodeUsingNonUiFrameworkSpecificRxDirectly,
    bool AppHasCodeUsingUiFrameworkSpecificRxDirectly,
    bool AppInvokesLibraryMethodThatUsesNonUiFrameworkSpecificRxFeature,
    bool AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature) // TODO: do we need before/after/both flavours of this?
{
    private readonly static bool[] boolValues = [false, true];
    private readonly static bool[] boolJustFalse = [false];

    public static IEnumerable<Scenario> GetScenarios()
    {
        // Application dimensions.
        // There are essentially three of these.
        //
        // App dimension 1: Rx initially referenced directly by app csproj vs only referenced transitively
        // App dimension 2: latest version via csproj vs reference
        AppChoice[] appChoices =
        [
            // Dim 1: initially transitive reference to old
            // Dim 2: latest acquired by adding package ref to project
            // If System.Reactive is relegated to being a legacy facade, there are actually three variations here:
            //  Just add a reference to new main Rx package
            //  Just add a reference to new legacy Rx package
            //  Add references to both
            // This is the 'Just add a reference to new main Rx package' version, and this corresponds to an
            // application that had happily (perhaps obliviously) been using a package that depends on Rx 6,
            // and the app developer now decides to use Rx in the main app, and adds a reference to the new
            // main rx package.
            // If System.Reactive is relegated to a legacy facade, this causes build errors, because there are now two
            // versions of Rx available: the old via System.Reactive v6, and the new via System.Reactive.Net v7.
            new(
                RxBefore:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false)
                ],
                RxAfter:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false),
                    new NewRx(LegacyPackageChoice: NewRxLegacyOptions.JustMain, IncludeUiPackages: false),
                ]
            ),
            // This is the 'Just add a reference to new legacy Rx package' version, and this corresponds to
            // a situation where an application had been using a package that depends on Rx 6, and wants to
            // upgrade it to use the latest Rx (either because they're getting a deprecation warning from the NuGet
            // package manager, or because they are trying to get rid of bloat) but doesn't add a reference to
            // the main version.
            // We won't expect build errors with this.
            new(
                RxBefore:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false)
                ],
                RxAfter:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false),
                    new NewRx(LegacyPackageChoice: NewRxLegacyOptions.JustLegacy, IncludeUiPackages: false),
                ]
            ),
            // This is the 'Add references to both' version, which will typically correspond to what an app developer does
            // next after trying the 'Just add a reference to new main Rx package' version and getting a build error.
            // Hopefully we'll be able to emit a build message suggesting that they want to add a reference to the new
            // System.Reactive, and if they try that, this is what they'll end up with.
            // We won't expect build errors with this.
            new(
                RxBefore:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false)
                ],
                RxAfter:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false),
                    new NewRx(LegacyPackageChoice: NewRxLegacyOptions.MainAndLegacy, IncludeUiPackages: false),
                ]
            ),
            // TODO: scenarios around using UI features?

            // Dim 1: initially transitive AND package reference to old
            // Dim 2: latest acquired by updating package ref in project
            // As with the preceding cases, if System.Reactive is relegated to being a legacy facade, there are actually
            // three variations here:
            //  Replace reference with new main Rx package  (System.Reactive v6 -> System.Reactive.Net v7)
            //  Update reference to new legacy Rx package (System.Reactive v6 -> System.Reactive v7)
            //  Update legacy package reference AND add new main (System.Reactive v6 ->
            //                                                     System.Reactive v7 + System.Reactive.Net v7)
            // This first one, in which we replace the app's reference with the new main package will hit build errors
            // because we end up with two versions of Rx in scope.
            new(
                RxBefore:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false),
                    new OldRx()
                ],
                RxAfter:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false),
                    new NewRx(NewRxLegacyOptions.JustMain, IncludeUiPackages: false),
                ]
            ),
            // This second one, in which we just upgrade the existing System.Reactive reference to v7 (which means our
            // app will now explicitly be using Rx via the legacy facade) shouldn't cause compile errors, but may result
            // in bloat. But it's also not the preferred way to use Rx. Code written against v7 shouldn't really be using
            // the legacy facade as its way of getting access to the Rx API.
            // (We may want to look into issuing a compiler diagnostic about that.)
            new(
                RxBefore:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false),
                    new OldRx()
                ],
                RxAfter:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false),
                    new NewRx(NewRxLegacyOptions.JustLegacy, IncludeUiPackages: false),
                ]
            ),
            // Here, we're upgrading the legacy System.Reactive and also using the new System.Reactive.Net. Since we're using
            // the new System.Reactive, its type forwarders make it clear that everything really lives in System.Reactive.Net,
            // and so we're back to having just one Rx.
            // There may be bloat issues with this depending on whether System.Reactive v7 continues to force a dependency
            // on the desktop framework.
            new(
                RxBefore:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false),
                    new OldRx()
                ],
                RxAfter:
                [
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false),
                    new NewRx(NewRxLegacyOptions.MainAndLegacy, IncludeUiPackages: false),
                ]
            ),

            // Dim 1: initially package reference to new, then we add a transitive ref to the old
            // Dim 2: direct ref to latest package in csproj (both before and after)
            //
            // TODO: I'm starting to think that this AppChoice needs to include whether we have UseWpf/UseWindowsForms,
            // and also a more flexible before/after spec: it might need to be able to provide a list of what to
            // do there, and when we specify an Rx package reference direct from the app, that needs to be able
            // to say whether it's old or new, and whether it should include the legacy System.Reactive where
            // that's appropriate.

            // This models a scenario a developer will typically stumble into:
            //  * already using new Rx
            //  * adds reference to a library that uses old Rx
            // In designs where System.Reactive is no longer the main package, we expect compiler errors
            // if the main app code itself use using Rx, because we will now effectively have two
            // versions of Rx in scope. If the main app itself doesn't use Rx directly, then we don't
            // expect compiler errors—having two versions of Rx around is fine in that case, because
            // nobody anywhere is trying to compiler Rx code in the scopes where both versions are
            // available.
            // TODO: In either case, would we also want to check whether the build issues a warning advising
            // you to add a reference to a newer version of the legacy package?
            new(
                RxBefore:
                [
                    new NewRx(LegacyPackageChoice: NewRxLegacyOptions.JustMain, IncludeUiPackages: false)
                ],
                RxAfter:
                [
                    new NewRx(LegacyPackageChoice: NewRxLegacyOptions.JustMain, IncludeUiPackages: false),
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false)
                ]
            ),

            // This models the case where the developer stumbled into the preceding scenario, and got
            // compiler errors (which only happens when we're looking at a future Rx design that relegates
            // System.Reactive to a legacy package) but they then added a reference to the new version of
            // the legacy System.Reactive package to fix the compiler errors.
            // This fixes the compiler errors, but if the legacy System.Reactive package continues to cause
            // a framework reference to Microsoft.WindowsDesktop.App, we now get bloat in self-contained
            // deployments.
            new(
                RxBefore:
                [
                    new NewRx(LegacyPackageChoice: NewRxLegacyOptions.JustMain, IncludeUiPackages: false)
                ],
                RxAfter:
                [
                    new NewRx(LegacyPackageChoice: NewRxLegacyOptions.MainAndLegacy, IncludeUiPackages: false),
                    new TransitiveRxReferenceViaLibrary("net8.0;net8.0-windows10.0.19041", ReferencesNewRxVersion: false, HasWindowsTargetUsingUiFrameworkSpecificRxFeature: false)
                ]
            ),
            // Variation in which instead of adding a reference to the new System.Reactive legacy package, they
            // *replace* the System.Reactive.Net package reference with a reference to the new System.Reactive
            // legacy package. (People who like to minize their package references may well do this because
            // the new System.Reactive legacy package depends on System.Reactive.Net, so you don't really need both)
            new(
                RxBefore:
                [
                    new NewRx(LegacyPackageChoice: NewRxLegacyOptions.JustMain, IncludeUiPackages: false)
                ],
                RxAfter:
                [
                    new NewRx(LegacyPackageChoice: NewRxLegacyOptions.JustLegacy, IncludeUiPackages: false),
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
            // Work out whether the app has a reference to the library that uses Rx. (This determines
            // whether we emit code in the app that calls into that library.)
            bool ReferencesLib(RxDependency[] deps) => deps
                .Any(ac => ac.Match((DirectRxPackageReference _) => false, (TransitiveRxReferenceViaLibrary _) => true));

            bool libAvailableBefore = ReferencesLib(appChoice.RxBefore);
            bool libAvailableAfter = ReferencesLib(appChoice.RxAfter);
            bool libAvailableBeforeAndAfter = libAvailableBefore && libAvailableAfter;

            // Determine whether Rx UI features are available to the library that gives us a transitive
            // Rx reference (if such a library is referenced at all).
            bool ReferenceIsLibThatHasCouldUseRxUi(RxDependency ac) => ac.Match(
                    (DirectRxPackageReference rx) => false,
                    (TransitiveRxReferenceViaLibrary t) =>
                    // Rx UI always available to old version.
                    (t.Tfms.Contains("-windows") && !t.ReferencesNewRxVersion)
                    // Currently, this check is set up so that when using the new Rx, we only build the library
                    // with references to the Rx UI framework packages if the library is going to use them.
                    // In principle, it would be possible for the library to refer to, say, System.Reactive.For.Wpf,
                    // but not use it. Since this doesn't seem like a useful scenario, we don't model it.
                    || t.HasWindowsTargetUsingUiFrameworkSpecificRxFeature);
            bool ReferencesLibThatHasCouldUseRxUi(RxDependency[] deps) => deps.Any(ReferenceIsLibThatHasCouldUseRxUi);
            bool uiAvailableToLibInBefore = ReferencesLibThatHasCouldUseRxUi(appChoice.RxBefore);
            bool uiAvailableToLibInAfter = ReferencesLibThatHasCouldUseRxUi(appChoice.RxAfter);
            bool uiAvailableToLibInBeforeAndAfter = uiAvailableToLibInBefore && uiAvailableToLibInAfter;

            // Determines whether the library will in fact offer a public API that uses UI-specific Rx features.
            bool ReferencesLibThatProvidesUiFeature(RxDependency[] deps) => deps
                .Any(ac => ReferenceIsLibThatHasCouldUseRxUi(ac) && ac.Match(
                    (DirectRxPackageReference rx) => false,
                    (TransitiveRxReferenceViaLibrary t) => t.HasWindowsTargetUsingUiFrameworkSpecificRxFeature));
            bool libProvidesUiFeatureInBefore = ReferencesLibThatProvidesUiFeature(appChoice.RxBefore);
            bool libProvidesUiFeatureInAfter = ReferencesLibThatProvidesUiFeature(appChoice.RxAfter);
            bool libProvidesUiFeatureInBeforeAndAfter = libProvidesUiFeatureInBefore && libProvidesUiFeatureInAfter;

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
                    from appInvokesLibUi in (libProvidesUiFeatureInBeforeAndAfter ? boolValues : boolJustFalse)
                    select new RxUsageChoices(
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
                        (NewRx n) => n.LegacyPackageChoice is not NewRxLegacyOptions.JustMain),
                    (TransitiveRxReferenceViaLibrary t) => !t.ReferencesNewRxVersion);

            // Note that we are ignoring IncludeUiPackages because that means an explicit choice to do UI things,
            // at which point a self-contained deployment has to include the desktop framework.
            return appChoice.RxAfter.Any(DependencyMayCauseImplicitDesktopFrameworkReference);
        }

        return
            from appChoice in appChoices
            from rxUsage in GetRxUsages(appChoice)
            from disableTransitiveFrameworkReferences in (ShouldTestDisableTransitiveFrameworksWorkaround(appChoice) ? boolValues : [false])
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
        bool AppInvokesLibraryCodePathsUsingRxNonUiFeatures,
        bool AppInvokesLibraryCodePathsUsingRxUiFeatures,
        bool AppUseRxNonUiFeaturesDirectly,
        bool AppUseRxUiFeaturesDirectly);
}

internal readonly record struct OldRx();

internal enum NewRxLegacyOptions
{
    JustMain,
    MainAndLegacy,
    JustLegacy,
}
internal readonly record struct NewRx(
    NewRxLegacyOptions LegacyPackageChoice,
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
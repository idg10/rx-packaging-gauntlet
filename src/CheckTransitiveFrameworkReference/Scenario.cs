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
    string TfmOfBeforeAndAfterLibrary,
    RxAcquiredVia RxBefore,
    RxAcquiredVia RxUpgrade,
    RxAcquiredVia RxBeforeAndAfter,
    LibraryDetails? BeforeLibrary,
    LibraryDetails? BeforeAndAfterLibrary,
    LibraryDetails? AfterLibrary,
    bool AppHasCodeUsingNonUiFrameworkSpecificRxDirectly,
    bool AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature); // TODO: do we need before/after/both flavours of this?

internal record LibraryDetails(
    string Tfms,
    bool ReferencesOldRxVersion,
    bool HasWindowsTargetUsingUiFrameworkSpecificRxFeature);
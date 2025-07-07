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
    RxAcquiredVia RxBefore,
    RxAcquiredVia RxUpgrade,
    RxAcquiredVia RxBeforeAndAfter,
    bool BeforeAndAfterLibraryIsOldVersion,
    bool AppHasCodeUsingNonUiFrameworkSpecificRxDirectly,
    bool BeforeLibraryHasWindowsTargetUsingUiFrameworkSpecificRxFeature,
    bool AppInvokesLibraryMethodThatUsesUiFrameworkSpecificRxFeature);

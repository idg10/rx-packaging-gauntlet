using RxGauntlet.Build;

namespace CheckIssue1745;

internal record Scenario(
    string BaseNetTfm,
    string WindowsVersion,
    bool? UseWpf,
    bool? UseWindowsForms,
    bool EmitDisableTransitiveFrameworkReferences,
    PackageIdAndVersion RxMainPackage,
    string? PackageSource);
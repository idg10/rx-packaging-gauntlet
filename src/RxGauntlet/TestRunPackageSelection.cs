using RxGauntlet.Build;

namespace RxGauntlet;

/// <summary>
/// An Rx.NET package selection for a test run.
/// </summary>
/// <param name="Packages">
/// One or more <see cref="Package"/> details. Some tests may require multiple packages, e.g., it may be necessary
/// to specify the main Rx package and also one or more UI-framework-specific pagkages. Where multiple packages
/// are specified, each entry in this list should have a different <see cref="Package.PackageId"/>.
/// </param>
/// <param name="CustomPackageSource">
/// The URL or local file path of a custom NuGet package source, or <c>null</c> when the public NuGet feed should be
/// used.
/// </param>
internal record TestRunPackageSelection(PackageIdAndVersion[] Packages, string? CustomPackageSource);
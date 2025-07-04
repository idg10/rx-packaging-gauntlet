using RxGauntlet.Build;

using Spectre.Console;
using Spectre.Console.Cli;

using System.ComponentModel;
using System.Diagnostics;

namespace RxGauntlet.CommandLine;

public class RxSourceSettings : CommandSettings
{
    private PackageIdAndVersion[]? _parsedRxPackages;

    [Description("The URL of an additional NuGet package source, or the file path of a local package store. (The public NuGet feed will remain available.)")]
    [CommandOption("--package-source")]
    public string? PackageSource { get; init; }

    [Description("Package (as PackageId,Version, e.g. System.Reactive.Net,7.0.0-preview.17.g58342773bd) to replace existing System.Reactive PackageReference")]
    [CommandOption("--rx-package")]
    public string[] RxPackages { get; init; } = [];

    public PackageIdAndVersion[] RxPackagesParsed
    {
        get
        {
            if (_parsedRxPackages is not null)
            {
                return _parsedRxPackages;
            }

            if (RxPackages.Length == 0)
            {
                _parsedRxPackages = [];
            }
            else
            {
                ValidationResult rxPackagesValidationResult = ValidateRxPackages(packageRequired: false);
                if (!rxPackagesValidationResult.Successful)
                {
                    throw new InvalidOperationException($"{nameof(RxPackages)} is invalid: {rxPackagesValidationResult.Message}");
                }

                Debug.Assert(_parsedRxPackages is not null, "RxPackagesParsed should have been set by ValidateRxPackages.");
            }

            return _parsedRxPackages;
        }
    }

    /// <summary>
    /// Validates the command line argument(s).
    /// </summary>
    /// <returns>A validation result.</returns>
    /// <remarks>
    /// This just calls <see cref="ValidateRxPackages(bool)"/> with <c>packageRequired</c> set to <c>true</c>.
    /// Derived classes can override this method if they do want to make the <c>--rx-package</c> argument optional.
    /// Individual repro projects typically won't do this, but the RxGauntlet tool does.
    /// </remarks>
    public override ValidationResult Validate()
    {
        return ValidateRxPackages(packageRequired: true);
    }

    /// <summary>
    /// Validates the <c>--rx-package</c> argument(s).
    /// </summary>
    /// <param name="packageRequired">
    /// True if at least one package must be specified. (Individual repro projects typically do this.)
    /// False if the tool allows no packages to be specified. (The RxGauntlet tool allows this because it
    /// offers a mode where it runs for all published Rx packages, which is mutually exclusive with the
    /// <c>--rx-package</c> argument.)
    /// </param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public ValidationResult ValidateRxPackages(bool packageRequired)
    {
        if (this.RxPackages is not string[] rxPackages)
        {
            // Spectre Console will never make this null, so this can only mean that we're being used in an
            // unsupported way (probably not via Spectre.Console) so we throw instead of reporting a validation
            // failure.
            throw new InvalidOperationException("RxPackages must not be null.");
        }

        if (this.RxPackages.Length == 0)
        {
            _parsedRxPackages = [];
            return packageRequired
                ? ValidationResult.Error("At least one --rx-package must be specified.")
                : ValidationResult.Success();
        }

        HashSet<string> packageIdsSeen = new(capacity: rxPackages.Length);
        var result = new PackageIdAndVersion[rxPackages.Length];
        for (int i = 0; i < rxPackages.Length; i++)
        {
            if (!PackageIdAndVersion.TryParse(rxPackages[i], out PackageIdAndVersion? packageIdAndVersion))
            {
                return ValidationResult.Error($"Invalid package specification: {rxPackages[i]}. Must be <PackageId>,<Version>");
            }
            if (!packageIdsSeen.Add(packageIdAndVersion.PackageId))
            {
                return ValidationResult.Error($"Duplicate package id: {packageIdAndVersion.PackageId}.");
            }
            result[i] = packageIdAndVersion;
        }

        _parsedRxPackages = result;
        return ValidationResult.Success();
    }
}

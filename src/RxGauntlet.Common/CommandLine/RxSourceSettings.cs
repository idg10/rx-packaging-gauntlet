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
                ValidationResult rxPackagesValidationResult = ValidateRxPackages();
                if (!rxPackagesValidationResult.Successful)
                {
                    throw new InvalidOperationException($"{nameof(RxPackages)} is invalid: {rxPackagesValidationResult.Message}");
                }

                Debug.Assert(_parsedRxPackages is not null, "RxPackagesParsed should have been set by ValidateRxPackages.");
            }

            return _parsedRxPackages;
        }
    }


    public override ValidationResult Validate()
    {
        return ValidateRxPackages();
    }

    private ValidationResult ValidateRxPackages()
    {
        if (this.RxPackages is not string[] rxPackages)
        {
            // Not a validation failure, because Spectre Console will never make this null. It can
            // only mean that we're being used in an unsupported way.
            throw new InvalidOperationException("RxPackages must not be null.");
        }

        if (this.RxPackages.Length == 0)
        {
            _parsedRxPackages = [];
            return ValidationResult.Success();
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

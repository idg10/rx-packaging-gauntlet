using Spectre.Console.Cli;

using System.ComponentModel;

namespace RxGauntlet.CommandLine;

public class RxSourceSettings : CommandSettings
{
    [Description("The URL of an additional NuGet package source, or the file path of a local package store. (The public NuGet feed will remain available.)")]
    [CommandOption("-ps|--package-source")]
    public string? PackageSource { get; init; }

    [Description("Package IDs of NuGet packages to add")]
    [CommandOption("-pids|--package-ids")]
    public required string[] PackageIds { get; init; }
}

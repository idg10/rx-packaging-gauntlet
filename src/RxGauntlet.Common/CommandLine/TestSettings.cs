using Spectre.Console.Cli;

using System.ComponentModel;

namespace RxGauntlet.CommandLine;

public class TestSettings : CommandSettings
{
    [Description("The output path to which to write the test results. Defaults to a file named for the test in the current working directory.")]
    [CommandOption("--output")]
    public string? OutputPath { get; init; }

    [Description("The timestamp value for the test run. Defaults to the current date and time. (Used when orchestrating multiple test runners in a single run)")]
    [CommandOption("--test-timestamp")]
    public string? TestTimestamp { get; init; }

    [Description("The id of this test run. Defaults to a generated GUID.")]
    [CommandOption("--test-run-id")]
    public string? TestRunId { get; init; }
}

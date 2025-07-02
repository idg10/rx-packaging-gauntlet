using RxGauntlet.CommandLine;

using Spectre.Console.Cli;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CheckIssue97;

internal class CheckPluginIssue97Command : TestCommandBase<TestSettings>
{
    private static readonly string[] netFxHostRuntimes = ["net462", "net472", "net481"];

    protected override string DefaultOutputFilename => "CheckPluginIssue97.json";

    protected override Task<int> ExecuteTestAsync(TestDetails testDetails, CommandContext context, TestSettings settings, Utf8JsonWriter jsonWriter)
    {
        throw new NotImplementedException();
    }
}

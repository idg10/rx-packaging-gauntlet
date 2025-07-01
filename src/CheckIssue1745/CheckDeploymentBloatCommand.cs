using RxGauntlet.CommandLine;
using RxGauntlet.LogModel;

using Spectre.Console.Cli;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckIssue1745;

internal class CheckDeploymentBloatCommand : AsyncCommand<RxSourceSettings>
{
    private static readonly string[] baseNetTfms =
    [
        "net6.0",
        "net8.0",
        "net9.0"
    ];

    private static readonly string[] windowsVersions =
    [
        "windows10.0.18362.0",
        "windows10.0.19041.0",
        "windows10.0.22000.0"
    ];

    private static readonly bool?[] boolsWithNull = [null, true, false];
    private static readonly bool[] bools = [true, false];

    public override async Task<int> ExecuteAsync(CommandContext context, RxSourceSettings settings)
    {
        string outputPath = settings.OutputPath ?? "CheckIssue1745.json";
        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        using (var jsonWriter = new System.Text.Json.Utf8JsonWriter(output))
        {
            jsonWriter.WriteStartArray();

            IEnumerable<Scenario> scenarios =
                from baseNetTfm in baseNetTfms
                from windowsVersion in windowsVersions
                from useWpf in boolsWithNull
                from useWindowsForms in boolsWithNull
                from useTransitiveFrameworksWorkaround in bools
                select new Scenario(baseNetTfm, windowsVersion, useWpf, useWindowsForms, useTransitiveFrameworksWorkaround, settings.RxPackagesParsed, settings.PackageSource);

            foreach (Scenario scenario in scenarios)
            {
                try
                {
                    Issue1745TestRun result = await RunDeploymentBloatCheck.RunAsync(scenario);
                    result.WriteTo(jsonWriter);
                    jsonWriter.Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error running scenario {scenario}: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }
            jsonWriter.WriteEndArray();
        }

        return 0;
    }
}

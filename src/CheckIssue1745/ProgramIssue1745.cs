
// Dimensions:
//  RX version (published or proposed)
//  TFM
//  Whether project uses WPF and/or Windows Forms
//  Whether the transitive frameworks workaround is in use.
// We should also check an application that doesn't actually use Rx, as a baseline for whether WPF and/or
// Windows Forms are included in the output.

using CheckIssue1745;

using Spectre.Console.Cli;

var app = new CommandApp<CheckDeploymentBloatCommand>();

app.Configure(config =>
{
    //config.AddCommand<CheckDeploymentBloatCommand>("check");
});

return await app.RunAsync(args);

////RxVersions[] rxVersions =
////[
////    RxVersions.Rx30,
////    RxVersions.Rx31,
////    RxVersions.Rx44,
////    RxVersions.Rx50,
////    RxVersions.Rx60,
////];



////using (var output = new FileStream("CheckIssue1745.json", FileMode.Create, FileAccess.Write, FileShare.Read))
////using (var jsonWriter = new System.Text.Json.Utf8JsonWriter(output))
////{
////    jsonWriter.WriteStartArray();
////    foreach (Scenario scenario in scenarios)
////    {
////        try
////        {
////            Issue1745TestRun result = await RunScenario(scenario);
////            result.WriteTo(jsonWriter);
////            jsonWriter.Flush();
////        }
////        catch (Exception ex)
////        {
////            Console.WriteLine($"Error running scenario {scenario}: {ex.Message}");
////            Console.WriteLine(ex.StackTrace);
////        }
////    }
////    jsonWriter.WriteEndArray();
////}
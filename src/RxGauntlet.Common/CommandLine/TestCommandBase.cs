using NodaTime;

using Spectre.Console.Cli;

using System.Globalization;
using System.Text.Json;

namespace RxGauntlet.CommandLine;

public abstract class TestCommandBase<TSettings> : AsyncCommand<TSettings>
    where TSettings : TestSettings
{
    protected abstract string DefaultOutputFilename { get; }

    public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        string testTimestampText = settings.TestTimestamp ?? DateTimeOffset.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
        string testRunId = settings.TestRunId ?? $"{testTimestampText}-{System.Security.Cryptography.RandomNumberGenerator.GetHexString(8)}";
        var testDateTime = OffsetDateTime.FromDateTimeOffset(DateTimeOffset.ParseExact(testTimestampText, "yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture));

        TestDetails testDetails = new(testDateTime, testRunId);
        string outputPath = settings.OutputPath ?? DefaultOutputFilename;

        using (FileStream output = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        using (Utf8JsonWriter jsonWriter = new(output))
        {
            return await this.ExecuteTestAsync(testDetails, context, settings, jsonWriter);
        }
    }

    protected abstract Task<int> ExecuteTestAsync(
        TestDetails testDetails,
        CommandContext context,
        TSettings settings,
        Utf8JsonWriter jsonWriter);

    protected record TestDetails(
        OffsetDateTime TestRunDateTime,
        string TestRunId);
}

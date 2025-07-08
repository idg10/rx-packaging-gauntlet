using Spectre.Console.Cli;

namespace RxGauntlet.Cli;

internal abstract class RxGauntletCommandBase<TSettings> : AsyncCommand<TSettings>
    where TSettings : CommandSettings, IOrchestrationCommandSettings
{
    protected abstract TestRunPackageSelection[] GetPackageSelection(TSettings settings);

    public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        TestRunPackageSelection[] packageSelections = GetPackageSelection(settings);

        TestType[] testTypes = TestType.All;

        DateTimeOffset testRunDateTime = DateTimeOffset.UtcNow;
        string outputFolder;
        if (settings.OutputDirectory is not null)
        {
            outputFolder = settings.OutputDirectory;
        }
        else
        {
            outputFolder = Path.Combine(
                AppContext.BaseDirectory,
                testRunDateTime.ToString("yyyy-MM-dd_HH-mm-ss"));
            Console.WriteLine($"Output folder: {outputFolder}");
        }
        string testId = settings.TestId ?? testRunDateTime.ToString("yyyy-MM-dd_HH-mm-ss-") + System.Security.Cryptography.RandomNumberGenerator.GetHexString(8);

        try
        {
            RunGauntlet runner = new(
                testTypes,
                packageSelections,
                outputFolder,
                testId);
            int result = await runner.RunAsync();

            return result;
        }
        catch (Exception x)
        {
            Console.Error.WriteLine($"An error occurred while running the tests: {x}");
            return 1;
        }
    }
}

using Spectre.Console.Cli;

namespace RxGauntlet;

internal sealed class RxGauntletCommand : AsyncCommand<RxGauntletCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RxGauntletCommandSettings settings)
    {
        TestRunPackageSelection[] packageSelections;
        if (settings.AllPublishedRx)
        {
            packageSelections =
            [
                new([new("System.Reactive.Linq", "3.0.0")], null),
                new([new("System.Reactive.Linq", "3.1.0")], null),
                new([new("System.Reactive", "4.4.1")], null),
                new([new("System.Reactive", "5.0.0")], null),
                new([new("System.Reactive", "6.0.1")], null),
            ];
        }
        else
        {
            packageSelections = [new TestRunPackageSelection(settings.RxPackagesParsed, settings.PackageSource)];
        }

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

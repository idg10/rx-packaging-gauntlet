using RxGauntlet.Build;

using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace RxGauntlet;

internal class RunGauntlet(
    TestType[] testTypes,
    TestRunPackageSelection[] packageSelections,
    string outputFolder,
    string testId)
{
#if DEBUG
    private const string Configuration = "Debug";
#else
    private const string Configuration = "Release";
#endif

    internal async Task<int> RunAsync()
    {
        if (Directory.Exists(outputFolder))
        {
            Console.Error.WriteLine($"Output folder {outputFolder} already exists. Each test run should create a new output folder.");
            return 1;
        }

        Directory.CreateDirectory(outputFolder);

        TransformManyBlock<TestType[], TestType> expandTestTypes = new(types => types);
        TransformManyBlock<TestType, TestTypeAndPackageSelection> expandPackageSelections = new(type => packageSelections
            .Select(packageSelection => new TestTypeAndPackageSelection(type, packageSelection)));

        ActionBlock<TestTypeAndPackageSelection> runTest = new(RunTestAsync);

        expandTestTypes.LinkTo(expandPackageSelections, new() {  PropagateCompletion = true });
        expandPackageSelections.LinkTo(runTest, new() { PropagateCompletion = true });

        expandTestTypes.Post(testTypes);
        expandTestTypes.Complete();
        await runTest.Completion.ConfigureAwait(false);

        return 0;
    }

    private async Task RunTestAsync(TestTypeAndPackageSelection typeAndPackageSelection)
    {
        Console.WriteLine(typeAndPackageSelection);

        TestType testType = typeAndPackageSelection.Type;
        string testRunnerExecutableFolder = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            $@"..\..\..\..\{testType.SrcFolderRelativePath}\bin\{Configuration}\net9.0\"));
        string testRunnerExecutablePath = Path.Combine(
            testRunnerExecutableFolder,
            testType.ExecutableName);

        string uiPackageArguments = string.Join(
            " ",
            typeAndPackageSelection.PackageSelection.RxUiPackages.Select(package =>
                $"--rx-package {package.PackageId},{package.Version} " +
                $""));

        uiPackageArguments = uiPackageArguments == "" ? "" : " " + uiPackageArguments; // Add space where necessary
        PackageIdAndVersion mainRxPackage = typeAndPackageSelection.PackageSelection.MainRxPackage;
        string packageArguments = $"--rx-main-package {mainRxPackage.PackageId},{mainRxPackage.Version}{uiPackageArguments}";
        packageArguments = typeAndPackageSelection.PackageSelection.LegacyRxPackage is PackageIdAndVersion legacyRxPackage
            ? $"{packageArguments} --rx-legacy-package {legacyRxPackage.PackageId},{legacyRxPackage.PackageId}"
            : packageArguments;
        string customFeedArgumentIfRequired = typeAndPackageSelection.PackageSelection.CustomPackageSource is string packageSource
            ? $" --package-source {packageSource}"
            : string.Empty;
        string testIdArgument = $" --test-id {testId}";

        string outputBaseName = Path.GetFileNameWithoutExtension(testType.OutputName);
        string outputExtension = Path.GetExtension(testType.OutputName);
        string outputForThisPackageSelection = $"{outputBaseName}-{typeAndPackageSelection.PackageSelection.MainRxPackage.PackageId}-{typeAndPackageSelection.PackageSelection.MainRxPackage.Version}{outputExtension}";
        string outputPath = Path.Combine(outputFolder, outputForThisPackageSelection);
        string outputArgument = $" --output {outputPath}";
        var startInfo = new ProcessStartInfo
        {
            FileName = testRunnerExecutablePath,
            //RedirectStandardOutput = true,
            UseShellExecute = false,
            //CreateNoWindow = true,
            Arguments = packageArguments + customFeedArgumentIfRequired + testIdArgument + outputArgument,
            WorkingDirectory = testRunnerExecutableFolder,
        };

        Console.WriteLine($"{startInfo.FileName} {startInfo.Arguments}");
        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();

            await process.WaitForExitAsync().ConfigureAwait(false);

            Console.WriteLine();
        }
        catch (Exception x)
        {
            Console.WriteLine(x);
            throw;
        }
    }

    private record TestTypeAndPackageSelection(TestType Type, TestRunPackageSelection PackageSelection);
}

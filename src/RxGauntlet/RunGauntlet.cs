// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

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

        var testType = typeAndPackageSelection.Type;
        var testRunnerExecutableFolder = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            $@"..\..\..\..\{testType.SrcFolderRelativePath}\bin\{Configuration}\net9.0\"));
        var testRunnerExecutablePath = Path.Combine(
            testRunnerExecutableFolder,
            testType.ExecutableName);

        var uiPackageArguments = string.Join(
            " ",
            typeAndPackageSelection.PackageSelection.RxUiPackages.Select(package =>
                $"--rx-package {package.PackageId},{package.Version} " +
                $""));

        uiPackageArguments = uiPackageArguments == "" ? "" : " " + uiPackageArguments; // Add space where necessary
        var mainRxPackage = typeAndPackageSelection.PackageSelection.MainRxPackage;
        var packageArguments = $"--rx-main-package {mainRxPackage.PackageId},{mainRxPackage.Version}{uiPackageArguments}";
        packageArguments = typeAndPackageSelection.PackageSelection.LegacyRxPackage is PackageIdAndVersion legacyRxPackage
            ? $"{packageArguments} --rx-legacy-package {legacyRxPackage.PackageId},{legacyRxPackage.Version}"
            : packageArguments;
        var customFeedArgumentIfRequired = typeAndPackageSelection.PackageSelection.CustomPackageSource is string packageSource
            ? $" --package-source {packageSource}"
            : string.Empty;
        var testIdArgument = $" --test-id {testId}";

        var outputBaseName = Path.GetFileNameWithoutExtension(testType.OutputName);
        var outputExtension = Path.GetExtension(testType.OutputName);
        var outputForThisPackageSelection = $"{outputBaseName}-{typeAndPackageSelection.PackageSelection.MainRxPackage.PackageId}-{typeAndPackageSelection.PackageSelection.MainRxPackage.Version}{outputExtension}";
        var outputPath = Path.Combine(outputFolder, outputForThisPackageSelection);
        var outputArgument = $" --output {outputPath}";
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

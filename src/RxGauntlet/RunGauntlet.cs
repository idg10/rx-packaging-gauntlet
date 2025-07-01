using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RxGauntlet;

internal class RunGauntlet
{
#if DEBUG
    private const string Configuration = "Debug";
#else
    private const string Configuration = "Release";
#endif

    internal async Task RunAsync(TestType[] testTypes, TestRunPackageSelection[] packageSelections)
    {
        string testRunDateTimeString = DateTime.UtcNow.ToString("s");
        string testRunId = Guid.NewGuid().ToString();

        TransformManyBlock<TestType[], TestType> expandTestTypes = new(types => types);
        TransformManyBlock<TestType, TestTypeAndPackageSelection> expandPackageSelections = new(type => packageSelections
            .Select(packageSelection => new TestTypeAndPackageSelection(type, packageSelection)));

        ActionBlock<TestTypeAndPackageSelection> runTest = new(RunTestAsync);

        expandTestTypes.LinkTo(expandPackageSelections);
        expandPackageSelections.LinkTo(runTest);

        expandTestTypes.Post(testTypes);
        expandTestTypes.Complete();
        await runTest.Completion;
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

        string packageArguments = string.Join(
            " ",
            typeAndPackageSelection.PackageSelection.Packages.Select(package => $"--rx-package {package.PackageId},{package.Version}"));
        string customFeedArgumentIfRequired = typeAndPackageSelection.PackageSelection.CustomPackageSource is string packageSource
            ? $" --package-source {packageSource}"
            : string.Empty;
        var startInfo = new ProcessStartInfo
        {
            FileName = testRunnerExecutablePath,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            //CreateNoWindow = true,
            Arguments = packageArguments + customFeedArgumentIfRequired,
            WorkingDirectory = testRunnerExecutableFolder,
        };

        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();

            await process.WaitForExitAsync();

        }
        catch (Exception x)
        {
            Console.WriteLine(x);
            throw;
        }
    }

    private record TestTypeAndPackageSelection(TestType Type, TestRunPackageSelection PackageSelection);
}

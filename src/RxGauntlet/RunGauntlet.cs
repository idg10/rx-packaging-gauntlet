using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RxGauntlet;

internal class RunGauntlet
{
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
        await Task.Yield();
    }

    private record TestTypeAndPackageSelection(TestType Type, TestRunPackageSelection PackageSelection);
}

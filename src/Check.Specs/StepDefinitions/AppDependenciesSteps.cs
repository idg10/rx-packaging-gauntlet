using CheckTransitiveFrameworkReference;
using CheckTransitiveFrameworkReference.ScenarioGeneration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Check.Specs.StepDefinitions;

[Binding]
public class AppDependenciesSteps
{
    private AppDependencies[]? dependenciesCombinations;
    
    private AppDependencies[] DependenciesCombinations => this.dependenciesCombinations
        ?? throw new InvalidOperationException("Dependencies combinations have not been initialized. Please call the appropriate Given step first.");

    [Given("I get all the App Dependency combinations for upgrading an old Rx reference acquired transitively")]
    public void GivenIGetAllTheAppDependencyCombinationsForUpgradingAnOldRxReferenceAcquiredTransitively()
    {
        this.dependenciesCombinations = AppDependenciesScenarioGeneration.Generate();
    }


    [Given("only the scenarios where Before App Dependencies are exactly")]
    public void GivenOnlyTheScenariosWhereBeforeAppDependenciesAreExactly(Table table)
    {
        ExpectedAppDependencyRow[] expectedDependencies =
            table.CreateSet<ExpectedAppDependencyRow>().ToArray();
        this.dependenciesCombinations = this.DependenciesCombinations
            .Where(dep => expectedDependencies.Any(expected => expected.Dependency.Equals(dep.BeforeAppDependencies)))
            .ToArray();
        //this.dependenciesCombinations = this.DependenciesCombinations
        //    .Where(dep => expectedDependencies.Any(expected => expected.Equals(dep)))
        //    .ToArray();
    }

    private bool DependenciesMatchExactly(
        ExpectedAppDependencyRow[] expected,
        RxDependency[] actual)
    {
        HashSet<ExpectedAppDependency> expectedSet = new(expected.Select(e => e.Dependency));
        HashSet<ExpectedAppDependency> actualSet = new(actual.Select(a => 
    }

    private ExpectedAppDependency ConvertRxDependencyToExpectedAppDependency(
        RxDependency dependency)
    {
        return dependency.Match(
            (DirectRxPackageReference rx) => rx.Match((OldRx _) => ExpectedAppDependency.OldRx));

    }

    private enum ExpectedAppDependency
    {
        LibUsingOldRx,
        OldRx,
        NewRxMain,
        NewRxLegacyFacade
    }

    private record ExpectedAppDependencyRow(ExpectedAppDependency Dependency);
}

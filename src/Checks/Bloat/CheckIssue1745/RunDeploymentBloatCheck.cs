// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

using Corvus.Json;

using NodaTime;

using RxGauntlet.Build;
using RxGauntlet.LogModel;

namespace CheckIssue1745;

internal class RunDeploymentBloatCheck
{
    public static async Task<Issue1745TestRun> RunAsync(string testRunId, OffsetDateTime testRunDateTime, Scenario scenario, string? packageSource)
    {
        Console.WriteLine(scenario);
        var tfm = scenario.WindowsVersion is string windowsVersion
            ? $"{scenario.BaseNetTfm}-{windowsVersion}"
            : scenario.BaseNetTfm;

        DirectoryInfo templateProjectFolder = new(
            Path.Combine(AppContext.BaseDirectory, "../../../../Bloat.ConsoleWinRtTemplate/"));


        using (var projectClone = ModifiedProjectClone.Create(
            templateProjectFolder.FullName,
            "CheckIssue1745",
            (projectFileRewriter) => RewriteProjectXmlDocument(
                projectFileRewriter,
                tfm,
                [scenario.RxMainPackage],
                scenario.UseWpf,
                scenario.UseWindowsForms,
                scenario.EmitDisableTransitiveFrameworkReferences),
                packageSource is not null ? [("loc", packageSource)] : null))
        {
            var buildResult = await projectClone.RunDotnetPublish("Bloat.ConsoleWinRtTemplate.csproj");

            (var includesWpf, var includesWindowsForms) = buildResult.CheckForUiComponentsInOutput();

            Console.WriteLine($"WPF: {includesWpf}");
            Console.WriteLine($"Windows Forms: {includesWindowsForms}");
            Console.WriteLine();

            // Note: currently this test run has no specialized config so the schema generation
            // doesn't create a type to represent issue1745TestRunConfig. That's why we use
            // the common TestRunConfigWithUiFrameworkSettings here.
            var rxVersionPackage = NuGetPackage.Create(
                id: scenario.RxMainPackage.PackageId,
                version: scenario.RxMainPackage.Version,
                packageSource: packageSource.AsNullableJsonString());
            var config = TestRunConfigWithUiFrameworkSettings.Create(
                baseNetTfm: scenario.BaseNetTfm,
                emitDisableTransitiveFrameworkReferences: scenario.EmitDisableTransitiveFrameworkReferences,
                // TODO: shouldn't we be capturing all packages, not just the first?
                // Also, really want to be sharing this code because all test types need to log this.
                rxVersion: rxVersionPackage,
                useWindowsForms: scenario.UseWindowsForms,
                windowsVersion: scenario.WindowsVersion,
                useWpf: scenario.UseWpf);
            if (scenario.WindowsVersion is string wv)
            {
                config = config.WithWindowsVersion(wv);
            }

            return Issue1745TestRun.Create(
                config: config,
                deployedWindowsForms: includesWindowsForms,
                deployedWpf: includesWpf,
                testRunDateTime: testRunDateTime,
                testRunId: testRunId);
        }
    }

    private static void RewriteProjectXmlDocument(
        ProjectFileRewriter project,
        string tfm,
        PackageIdAndVersion[] replaceSystemReactiveWith,
        bool? useWpf,
        bool? useWindowsForms,
        bool emitDisableTransitiveFrameworkReferences)
    {
        project.SetTargetFramework(tfm);
        project.ReplacePackageReference("System.Reactive", replaceSystemReactiveWith);
        project.AddUseUiFrameworksIfRequired(useWpf, useWindowsForms);

        if (emitDisableTransitiveFrameworkReferences)
        {
            project.AddPropertyGroup([new("DisableTransitiveFrameworkReferences", "True")]);
        }
    }
}

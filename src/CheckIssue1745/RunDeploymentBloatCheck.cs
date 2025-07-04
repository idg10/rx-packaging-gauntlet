using Corvus.Json;

using NodaTime;

using RxGauntlet.Build;
using RxGauntlet.LogModel;

namespace CheckIssue1745;

internal class RunDeploymentBloatCheck
{
    public static async Task<Issue1745TestRun> RunAsync(string testRunId, OffsetDateTime testRunDateTime, Scenario scenario, string? packageSource)
    {
        if (scenario.RxPackages is not [PackageIdAndVersion firstRxPackage, ..])
        {
            // This should be caught during command line parsing, so we don't expect this.
            throw new ArgumentException("scenario.RxPackages should not be empty");
        }

        Console.WriteLine(scenario);
        string tfm = scenario.WindowsVersion is string windowsVersion
            ? $"{scenario.BaseNetTfm}-{windowsVersion}"
            : scenario.BaseNetTfm;

        DirectoryInfo templateProjectFolder = new(
            Path.Combine(AppContext.BaseDirectory, "../../../../Bloat/Bloat.ConsoleWinRtTemplate/"));


        using (var projectClone = ModifiedProjectClone.Create(
            templateProjectFolder.FullName,
            "CheckIssue1745",
            (projectFileRewriter) => RewriteProjectXmlDocument(
                projectFileRewriter,
                tfm,
                scenario.RxPackages,
                scenario.UseWpf,
                scenario.UseWindowsForms,
                scenario.EmitDisableTransitiveFrameworkReferences),
                packageSource is not null ? [("loc", packageSource)] : null))
        {
            await projectClone.RunDotnetPublish("Bloat.ConsoleWinRtTemplate.csproj");
            string binFolder = Path.Combine(projectClone.ClonedProjectFolderPath, "bin");

            bool includesWpf = false;
            bool includesWindowsForms = false;
            foreach (string file in Directory.GetFiles(binFolder, "*", new EnumerationOptions { RecurseSubdirectories = true }))
            {
                string filename = Path.GetFileName(file);
                if (filename.Equals("PresentationFramework.dll", StringComparison.InvariantCultureIgnoreCase))
                {
                    includesWpf = true;
                }

                if (filename.Equals("System.Windows.Forms.dll", StringComparison.InvariantCultureIgnoreCase))
                {
                    includesWindowsForms = true;
                }
            }

            Console.WriteLine($"WPF: {includesWpf}");
            Console.WriteLine($"Windows Forms: {includesWindowsForms}");
            Console.WriteLine();

            // Note: currently this test run has no specialized config so the schema generation
            // doesn't create a type to represent issue1745TestRunConfig. That's why we use
            // the common TestRunConfig here.
            NuGetPackage rxVersionPackage = NuGetPackage.Create(
                id: firstRxPackage.PackageId,
                version: firstRxPackage.Version,
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

    static void RewriteProjectXmlDocument(
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

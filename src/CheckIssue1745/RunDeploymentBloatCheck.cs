using Corvus.Json;

using NodaTime;

using RxGauntlet.Build;
using RxGauntlet.LogModel;
using RxGauntlet.Xml;

using System.Xml;

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
            (projectFilePath, xmlDoc) => RewriteProjectXmlDocument(
                projectFilePath,
                tfm,
                scenario.RxPackages,
                scenario.UseWpf,
                scenario.UseWindowsForms,
                scenario.EmitDisableTransitiveFrameworkReferences,
                xmlDoc),
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
            var config = TestRunConfig.Create(
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
        string file,
        string tfm,
        PackageIdAndVersion[] replaceSystemReactiveWith,
        bool? useWpf,
        bool? useWindowsForms,
        bool emitDisableTransitiveFrameworkReferences,
        XmlDocument document)
    {
        XmlNode targetFrameworkNode = document.GetRequiredNode("/Project/PropertyGroup/TargetFramework");
        targetFrameworkNode.InnerText = tfm;

        XmlNode rxPackageRefNode = document.GetRequiredNode("/Project/ItemGroup/PackageReference[@Include='System.Reactive']");

        if (replaceSystemReactiveWith is [PackageIdAndVersion singleReplacement])
        {
            rxPackageRefNode.SetAttribute("Include", singleReplacement.PackageId);
            rxPackageRefNode.SetAttribute("Version", singleReplacement.Version);
        }
        else
        {
            // The command line arguments specified multiple packages to replace System.Reactive with,
            // so we remove the original PackageReference and add new ones.
            XmlNode packageRefItemGroup = rxPackageRefNode.ParentNode!;
            packageRefItemGroup.RemoveChild(rxPackageRefNode);

            foreach (PackageIdAndVersion packageIdAndVersion in replaceSystemReactiveWith)
            {
                XmlNode rxNewPackageRefNode = packageRefItemGroup.OwnerDocument!.CreateElement("PackageReference");
                rxNewPackageRefNode.SetAttribute("Include", packageIdAndVersion.PackageId);
                rxNewPackageRefNode.SetAttribute("Version", packageIdAndVersion.Version);
                packageRefItemGroup.AppendChild(rxNewPackageRefNode);
            }
        }

        if (useWpf.HasValue || useWindowsForms.HasValue)
        {
            XmlElement uiFrameworksPropertyGroup = document.CreateElement("PropertyGroup");

            if (useWpf.HasValue)
            {
                XmlElement useWpfElement = document.CreateElement("UseWPF");
                useWpfElement.InnerText = useWpf.Value.ToString();
                uiFrameworksPropertyGroup.AppendChild(useWpfElement);
            }

            if (useWindowsForms.HasValue)
            {
                XmlElement useWindowsFormsElement = document.CreateElement("UseWindowsForms");
                useWindowsFormsElement.InnerText = useWindowsForms.Value.ToString();
                uiFrameworksPropertyGroup.AppendChild(useWindowsFormsElement);
            }

            document.SelectSingleNode("/Project")!.AppendChild(uiFrameworksPropertyGroup);
        }

        if (emitDisableTransitiveFrameworkReferences)
        {
            // <PropertyGroup>
            //   <DisableTransitiveFrameworkReferences>true</DisableTransitiveFrameworkReferences>
            // </PropertyGroup>
            XmlElement transitiveWorkaroundPropertyGroup = document.CreateElement("PropertyGroup");
            XmlElement disableTransitiveFrameworkReferencesElement = document.CreateElement("DisableTransitiveFrameworkReferences");
            disableTransitiveFrameworkReferencesElement.InnerText = "True";
            transitiveWorkaroundPropertyGroup.AppendChild(disableTransitiveFrameworkReferencesElement);
            document.SelectSingleNode("/Project")!.AppendChild(transitiveWorkaroundPropertyGroup);
        }
    }
}

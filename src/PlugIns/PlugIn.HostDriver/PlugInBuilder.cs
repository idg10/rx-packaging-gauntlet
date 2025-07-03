using RxGauntlet.Build;

namespace PlugIn.HostDriver;

/// <summary>
/// Builds variations of the test plug-in for different target frameworks and Rx versions.
/// </summary>
public class PlugInBuilder : IDisposable
{
    private const string PlugInTempFolderName = "PlugInHost";
    private static readonly string PlugInTemplateProjectFolder = 
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../PlugIns/PlugIn"));

    private readonly Dictionary<PlugInDescriptor, GeneratedProject> _plugInProjects = new();

    public async Task<string> GetPlugInDllPathAsync(PlugInDescriptor plugInDescriptor)
    {
        if (!_plugInProjects.TryGetValue(plugInDescriptor, out GeneratedProject? project))
        {
            project = await CreateProjectForPlugIn(plugInDescriptor);
            _plugInProjects.Add(plugInDescriptor, project);
        }

        return Path.Combine(
            project.Project.ClonedProjectFolderPath,
            "bin",
            "Release",
            plugInDescriptor.TargetFrameworkMoniker,
            $"{project.AssemblyName}.dll");
    }

    private record GeneratedProject(ModifiedProjectClone Project, string AssemblyName);

    private async Task<GeneratedProject> CreateProjectForPlugIn(PlugInDescriptor plugInDescriptor)
    {
        // Give each distinct framework/rx version a different assembly name, because the
        // .NET Fx plug-in host will only ever load the first assembly with any particular name.
        string simplifiedRxVersion = plugInDescriptor.RxPackages[0].Version.Replace(".", "")[..2];
        string assemblyName = $"PlugIn.{plugInDescriptor.TargetFrameworkMoniker}.Rx{simplifiedRxVersion}";
        var projectClone = ModifiedProjectClone.Create(
            PlugInTemplateProjectFolder,
            PlugInTempFolderName,
            (project) =>
            {
                project.SetTargetFramework(plugInDescriptor.TargetFrameworkMoniker);
                project.AddPropertyGroup([new("AssemblyName", assemblyName)]);
                project.ReplacePackageReference("System.Reactive", plugInDescriptor.RxPackages);
                project.FixUpProjectReferences(PlugInTemplateProjectFolder);
            },
            plugInDescriptor.PackageSource is string packageSource ? [("loc", packageSource)] : null);

        await projectClone.RunDotnetBuild("PlugIn.csproj");
        return new GeneratedProject(projectClone, assemblyName);
    }

    public void Dispose()
    {
        foreach (GeneratedProject projectClone in _plugInProjects.Values)
        {
            projectClone.Project.Dispose();
        }
    }
}

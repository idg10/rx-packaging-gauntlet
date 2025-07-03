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

    private readonly Dictionary<PlugInDescriptor, ModifiedProjectClone> _plugInProjects = new();

    public async Task<string> GetPlugInDllPathAsync(PlugInDescriptor plugInDescriptor)
    {
        if (!_plugInProjects.TryGetValue(plugInDescriptor, out ModifiedProjectClone? project))
        {
            project = await CreateProjectForPlugIn(plugInDescriptor);
            _plugInProjects.Add(plugInDescriptor, project);
        }

        return Path.Combine(project.ClonedProjectFolderPath, "bin", "Release", plugInDescriptor.TargetFrameworkMoniker, $"PlugIn.dll");
    }

    private async Task<ModifiedProjectClone> CreateProjectForPlugIn(PlugInDescriptor plugInDescriptor)
    {
        var projectClone = ModifiedProjectClone.Create(
            PlugInTemplateProjectFolder,
            PlugInTempFolderName,
            (project) =>
            {
                project.SetTargetFramework(plugInDescriptor.TargetFrameworkMoniker);
                project.ReplacePackageReference("System.Reactive", plugInDescriptor.RxPackages);
                project.FixUpProjectReferences(PlugInTemplateProjectFolder);
            },
            plugInDescriptor.PackageSource is string packageSource ? [("loc", packageSource)] : null);

        await projectClone.RunDotnetBuild("PlugIn.csproj");
        return projectClone;
    }

    public void Dispose()
    {
        foreach (ModifiedProjectClone projectClone in _plugInProjects.Values)
        {
            projectClone.Dispose();
        }
    }
}

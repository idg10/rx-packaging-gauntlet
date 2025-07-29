// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

using RxGauntlet.Xml;

using System.Xml;

namespace RxGauntlet.Build;

public class ProjectFileRewriter
{
    private readonly XmlDocument _document = new();

    private ProjectFileRewriter(string template)
    {
        _document.Load(template);
    }

    public void SetTargetFramework(string targetFrameworkMoniker)
    {
        var targetFrameworkNode = _document.GetRequiredNode("/Project/PropertyGroup/TargetFramework");
        targetFrameworkNode.InnerText = targetFrameworkMoniker;
    }

    public void SetTargetFrameworks(string targetFrameworkMonikerList)
    {
        ReplaceProperty("TargetFrameworks", targetFrameworkMonikerList);
    }

    public void AddAssemblyNameProperty(string assemblyName)
    {
        AddPropertyGroup([new("AssemblyName", assemblyName)]);
    }

    public void ReplaceProperty(string propertyName, string newValue)
    {
        var propertyNode = _document.GetRequiredNode($"/Project/PropertyGroup/{propertyName}");
        propertyNode.InnerText = newValue;
    }

    public void ReplacePackageReference(string packageId, PackageIdAndVersion[] replacementPackages)
    {
        var packageRefNode = _document.GetRequiredNode($"/Project/ItemGroup/PackageReference[@Include='{packageId}']");

        if (replacementPackages is [PackageIdAndVersion singleReplacement])
        {
            packageRefNode.SetAttribute("Include", singleReplacement.PackageId);
            packageRefNode.SetAttribute("Version", singleReplacement.Version);
        }
        else
        {
            // We are to replace a single package reference with multiple package references
            // so we remove the original PackageReference and add new ones.
            ReplaceNodeWithPackageReferences(packageRefNode, replacementPackages);
        }
    }

    public void ReplaceProjectReferenceWithPackageReference(
        string targetCsProjNameWithoutDirectory,
        PackageIdAndVersion[] replacementPackages)
    {
        var projectRefNode = _document.GetRequiredNode($"/Project/ItemGroup/ProjectReference[contains(@Include, '{targetCsProjNameWithoutDirectory}')]");
        ReplaceNodeWithPackageReferences(projectRefNode, replacementPackages);
    }

    public void AddPropertyGroup(IEnumerable<KeyValuePair<string, string>> properties)
    {
        XmlNode propertyGroupNode = _document.CreateElement("PropertyGroup");
        foreach ((var name, var value) in properties)
        {
            XmlNode propertyNode = _document.CreateElement(name);
            propertyNode.InnerText = value;
            propertyGroupNode.AppendChild(propertyNode);
        }

        _document.SelectSingleNode("/Project")!.AppendChild(propertyGroupNode);
    }

    public void AddUseUiFrameworksIfRequired(bool? useWpf, bool? useWindowsForms)
    {
        if (useWpf.HasValue || useWindowsForms.HasValue)
        {
            List<KeyValuePair<string, string>> useUiFrameworksProperties = [];

            if (useWpf.HasValue)
            {
                useUiFrameworksProperties.Add(new("UseWPF", useWpf.Value.ToString()));
            }

            if (useWindowsForms.HasValue)
            {
                useUiFrameworksProperties.Add(new("UseWindowsForms", useWindowsForms.Value.ToString()));
            }

            AddPropertyGroup(useUiFrameworksProperties);
        }
    }

    internal void WriteModified(string destinationPath)
    {
        _document.Save(destinationPath);
    }

    private static void ReplaceNodeWithPackageReferences(
        XmlNode nodeToReplace,
        PackageIdAndVersion[] replacementPackages)
    {
        var packageRefItemGroup = nodeToReplace.ParentNode!;
        packageRefItemGroup.RemoveChild(nodeToReplace);

        foreach (var packageIdAndVersion in replacementPackages)
        {
            XmlNode rxNewPackageRefNode = packageRefItemGroup.OwnerDocument!.CreateElement("PackageReference");
            rxNewPackageRefNode.SetAttribute("Include", packageIdAndVersion.PackageId);
            rxNewPackageRefNode.SetAttribute("Version", packageIdAndVersion.Version);
            packageRefItemGroup.AppendChild(rxNewPackageRefNode);
        }
    }

    public static ProjectFileRewriter CreateForCsProj(string template)
    {
        return new ProjectFileRewriter(template);
    }

    public void FixUpProjectReferences(string templateProjectFolder)
    {
        if (_document.SelectNodes("/Project/ItemGroup/ProjectReference") is XmlNodeList projectReferences)
        {
            foreach (var projectReference in projectReferences.OfType<XmlElement>())
            {
                var relativePath = projectReference.GetAttribute("Include");
                var absolutePath = Path.GetFullPath(Path.Combine(templateProjectFolder, relativePath));
                projectReference.SetAttribute("Include", absolutePath);
            }
        }
    }
}

using Microsoft.Build.Construction;
using System.Xml.Linq;

namespace Tailwind.App;

public static class SdkHelper
{
    const string WebSdk = "Microsoft.NET.Sdk.Web";
    const string BlazorWasmSdk = "Microsoft.NET.Sdk.BlazorWebAssembly";

    public static ProjectFile? FindWebSdkProject(string directory)
    {
        var projects = FindWebSdkProjects(directory).ToList();

        // try to find web project first, then WASM if web not found
        var web = projects.SingleOrDefault(p => p.Type == ProjectType.Web)
                  ?? projects.SingleOrDefault(p => p.Type == ProjectType.Wasm);

        return web;
    }

    private static IEnumerable<ProjectFile> FindWebSdkProjects(string directory)
    {
        var csprojFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories);
        foreach (var file in csprojFiles)
        {
            var doc = XDocument.Load(file);
            var sdkAttribute = doc.Root?.Attribute("Sdk");
            // TODO: find assembly name if it is set
            var assemblyName = GetAssemblyName(file);
            var dotnetVersion = GetDotnetVersion(file);

            // .NET 8.0 has this for the primary Web project
            if (sdkAttribute != null &&
                sdkAttribute.Value.Equals(WebSdk, StringComparison.OrdinalIgnoreCase))
            {
                yield return new ProjectFile(ProjectType.Web, file, assemblyName, dotnetVersion);
            }

            // .NET 8.0 has this for the .Client project. Older versions use this for standalone WASM projects.
            if (sdkAttribute != null && sdkAttribute.Value.Equals(BlazorWasmSdk,
                    StringComparison.OrdinalIgnoreCase))
            {
                yield return new ProjectFile(ProjectType.Wasm, file, assemblyName, dotnetVersion);
            }
        }
    }

    private static string GetDotnetVersion(string path)
    {
        var defaultVersion = "8.0";
        var root = ProjectRootElement.Open(path);
        var groups = root.PropertyGroups;
        var ele = groups.FirstOrDefault(p => p.Children.Any(c => c.ElementName == "TargetFramework"));
        if (ele == null)
        {
            return defaultVersion;
        }

        var element = ele.Children.FirstOrDefault(p => p.ElementName == "TargetFramework");
        var propertyElement = element as ProjectPropertyElement;
        return propertyElement?.Value?.Replace("net", "") ?? defaultVersion;
    }

    private static string GetAssemblyName(string path)
    {
        var defaultAssemblyName = Path.GetFileNameWithoutExtension(path);
        var root = ProjectRootElement.Open(path);
        var groups = root.PropertyGroups;
        var ele = groups.FirstOrDefault(p => p.Children.Any(c => c.ElementName == "AssemblyName"));
        if (ele == null)
        {
            return defaultAssemblyName;
        }

        var assemblyNameElement = ele.Children.FirstOrDefault(p => p.ElementName == "AssemblyName");
        var assemblyName = assemblyNameElement as ProjectPropertyElement;
        return assemblyName?.Value ?? defaultAssemblyName;
    }

    public enum ProjectType
    {
        NotSet,
        Web,
        Wasm
    }

    public record ProjectFile(ProjectType Type, string Path, string AssemblyName, string DotnetVersion);
}


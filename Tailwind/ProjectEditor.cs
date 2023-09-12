using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging;

namespace Tailwind;

public class ProjectEditor
{
    public const string TailwindTarget = "Tailwind";
    public const string InstallTailwindTarget = "InstallTailwind";

    private readonly ILogger _log;
    public ProjectEditor(ILogger log)
    {
        _log = log;
    }

    public Project LoadProject(CancellationToken cancel)
    {
        var file = FindProjectFile(cancel)!;
        var collection = new ProjectCollection();
        var project = collection.LoadProject(file);
        return project;
    }

    public void AddTargetIfNotExists(Project project, string targetName)
    {
        project.Targets.TryGetValue(targetName, out var target);
        if (target != null)
        {
            _log.LogInformation("{targetName} already added as a target", targetName);
        }
        else
        {
            if (targetName == TailwindTarget)
            {
                AddTailwindTarget(project);
            }
            if (targetName == InstallTailwindTarget)
            {
                AddTailwindInstallerTarget(project);
            }
        }
    }

    public void AddTailwindInstallerTarget(Project project)
    {
        SetWindowsInstaller(project);
        SetLinuxInstaller(project);
        SetMacInstaller(project);

        var target = project.Xml.AddTarget(InstallTailwindTarget);
        target.AfterTargets = "AfterBuild";
        var task = target.AddTask("Message");
        task.SetParameter("Importance", "high");
        task.SetParameter("Text", "Installing Tailwind CLI...");

        var download = target.AddTask("DownloadFile");
        download.SetParameter("SkipUnchangedFiles", "true");
        download.SetParameter("SourceUrl", "$(TailwindInstallUrl)");
        download.SetParameter("DestinationFileName", "$(TailwindCliName)");
        download.SetParameter("DestinationFolder", "$(MSBuildProjectDirectory)");
        project.Save();
    }

    public void AddTailwindTarget(Project project)
    {
        var target = project.Xml.AddTarget(TailwindTarget);
        target.AfterTargets = "AfterBuild";
        target.DependsOnTargets = "InstallTailwind";
        var task = target.AddTask("Message");
        task.SetParameter("Importance", "high");
        task.SetParameter("Text", "Building css with Tailwind...");

        var exec = target.AddTask("Exec");
        exec.SetParameter("Command", ".\\tailwindcss -i .\\app.css -o .\\wwwroot\\css\\site.css");
        project.Save();
    }

    private void SetWindowsInstaller(Project project)
    {
        var group = project.Xml.AddPropertyGroup();
        group.Condition = "$([MSBuild]::IsOSPlatform('Windows'))";
        const string url = "https://github.com/tailwindlabs/tailwindcss/releases/download/v3.3.3/tailwindcss-windows-x64.exe";

        group.AddProperty("TailwindInstallUrl", url);
        group.AddProperty("TailwindCliName", "tailwindcss.exe");
    }

    private void SetLinuxInstaller(Project project)
    {
        var group = project.Xml.AddPropertyGroup();
        group.Condition = "$([MSBuild]::IsOSPlatform('Linux'))";
        const string url = "https://github.com/tailwindlabs/tailwindcss/releases/download/v3.3.3/tailwindcss-linux-x64";

        group.AddProperty("TailwindInstallUrl", url);
        group.AddProperty("TailwindCliName", "tailwindcss");
    }

    private void SetMacInstaller(Project project)
    {
        var group = project.Xml.AddPropertyGroup();
        group.Condition = "$([MSBuild]::IsOSPlatform('OSX'))";
        const string url = "https://github.com/tailwindlabs/tailwindcss/releases/download/v3.3.3/tailwindcss-macos-x64";

        group.AddProperty("TailwindInstallUrl", url);
        group.AddProperty("TailwindCliName", "tailwindcss");
    }
    private string FindProjectFile(CancellationToken cancel)
    {
        var files = Directory.EnumerateFiles(".", "*.csproj");
        if (files.Count() == 0)
        {
            _log.LogError("No .csproj file found in {dir}", Directory.GetCurrentDirectory());
            Environment.Exit(1);
            return null;
        }

        if (files.Count() == 1)
        {
            var file = files.First();
            _log.LogInformation("Found project file {file}", file);
            return file;
        }

        _log.LogError("Multiple .csproj files detected");
        Environment.Exit(1);
        return null;
    }
}
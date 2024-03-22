using System.Text;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Spectre.Console;
using Tailwind.Templates;

namespace Tailwind.App;

public class TailwindConfigurationService(IAnsiConsole console, Project project, TailwindSettings settings)
{
    public static TailwindConfigurationService Create(IAnsiConsole console, TailwindSettings settings)
    {
        var cts = new CancellationTokenSource();
        var projectService = new ProjectService(console);
        var project = projectService.LoadProject(settings, cts.Token);
        return new TailwindConfigurationService(console, project, settings);
    }

    private string GetInstallerUrl()
    {
        var installUrlRoot = $"https://github.com/tailwindlabs/tailwindcss/releases/download/v{settings.Version}";
        return installUrlRoot;
    }

    public async Task CreateTailwindConfigIfNotExists()
    {
        var path = GetNormalizedPath("tailwind.config.js");
        if (!File.Exists(path))
        {
            console.MarkupLineInterpolated($"{path} not found, creating one for you...");
            await CreateFile(path, TailwindConfigTemplates.Basic);
        }
    }

    public async Task CreateBaseCssIfNotExists()
    {
        var path = GetNormalizedPath("tailwind.css");
        if (!File.Exists(path))
        {
            console.MarkupLineInterpolated($"{path} not found, creating one for you...");
            const string contents = "@tailwind base;\n@tailwind components;\n@tailwind utilities;";
            await CreateFile(path, contents);
        }
    }

    /// <summary>
    /// Remove files added by this tool
    /// </summary>
    /// <param name="apply">What-if analysis, no actions applied</param>
    /// <returns>The files that were removed (or would be removed).</returns>
    public async Task<List<string>> RemovePluginFiles(bool apply)
    {
        var files = await GetPluginFiles();
        List<string> filesToRemove = [];
        filesToRemove.AddRange(files.Where(File.Exists));

        if (!apply)
        {
            return filesToRemove;
        }

        foreach (var file in filesToRemove)
        {
            File.Delete(file);
            AnsiConsole.MarkupLineInterpolated($"[bold maroon]Deleted file[/] {Path.GetFileName(file)}");
        }

        return filesToRemove;
    }

    private Task<List<string>> GetPluginFiles()
    {
        List<string> files =
        [
            GetNormalizedPath("tailwind.config.js"),
            GetNormalizedPath("tailwind.css"),
            GetNormalizedPath("tailwindcss.exe"),
            GetNormalizedPath("tailwindcss"),
            GetNormalizedPath("wwwroot/css/site.css")
        ];

        return Task.FromResult(files);
    }

    private Task<List<BuildTask>> GetBuildTasks()
    {
        var permissionTask = new BuildTask("Tailwind:Permission", "Making Tailwind CLI executable", TaskType.Exec, [
                new("Command", "chmod +x $(TailwindExecutable)")
            ],
            Platforms: [new OsPlatform("Linux"), new OsPlatform("OSX"), new OsPlatform("OSX", "arm64")]);

        List<TargetParameter> baseInstallParameters =
        [
            new("SkipUnchangedFiles", "true"),
            new("DestinationFolder", "$(MSBuildProjectDirectory)")
        ];

        // todo: there has to be a better way to do this
        var installTaskWindows = new BuildTask(
            "Tailwind:Install",
            "Installing Tailwind CLI",
            TaskType.Download,
            [.. baseInstallParameters, new("SourceUrl", $"{GetInstallerUrl()}/tailwindcss-windows-x64.exe")],
            Platforms: [new OsPlatform("Windows")]);

        var installTaskLinux = installTaskWindows with
        {
            Name = "Tailwind:InstallLinux",
            Platforms = [new OsPlatform("Linux")],
            Parameters = [.. baseInstallParameters, new("SourceUrl", $"{GetInstallerUrl()}/tailwindcss-linux-x64")]
        };

        var installTaskMac = installTaskWindows with
        {
            Name = "Tailwind:InstallMac",
            Platforms = [new OsPlatform("OSX")],
            Parameters = [.. baseInstallParameters, new("SourceUrl", $"{GetInstallerUrl()}/tailwindcss-macos-x64")]
        };

        var installTaskMacArm = installTaskWindows with
        {
            Name = "Tailwind:InstallMacArm",
            Platforms = [new OsPlatform("OSX", "arm64")],
            Parameters = [.. baseInstallParameters, new("SourceUrl", $"{GetInstallerUrl()}/tailwindcss-macos-arm64")]
        };
        
        var cssTask = new BuildTask(
            "Tailwind:Run",
            "Building CSS with Tailwind",
            TaskType.Exec,
            [new("Command", "$(TailwindExecutable) -i .\\tailwind.css -o .\\wwwroot\\css\\site.css")],
            DependsOnTask: permissionTask);

        return Task.FromResult<List<BuildTask>>([installTaskWindows, installTaskLinux, installTaskMac, installTaskMacArm, permissionTask, cssTask]);
    }

    public async Task RemoveBuildTasks()
    {
        var tasks = await GetBuildTasks();
        foreach (var target in tasks
                     .Select(buildTask => project.Xml.Targets.FirstOrDefault(p => p.Name == buildTask.Name))
                     .OfType<ProjectTargetElement>())
        {
            target.RemoveAllChildren();
        }

        project.Save();
    }

    private string GetNormalizedPath(string path)
    {
        return Path.Combine(project.DirectoryPath, path);
    }

    private async Task CreateFile(string path, string contents)
    {
        await using var stream = File.Create(path);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(contents));
        await stream.FlushAsync();
    }

    public async Task AddBuildTasks()
    {
        var tasks = await GetBuildTasks();
        foreach (var buildTask in tasks)
        {
            var target = project.Xml.Targets.FirstOrDefault(p => p.Name == buildTask.Name);
            if (target == null)
            {
                target = project.Xml.AddTarget(buildTask.Name);
            }
            else
            {
                // reset the target so we can just call init over and over again
                target.RemoveAllChildren();
            }

            target.AfterTargets = "AfterBuild";
            if (buildTask.DependsOnTask != null)
            {
                target.DependsOnTargets = buildTask.DependsOnTask.Name;
            }

            var task = target.AddTask("Message");
            task.SetParameter("Importance", "high");
            task.SetParameter("Text", buildTask.Description);

            var taskType = buildTask.TaskType switch
            {
                TaskType.Download => "DownloadFile",
                TaskType.Exec => "Exec",
                TaskType.Message => "Message",
                _ => throw new ArgumentOutOfRangeException(nameof(buildTask.TaskType), buildTask.TaskType, null)
            };

            var taskElement = target.AddTask(taskType);
            foreach (var parameter in buildTask.Parameters)
            {
                taskElement.SetParameter(parameter.Key, parameter.Value);
            }

            if (buildTask.Platforms != null && buildTask.Platforms.Any())
            {
                var condition = $"($([MSBuild]::IsOSPlatform('{buildTask.Platforms.First().Name}')) AND '$([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture)' == '{buildTask.Platforms.First().Arch.ToUpper()}')";
                condition = buildTask.Platforms.Skip(1)
                    .Aggregate(condition, (current, platform) => current + $" OR ($([MSBuild]::IsOSPlatform('{platform.Name}')) AND '$([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture)' == '{platform.Arch.ToUpper()}')");

                taskElement.Condition = condition;
            }

            if (buildTask.TaskType == TaskType.Download)
            {
                taskElement.AddOutputProperty("DownloadedFile", "TailwindExecutable");
            }
        }

        project.Save();
    }
}

public record BuildTask(string Name, string Description, TaskType TaskType, List<TargetParameter> Parameters, BuildTask? DependsOnTask = null, List<OsPlatform>? Platforms = null);
public record OsPlatform(string Name, string Arch = "x64");
public enum TaskType
{
    Message,
    Exec,
    Download
}
public record TargetParameter(string Key, string Value);
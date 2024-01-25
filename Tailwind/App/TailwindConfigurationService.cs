using System.Text;
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
            var contents = "@tailwind base;\n@tailwind components;\n@tailwind utilities;";
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
        foreach (var file in files)
        {
            if (File.Exists(file))
            {
                filesToRemove.Add(file);
            }
        }

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

    public async Task<List<string>> GetPluginFiles()
    {
        List<string> files =
        [
            GetNormalizedPath("tailwind.config.js"),
            GetNormalizedPath("tailwind.css"),
            GetNormalizedPath("tailwindcss.exe"),
            GetNormalizedPath("tailwindcss"),
            GetNormalizedPath("wwwroot/css/site.css")
        ];

        return files;
    }

    public async Task<List<BuildTask>> GetBuildTasks()
    {
        var permissionTask = new BuildTask("Tailwind:Permission", "Building CSS with Tailwind", TaskType.Exec, [
                new("Command", "$(TailwindExecutable) -i .\\tailwind.css -o .\\wwwroot\\css\\site.css")
            ],
            Platform: "Linux");

        List<TargetParameter> baseInstallParameters =
        [
            new("SkipUnchangedFiles", "true"),
            new("DestinationFolder", "$(MSBuildProjectDirectory)")
        ];

        // todo: support arm64
        // todo: there has to be a better way to do this
        var installTaskWindows = new BuildTask(
            "Tailwind:Install",
            "Installing Tailwind CLI",
            TaskType.Download,
            [.. baseInstallParameters, new("SourceUrl", $"{GetInstallerUrl()}/tailwindcss-windows-x64.exe")],
            Platform: "Windows");

        var installTaskLinux = installTaskWindows with
        {
            Name = "Tailwind:InstallLinux",
            Platform = "Linux",
            Parameters = [.. baseInstallParameters, new("SourceUrl", $"{GetInstallerUrl()}/tailwindcss-linux-x64")]
        };

        var installTaskMac = installTaskWindows with
        {
            Name = "Tailwind:InstallMac",
            Platform = "MacOS",
            Parameters = [.. baseInstallParameters, new("SourceUrl", $"{GetInstallerUrl()}/tailwindcss-macos-x64")]
        };

        var cssTask = new BuildTask(
            "Tailwind:Run",
            "Building CSS with Tailwind",
            TaskType.Exec,
            [new("Command", "$(TailwindExecutable) -i .\\tailwind.css -o .\\wwwroot\\css\\site.css")],
            DependsOnTask: permissionTask);

        return [installTaskWindows, installTaskLinux, installTaskMac, permissionTask, cssTask];
    }

    public async Task RemoveBuildTasks()
    {
        var tasks = await GetBuildTasks();
        foreach (var buildTask in tasks)
        {
            var target = project.Xml.Targets.FirstOrDefault(p => p.Name == buildTask.Name);
            if (target != null)
            {
                target.RemoveAllChildren();
            }
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

            if (buildTask.Platform != null)
            {
                target.Condition = $"$([MSBuild]::IsOSPlatform('{buildTask.Platform}'))";
            }

            if (buildTask.TaskType == TaskType.Download)
            {
                taskElement.AddOutputProperty("DownloadedFile", "TailwindExecutable");
            }
        }

        project.Save();
    }
}

public record BuildTask(string Name, string Description, TaskType TaskType, List<TargetParameter> Parameters, BuildTask? DependsOnTask = null, string? Platform = null);
public enum TaskType
{
    Message,
    Exec,
    Download
}
public record TargetParameter(string Key, string Value);
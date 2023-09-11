using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging;

namespace Tailwind;

public class ProjectEditor
{
    private readonly ILogger _log;
    public ProjectEditor(ILogger log)
    {
        _log=log;
    }
    public Project LoadProject(CancellationToken cancel)
    {
        var file = FindProjectFile(cancel)!;
        var collection = new ProjectCollection();
        var project = collection.LoadProject(file);
        return project;
    }

    public void AddTargetIfNotExists(Project project)
    {
        const string TargetName = "Tailwind";
        project.Targets.TryGetValue(TargetName, out var target);
        if (target != null)
        {
            _log.LogInformation("{targetName} already added as a target", TargetName);
        }
        else
        {
            AddTarget(project);
        }
    }

    public void AddTarget(Project project)
    {
        var target = project.Xml.AddTarget("Tailwind");
        target.AfterTargets = "AfterBuild";
        var task = target.AddTask("Message");
        task.SetParameter("Text", "Building css with Tailwind...");

        var exec = target.AddTask("Exec");
        exec.SetParameter("Command", "npx tailwindcss -i .\\app.css -o .\\wwwroot\\css\\site.css");
        project.Save();
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
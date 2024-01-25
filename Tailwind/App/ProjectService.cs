using Microsoft.Build.Evaluation;
using Spectre.Console;

namespace Tailwind.App;

internal class ProjectService(IAnsiConsole console)
{
    public Project LoadProject(TailwindSettings settings, CancellationToken cancel)
    {
        var projectFile = SdkHelper.FindWebSdkProject(settings.Directory);
        if (projectFile == null)
        {
            throw new ApplicationException("No project files found");
        }

        AnsiConsole.MarkupLineInterpolated($"Found a project file at {projectFile.Path}");
        var collection = new ProjectCollection();
        var project = collection.LoadProject(projectFile.Path);
        return project;
    }
}
using Microsoft.Extensions.Logging;
using System.Text;

namespace Tailwind;

public class App
{
    private readonly ILogger _log;
    private readonly ProjectEditor _editor;
    public App(ILogger log, ProjectEditor editor)
    {
        _log = log;
        _editor = editor;
    }

    public Task Run()
    {
        return Task.CompletedTask;
    }

    public async Task Init(CancellationToken cancel)
    {
        var project = _editor.LoadProject(cancel);
        await CreateBaseCssIfNotExists();
        await CreateTailwindConfigIfNotExists();
        _editor.AddTargetIfNotExists(project, ProjectEditor.InstallTailwindTarget);
        _editor.AddTargetIfNotExists(project, ProjectEditor.TailwindTarget);
    }

    private async Task CreateTailwindConfigIfNotExists()
    {
        var file = "tailwind.config.js";
        var path = $"./{file}";
        if (!File.Exists(path))
        {
            _log.LogInformation("{file} not found, creating one for you...", file);
            await CreateFile(path, TailwindConfigTemplates.Basic);
        }
    }

    private async Task CreateBaseCssIfNotExists()
    {
        var file = "app.css";
        var path = $"./{file}";
        if (!File.Exists(path))
        {
            _log.LogInformation("{file} not found, creating one for you...", file);
            var contents = "@tailwind base;\n@tailwind components;\n@tailwind utilities;";
            await CreateFile(path, contents);
        }
    }

    private async Task CreateFile(string path, string contents)
    {
        using var stream = File.Create(path);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(contents));
        await stream.FlushAsync();
    }
}

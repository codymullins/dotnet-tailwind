using Spectre.Console;
using Spectre.Console.Cli;

namespace Tailwind.App;

internal class RemoveTailwindCommand(IAnsiConsole console) : AsyncCommand<RemoveTailwindSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RemoveTailwindSettings settings)
    {
        var tailwind = TailwindConfigurationService.Create(console, settings);
        var files = await tailwind.RemovePluginFiles(false);

        if (!files.Any())
        {
            AnsiConsole.MarkupLine("Tailwind already uninstalled.");
            return 0;
        }

        var innerGrid = new Grid();
        innerGrid.AddColumn();
        foreach (var file in files)
        {
            innerGrid.AddRow(new TextPath(file));
        }
        var panel = new Panel(new Padder(innerGrid, new Padding(1, 2)));
        panel.Header = new PanelHeader("Files will be removed:");

        var grid = new Grid();
        grid.AddColumn();
        grid.AddRow(panel);
        console.Write(new Padder(grid, new Padding(0, 1)));

        if (settings.DryRun == true)
        {
            return 0;
        }

        if (AnsiConsole.Confirm("Delete these files?"))
        {
            await tailwind.RemovePluginFiles(true);
        }

        await tailwind.RemoveBuildTasks();

        return 0;
    }
}
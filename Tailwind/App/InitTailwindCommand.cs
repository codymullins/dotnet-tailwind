using Spectre.Console;
using Spectre.Console.Cli;

namespace Tailwind.App;

internal class InitTailwindCommand(IAnsiConsole console) : AsyncCommand<TailwindSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TailwindSettings settings)
    {
        AnsiConsole.MarkupLine("Initializing tailwind");
        var tailwind = TailwindConfigurationService.Create(console, settings);
        await tailwind.CreateBaseCssIfNotExists();
        await tailwind.CreateTailwindConfigIfNotExists();
        await tailwind.AddBuildTasks();
        //tailwind.AddTailwindTargets(settings.Version);
        return 0;
    }
}


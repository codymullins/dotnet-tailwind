using Spectre.Console;
using Spectre.Console.Cli;

namespace Tailwind.App;

internal class UpdateTailwindCommand(IAnsiConsole console) : AsyncCommand<TailwindSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, TailwindSettings settings)
    {
        throw new NotImplementedException();
    }
}
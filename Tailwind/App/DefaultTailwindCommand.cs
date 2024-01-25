using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Tailwind.App;

internal class DefaultTailwindCommand(IAnsiConsole console) : Command<DefaultTailwindCommand.DefaultSettings>
{
    public class DefaultSettings : CommandSettings
    {
    }

    public override int Execute(CommandContext context, DefaultSettings settings)
    {
        var versionString = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            .ToString();

        console.MarkupLine($"dotnet-tailwind v{versionString}");
        console.MarkupLine("-------------");
        console.MarkupLine("\nUsage:");
        console.MarkupLine("  dotnet tailwind init");
        return 0;
    }
}
using System.ComponentModel;
using Spectre.Console.Cli;

namespace Tailwind.App;

public class RemoveTailwindSettings : TailwindSettings
{
    
    [CommandOption("--dry-run")]
    [Description("Present the 'what-if' analysis")]
    public bool? DryRun { get; init; }
}
using System.ComponentModel;
using Spectre.Console.Cli;

namespace Tailwind.App;

public class TailwindSettings : CommandSettings
{
    [CommandOption("-v|--version")]
    [DefaultValue("3.4.1")]
    [Description("The version of Tailwind to install")]
    public required string Version { get; init; }

    [CommandOption("-o|--output")]
    [DefaultValue(@".\wwwroot\css\site.css")]
    [Description("The path to the output css file relative to the project file")]
    public required string Output { get; init; }

    [CommandOption("-d|--dir")]
    [DefaultValue(".")]
    [Description("The root directory for your code")]
    public required string Directory { get; init; }
}
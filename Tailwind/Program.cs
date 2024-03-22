using Microsoft.Build.Locator;
using Spectre.Console;
using Spectre.Console.Cli;
using Tailwind.App;

// Register the build locator once outside any commands or logic
// otherwise you may run into issues where this locator isn't registered
RegisterBuildLocator();

var app = new CommandApp<DefaultTailwindCommand>();
app.Configure(config =>
{
    config.SetApplicationName("dotnet tailwind");
    config.AddCommand<InitTailwindCommand>("init");
    config.AddCommand<RemoveTailwindCommand>("remove");
});

static void RegisterBuildLocator()
{
    var queryOptions = new VisualStudioInstanceQueryOptions { DiscoveryTypes = DiscoveryType.DotNetSdk };
    var instances = MSBuildLocator.QueryVisualStudioInstances(queryOptions).ToList();

    VisualStudioInstance instance;
    switch (instances.Count)
    {
        case 1:
            instance = instances.First();
            AnsiConsole.MarkupLine("Using .NET SDK version {0}", instance.Version);
            break;
        case > 1:
            instance = instances.OrderByDescending(p => p.Version).First();
            AnsiConsole.MarkupLine("Multiple .NET SDK versions found, using {0}", instance.Version);
            break;
        case <= 0:
            throw new ApplicationException("No .NET SDK was found");
    }

    MSBuildLocator.RegisterInstance(instance);
}

return await app.RunAsync(args);
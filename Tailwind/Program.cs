using Microsoft.Extensions.Logging;
using Tailwind;
using System.Reflection;
using Microsoft.Build.Locator;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddFilter("System", LogLevel.Warning)
        .AddFilter("Tailwind.Program", LogLevel.Debug)
        .AddCustomFormatter(options => { });
});

var log = loggerFactory.CreateLogger<Program>();
log.LogDebug("Starting dotnet-tailwind");

try
{
    var cts = new CancellationTokenSource();
    if (args.Length == 0)
    {
        ShowHelp(log);
        return;
    }

    RegisterBuildLocator();

    var editor = new ProjectEditor(loggerFactory.CreateLogger<ProjectEditor>());
    var app = new App(loggerFactory.CreateLogger<App>(), editor);

    if (args[0] == "init")
    {
        await app.Init(cts.Token);
    } 
}
catch (Exception ex)
{
    log.LogCritical(ex, "dotnet-tailwind failed");
}

static void ShowHelp(ILogger<Program> log)
{
    var versionString = Assembly.GetEntryAssembly()?
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                        .InformationalVersion
                        .ToString();

    log.LogInformation($"dotnet-tailwind v{versionString}");
    log.LogInformation("-------------");
    log.LogInformation("\nUsage:");
    log.LogInformation("  dotnet tailwind init");
    return;
}

static void RegisterBuildLocator()
{
    var instances = MSBuildLocator.QueryVisualStudioInstances(new VisualStudioInstanceQueryOptions { DiscoveryTypes = DiscoveryType.DotNetSdk });
    switch (instances.Count())
    {
        case 0:
            Console.WriteLine("dotnet SDK not found");
            throw new ApplicationException("dotnet sdk not found");
        case 1:
            Console.WriteLine("multiple dotnet SDK versions found; using first");
            break;
    }

    var instance = instances.First();
    MSBuildLocator.RegisterInstance(instance);
}
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortCheck.Services;
using PortCheck.ViewModels;

namespace PortCheck;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            MessageBox.Show($"An error occurred: {e.Exception.Message}", "PortCheck",
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var host = Services.GetRequiredService<TrayHost>();
        host.Initialize();

        var vm = Services.GetRequiredService<TrayViewModel>();
        await vm.InitializeAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Services.GetService<TrayHost>()?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var configuration = BuildConfiguration();
        services.AddSingleton<IConfiguration>(configuration);

        var pipeName = configuration.GetValue("appSettings:dockerEnginePipeName", "docker_engine")!;
        var engineTimeout = configuration.GetValue("appSettings:dockerEngineTimeoutMs", 2000);
        var skipDockerProxy = configuration.GetValue("appSettings:skipHeavyProcessInfoForDockerProxy", true);

        services.AddSingleton(_ => new DockerEngineClient(pipeName));
        services.AddSingleton(sp => new DockerPortCatalogService(
            sp.GetRequiredService<DockerEngineClient>(),
            engineTimeout));
        services.AddSingleton(sp => new DockerContainerStopService(
            sp.GetRequiredService<DockerEngineClient>(),
            engineTimeout));
        services.AddSingleton(sp => new PortScannerService(skipDockerProxy));
        services.AddSingleton<ProcessKillerService>();

        services.AddSingleton<TrayViewModel>(sp => new TrayViewModel(
            sp.GetRequiredService<PortScannerService>(),
            sp.GetRequiredService<ProcessKillerService>(),
            sp.GetRequiredService<DockerEngineClient>(),
            sp.GetRequiredService<DockerPortCatalogService>(),
            sp.GetRequiredService<DockerContainerStopService>(),
            sp.GetRequiredService<IConfiguration>(),
            Current.Dispatcher));

        services.AddSingleton<TrayHost>();
        services.AddSingleton<TrayPopupWindow>();
    }

    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["appSettings:refreshIntervalSeconds"] = "5",
                ["appSettings:dockerRefreshIntervalSeconds"] = "10",
                ["appSettings:dockerEnginePipeName"] = "docker_engine",
                ["appSettings:dockerCatalogEnabled"] = "true",
                ["appSettings:dockerEngineTimeoutMs"] = "2000",
                ["appSettings:dockerEngineProbeTimeoutMs"] = "400",
                ["appSettings:skipHeavyProcessInfoForDockerProxy"] = "true"
            });

        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "appsettings.json")))
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        return builder.Build();
    }
}

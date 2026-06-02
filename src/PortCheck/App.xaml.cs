using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortCheck.Services;
using PortCheck.Validation;
using PortCheck.ViewModels;

namespace PortCheck;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        if (IsGlassValidationRequest(Environment.GetCommandLineArgs()))
            return;

        DispatcherUnhandledException += (_, e) =>
        {
            MessageBox.Show($"An error occurred: {e.Exception.Message}", "PortCheck",
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"{ex.Message}\n\nRun PortCheck.exe from the full publish folder (see README) and approve the UAC prompt for Release builds.",
                "PortCheck",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        if (IsValidateFavouritePorts(e.Args))
        {
            var outDir = ResolveValidateGlassOutputDir(e.Args, "portcheck-favourite-ports-validate");
            Environment.Exit(FavouritePortsHarness.Run(outDir));
        }

        if (IsValidateGlassPaneTab(e.Args))
        {
            var outDir = ResolveValidateGlassOutputDir(e.Args, "portcheck-glass-pane-tab-validate");
            Environment.Exit(GlassPaneTabHarness.Run(outDir));
        }

        if (IsValidateGlassRoundButton(e.Args))
        {
            var outDir = ResolveValidateGlassOutputDir(e.Args, "portcheck-glass-round-validate");
            Environment.Exit(GlassRoundButtonHarness.Run(outDir));
        }

        base.OnStartup(e);

        if (Services is null)
            return;

        var vm = Services.GetRequiredService<TrayViewModel>();
        await vm.InitializeAsync();

        if (IsCaptureRequest(e.Args))
        {
            var popup = Services.GetRequiredService<TrayPopupWindow>();
            popup.ShowForCapture();
            await popup.CaptureForValidationAsync();
            return;
        }

        var host = Services.GetRequiredService<TrayHost>();
        host.Initialize();

        if (e.Args.Any(arg => string.Equals(arg, "--show-popup", StringComparison.OrdinalIgnoreCase)))
            host.ShowPopup();
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
        var cliTimeout = configuration.GetValue("appSettings:dockerCliTimeoutMs", 5000);
        var cliDistribution = configuration.GetValue<string?>("appSettings:dockerCliWslDistribution");
        var skipDockerProxy = configuration.GetValue("appSettings:skipHeavyProcessInfoForDockerProxy", true);
        var refreshInterval = configuration.GetValue("appSettings:refreshIntervalSeconds", 5);

        services.AddSingleton(_ => new DockerEngineClient(pipeName));
        services.AddSingleton(_ => new DockerWslCliClient(cliDistribution, cliTimeout));
        services.AddSingleton(sp => new DockerPortCatalogService(
            sp.GetRequiredService<DockerEngineClient>(),
            sp.GetRequiredService<DockerWslCliClient>(),
            engineTimeout));
        services.AddSingleton(sp => new DockerContainerStopService(
            sp.GetRequiredService<DockerEngineClient>(),
            sp.GetRequiredService<DockerWslCliClient>(),
            engineTimeout));
        services.AddSingleton(sp => new PortScannerService(skipDockerProxy));
        services.AddSingleton<ProcessKillerService>();
        services.AddSingleton(_ => new SettingsService(refreshInterval));
        services.AddSingleton<ProtectedPortCatalogService>();
        services.AddSingleton<PortExclusionService>();
        services.AddSingleton<FavouritePortsService>();

        services.AddSingleton<TrayViewModel>(sp => new TrayViewModel(
            sp.GetRequiredService<PortScannerService>(),
            sp.GetRequiredService<ProcessKillerService>(),
            sp.GetRequiredService<DockerPortCatalogService>(),
            sp.GetRequiredService<DockerContainerStopService>(),
            sp.GetRequiredService<SettingsService>(),
            sp.GetRequiredService<PortExclusionService>(),
            sp.GetRequiredService<FavouritePortsService>(),
            sp.GetRequiredService<IConfiguration>(),
            Current.Dispatcher));

        services.AddSingleton<TrayHost>();
        services.AddSingleton<TrayPopupWindow>();
    }

    private static bool IsCaptureRequest(IEnumerable<string> args) =>
        args.Any(arg => arg.StartsWith("--capture-to=", StringComparison.OrdinalIgnoreCase));

    private static bool IsGlassValidationRequest(IEnumerable<string> args) =>
        IsValidateGlassRoundButton(args) || IsValidateGlassPaneTab(args) || IsValidateFavouritePorts(args);

    private static bool IsValidateFavouritePorts(IEnumerable<string> args) =>
        ArgsContain(args, "--validate-favourite-ports");

    private static bool IsValidateGlassRoundButton(IEnumerable<string> args) =>
        ArgsContain(args, "--validate-glass-round-button");

    private static bool IsValidateGlassPaneTab(IEnumerable<string> args) =>
        ArgsContain(args, "--validate-glass-pane-tab");

    private static bool ArgsContain(IEnumerable<string> args, string flag) =>
        args.Any(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase))
        || Environment.GetCommandLineArgs().Any(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));

    private static string ResolveValidateGlassOutputDir(IEnumerable<string> args, string defaultSubdir)
    {
        foreach (var arg in args.Concat(Environment.GetCommandLineArgs()))
        {
            const string prefix = "--validate-glass-out=";
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return arg[prefix.Length..];
        }

        return Path.Combine(Path.GetTempPath(), defaultSubdir);
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
                ["appSettings:dockerCliTimeoutMs"] = "5000",
                ["appSettings:dockerCliWslDistribution"] = "",
                ["appSettings:dockerEngineProbeTimeoutMs"] = "400",
                ["appSettings:skipHeavyProcessInfoForDockerProxy"] = "true"
            });

        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "appsettings.json")))
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        return builder.Build();
    }
}

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
        services.AddSingleton<IConfiguration>(_ =>
            new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build());

        services.AddSingleton<PortScannerService>();
        services.AddSingleton<ProcessKillerService>();

        services.AddSingleton<TrayViewModel>(sp => new TrayViewModel(
            sp.GetRequiredService<PortScannerService>(),
            sp.GetRequiredService<ProcessKillerService>(),
            sp.GetRequiredService<IConfiguration>(),
            Current.Dispatcher));

        services.AddSingleton<TrayHost>();
        services.AddSingleton<TrayPopupWindow>();
    }
}

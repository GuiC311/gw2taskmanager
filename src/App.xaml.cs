using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using GW2TaskManager.Services;
using GW2TaskManager.ViewModels;

namespace GW2TaskManager;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Services = ConfigureServices();

        // Apply saved language before the window renders (avoids FR→EN flash)
        var settings = Services.GetRequiredService<SettingsService>();
        settings.Load();
        var langManager = Services.GetRequiredService<LanguageManager>();
        langManager.SetLanguage(settings.Settings.Language == "en"
            ? AppLanguage.English
            : AppLanguage.French);

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services
        services.AddSingleton<TaskRepository>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ThemeManager>();
        services.AddSingleton<LanguageManager>();
        services.AddSingleton<ResetScheduler>();
        services.AddSingleton<Gw2ApiClient>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<EventTimerService>();

        // ViewModels
        services.AddSingleton<TodoViewModel>();
        services.AddSingleton<CatalogViewModel>();
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}

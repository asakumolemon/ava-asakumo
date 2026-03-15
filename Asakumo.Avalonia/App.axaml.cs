using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Asakumo.Avalonia.ViewModels;
using Asakumo.Avalonia.Views;
using Asakumo.Avalonia.Services;
using Asakumo.Avalonia.Services.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Asakumo.Avalonia;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddDebug());

        // Register services
        services.AddSingleton<IDataService, DataService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<AIProviderFactory>();
        services.AddSingleton<IAIService, AIService>();

        // Register view models
        services.AddTransient<MainViewModel>();
        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<ConversationListViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ProviderSelectionViewModel>();
        services.AddTransient<ApiKeyConfigViewModel>();
        services.AddTransient<ModelSelectionViewModel>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            var mainViewModel = _serviceProvider!.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var mainViewModel = _serviceProvider!.GetRequiredService<MainViewModel>();
            singleViewPlatform.MainView = new MainView
            {
                DataContext = mainViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// View model for the AI provider selection view.
/// </summary>
public partial class ProviderSelectionViewModel : ViewModelBase
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Gets or sets the quick start providers.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AIProvider> _quickStartProviders = new();

    /// <summary>
    /// Gets or sets the popular providers.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AIProvider> _popularProviders = new();

    /// <summary>
    /// Gets or sets the local providers.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AIProvider> _localProviders = new();

    /// <summary>
    /// Gets or sets the selected provider.
    /// </summary>
    [ObservableProperty]
    private AIProvider? _selectedProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderSelectionViewModel"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="navigationService">The navigation service.</param>
    public ProviderSelectionViewModel(IDataService dataService, INavigationService navigationService)
    {
        _dataService = dataService;
        _navigationService = navigationService;
        _ = LoadProvidersAsync();
    }

    /// <summary>
    /// Command to go back.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Command to select a provider.
    /// </summary>
    /// <param name="provider">The selected provider.</param>
    [RelayCommand]
    private async Task SelectProviderAsync(AIProvider provider)
    {
        SelectedProvider = provider;

        var settings = await _dataService.GetSettingsAsync();
        settings.CurrentProviderId = provider.Id;
        await _dataService.SaveSettingsAsync(settings);

        if (provider.RequiresApiKey)
        {
            _navigationService.NavigateTo<ApiKeyConfigViewModel>();
        }
        else
        {
            // For quick start or local providers without API key requirement
            var config = new ProviderConfig { IsValid = true };
            settings.ProviderConfigs[provider.Id] = config;
            await _dataService.SaveSettingsAsync(settings);
            _navigationService.NavigateTo<ModelSelectionViewModel>();
        }
    }

    private async Task LoadProvidersAsync()
    {
        var providers = _dataService.GetProviders();

        QuickStartProviders.Clear();
        PopularProviders.Clear();
        LocalProviders.Clear();

        foreach (var provider in providers)
        {
            switch (provider.Category)
            {
                case ProviderCategory.QuickStart:
                    QuickStartProviders.Add(provider);
                    break;
                case ProviderCategory.Popular:
                    PopularProviders.Add(provider);
                    break;
                case ProviderCategory.Local:
                    LocalProviders.Add(provider);
                    break;
            }
        }

        // Mark current provider as selected
        var settings = await _dataService.GetSettingsAsync();
        if (!string.IsNullOrEmpty(settings.CurrentProviderId))
        {
            var currentProvider = providers.FirstOrDefault(p => p.Id == settings.CurrentProviderId);
            if (currentProvider != null)
            {
                SelectedProvider = currentProvider;
            }
        }
    }
}

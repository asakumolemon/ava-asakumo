using System;
using System.Linq;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// View model for the API key configuration view.
/// </summary>
public partial class ApiKeyConfigViewModel : ViewModelBase
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    [ObservableProperty]
    private string _providerName = string.Empty;

    /// <summary>
    /// Gets or sets the API key.
    /// </summary>
    [ObservableProperty]
    private string _apiKey = string.Empty;

    /// <summary>
    /// Gets or sets the base URL.
    /// </summary>
    [ObservableProperty]
    private string _baseUrl = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the API key is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isApiKeyVisible;

    /// <summary>
    /// Gets or sets a value indicating whether advanced settings are shown.
    /// </summary>
    [ObservableProperty]
    private bool _showAdvancedSettings;

    /// <summary>
    /// Gets or sets a value indicating whether validation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isValidating;

    /// <summary>
    /// Gets or sets the provider icon.
    /// </summary>
    [ObservableProperty]
    private string _providerIcon = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyConfigViewModel"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="navigationService">The navigation service.</param>
    public ApiKeyConfigViewModel(IDataService dataService, INavigationService navigationService)
    {
        _dataService = dataService;
        _navigationService = navigationService;
        _ = LoadProviderInfoAsync();
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
    /// Command to toggle API key visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleApiKeyVisibility()
    {
        IsApiKeyVisible = !IsApiKeyVisible;
    }

    /// <summary>
    /// Command to toggle advanced settings.
    /// </summary>
    [RelayCommand]
    private void ToggleAdvancedSettings()
    {
        ShowAdvancedSettings = !ShowAdvancedSettings;
    }

    /// <summary>
    /// Command to validate and save the API key.
    /// </summary>
    [RelayCommand]
    private async Task ValidateAndSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            return;

        IsValidating = true;

        try
        {
            // Simulate API validation
            await Task.Delay(1500);

            var settings = await _dataService.GetSettingsAsync();
            var config = new ProviderConfig
            {
                ApiKey = ApiKey,
                BaseUrl = BaseUrl,
                IsValid = true
            };

            if (!string.IsNullOrEmpty(settings.CurrentProviderId))
            {
                settings.ProviderConfigs[settings.CurrentProviderId] = config;
            }

            await _dataService.SaveSettingsAsync(settings);
            _navigationService.NavigateTo<ModelSelectionViewModel>();
        }
        finally
        {
            IsValidating = false;
        }
    }

    private async Task LoadProviderInfoAsync()
    {
        var settings = await _dataService.GetSettingsAsync();
        if (string.IsNullOrEmpty(settings.CurrentProviderId))
            return;

        var providers = _dataService.GetProviders();
        var provider = providers.FirstOrDefault(p => p.Id == settings.CurrentProviderId);

        if (provider != null)
        {
            ProviderName = provider.Name;
            ProviderIcon = provider.Icon;
            BaseUrl = provider.DefaultBaseUrl;

            // Load existing config if available
            if (settings.ProviderConfigs.TryGetValue(provider.Id, out var config))
            {
                ApiKey = config.ApiKey ?? string.Empty;
                BaseUrl = config.BaseUrl ?? provider.DefaultBaseUrl;
            }
        }
    }
}

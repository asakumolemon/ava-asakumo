using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using ModelDescriptor = Asakumo.Avalonia.Services.ModelDescriptor;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// View model for the provider management view.
/// </summary>
public partial class ProviderManagementViewModel : ViewModelBase
{
    private readonly IModelService _modelService;
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the list of providers.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProviderItemViewModel> _providers = new();

    /// <summary>
    /// Gets or sets the selected provider.
    /// </summary>
    [ObservableProperty]
    private ProviderItemViewModel? _selectedProvider;

    /// <summary>
    /// Gets or sets a value indicating whether a test is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isTesting;

    /// <summary>
    /// Gets or sets the test result message.
    /// </summary>
    [ObservableProperty]
    private string? _testResultMessage;

    /// <summary>
    /// Gets or sets a value indicating whether the test was successful.
    /// </summary>
    [ObservableProperty]
    private bool? _testSuccess;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderManagementViewModel"/> class.
    /// </summary>
    public ProviderManagementViewModel(
        IModelService modelService,
        IDataService dataService,
        INavigationService navigationService)
    {
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        _ = LoadProvidersAsync();
    }

    /// <inheritdoc/>
    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        _ = LoadProvidersAsync();
    }

    #region Commands

    /// <summary>
    /// Command to go back.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Command to toggle provider enabled state.
    /// </summary>
    [RelayCommand]
    private async Task ToggleProviderAsync(ProviderItemViewModel? provider)
    {
        if (provider == null)
            return;

        var newState = !provider.IsEnabled;
        await _modelService.SetProviderEnabledAsync(provider.Id, newState);
        provider.IsEnabled = newState;
    }

    /// <summary>
    /// Command to configure a provider.
    /// </summary>
    [RelayCommand]
    private async Task ConfigureProviderAsync(ProviderItemViewModel? provider)
    {
        if (provider == null)
            return;

        // Navigate to API key configuration
        var settings = await _dataService.GetSettingsAsync();
        settings.CurrentProviderId = provider.Id;
        await _dataService.SaveSettingsAsync(settings);

        _navigationService.NavigateTo<ApiKeyConfigViewModel>();
    }

    /// <summary>
    /// Command to test a provider connection.
    /// </summary>
    [RelayCommand]
    private async Task TestProviderAsync(ProviderItemViewModel? provider)
    {
        if (provider == null)
            return;

        IsTesting = true;
        TestResultMessage = null;
        TestSuccess = null;

        try
        {
            var success = await _modelService.TestProviderAsync(provider.Id);
            TestSuccess = success;
            TestResultMessage = success
                ? $"{provider.Name} 连接成功！"
                : $"{provider.Name} 连接失败，请检查配置。";

            if (success)
            {
                provider.IsValid = true;
            }
        }
        catch (Exception ex)
        {
            TestSuccess = false;
            TestResultMessage = $"测试失败: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    /// <summary>
    /// Command to refresh models for a provider.
    /// </summary>
    [RelayCommand]
    private async Task RefreshModelsAsync(ProviderItemViewModel? provider)
    {
        if (provider == null)
            return;

        provider.IsRefreshing = true;

        try
        {
            var success = await _modelService.RefreshProviderModelsAsync(provider.Id);
            if (success)
            {
                // Reload to update model count
                var providers = await _modelService.GetProvidersAsync();
                var updated = providers.FirstOrDefault(p => p.Id == provider.Id);
                if (updated != null && updated.IsConfigured)
                {
                    var models = await _modelService.GetModelsByProviderAsync(provider.Id);
                    provider.EnabledModelCount = models.Count;
                }
            }
        }
        finally
        {
            provider.IsRefreshing = false;
        }
    }

    /// <summary>
    /// Command to update provider display name.
    /// </summary>
    [RelayCommand]
    private async Task UpdateDisplayNameAsync(ProviderItemViewModel? provider)
    {
        if (provider == null)
            return;

        await _modelService.SetProviderDisplayNameAsync(provider.Id, provider.DisplayName);
    }

    /// <summary>
    /// Command to navigate to model management for a provider.
    /// </summary>
    [RelayCommand]
    private async Task ManageModelsAsync(ProviderItemViewModel? provider)
    {
        if (provider == null)
            return;

        SelectedProvider = provider;
        // Could navigate to a model management view here
        // For now, just refresh models
        await RefreshModelsAsync(provider);
    }

    #endregion

    #region Private Methods

    private async Task LoadProvidersAsync()
    {
        var providers = await _modelService.GetProvidersAsync();

        Providers.Clear();

        foreach (var provider in providers)
        {
            var models = provider.IsConfigured
                ? await _modelService.GetModelsByProviderAsync(provider.Id)
                : new List<ModelDescriptor>();

            Providers.Add(new ProviderItemViewModel
            {
                Id = provider.Id,
                Name = provider.Name,
                DisplayName = provider.Name,
                Category = ProviderCategory.Popular,
                IsEnabled = provider.IsEnabled,
                IsConfigured = provider.IsConfigured,
                IsValid = provider.IsConfigured,
                EnabledModelCount = models.Count,
                Icon = GetProviderIcon(provider.Id)
            });
        }
    }

    private static string GetProviderIcon(string providerId)
    {
        return providerId.ToLower() switch
        {
            "openai" => "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6Z",
            "anthropic" => "M12,2L2,7L12,12L22,7L12,2M2,17L12,22L22,17L22,7L12,12L2,7V17Z",
            "google" => "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4Z",
            "deepseek" => "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4Z",
            "ollama" => "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4Z",
            _ => "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z"
        };
    }

    #endregion
}

/// <summary>
/// View model for a provider item in the management list.
/// </summary>
public partial class ProviderItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private ProviderCategory _category;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isConfigured;

    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private int _enabledModelCount;

    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private bool _isRefreshing;

    /// <summary>
    /// Gets a value indicating whether this provider can be tested.
    /// </summary>
    public bool CanTest => IsConfigured && IsEnabled;

    /// <summary>
    /// Gets a value indicating whether this provider can be configured.
    /// </summary>
    public bool CanConfigure => !IsConfigured || !IsValid;
}

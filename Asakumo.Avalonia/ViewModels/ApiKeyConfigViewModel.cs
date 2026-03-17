using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using Asakumo.Avalonia.Services.Providers;
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
    private readonly AIProviderFactory _providerFactory;

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
    /// Gets or sets a value indicating whether models are being loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingModels;

    /// <summary>
    /// Gets or sets a value indicating whether models have been loaded.
    /// </summary>
    [ObservableProperty]
    private bool _hasLoadedModels;

    /// <summary>
    /// Gets or sets a value indicating whether an error occurred.
    /// </summary>
    [ObservableProperty]
    private bool _hasError;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Gets or sets the provider icon.
    /// </summary>
    [ObservableProperty]
    private string _providerIcon = string.Empty;

    /// <summary>
    /// Gets the collection of available models.
    /// </summary>
    public ObservableCollection<ModelSelectionItemViewModel> Models { get; } = new();

    /// <summary>
    /// Gets or sets the select all state.
    /// </summary>
    [ObservableProperty]
    private bool _selectAll = true;

    /// <summary>
    /// Gets the current provider ID.
    /// </summary>
    private string? _currentProviderId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyConfigViewModel"/> class.
    /// </summary>
    public ApiKeyConfigViewModel(
        IDataService dataService,
        INavigationService navigationService,
        AIProviderFactory providerFactory)
    {
        _dataService = dataService;
        _navigationService = navigationService;
        _providerFactory = providerFactory;
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
    /// Command to fetch model list from API.
    /// </summary>
    [RelayCommand]
    private async Task FetchModelsAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            HasError = true;
            ErrorMessage = "请输入 API Key";
            return;
        }

        if (string.IsNullOrEmpty(_currentProviderId))
            return;

        IsLoadingModels = true;
        HasError = false;
        ErrorMessage = string.Empty;
        Models.Clear();

        try
        {
            // Create a temporary provider to fetch models
            // Use a placeholder model ID - we only need to fetch the model list
            var provider = _providerFactory.CreateProvider(
                _currentProviderId,
                ApiKey,
                BaseUrl,
                "placeholder");

            // Fetch models directly - this will fail if API key is invalid
            var models = await provider.GetModelsAsync();

            var modelList = models.ToList();
            if (modelList.Count == 0)
            {
                HasError = true;
                ErrorMessage = "未找到可用模型，请检查 API Key 和 Base URL 是否正确";
                return;
            }

            foreach (var model in modelList.OrderBy(m => m.Name))
            {
                Models.Add(new ModelSelectionItemViewModel
                {
                    Id = model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    ProviderId = _currentProviderId,
                    IsSelected = true
                });
            }

            HasLoadedModels = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"获取模型列表失败: {ex.Message}";
        }
        finally
        {
            IsLoadingModels = false;
        }
    }

    /// <summary>
    /// Command to toggle select all models.
    /// </summary>
    partial void OnSelectAllChanged(bool value)
    {
        foreach (var model in Models)
        {
            model.IsSelected = value;
        }
    }

    /// <summary>
    /// Command to save selected models.
    /// </summary>
    [RelayCommand]
    private async Task SaveModelsAsync()
    {
        if (string.IsNullOrEmpty(_currentProviderId))
            return;

        var selectedModels = Models.Where(m => m.IsSelected).ToList();
        if (selectedModels.Count == 0)
        {
            HasError = true;
            ErrorMessage = "请至少选择一个模型";
            return;
        }

        try
        {
            var settings = await _dataService.GetSettingsAsync();

            // Get or create config
            if (!settings.ProviderConfigs.TryGetValue(_currentProviderId, out var config))
            {
                config = new ProviderConfig();
                settings.ProviderConfigs[_currentProviderId] = config;
            }

            // Update config
            config.ApiKey = ApiKey;
            config.BaseUrl = string.IsNullOrWhiteSpace(BaseUrl) ? null : BaseUrl;
            config.IsValid = true;
            config.AvailableModels = selectedModels.Select(m => new ModelInfo
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                ProviderId = m.ProviderId,
                IsSelected = true
            }).ToList();
            config.ModelsLastUpdated = DateTime.Now;

            // Set as current provider if not set
            if (string.IsNullOrEmpty(settings.CurrentProviderId))
            {
                settings.CurrentProviderId = _currentProviderId;
            }

            // Set default model if not set
            if (string.IsNullOrEmpty(settings.CurrentModelId) && config.AvailableModels.Count > 0)
            {
                settings.CurrentModelId = config.AvailableModels[0].Id;
                config.LastUsedModelId = config.AvailableModels[0].Id;
            }

            await _dataService.SaveSettingsAsync(settings);

            // Navigate back to settings
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"保存失败: {ex.Message}";
        }
    }

    /// <summary>
    /// Command to dismiss error.
    /// </summary>
    [RelayCommand]
    private void DismissError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    private async Task LoadProviderInfoAsync()
    {
        var settings = await _dataService.GetSettingsAsync();
        if (string.IsNullOrEmpty(settings.CurrentProviderId))
            return;

        _currentProviderId = settings.CurrentProviderId;

        var providers = _dataService.GetProviders();
        var provider = providers.FirstOrDefault(p => p.Id == settings.CurrentProviderId);

        if (provider != null)
        {
            ProviderName = provider.Name;
            ProviderIcon = provider.Icon;
            BaseUrl = provider.DefaultBaseUrl ?? string.Empty;

            // Load existing config if available
            if (settings.ProviderConfigs.TryGetValue(provider.Id, out var config))
            {
                ApiKey = config.ApiKey ?? string.Empty;
                BaseUrl = config.BaseUrl ?? provider.DefaultBaseUrl ?? string.Empty;

                // Load existing models if available
                if (config.AvailableModels.Count > 0)
                {
                    foreach (var model in config.AvailableModels.OrderBy(m => m.Name))
                    {
                        Models.Add(new ModelSelectionItemViewModel
                        {
                            Id = model.Id,
                            Name = model.Name,
                            Description = model.Description,
                            ProviderId = model.ProviderId,
                            IsSelected = model.IsSelected
                        });
                    }
                    HasLoadedModels = true;
                }
            }
        }
    }
}

/// <summary>
/// View model for a model selection item.
/// </summary>
public partial class ModelSelectionItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string _providerId = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;
}
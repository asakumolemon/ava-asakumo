using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// Represents a model group by provider for default model selection.
/// </summary>
public partial class ProviderModelGroup : ObservableObject
{
    [ObservableProperty]
    private string _providerId = string.Empty;

    [ObservableProperty]
    private string _providerName = string.Empty;

    [ObservableProperty]
    private string _providerIcon = string.Empty;

    [ObservableProperty]
    private ObservableCollection<AIModel> _models = new();
}

/// <summary>
/// ViewModel for default model selection page.
/// </summary>
public partial class DefaultModelSelectionViewModel : ViewModelBase
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;
    private readonly IAIService _aiService;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<ProviderModelGroup> _providerGroups = new();

    [ObservableProperty]
    private AIModel? _selectedModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private string? _selectedProviderId;

    [ObservableProperty]
    private string? _selectedModelId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccess))]
    private string? _successMessage;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets a value indicating whether a model is selected.
    /// </summary>
    public bool HasSelection => SelectedModel != null;

    /// <summary>
    /// Gets a value indicating whether there is an error message.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Gets a value indicating whether there is a success message.
    /// </summary>
    public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultModelSelectionViewModel"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="aiService">The AI service.</param>
    public DefaultModelSelectionViewModel(
        IDataService dataService,
        INavigationService navigationService,
        IAIService aiService)
    {
        _dataService = dataService;
        _navigationService = navigationService;
        _aiService = aiService;

        _ = InitializeAsync();
    }

    /// <inheritdoc/>
    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // Load current settings
            var settings = await _dataService.GetSettingsAsync();
            SelectedProviderId = settings.SelectedProviderId;
            SelectedModelId = settings.SelectedModelId;

            // Load all provider configs
            var configs = await _dataService.GetAllProviderConfigsAsync();
            var allProviders = _dataService.GetProviders();

            var groups = new ObservableCollection<ProviderModelGroup>();

            foreach (var config in configs.Where(c => c.HasAvailableModels))
            {
                var provider = allProviders.FirstOrDefault(p => p.Id == config.ProviderId);
                if (provider == null) continue;

                // Get available models for this provider
                var availableModels = provider.Models
                    .Where(m => config.AvailableModelIds.Contains(m.Id))
                    .ToList();

                if (availableModels.Count == 0) continue;

                var group = new ProviderModelGroup
                {
                    ProviderId = provider.Id,
                    ProviderName = provider.Name,
                    ProviderIcon = provider.Icon,
                    Models = new ObservableCollection<AIModel>(availableModels)
                };

                groups.Add(group);

                // Pre-select current default model
                if (provider.Id == SelectedProviderId && SelectedModelId != null)
                {
                    SelectedModel = availableModels.FirstOrDefault(m => m.Id == SelectedModelId);
                }
            }

            ProviderGroups = groups;

            if (ProviderGroups.Count == 0)
            {
                ErrorMessage = "请先在供应商配置中添加可用模型";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    #region Commands

    [RelayCommand]
    private void SelectModel(AIModel model)
    {
        SelectedModel = model;
        SelectedModelId = model.Id;
        SelectedProviderId = model.ProviderId;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedModel == null)
        {
            ErrorMessage = "请选择一个模型";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // Update app settings
            var settings = await _dataService.GetSettingsAsync();
            settings.SelectedProviderId = SelectedModel.ProviderId;
            settings.SelectedModelId = SelectedModel.Id;
            await _dataService.SaveSettingsAsync(settings);

            // Reload AI service configuration
            await _aiService.ReloadConfigurationAsync();

            SuccessMessage = $"已设置 {SelectedModel.Name} 为默认模型";

            // Navigate back after a short delay
            await Task.Delay(1000);
            _navigationService.GoBack();
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"保存失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// View model for the model selector dialog/view.
/// </summary>
public partial class ModelSelectorViewModel : ViewModelBase
{
    private readonly IProviderManager _providerManager;
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the search query.
    /// </summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Gets or sets the list of recent models.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ModelSelectorItemViewModel> _recentModels = new();

    /// <summary>
    /// Gets a value indicating whether there are recent models.
    /// </summary>
    public bool HasRecentModels => RecentModels.Count > 0;

    /// <summary>
    /// Gets or sets the grouped providers with models.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProviderGroupViewModel> _providerGroups = new();

    /// <summary>
    /// Gets a value indicating whether there are provider groups.
    /// </summary>
    public bool HasProviderGroups => ProviderGroups.Count > 0;

    /// <summary>
    /// Gets or sets the currently selected model.
    /// </summary>
    [ObservableProperty]
    private ModelSelectorItemViewModel? _selectedModel;

    /// <summary>
    /// Gets or sets a value indicating whether the selector is loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the current provider and model display.
    /// </summary>
    [ObservableProperty]
    private string _currentModelDisplay = string.Empty;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelSelectorViewModel"/> class.
    /// </summary>
    public ModelSelectorViewModel(
        IProviderManager providerManager,
        IDataService dataService,
        INavigationService navigationService)
    {
        _providerManager = providerManager ?? throw new ArgumentNullException(nameof(providerManager));
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        // Subscribe to search query changes
        _searchQuery = string.Empty;

        _ = InitializeAsync();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelSelectorViewModel"/> class for design-time.
    /// </summary>
    public ModelSelectorViewModel(IProviderManager providerManager, IDataService dataService)
    {
        _providerManager = providerManager ?? throw new ArgumentNullException(nameof(providerManager));
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _navigationService = null!;
    }

    partial void OnSearchQueryChanged(string value)
    {
        FilterModels(value);
    }

    #region Commands

    /// <summary>
    /// Command to close the selector.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Command to select a model.
    /// </summary>
    [RelayCommand]
    private async Task SelectModelAsync(ModelSelectorItemViewModel? model)
    {
        if (model == null)
            return;

        await _providerManager.SwitchModelAsync(model.ProviderId, model.ModelId);
        _navigationService.GoBack();
    }

    /// <summary>
    /// Command to navigate to provider management.
    /// </summary>
    [RelayCommand]
    private void ManageProviders()
    {
        _navigationService.NavigateTo<ProviderManagementViewModel>();
    }

    /// <summary>
    /// Command to clear search.
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    /// <summary>
    /// Command to refresh all model lists.
    /// </summary>
    [RelayCommand]
    private async Task RefreshModelsAsync()
    {
        IsLoading = true;

        try
        {
            var providers = await _providerManager.GetConfiguredProvidersAsync();
            foreach (var provider in providers.Where(p => p.IsConfigured))
            {
                await _providerManager.RefreshProviderModelsAsync(provider.Id);
            }

            await InitializeAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Private Methods

    private async Task InitializeAsync()
    {
        IsLoading = true;

        try
        {
            // Load current model info
            var currentModel = await _providerManager.GetCurrentModelAsync();
            if (currentModel != null)
            {
                CurrentModelDisplay = $"{currentModel.ProviderName} / {currentModel.Name}";
            }

            // Load recent models
            await LoadRecentModelsAsync();

            // Load all enabled providers with models
            await LoadProviderGroupsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadRecentModelsAsync()
    {
        var recents = await _providerManager.GetRecentModelsAsync(5);

        RecentModels.Clear();

        foreach (var recent in recents)
        {
            RecentModels.Add(new ModelSelectorItemViewModel
            {
                ProviderId = recent.ProviderId,
                ProviderName = recent.ProviderName,
                ModelId = recent.ModelId,
                ModelName = recent.ModelName,
                DisplayName = recent.DisplayName,
                Icon = GetProviderIcon(recent.ProviderName),
                IsRecent = true,
                LastUsedAt = recent.UsedAt
            });
        }
    }

    private async Task LoadProviderGroupsAsync()
    {
        var providers = await _providerManager.GetEnabledProvidersAsync();
        var groups = new List<ProviderGroupViewModel>();

        foreach (var provider in providers)
        {
            var models = await _providerManager.GetProviderModelsAsync(provider.Id);
            var enabledModels = models.Where(m => m.IsEnabled).ToList();

            if (!enabledModels.Any())
                continue;

            var group = new ProviderGroupViewModel
            {
                ProviderId = provider.Id,
                ProviderName = provider.Name,
                Icon = GetProviderIcon(provider.Name),
                IsExpanded = true,
                Models = new ObservableCollection<ModelSelectorItemViewModel>(
                    enabledModels.Select(m => new ModelSelectorItemViewModel
                    {
                        ProviderId = provider.Id,
                        ProviderName = provider.Name,
                        ModelId = m.Id,
                        ModelName = m.Name,
                        DisplayName = m.Name,
                        Description = m.Description,
                        Icon = GetProviderIcon(provider.Name),
                        IsRecent = false,
                        Category = m.Category
                    })
                )
            };

            groups.Add(group);
        }

        ProviderGroups = new ObservableCollection<ProviderGroupViewModel>(groups);
    }

    private void FilterModels(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // Show all
            foreach (var group in ProviderGroups)
            {
                foreach (var model in group.Models)
                {
                    model.IsVisible = true;
                }
                group.IsVisible = group.Models.Any(m => m.IsVisible);
            }
            return;
        }

        var lowerQuery = query.ToLowerInvariant();

        foreach (var group in ProviderGroups)
        {
            foreach (var model in group.Models)
            {
                model.IsVisible = model.ModelName.ToLowerInvariant().Contains(lowerQuery) ||
                                  model.ProviderName.ToLowerInvariant().Contains(lowerQuery);
            }
            group.IsVisible = group.Models.Any(m => m.IsVisible);
        }
    }

    private static string GetProviderIcon(string providerName)
    {
        var lower = providerName.ToLower();
        if (lower.Contains("openai") || lower.Contains("gpt"))
            return "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6Z";
        if (lower.Contains("claude") || lower.Contains("anthropic"))
            return "M12,2L2,7L12,12L22,7L12,2M2,17L12,22L22,17L22,7L12,12L2,7V17Z";
        if (lower.Contains("gemini") || lower.Contains("google"))
            return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4Z";
        if (lower.Contains("deepseek"))
            return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z";
        if (lower.Contains("ollama"))
            return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4Z";

        return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z";
    }

    #endregion
}

/// <summary>
/// View model for a model item in the selector.
/// </summary>
public partial class ModelSelectorItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _providerId = string.Empty;

    [ObservableProperty]
    private string _providerName = string.Empty;

    [ObservableProperty]
    private string _modelId = string.Empty;

    [ObservableProperty]
    private string _modelName = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private bool _isRecent;

    [ObservableProperty]
    private DateTime _lastUsedAt;

    [ObservableProperty]
    private ModelCategory _category;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isCurrent;
}

/// <summary>
/// View model for a provider group in the selector.
/// </summary>
public partial class ProviderGroupViewModel : ObservableObject
{
    [ObservableProperty]
    private string _providerId = string.Empty;

    [ObservableProperty]
    private string _providerName = string.Empty;

    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private ObservableCollection<ModelSelectorItemViewModel> _models = new();

    /// <summary>
    /// Toggles the expanded state of the group.
    /// </summary>
    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }
}

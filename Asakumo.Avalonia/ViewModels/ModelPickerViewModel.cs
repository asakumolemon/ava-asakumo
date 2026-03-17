using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Asakumo.Avalonia.Services;
using ModelDescriptor = Asakumo.Avalonia.Services.ModelDescriptor;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// View model for the model picker popup/dialog.
/// Designed to be lightweight and easy to use from ChatView.
/// </summary>
public partial class ModelPickerViewModel : ObservableObject
{
    private readonly IModelService _modelService;
    private readonly Action<ModelDescriptor>? _onModelSelected;
    private readonly Action? _onClose;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the search query.
    /// </summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the picker is loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the currently selected model.
    /// </summary>
    [ObservableProperty]
    private ModelDescriptor? _selectedModel;

    #endregion

    #region Collections

    /// <summary>
    /// Gets the collection of recently used models.
    /// </summary>
    public ObservableCollection<ModelListItemViewModel> RecentModels { get; } = new();

    /// <summary>
    /// Gets the collection of provider groups with their models.
    /// </summary>
    public ObservableCollection<ProviderGroupViewModel> ProviderGroups { get; } = new();

    /// <summary>
    /// Gets a value indicating whether there are recent models.
    /// </summary>
    public bool HasRecentModels => RecentModels.Count > 0;

    /// <summary>
    /// Gets a value indicating whether there are provider groups.
    /// </summary>
    public bool HasProviderGroups => ProviderGroups.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the search has no results.
    /// </summary>
    public bool HasNoResults => !string.IsNullOrEmpty(SearchQuery) &&
                                !ProviderGroups.Any(g => g.Models.Any(m => m.IsVisible));

    #endregion

    #region Commands

    /// <summary>
    /// Command to select a model.
    /// </summary>
    public ICommand SelectModelCommand { get; }

    /// <summary>
    /// Command to close the picker.
    /// </summary>
    public ICommand CloseCommand { get; }

    /// <summary>
    /// Command to clear the search query.
    /// </summary>
    public ICommand ClearSearchCommand { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelPickerViewModel"/> class.
    /// </summary>
    public ModelPickerViewModel(
        IModelService modelService,
        Action<ModelDescriptor>? onModelSelected = null,
        Action? onClose = null)
    {
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
        _onModelSelected = onModelSelected;
        _onClose = onClose;

        SelectModelCommand = new AsyncRelayCommand<ModelListItemViewModel?>(SelectModelAsync);
        CloseCommand = new RelayCommand(Close);
        ClearSearchCommand = new RelayCommand(() => SearchQuery = string.Empty);

        // Fire-and-forget with error handling
        _ = LoadModelsAsync().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load models: {t.Exception?.Message}");
            }
        }, TaskScheduler.Current);
    }

    #endregion

    #region Search Handling

    partial void OnSearchQueryChanged(string value)
    {
        FilterModels(value);
        OnPropertyChanged(nameof(HasNoResults));
    }

    private void FilterModels(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // Show all models
            foreach (var group in ProviderGroups)
            {
                foreach (var model in group.Models)
                {
                    model.IsVisible = true;
                }
                group.IsVisible = group.Models.Any();
            }
            return;
        }

        var lowerQuery = query.ToLowerInvariant();

        foreach (var group in ProviderGroups)
        {
            foreach (var model in group.Models)
            {
                model.IsVisible = model.Name.ToLowerInvariant().Contains(lowerQuery) ||
                                  model.ProviderName.ToLowerInvariant().Contains(lowerQuery);
            }
            group.IsVisible = group.Models.Any(m => m.IsVisible);
        }
    }

    #endregion

    #region Model Loading

    private async Task LoadModelsAsync()
    {
        IsLoading = true;

        try
        {
            await LoadRecentModelsAsync();
            await LoadProviderGroupsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadRecentModelsAsync()
    {
        var recents = await _modelService.GetRecentModelsAsync(5);
        var currentModel = _modelService.CurrentModel;

        RecentModels.Clear();

        foreach (var recent in recents)
        {
            RecentModels.Add(new ModelListItemViewModel
            {
                ModelId = recent.Id,
                Name = recent.Name,
                ProviderId = recent.ProviderId,
                ProviderName = recent.ProviderName,
                ProviderIcon = recent.ProviderIcon,
                IsCurrent = currentModel?.Id == recent.Id && currentModel?.ProviderId == recent.ProviderId,
                IsRecent = true
            });
        }

        OnPropertyChanged(nameof(HasRecentModels));
    }

    private async Task LoadProviderGroupsAsync()
    {
        var allModels = await _modelService.GetAllModelsAsync();
        var currentModel = _modelService.CurrentModel;

        // Group models by provider
        var grouped = allModels
            .GroupBy(m => m.ProviderId)
            .Select(g => new ProviderGroupViewModel
            {
                ProviderId = g.Key,
                ProviderName = g.First().ProviderName,
                ProviderIcon = g.First().ProviderIcon,
                IsVisible = true,
                Models = new ObservableCollection<ModelListItemViewModel>(
                    g.Select(m => new ModelListItemViewModel
                    {
                        ModelId = m.Id,
                        Name = m.Name,
                        Description = m.Description,
                        ProviderId = m.ProviderId,
                        ProviderName = m.ProviderName,
                        ProviderIcon = m.ProviderIcon,
                        IsCurrent = currentModel?.Id == m.Id && currentModel?.ProviderId == m.ProviderId,
                        IsVisible = true
                    })
                )
            })
            .ToList();

        ProviderGroups.Clear();

        foreach (var group in grouped)
        {
            ProviderGroups.Add(group);
        }

        OnPropertyChanged(nameof(HasProviderGroups));
    }

    #endregion

    #region Actions

    private async Task SelectModelAsync(ModelListItemViewModel? item)
    {
        if (item is null)
            return;

        await _modelService.SwitchModelAsync(item.ProviderId, item.ModelId);
        _onModelSelected?.Invoke(new ModelDescriptor
        {
            Id = item.ModelId,
            Name = item.Name,
            ProviderId = item.ProviderId,
            ProviderName = item.ProviderName,
            ProviderIcon = item.ProviderIcon
        });
        Close();
    }

    private void Close()
    {
        _onClose?.Invoke();
    }

    #endregion
}

/// <summary>
/// View model for a model item in the list.
/// </summary>
public partial class ModelListItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _modelId = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string _providerId = string.Empty;

    [ObservableProperty]
    private string _providerName = string.Empty;

    [ObservableProperty]
    private string _providerIcon = string.Empty;

    [ObservableProperty]
    private bool _isCurrent;

    [ObservableProperty]
    private bool _isRecent;

    [ObservableProperty]
    private bool _isVisible = true;
}

/// <summary>
/// View model for a provider group.
/// </summary>
public partial class ProviderGroupViewModel : ObservableObject
{
    [ObservableProperty]
    private string _providerId = string.Empty;

    [ObservableProperty]
    private string _providerName = string.Empty;

    [ObservableProperty]
    private string _providerIcon = string.Empty;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private ObservableCollection<ModelListItemViewModel> _models = new();

    /// <summary>
    /// Toggles the expanded state.
    /// </summary>
    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }
}

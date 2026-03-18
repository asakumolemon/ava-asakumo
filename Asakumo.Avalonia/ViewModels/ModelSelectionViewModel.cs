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
/// Represents a model item in the selection list.
/// </summary>
public partial class ModelSelectionItem : ObservableObject
{
    [ObservableProperty]
    private AIModel _model;

    [ObservableProperty]
    private bool _isSelected;

    public ModelSelectionItem(AIModel model, bool isSelected)
    {
        _model = model;
        _isSelected = isSelected;
    }
}

/// <summary>
/// ViewModel for model selection page (multi-select mode).
/// </summary>
public partial class ModelSelectionViewModel : ViewModelBase, INavigationAware
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;

    private string _providerId = string.Empty;
    private AIProvider? _provider;
    private List<string> _selectedModelIds = new();

    #region Observable Properties

    [ObservableProperty]
    private string _providerName = string.Empty;

    [ObservableProperty]
    private string _providerIcon = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ModelSelectionItem> _models = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private int _selectedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
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
    /// Gets a value indicating whether any model is selected.
    /// </summary>
    public bool HasSelection => SelectedCount > 0;

    /// <summary>
    /// Gets a value indicating whether there is an error message.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Gets a value indicating whether there is a success message.
    /// </summary>
    public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

    /// <summary>
    /// Gets a value indicating whether the save button can be clicked.
    /// </summary>
    public bool CanSave => !IsLoading;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelSelectionViewModel"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="navigationService">The navigation service.</param>
    public ModelSelectionViewModel(
        IDataService dataService,
        INavigationService navigationService)
    {
        _dataService = dataService;
        _navigationService = navigationService;
    }

    /// <inheritdoc/>
    public void OnNavigatedTo(string? providerId)
    {
        if (!string.IsNullOrEmpty(providerId))
        {
            _ = InitializeAsync(providerId);
        }
    }

    /// <summary>
    /// Initializes the view model with the provider ID.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    private async Task InitializeAsync(string providerId)
    {
        _providerId = providerId;
        _provider = _dataService.GetProvider(providerId);

        if (_provider == null)
        {
            ErrorMessage = $"未找到提供商: {providerId}";
            return;
        }

        ProviderName = _provider.Name;
        ProviderIcon = _provider.Icon;

        // Load existing configuration
        var config = await _dataService.GetProviderConfigAsync(providerId);
        _selectedModelIds = config?.AvailableModelIds ?? new List<string>();

        // Create selection items
        var items = _provider.Models.Select(m => new ModelSelectionItem(
            m,
            _selectedModelIds.Contains(m.Id)
        )).ToList();

        Models = new ObservableCollection<ModelSelectionItem>(items);
        UpdateSelectedCount();
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = Models.Count(m => m.IsSelected);
    }

    #region Commands

    [RelayCommand]
    private void ToggleModel(ModelSelectionItem item)
    {
        if (item == null) return;

        item.IsSelected = !item.IsSelected;
        UpdateSelectedCount();

        // Update tracking list
        if (item.IsSelected)
        {
            if (!_selectedModelIds.Contains(item.Model.Id))
                _selectedModelIds.Add(item.Model.Id);
        }
        else
        {
            _selectedModelIds.Remove(item.Model.Id);
        }
    }

    [RelayCommand]
    private void AddModel(AIModel model)
    {
        var item = Models.FirstOrDefault(m => m.Model.Id == model.Id);
        if (item != null && !item.IsSelected)
        {
            ToggleModel(item);
        }
    }

    [RelayCommand]
    private void RemoveModel(AIModel model)
    {
        var item = Models.FirstOrDefault(m => m.Model.Id == model.Id);
        if (item != null && item.IsSelected)
        {
            ToggleModel(item);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // Get or create provider config
            var config = await _dataService.GetProviderConfigAsync(_providerId);
            if (config == null)
            {
                config = new ProviderConfig
                {
                    ProviderId = _providerId,
                    ApiKey = string.Empty
                };
            }

            // Update available models
            config.AvailableModelIds = _selectedModelIds.ToList();
            config.UpdatedAt = System.DateTime.UtcNow;

            // Save provider config only (don't change AppSettings)
            await _dataService.SaveProviderConfigAsync(config);

            SuccessMessage = $"已保存 {SelectedCount} 个模型";

            // Navigate back to provider selection after a short delay
            await Task.Delay(1000);
            _navigationService.NavigateTo<ProviderSelectionViewModel>();
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
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
/// ViewModel for API key configuration page.
/// </summary>
public partial class ApiKeyConfigViewModel : ViewModelBase, INavigationAware
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;
    private readonly IAIService _aiService;

    private string _providerId = string.Empty;
    private AIProvider? _provider;
    private List<string> _selectedModelIds = new();

    #region Observable Properties

    [ObservableProperty]
    private string _providerName = string.Empty;

    [ObservableProperty]
    private string _providerIcon = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
    private string _baseUrl = string.Empty;

    [ObservableProperty]
    private bool _showBaseUrl;

    [ObservableProperty]
    private string _baseUrlPlaceholder = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValidateButtonText))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private bool _isValidating;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private bool _validationSuccess;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    /// <summary>
    /// Gets or sets a value indicating whether the API key is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isApiKeyVisible;

    /// <summary>
    /// Gets the API key help URL for the current provider.
    /// </summary>
    [ObservableProperty]
    private string? _apiKeyHelpUrl;

    /// <summary>
    /// Gets or sets the available models for selection.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ModelSelectionItem> _models = new();

    /// <summary>
    /// Gets or sets the number of selected models.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveButtonText))]
    private int _selectedCount;

    /// <summary>
    /// Gets or sets a value indicating whether the view model is saving.
    /// </summary>
    [ObservableProperty]
    private bool _isSaving;

    /// <summary>
    /// Gets or sets the model error message.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasModelError))]
    private string? _modelErrorMessage;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the validate button text based on validation state.
    /// </summary>
    public string ValidateButtonText => IsValidating ? "验证中..." : "验证 API Key";

    /// <summary>
    /// Gets a value indicating whether there is an error message.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Gets a value indicating whether there is a help URL available.
    /// </summary>
    public bool HasHelpUrl => !string.IsNullOrEmpty(ApiKeyHelpUrl);

    /// <summary>
    /// Gets a value indicating whether there is a model error message.
    /// </summary>
    public bool HasModelError => !string.IsNullOrEmpty(ModelErrorMessage);

    /// <summary>
    /// Gets the save button text based on selection state.
    /// </summary>
    public string SaveButtonText => IsSaving
        ? "保存中..."
        : ValidationSuccess
            ? $"保存 ({SelectedCount})"
            : "请先验证 API Key";

    /// <summary>
    /// Gets a value indicating whether the save button can be clicked.
    /// </summary>
    public bool CanSave => ValidationSuccess && !IsSaving;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyConfigViewModel"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="aiService">The AI service.</param>
    public ApiKeyConfigViewModel(
        IDataService dataService,
        INavigationService navigationService,
        IAIService aiService)
    {
        _dataService = dataService;
        _navigationService = navigationService;
        _aiService = aiService;
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
        ShowBaseUrl = _provider.RequiresBaseUrl;
        BaseUrlPlaceholder = _provider.DefaultBaseUrl;
        ApiKeyHelpUrl = _provider.ApiKeyHelpUrl;

        // Load existing configuration if any
        var existingConfig = await _dataService.GetProviderConfigAsync(providerId);
        if (existingConfig != null)
        {
            ApiKey = existingConfig.ApiKey ?? string.Empty;
            BaseUrl = existingConfig.BaseUrl ?? _provider.DefaultBaseUrl;
            _selectedModelIds = existingConfig.AvailableModelIds ?? new List<string>();
        }
        else if (!string.IsNullOrEmpty(_provider.DefaultBaseUrl))
        {
            BaseUrl = _provider.DefaultBaseUrl;
        }

        // Initialize model list
        InitializeModels();
    }

    /// <summary>
    /// Initializes the model list.
    /// </summary>
    private void InitializeModels()
    {
        if (_provider == null) return;

        var items = _provider.Models.Select(m => new ModelSelectionItem(
            m,
            _selectedModelIds.Contains(m.Id)
        )).ToList();

        Models = new ObservableCollection<ModelSelectionItem>(items);
        UpdateSelectedCount();
    }

    /// <summary>
    /// Updates the selected count.
    /// </summary>
    private void UpdateSelectedCount()
    {
        SelectedCount = Models.Count(m => m.IsSelected);
    }

    #region Commands

    private bool CanValidate() => !string.IsNullOrWhiteSpace(ApiKey) && !IsValidating;

    [RelayCommand(CanExecute = nameof(CanValidate))]
    private async Task ValidateAsync()
    {
        if (_provider == null) return;

        IsValidating = true;
        ValidationSuccess = false;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var isValid = await _aiService.ValidateProviderAsync(
                _providerId,
                ApiKey,
                ShowBaseUrl ? BaseUrl : null);

            if (isValid)
            {
                ValidationSuccess = true;
                SuccessMessage = "验证成功！API Key 有效。";

                // Save configuration
                var config = new ProviderConfig
                {
                    ProviderId = _providerId,
                    ApiKey = ApiKey,
                    BaseUrl = ShowBaseUrl ? BaseUrl : null,
                    IsEnabled = true,
                    AvailableModelIds = _selectedModelIds.ToList()
                };

                await _dataService.SaveProviderConfigAsync(config);

                // Initialize model list for selection
                InitializeModels();
            }
            else
            {
                ErrorMessage = "验证失败，请检查 API Key 是否正确";
            }
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"验证出错: {ex.Message}";
        }
        finally
        {
            IsValidating = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!ValidationSuccess)
        {
            ErrorMessage = "请先验证 API Key";
            return;
        }

        if (_provider == null) return;

        IsSaving = true;
        ModelErrorMessage = null;

        try
        {
            // Get existing config
            var config = await _dataService.GetProviderConfigAsync(_providerId);
            if (config == null)
            {
                config = new ProviderConfig
                {
                    ProviderId = _providerId,
                    ApiKey = ApiKey,
                    BaseUrl = ShowBaseUrl ? BaseUrl : null,
                    IsEnabled = true
                };
            }

            // Update model list
            config.AvailableModelIds = _selectedModelIds.ToList();
            config.UpdatedAt = System.DateTime.UtcNow;

            await _dataService.SaveProviderConfigAsync(config);

            // Clear configuration pages from navigation stack and navigate to settings
            // If Settings is already in stack (entered from Settings), go back to it
            // Otherwise (entered from ChatView), go back to ChatView and navigate to Settings
            if (!_navigationService.GoBackTo<SettingsViewModel>())
            {
                _navigationService.GoBackToAndNavigate<ChatViewModel, SettingsViewModel>();
            }
        }
        catch (System.Exception ex)
        {
            ModelErrorMessage = $"保存失败: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void ToggleModel(ModelSelectionItem? item)
    {
        if (item?.Model == null) return;

        item.IsSelected = !item.IsSelected;
        UpdateSelectedCount();

        var modelId = item.Model.Id;
        if (item.IsSelected)
        {
            if (!_selectedModelIds.Contains(modelId))
                _selectedModelIds.Add(modelId);
        }
        else
        {
            _selectedModelIds.Remove(modelId);
        }
    }

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
    /// Command to open the API key help URL.
    /// </summary>
    [RelayCommand]
    private void OpenApiKeyHelp()
    {
        if (!string.IsNullOrEmpty(ApiKeyHelpUrl))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ApiKeyHelpUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore errors when opening URL
            }
        }
    }

    #endregion
}

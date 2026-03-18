using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// ViewModel for model selection page.
/// </summary>
public partial class ModelSelectionViewModel : ViewModelBase, INavigationAware
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;
    private readonly IAIService _aiService;

    private string _providerId = string.Empty;
    private AIProvider? _provider;

    #region Observable Properties

    [ObservableProperty]
    private string _providerName = string.Empty;

    [ObservableProperty]
    private string _providerIcon = string.Empty;

    [ObservableProperty]
    private ObservableCollection<AIModel> _models = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private AIModel? _selectedModel;

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
    /// Initializes a new instance of the <see cref="ModelSelectionViewModel"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="aiService">The AI service.</param>
    public ModelSelectionViewModel(
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

        // Load models from provider definition
        Models = new ObservableCollection<AIModel>(_provider.Models);

        // Load existing configuration to pre-select model
        var existingConfig = await _dataService.GetProviderConfigAsync(providerId);
        if (existingConfig?.SelectedModelId != null)
        {
            SelectedModel = Models.FirstOrDefault(m => m.Id == existingConfig.SelectedModelId);
        }

        // Default to first model if none selected
        SelectedModel ??= Models.FirstOrDefault();
    }

    #region Commands

    [RelayCommand]
    private void SelectModel(AIModel model)
    {
        SelectedModel = model;
    }

    [RelayCommand]
    private async Task ConfirmAsync()
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
            // Update provider config with selected model
            var config = await _dataService.GetProviderConfigAsync(_providerId);
            if (config != null)
            {
                config.SelectedModelId = SelectedModel.Id;
                await _dataService.SaveProviderConfigAsync(config);
            }

            // Update app settings
            var settings = await _dataService.GetSettingsAsync();
            settings.SelectedProviderId = _providerId;
            settings.SelectedModelId = SelectedModel.Id;
            await _dataService.SaveSettingsAsync(settings);

            // Reload AI service configuration
            await _aiService.ReloadConfigurationAsync();

            SuccessMessage = $"已选择 {SelectedModel.Name}";

            // Navigate back to chat or main page
            _navigationService.NavigateTo<ChatViewModel>();
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

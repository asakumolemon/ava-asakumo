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
/// Represents a provider item with configuration status for display.
/// </summary>
public partial class ProviderItem : ObservableObject
{
    /// <summary>
    /// Gets the underlying AI provider.
    /// </summary>
    public AIProvider Provider { get; }

    /// <summary>
    /// Gets the provider ID.
    /// </summary>
    public string Id => Provider.Id;

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public string Name => Provider.Name;

    /// <summary>
    /// Gets the provider icon.
    /// </summary>
    public string Icon => Provider.Icon;

    /// <summary>
    /// Gets the provider description.
    /// </summary>
    public string? Description => Provider.Description;

    /// <summary>
    /// Gets or sets a value indicating whether this provider is configured.
    /// </summary>
    [ObservableProperty]
    private bool _isConfigured;

    /// <summary>
    /// Gets or sets the configured model name (if configured).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConfiguredStatus))]
    private string? _configuredModelName;

    /// <summary>
    /// Gets or sets a value indicating whether this is the currently active provider.
    /// </summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>
    /// Gets the configured status text.
    /// </summary>
    public string ConfiguredStatus => IsConfigured
        ? ConfiguredModelName ?? "已配置"
        : string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderItem"/> class.
    /// </summary>
    public ProviderItem(AIProvider provider)
    {
        Provider = provider;
    }
}

/// <summary>
/// ViewModel for provider selection page.
/// </summary>
public partial class ProviderSelectionViewModel : ViewModelBase
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;
    private readonly IAIService _aiService;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<ProviderItem> _providers = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private ProviderItem? _selectedProviderItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets a value indicating whether a provider is selected.
    /// </summary>
    public bool HasSelection => SelectedProviderItem != null;

    /// <summary>
    /// Gets a value indicating whether there is an error message.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderSelectionViewModel"/> class.
    /// </summary>
    public ProviderSelectionViewModel(
        IDataService dataService,
        INavigationService navigationService,
        IAIService aiService)
    {
        _dataService = dataService;
        _navigationService = navigationService;
        _aiService = aiService;
    }

    /// <inheritdoc/>
    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        _ = LoadProvidersAsync();
    }

    private async Task LoadProvidersAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var providers = _dataService.GetProviders();
            var allConfigs = await _dataService.GetAllProviderConfigsAsync();
            var settings = await _dataService.GetSettingsAsync();

            var currentProviderId = _aiService.CurrentProviderId ?? settings.SelectedProviderId;
            var currentModelId = _aiService.CurrentModelId ?? settings.SelectedModelId;

            var items = providers.Select(p =>
            {
                var item = new ProviderItem(p);
                var config = allConfigs.FirstOrDefault(c => c.ProviderId == p.Id);

                if (config != null && config.HasValidCredentials)
                {
                    item.IsConfigured = true;
                    item.ConfiguredModelName = config.SelectedModelId;
                }

                if (p.Id == currentProviderId)
                {
                    item.IsActive = true;
                    if (!string.IsNullOrEmpty(currentModelId))
                    {
                        item.ConfiguredModelName = currentModelId;
                    }
                }

                return item;
            }).ToList();

            Providers = new ObservableCollection<ProviderItem>(items);

            // Pre-select active provider
            if (!string.IsNullOrEmpty(currentProviderId))
            {
                SelectedProviderItem = Providers.FirstOrDefault(p => p.Id == currentProviderId);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    #region Commands

    [RelayCommand]
    private void SelectProvider(ProviderItem item)
    {
        SelectedProviderItem = item;
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (SelectedProviderItem == null)
        {
            ErrorMessage = "请选择一个 AI 服务提供商";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            _navigationService.NavigateTo<ApiKeyConfigViewModel>(SelectedProviderItem.Id);
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

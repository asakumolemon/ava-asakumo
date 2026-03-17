using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// View model for the settings view.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;
    private readonly IProviderManager _providerManager;

    #region Observable Properties

    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _isDarkMode;

    /// <summary>
    /// Gets or sets the selected language.
    /// </summary>
    [ObservableProperty]
    private string _selectedLanguage = "简体中文";

    /// <summary>
    /// Gets or sets the current provider name.
    /// </summary>
    [ObservableProperty]
    private string? _currentProviderName;

    /// <summary>
    /// Gets or sets the current model name.
    /// </summary>
    [ObservableProperty]
    private string? _currentModelName;

    /// <summary>
    /// Gets or sets a value indicating whether a provider is configured.
    /// </summary>
    [ObservableProperty]
    private bool _isProviderConfigured;

    /// <summary>
    /// Gets or sets the list of providers.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SettingsProviderItemViewModel> _providers = new();

    /// <summary>
    /// Gets or sets the ID of the expanded provider.
    /// </summary>
    [ObservableProperty]
    private string? _expandedProviderId;

    [ObservableProperty]
    private string? _toastMessage;

    [ObservableProperty]
    private bool _showToast;

    #endregion

    /// <summary>
    /// Gets the available languages.
    /// </summary>
    public ObservableCollection<string> Languages { get; } = new()
    {
        "简体中文",
        "English",
        "日本語"
    };

    private CancellationTokenSource? _toastCts;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    public SettingsViewModel(
        IDataService dataService,
        INavigationService navigationService,
        IThemeService themeService,
        IProviderManager providerManager)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _providerManager = providerManager ?? throw new ArgumentNullException(nameof(providerManager));

        // Sync with theme service
        IsDarkMode = _themeService.IsDarkMode;
        _themeService.ThemeChanged += OnThemeChanged;
    }

    /// <inheritdoc/>
    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        _ = LoadSettingsAsync();
    }

    private void OnThemeChanged(bool isDarkMode)
    {
        IsDarkMode = isDarkMode;
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        _themeService.IsDarkMode = value;
    }

    #region Commands

    /// <summary>
    /// Command to go back.
    /// </summary>
    [RelayCommand]
    private async Task GoBackAsync()
    {
        await SaveSettingsAsync();
        _navigationService.GoBack();
    }

    /// <summary>
    /// Command to toggle provider enabled state.
    /// </summary>
    [RelayCommand]
    private async Task ToggleProviderAsync(SettingsProviderItemViewModel? provider)
    {
        if (provider == null)
            return;

        var newState = !provider.IsEnabled;
        await _providerManager.SetProviderEnabledAsync(provider.Id, newState);
        provider.IsEnabled = newState;

        ShowToastMessage($"{provider.Name} {(newState ? "已启用" : "已禁用")}");
    }

    /// <summary>
    /// Command to expand/collapse provider models.
    /// </summary>
    [RelayCommand]
    private async Task ToggleExpandProviderAsync(SettingsProviderItemViewModel? provider)
    {
        if (provider == null)
            return;

        if (ExpandedProviderId == provider.Id)
        {
            // Collapse
            ExpandedProviderId = null;
        }
        else
        {
            // Expand and load models
            ExpandedProviderId = provider.Id;
            await LoadProviderModelsAsync(provider);
        }
    }

    /// <summary>
    /// Command to configure a provider.
    /// </summary>
    [RelayCommand]
    private async Task ConfigureProviderAsync(SettingsProviderItemViewModel? provider)
    {
        if (provider == null)
            return;

        var settings = await _dataService.GetSettingsAsync();
        settings.CurrentProviderId = provider.Id;
        await _dataService.SaveSettingsAsync(settings);

        _navigationService.NavigateTo<ApiKeyConfigViewModel>();
    }

    /// <summary>
    /// Command to switch to a specific model.
    /// </summary>
    [RelayCommand]
    private async Task SelectModelAsync(SettingsModelItemViewModel? model)
    {
        if (model == null)
            return;

        await _providerManager.SwitchModelAsync(model.ProviderId, model.Id);

        // Update UI
        CurrentProviderName = model.ProviderName;
        CurrentModelName = model.Name;
        IsProviderConfigured = true;

        // Refresh providers list to update current model indicators
        await LoadProvidersAsync();

        ShowToastMessage($"已切换到 {model.Name}");
    }

    /// <summary>
    /// Command to navigate to provider management page.
    /// </summary>
    [RelayCommand]
    private void ManageProviders()
    {
        _navigationService.NavigateTo<ProviderManagementViewModel>();
    }

    /// <summary>
    /// Command to add a new provider.
    /// </summary>
    [RelayCommand]
    private void AddProvider()
    {
        _navigationService.NavigateTo<ProviderSelectionViewModel>();
    }

    /// <summary>
    /// Command to open language settings.
    /// </summary>
    [RelayCommand]
    private void OpenLanguageSettings()
    {
        // Could open a language selection dialog
    }

    /// <summary>
    /// Command to backup data.
    /// </summary>
    [RelayCommand]
    private async Task BackupAsync()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"asakumo_backup_{timestamp}.json";

            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var backupPath = Path.Combine(documentsPath, backupFileName);

            await _dataService.BackupDataAsync(backupPath);
            ShowToastMessage($"备份已保存至: {backupFileName}");
        }
        catch (Exception ex)
        {
            ShowToastMessage($"备份失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Command to clear all conversations.
    /// </summary>
    [RelayCommand]
    private async Task ClearConversationsAsync()
    {
        try
        {
            await _dataService.ClearAllConversationsAsync();
            ShowToastMessage("所有会话已清除");
        }
        catch (Exception ex)
        {
            ShowToastMessage($"清除失败: {ex.Message}");
        }
    }

    #endregion

    #region Private Methods

    private async Task LoadSettingsAsync()
    {
        var settings = await _dataService.GetSettingsAsync();

        // Sync language
        SelectedLanguage = settings.Language switch
        {
            "zh-CN" => "简体中文",
            "en-US" => "English",
            "ja-JP" => "日本語",
            _ => "简体中文"
        };

        // Load current model info
        var currentModel = await _providerManager.GetCurrentModelAsync();
        if (currentModel != null)
        {
            CurrentProviderName = currentModel.ProviderName;
            CurrentModelName = currentModel.Name;
            IsProviderConfigured = true;
        }

        // Load providers list
        await LoadProvidersAsync();
    }

    private async Task LoadProvidersAsync()
    {
        var providers = await _providerManager.GetConfiguredProvidersAsync();
        var currentModel = await _providerManager.GetCurrentModelAsync();

        Providers.Clear();

        foreach (var provider in providers)
        {
            var vm = new SettingsProviderItemViewModel
            {
                Id = provider.Id,
                Name = provider.Name,
                IsEnabled = provider.IsEnabled,
                IsConfigured = provider.IsConfigured,
                IsValid = provider.IsValid,
                EnabledModelCount = provider.EnabledModelCount,
                Icon = GetProviderIcon(provider.Id),
                IsExpanded = provider.Id == ExpandedProviderId
            };

            Providers.Add(vm);
        }
    }

    private async Task LoadProviderModelsAsync(SettingsProviderItemViewModel provider)
    {
        provider.IsLoadingModels = true;

        try
        {
            var models = await _providerManager.GetProviderModelsAsync(provider.Id);
            var currentModel = await _providerManager.GetCurrentModelAsync();

            provider.Models.Clear();

            foreach (var model in models)
            {
                provider.Models.Add(new SettingsModelItemViewModel
                {
                    Id = model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    ProviderId = model.ProviderId,
                    ProviderName = model.ProviderName,
                    IsEnabled = model.IsEnabled,
                    IsCurrent = currentModel?.ProviderId == model.ProviderId && currentModel?.Id == model.Id
                });
            }
        }
        finally
        {
            provider.IsLoadingModels = false;
        }
    }

    private async Task SaveSettingsAsync()
    {
        var settings = await _dataService.GetSettingsAsync();
        settings.IsDarkMode = IsDarkMode;
        settings.Language = SelectedLanguage switch
        {
            "简体中文" => "zh-CN",
            "English" => "en-US",
            "日本語" => "ja-JP",
            _ => "zh-CN"
        };
        await _dataService.SaveSettingsAsync(settings);
    }

    private void ShowToastMessage(string message)
    {
        _toastCts?.Cancel();
        _toastCts?.Dispose();
        _toastCts = new CancellationTokenSource();

        ToastMessage = message;
        ShowToast = true;

        _ = HideToastAsync(_toastCts.Token);
    }

    private async Task HideToastAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(3000, ct);
            ShowToast = false;
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
    }

    private static string GetProviderIcon(string providerId)
    {
        return providerId.ToLower() switch
        {
            "openai" => "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6Z",
            "anthropic" => "M12,2L2,7L12,12L22,7L12,2M2,17L12,22L22,17L22,7L12,12L2,7V17Z",
            "google" => "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4Z",
            "deepseek" => "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z",
            "ollama" => "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4Z",
            _ => "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z"
        };
    }

    #endregion
}

/// <summary>
/// View model for a provider item in settings.
/// </summary>
public partial class SettingsProviderItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

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
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isLoadingModels;

    [ObservableProperty]
    private ObservableCollection<SettingsModelItemViewModel> _models = new();

    /// <summary>
    /// Gets a value indicating whether this provider can be configured.
    /// </summary>
    public bool CanConfigure => !IsConfigured || !IsValid;
}

/// <summary>
/// View model for a model item in settings.
/// </summary>
public partial class SettingsModelItemViewModel : ObservableObject
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
    private string _providerName = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isCurrent;
}

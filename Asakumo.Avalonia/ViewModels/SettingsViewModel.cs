using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
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
    private readonly IModelService _modelService;

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
    /// Gets or sets the count of configured providers.
    /// </summary>
    [ObservableProperty]
    private int _configuredProviderCount;

    /// <summary>
    /// Gets or sets the list of all available models from configured providers.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SettingsModelItemViewModel> _allModels = new();

    /// <summary>
    /// Gets a value indicating whether there are any available models.
    /// </summary>
    public bool HasModels => AllModels.Count > 0;

    [ObservableProperty]
    private string? _toastMessage;

    [ObservableProperty]
    private bool _showToast;

    /// <summary>
    /// Gets or sets a value indicating whether the clear data confirmation dialog is visible.
    /// </summary>
    [ObservableProperty]
    private bool _showClearDataConfirmDialog;

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
        IModelService modelService)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));

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
    /// Command to navigate to provider management page.
    /// </summary>
    [RelayCommand]
    private void ManageProviders()
    {
        _navigationService.NavigateTo<ProviderManagementViewModel>();
    }

    /// <summary>
    /// Command to switch to a specific model.
    /// </summary>
    [RelayCommand]
    private async Task SelectModelAsync(SettingsModelItemViewModel? model)
    {
        if (model == null)
            return;

        await _modelService.SwitchModelAsync(model.ProviderId, model.Id);

        // Update UI
        CurrentProviderName = model.ProviderName;
        CurrentModelName = model.Name;
        IsProviderConfigured = true;

        // Refresh to update current model indicators
        await LoadModelsAsync();

        ShowToastMessage($"已切换到 {model.Name}");
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

    [RelayCommand]
    private void OpenClearDataDialog()
    {
        ShowClearDataConfirmDialog = true;
    }

    /// <summary>
    /// Command to confirm and clear all application data including settings.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmClearAllDataAsync()
    {
        ShowClearDataConfirmDialog = false;

        var result = await _dataService.ClearAllDataAsync();

        if (result.AllCleared)
        {
            await ClearDataSucceededAsync();
        }
        else
        {
            ShowClearDataErrors(result);
        }
    }

    private async Task ClearDataSucceededAsync()
    {
        ShowToastMessage("所有数据已清除，应用将重启");
        await Task.Delay(2000);
        RestartApplication();
    }

    private void ShowClearDataErrors(ClearDataResult result)
    {
        var errorMessage = result.Errors.Count > 0
            ? $"部分数据未清除: {string.Join(", ", result.Errors)}"
            : "清除数据失败，请重试";
        ShowToastMessage(errorMessage);
    }

    /// <summary>
    /// Command to cancel clear all data operation.
    /// </summary>
    [RelayCommand]
    private void CancelClearAllData()
    {
        ShowClearDataConfirmDialog = false;
    }

    private void RestartApplication()
    {
        var executablePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(executablePath))
        {
            StartNewProcess(executablePath);
        }

        ShutdownApplication();
    }

    private static void StartNewProcess(string executablePath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
        catch (Exception)
        {
            // Best effort - if we can't start new process, just shut down
        }
    }

    private static void ShutdownApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
        else
        {
            Environment.Exit(0);
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
        var currentModel = _modelService.CurrentModel;
        if (currentModel != null)
        {
            CurrentProviderName = currentModel.ProviderName;
            CurrentModelName = currentModel.Name;
            IsProviderConfigured = true;
        }

        // Load providers and models
        await LoadProvidersAndModelsAsync();
    }

    private async Task LoadProvidersAndModelsAsync()
    {
        var providers = await _modelService.GetProvidersAsync();
        var currentModel = _modelService.CurrentModel;

        ConfiguredProviderCount = providers.Count(p => p.IsConfigured);
        AllModels.Clear();

        foreach (var provider in providers.Where(p => p.IsConfigured))
        {
            var models = await _modelService.GetModelsByProviderAsync(provider.Id);

            foreach (var model in models)
            {
                AllModels.Add(new SettingsModelItemViewModel
                {
                    Id = model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    ProviderId = model.ProviderId,
                    ProviderName = model.ProviderName,
                    IsCurrent = currentModel?.ProviderId == model.ProviderId && currentModel?.Id == model.Id
                });
            }
        }

        OnPropertyChanged(nameof(HasModels));
    }

    private async Task LoadModelsAsync()
    {
        var providers = await _modelService.GetProvidersAsync();
        var currentModel = _modelService.CurrentModel;

        AllModels.Clear();

        foreach (var provider in providers.Where(p => p.IsConfigured))
        {
            var models = await _modelService.GetModelsByProviderAsync(provider.Id);

            foreach (var model in models)
            {
                AllModels.Add(new SettingsModelItemViewModel
                {
                    Id = model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    ProviderId = model.ProviderId,
                    ProviderName = model.ProviderName,
                    IsCurrent = currentModel?.ProviderId == model.ProviderId && currentModel?.Id == model.Id
                });
            }
        }

        OnPropertyChanged(nameof(HasModels));
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

    #endregion
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
    private bool _isCurrent;

    /// <summary>
    /// Gets the first character of the provider name for display.
    /// </summary>
    public string ProviderInitial => string.IsNullOrEmpty(ProviderName) ? "?" : ProviderName[0].ToString();
}
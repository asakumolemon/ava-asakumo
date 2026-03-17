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
    /// Gets the available languages.
    /// </summary>
    public ObservableCollection<string> Languages { get; } = new()
    {
        "简体中文",
        "English",
        "日本語"
    };

    [ObservableProperty]
    private string? _toastMessage;

    [ObservableProperty]
    private bool _showToast;

    private CancellationTokenSource? _toastCts;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="themeService">The theme service.</param>
    public SettingsViewModel(
        IDataService dataService, 
        INavigationService navigationService,
        IThemeService themeService)
    {
        _dataService = dataService;
        _navigationService = navigationService;
        _themeService = themeService;
        
        // Sync with theme service
        IsDarkMode = _themeService.IsDarkMode;
        _themeService.ThemeChanged += OnThemeChanged;
        
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
    /// Command to configure provider.
    /// </summary>
    [RelayCommand]
    private void ConfigureProvider()
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
            var timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"asakumo_backup_{timestamp}.json";

            // Use Documents folder for backup
            var documentsPath = System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.MyDocuments);
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

    private async Task LoadSettingsAsync()
    {
        var settings = await _dataService.GetSettingsAsync();
        // IsDarkMode is already synced from ThemeService
        SelectedLanguage = settings.Language switch
        {
            "zh-CN" => "简体中文",
            "en-US" => "English",
            "ja-JP" => "日本語",
            _ => "简体中文"
        };

        if (!string.IsNullOrEmpty(settings.CurrentProviderId))
        {
            var providers = _dataService.GetProviders();
            var provider = providers.FirstOrDefault(p => p.Id == settings.CurrentProviderId);
            CurrentProviderName = provider?.Name;

            if (!string.IsNullOrEmpty(settings.CurrentModelId))
            {
                var model = provider?.Models.FirstOrDefault(m => m.Id == settings.CurrentModelId);
                CurrentModelName = model?.Name;
            }

            IsProviderConfigured = settings.ProviderConfigs.TryGetValue(settings.CurrentProviderId, out var config) && config.IsValid;
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
}

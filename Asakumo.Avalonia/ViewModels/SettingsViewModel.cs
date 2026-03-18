using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
        IThemeService themeService)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

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

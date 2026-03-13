using System.Collections.ObjectModel;
using System.Linq;
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

    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _isDarkMode = true;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="navigationService">The navigation service.</param>
    public SettingsViewModel(IDataService dataService, INavigationService navigationService)
    {
        _dataService = dataService;
        _navigationService = navigationService;
        LoadSettings();
    }

    /// <summary>
    /// Command to go back.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        SaveSettings();
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
    private void Backup()
    {
        // Implement backup logic
    }

    /// <summary>
    /// Command to clear all conversations.
    /// </summary>
    [RelayCommand]
    private void ClearConversations()
    {
        // Implement clear conversations logic
    }

    private void LoadSettings()
    {
        var settings = _dataService.GetSettings();
        IsDarkMode = settings.IsDarkMode;
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

    private void SaveSettings()
    {
        var settings = _dataService.GetSettings();
        settings.IsDarkMode = IsDarkMode;
        settings.Language = SelectedLanguage switch
        {
            "简体中文" => "zh-CN",
            "English" => "en-US",
            "日本語" => "ja-JP",
            _ => "zh-CN"
        };
        _dataService.SaveSettingsAsync(settings);
    }
}

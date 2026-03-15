using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Asakumo.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// View model for the welcome/onboarding screen.
/// </summary>
public partial class WelcomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IDataService _dataService;

    /// <summary>
    /// Gets or sets the current page index (for onboarding carousel).
    /// </summary>
    [ObservableProperty]
    private int _currentPage;

    /// <summary>
    /// Gets or sets the selected language.
    /// </summary>
    [ObservableProperty]
    private string _selectedLanguage = "简体中文";

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
    /// Initializes a new instance of the <see cref="WelcomeViewModel"/> class.
    /// </summary>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="dataService">The data service.</param>
    public WelcomeViewModel(INavigationService navigationService, IDataService dataService)
    {
        _navigationService = navigationService;
        _dataService = dataService;
    }

    /// <summary>
    /// Gets the total number of onboarding pages.
    /// </summary>
    public int TotalPages => 4;

    /// <summary>
    /// Command to start using the app.
    /// </summary>
    [RelayCommand]
    private async Task StartUsingAsync()
    {
        var settings = await _dataService.GetSettingsAsync();
        settings.HasSeenWelcome = true;
        await _dataService.SaveSettingsAsync(settings);
        _navigationService.NavigateTo<ConversationListViewModel>();
    }

    /// <summary>
    /// Command to skip onboarding.
    /// </summary>
    [RelayCommand]
    private async Task SkipAsync()
    {
        await StartUsingAsync();
    }

    /// <summary>
    /// Command to go to next page.
    /// </summary>
    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages - 1)
        {
            CurrentPage++;
        }
        else
        {
            await StartUsingAsync();
        }
    }
}

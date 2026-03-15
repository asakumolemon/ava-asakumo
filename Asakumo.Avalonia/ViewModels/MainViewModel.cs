using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// Main view model that handles navigation and application state.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Gets or sets the current view model for navigation.
    /// </summary>
    [ObservableProperty]
    private ViewModelBase _currentView = null!;

    /// <summary>
    /// Gets or sets the application settings.
    /// </summary>
    [ObservableProperty]
    private AppSettings _settings = new();

    /// <summary>
    /// Gets the list of conversations.
    /// </summary>
    public ObservableCollection<Conversation> Conversations { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="navigationService">The navigation service.</param>
    public MainViewModel(IDataService dataService, INavigationService navigationService)
    {
        _dataService = dataService;
        _navigationService = navigationService;
        _navigationService.NavigationChanged += OnNavigationChanged;

        // Initialize asynchronously to avoid blocking the UI thread
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            Settings = await _dataService.GetSettingsAsync();

            // Initialize with welcome view if first time, otherwise conversation list
            if (!Settings.HasSeenWelcome)
            {
                _navigationService.NavigateTo<WelcomeViewModel>();
            }
            else
            {
                _navigationService.NavigateTo<ConversationListViewModel>();
            }
        }
        catch (Exception)
        {
            // If settings fail to load, show welcome view as fallback
            _navigationService.NavigateTo<WelcomeViewModel>();
        }
    }

    private void OnNavigationChanged(ViewModelBase viewModel)
    {
        CurrentView = viewModel;
    }
}
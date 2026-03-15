using System;
using Asakumo.Avalonia.ViewModels;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Provides navigation functionality for the application.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the current view model.
    /// </summary>
    ViewModelBase? CurrentView { get; }

    /// <summary>
    /// Navigates to the specified view model.
    /// </summary>
    /// <typeparam name="T">The type of view model to navigate to.</typeparam>
    void NavigateTo<T>() where T : ViewModelBase;

    /// <summary>
    /// Navigates to the specified view model with a parameter.
    /// </summary>
    /// <typeparam name="T">The type of view model to navigate to.</typeparam>
    /// <param name="parameter">The parameter to pass to the view model.</param>
    void NavigateTo<T>(string parameter) where T : ViewModelBase;

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Gets a value indicating whether navigation back is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Event raised when navigation occurs.
    /// </summary>
    event Action<ViewModelBase>? NavigationChanged;
}
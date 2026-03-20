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
    /// Navigates back to a specific view model type, removing all intermediate pages.
    /// </summary>
    /// <typeparam name="T">The type of view model to navigate back to.</typeparam>
    /// <returns>True if navigation succeeded, false if the type was not found in the stack.</returns>
    bool GoBackTo<T>() where T : ViewModelBase;

    /// <summary>
    /// Pops the current page and navigates to a new page.
    /// Useful for completing a flow and navigating to a result page.
    /// </summary>
    /// <typeparam name="T">The type of view model to navigate to.</typeparam>
    void NavigateReplacingCurrent<T>() where T : ViewModelBase;

    /// <summary>
    /// Navigates to a new page after popping back to a specific page type.
    /// Useful for completing a configuration flow and navigating to a result page.
    /// </summary>
    /// <typeparam name="TTarget">The type of view model to navigate back to first.</typeparam>
    /// <typeparam name="TNavigate">The type of view model to navigate to after going back.</typeparam>
    void GoBackToAndNavigate<TTarget, TNavigate>()
        where TTarget : ViewModelBase
        where TNavigate : ViewModelBase;

    /// <summary>
    /// Gets a value indicating whether navigation back is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Event raised when navigation occurs.
    /// </summary>
    event Action<ViewModelBase>? NavigationChanged;
}
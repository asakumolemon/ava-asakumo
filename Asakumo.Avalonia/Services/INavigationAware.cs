namespace Asakumo.Avalonia.Services;

/// <summary>
/// Interface for view models that need to handle navigation parameters.
/// </summary>
public interface INavigationAware
{
    /// <summary>
    /// Called when the view model is navigated to with a parameter.
    /// </summary>
    /// <param name="parameter">The navigation parameter.</param>
    void OnNavigatedTo(string? parameter);
}

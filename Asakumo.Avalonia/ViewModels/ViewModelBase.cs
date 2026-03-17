using CommunityToolkit.Mvvm.ComponentModel;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// Base class for all view models.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Called when the view model is navigated to (either new navigation or back navigation).
    /// Override this to refresh data when the page becomes visible.
    /// </summary>
    public virtual void OnNavigatedTo()
    {
    }
}

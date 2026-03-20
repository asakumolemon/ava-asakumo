using System;
using System.Collections.Generic;
using System.Linq;
using Asakumo.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Implementation of the navigation service.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly Stack<ViewModelBase> _navigationStack = new();
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public ViewModelBase? CurrentView => _navigationStack.Count > 0 ? _navigationStack.Peek() : null;

    /// <inheritdoc/>
    public bool CanGoBack => _navigationStack.Count > 1;

    /// <inheritdoc/>
    public event Action<ViewModelBase>? NavigationChanged;

    /// <inheritdoc/>
    public void NavigateTo<T>() where T : ViewModelBase
    {
        var viewModel = _serviceProvider.GetService(typeof(T)) as ViewModelBase;
        if (viewModel != null)
        {
            _navigationStack.Push(viewModel);
            NavigationChanged?.Invoke(viewModel);
            viewModel.OnNavigatedTo();
        }
    }

    /// <inheritdoc/>
    public void NavigateTo<T>(string parameter) where T : ViewModelBase
    {
        var viewModel = _serviceProvider.GetService(typeof(T)) as ViewModelBase;
        if (viewModel != null)
        {
            _navigationStack.Push(viewModel);
            NavigationChanged?.Invoke(viewModel);
            viewModel.OnNavigatedTo();

            // Handle navigation parameter using INavigationAware interface
            if (viewModel is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(parameter);
            }
        }
    }

    /// <inheritdoc/>
    public void GoBack()
    {
        if (CanGoBack)
        {
            _navigationStack.Pop();
            var currentView = _navigationStack.Peek();
            NavigationChanged?.Invoke(currentView);
            currentView.OnNavigatedTo();
        }
    }

    /// <inheritdoc/>
    public bool GoBackTo<T>() where T : ViewModelBase
    {
        // Find the target type in the stack
        var targetType = typeof(T);
        var stackArray = _navigationStack.ToArray();

        // Search from bottom (oldest) to top (newest) for the target
        for (int i = stackArray.Length - 1; i >= 0; i--)
        {
            if (stackArray[i].GetType() == targetType)
            {
                // Found target, pop pages until we reach it
                while (_navigationStack.Count > 0 && _navigationStack.Peek().GetType() != targetType)
                {
                    _navigationStack.Pop();
                }

                if (_navigationStack.Count > 0)
                {
                    var currentView = _navigationStack.Peek();
                    NavigationChanged?.Invoke(currentView);
                    currentView.OnNavigatedTo();
                    return true;
                }
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public void NavigateReplacingCurrent<T>() where T : ViewModelBase
    {
        if (_navigationStack.Count > 0)
        {
            _navigationStack.Pop();
        }

        var viewModel = _serviceProvider.GetService(typeof(T)) as ViewModelBase;
        if (viewModel != null)
        {
            _navigationStack.Push(viewModel);
            NavigationChanged?.Invoke(viewModel);
            viewModel.OnNavigatedTo();
        }
    }

    /// <inheritdoc/>
    public void GoBackToAndNavigate<TTarget, TNavigate>()
        where TTarget : ViewModelBase
        where TNavigate : ViewModelBase
    {
        // First go back to the target page
        GoBackTo<TTarget>();

        // Then navigate to the new page
        var viewModel = _serviceProvider.GetService(typeof(TNavigate)) as ViewModelBase;
        if (viewModel != null)
        {
            _navigationStack.Push(viewModel);
            NavigationChanged?.Invoke(viewModel);
            viewModel.OnNavigatedTo();
        }
    }
}
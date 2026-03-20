using System;
using System.Collections.Generic;
using System.Linq;
using Asakumo.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Implementation of the navigation service.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly Stack<ViewModelBase> _navigationStack = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
        var targetType = typeof(T);
        var stackArray = _navigationStack.ToArray();

        for (int i = stackArray.Length - 1; i >= 0; i--)
        {
            if (stackArray[i].GetType() == targetType)
            {
                while (_navigationStack.Count > 0 && _navigationStack.Peek().GetType() != targetType)
                {
                    _navigationStack.Pop();
                }

                if (_navigationStack.Count > 0)
                {
                    var currentView = _navigationStack.Peek();
                    NavigationChanged?.Invoke(currentView);
                    currentView.OnNavigatedTo();
                    _logger?.LogDebug("GoBackTo<{TargetType}> succeeded, stack depth: {Count}", targetType.Name, _navigationStack.Count);
                    return true;
                }
            }
        }

        _logger?.LogWarning("GoBackTo<{TargetType}> failed: type not found in navigation stack", targetType.Name);
        return false;
    }

    /// <inheritdoc/>
    public void NavigateReplacingCurrent<T>() where T : ViewModelBase
    {
        if (_navigationStack.Count > 0)
        {
            _navigationStack.Pop();
        }
        else
        {
            _logger?.LogWarning("NavigateReplacingCurrent<{Type}> called on empty stack, behaving as NavigateTo", typeof(T).Name);
        }

        var viewModel = _serviceProvider.GetService(typeof(T)) as ViewModelBase;
        if (viewModel != null)
        {
            _navigationStack.Push(viewModel);
            NavigationChanged?.Invoke(viewModel);
            viewModel.OnNavigatedTo();
        }
        else
        {
            _logger?.LogError("NavigateReplacingCurrent<{Type}> failed: could not resolve ViewModel from DI", typeof(T).Name);
        }
    }

    /// <inheritdoc/>
    public void GoBackToAndNavigate<TTarget, TNavigate>()
        where TTarget : ViewModelBase
        where TNavigate : ViewModelBase
    {
        bool wentBack = GoBackTo<TTarget>();

        if (!wentBack)
        {
            _logger?.LogWarning(
                "GoBackToAndNavigate<{Target}, {Navigate}> failed: could not find {Target} in stack, clearing stack",
                typeof(TTarget).Name,
                typeof(TNavigate).Name,
                typeof(TTarget).Name);

            _navigationStack.Clear();
        }

        var viewModel = _serviceProvider.GetService(typeof(TNavigate)) as ViewModelBase;
        if (viewModel != null)
        {
            _navigationStack.Push(viewModel);
            NavigationChanged?.Invoke(viewModel);
            viewModel.OnNavigatedTo();
        }
        else
        {
            _logger?.LogError(
                "GoBackToAndNavigate<{Target}, {Navigate}> failed: could not resolve {Navigate} from DI",
                typeof(TTarget).Name,
                typeof(TNavigate).Name,
                typeof(TNavigate).Name);
        }
    }
}
using System;
using System.Collections.Generic;
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
        }
    }

    /// <inheritdoc/>
    public void NavigateTo<T>(string parameter) where T : ViewModelBase
    {
        var viewModel = _serviceProvider.GetService(typeof(T)) as ViewModelBase;
        if (viewModel != null)
        {
            // Handle parameter for ChatViewModel
            if (viewModel is ChatViewModel chatViewModel)
            {
                // Synchronously wait for conversation to load before navigating
                chatViewModel.SetConversationAsync(parameter).GetAwaiter().GetResult();
            }
            
            _navigationStack.Push(viewModel);
            NavigationChanged?.Invoke(viewModel);
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
        }
    }
}
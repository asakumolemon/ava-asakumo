using System;
using Avalonia.Controls;
using Asakumo.Avalonia.Services;
using Asakumo.Avalonia.ViewModels;

namespace Asakumo.Avalonia.Views;

public partial class ApiKeyConfigView : UserControl
{
    public ApiKeyConfigView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is INavigationAware navigationAware)
        {
            var providerId = GetProviderIdFromNavigation();
            if (!string.IsNullOrEmpty(providerId))
            {
                navigationAware.OnNavigatedTo(providerId);
            }
        }
    }

    private string? GetProviderIdFromNavigation()
    {
        return _providerId;
    }

    private string? _providerId;

    public void SetProviderId(string providerId)
    {
        _providerId = providerId;
        if (DataContext is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedTo(providerId);
        }
    }
}

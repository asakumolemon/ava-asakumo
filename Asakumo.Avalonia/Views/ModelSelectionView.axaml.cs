using System;
using Avalonia.Controls;
using Asakumo.Avalonia.Services;
using Asakumo.Avalonia.ViewModels;

namespace Asakumo.Avalonia.Views;

public partial class ModelSelectionView : UserControl
{
    public ModelSelectionView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is INavigationAware navigationAware)
        {
            var providerId = _providerId;
            if (!string.IsNullOrEmpty(providerId))
            {
                navigationAware.OnNavigatedTo(providerId);
            }
        }
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

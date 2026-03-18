using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Asakumo.Avalonia.Views;

public partial class DefaultModelSelectionView : UserControl
{
    public DefaultModelSelectionView()
    {
        InitializeComponent();
    }

    private void OnModelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is AIModel model)
        {
            if (DataContext is DefaultModelSelectionViewModel viewModel)
            {
                viewModel.SelectModelCommand.Execute(model);
            }
        }
    }
}

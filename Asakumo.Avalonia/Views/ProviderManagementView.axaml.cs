using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Asakumo.Avalonia.Views;

/// <summary>
/// Provider management view for managing AI providers.
/// </summary>
public partial class ProviderManagementView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderManagementView"/> class.
    /// </summary>
    public ProviderManagementView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

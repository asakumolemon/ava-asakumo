using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Asakumo.Avalonia.Views;

/// <summary>
/// Model selector view for choosing AI models.
/// </summary>
public partial class ModelSelectorView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelSelectorView"/> class.
    /// </summary>
    public ModelSelectorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

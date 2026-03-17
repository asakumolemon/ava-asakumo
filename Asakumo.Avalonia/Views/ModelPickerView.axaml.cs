using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace Asakumo.Avalonia.Views;

/// <summary>
/// Code-behind for the model picker view.
/// Handles drag-to-dismiss interactions.
/// </summary>
public partial class ModelPickerView : UserControl
{
    private bool _isDragging;
    private Point _startPosition;
    private const double DragThreshold = 100;
    private const double MaxDragDistance = 200;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelPickerView"/> class.
    /// </summary>
    public ModelPickerView()
    {
        InitializeComponent();
    }

    private void OnDragPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDragging = true;
        _startPosition = e.GetPosition(this);
        if (sender is InputElement inputElement)
        {
            e.Pointer.Capture(inputElement);
        }
    }

    private void OnDragPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging)
            return;

        var currentPosition = e.GetPosition(this);
        var deltaY = currentPosition.Y - _startPosition.Y;

        // Only allow dragging down (positive delta)
        if (deltaY < 0)
            deltaY = 0;

        // Limit drag distance
        if (deltaY > MaxDragDistance)
            deltaY = MaxDragDistance;

        // Apply transform
        if (Content is Control content)
        {
            content.RenderTransform = new TranslateTransform(0, deltaY);
            content.Opacity = 1 - (deltaY / MaxDragDistance) * 0.5;
        }
    }

    private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        e.Pointer.Capture(null);

        var currentPosition = e.GetPosition(this);
        var deltaY = currentPosition.Y - _startPosition.Y;

        // Check if dragged enough to close
        if (deltaY > DragThreshold)
        {
            // Close the picker
            ClosePicker();
        }
        else
        {
            // Snap back to original position
            SnapBack();
        }
    }

    private void ClosePicker()
    {
        // Find the view model and call close command
        if (DataContext is ViewModels.ModelPickerViewModel vm)
        {
            // Animate out
            if (Content is Control content)
            {
                content.Opacity = 0;
            }

            // Small delay to allow animation
            Dispatcher.UIThread.Post(() =>
            {
                vm.CloseCommand.Execute(null);
            });
        }
    }

    private void SnapBack()
    {
        if (Content is Control content)
        {
            // Reset transform with animation
            content.RenderTransform = null;
            content.Opacity = 1;
        }
    }
}

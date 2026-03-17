using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace Asakumo.Avalonia.Views;

/// <summary>
/// Conversation list view.
/// </summary>
public partial class ConversationListView : UserControl
{
    private const double SwipeThreshold = 72; // Width of swipe action button
    private Control? _currentSwipeItem;
    private double _startX;
    private bool _isSwiping;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationListView"/> class.
    /// </summary>
    public ConversationListView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles pointer pressed event for swipe gesture.
    /// </summary>
    private void OnSwipePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control)
            return;

        // Close any previously swiped item
        if (_currentSwipeItem != null && _currentSwipeItem != control)
        {
            ResetSwipe(_currentSwipeItem);
        }

        _currentSwipeItem = control;
        _startX = e.GetPosition(control).X;
        _isSwiping = true;
    }

    /// <summary>
    /// Handles pointer moved event for swipe gesture.
    /// </summary>
    private void OnSwipePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isSwiping || _currentSwipeItem == null)
            return;

        var currentX = e.GetPosition(_currentSwipeItem).X;
        var deltaX = currentX - _startX;

        // Limit swipe distance
        if (deltaX > SwipeThreshold)
            deltaX = SwipeThreshold;
        if (deltaX < -SwipeThreshold)
            deltaX = -SwipeThreshold;

        // Apply transform
        var transform = TransformOperations.CreateBuilder(1);
        transform.AppendTranslate(deltaX, 0);
        _currentSwipeItem.RenderTransform = transform.Build();
    }

    /// <summary>
    /// Handles pointer released event for swipe gesture.
    /// </summary>
    private void OnSwipePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isSwiping || _currentSwipeItem == null)
            return;

        var currentX = e.GetPosition(_currentSwipeItem).X;
        var deltaX = currentX - _startX;

        // Snap to open or closed position
        if (Math.Abs(deltaX) > SwipeThreshold / 2)
        {
            // Snap open
            var snapX = deltaX > 0 ? SwipeThreshold : -SwipeThreshold;
            var transform = TransformOperations.CreateBuilder(1);
            transform.AppendTranslate(snapX, 0);
            _currentSwipeItem.RenderTransform = transform.Build();
        }
        else
        {
            // Snap closed
            ResetSwipe(_currentSwipeItem);
        }

        _isSwiping = false;
    }

    /// <summary>
    /// Resets the swipe position of an item.
    /// </summary>
    private static void ResetSwipe(Control item)
    {
        item.RenderTransform = TransformOperations.Identity;
    }
}

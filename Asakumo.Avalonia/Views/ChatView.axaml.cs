using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Asakumo.Avalonia.ViewModels;

namespace Asakumo.Avalonia.Views;

/// <summary>
/// Code-behind for the chat view, handling UI-specific behaviors like auto-scroll.
/// </summary>
public partial class ChatView : UserControl
{
    private ScrollViewer? _messagesScrollViewer;
    private ChatViewModel? _currentViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatView"/> class.
    /// </summary>
    public ChatView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _messagesScrollViewer = this.FindControl<ScrollViewer>("MessagesScrollViewer");
        SubscribeToViewModel();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        UnsubscribeFromViewModel();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UnsubscribeFromViewModel();
        SubscribeToViewModel();
    }

    private void SubscribeToViewModel()
    {
        if (DataContext is ChatViewModel viewModel)
        {
            _currentViewModel = viewModel;
            _currentViewModel.MessageAdded += OnMessageAdded;
        }
    }

    private void UnsubscribeFromViewModel()
    {
        if (_currentViewModel != null)
        {
            _currentViewModel.MessageAdded -= OnMessageAdded;
            _currentViewModel = null;
        }
    }

    private void OnMessageAdded(object? sender, EventArgs e)
    {
        // Use Dispatcher to ensure scroll happens after UI update
        Dispatcher.UIThread.Post(ScrollToBottom, DispatcherPriority.Background);
    }

    private void ScrollToBottom()
    {
        _messagesScrollViewer?.ScrollToEnd();
    }
}
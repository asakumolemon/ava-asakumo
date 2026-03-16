using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Asakumo.Avalonia.ViewModels;

namespace Asakumo.Avalonia.Views;

/// <summary>
/// Chat view.
/// </summary>
public partial class ChatView : UserControl
{
    private ScrollViewer? _messagesScrollViewer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatView"/> class.
    /// </summary>
    public ChatView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _messagesScrollViewer = this.FindControl<ScrollViewer>("MessagesScrollViewer");
        
        if (DataContext is ChatViewModel viewModel)
        {
            viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        }
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is ChatViewModel viewModel)
        {
            viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        }
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            // Scroll to bottom when new message is added
            ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        if (_messagesScrollViewer != null)
        {
            _messagesScrollViewer.ScrollToEnd();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// View model for the chat view, managing conversation state.
/// </summary>
public partial class ChatViewModel : ViewModelBase
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;
    private string _conversationId = Guid.NewGuid().ToString();

    private const int MaxTitleLength = 30;
    private const int MaxPreviewLength = 50;
    private const int SuccessMessageDisplayMs = 3000;

    #region Observable Properties

    [ObservableProperty]
    private string _title = "新会话";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _inputMessage = string.Empty;

    [ObservableProperty]
    private bool _isMoreMenuOpen;

    [ObservableProperty]
    private bool _isEditingTitle;

    [ObservableProperty]
    private string _editableTitle = string.Empty;

    [ObservableProperty]
    private string? _toastMessage;

    [ObservableProperty]
    private bool _showToast;

    private System.Threading.CancellationTokenSource? _toastCts;

    #endregion

    #region Computed Properties

    public ObservableCollection<ChatMessage> Messages { get; }

    public bool HasMessages => Messages.Count > 0;

    public bool CanSend => !string.IsNullOrWhiteSpace(InputMessage);

    public string ConversationId => _conversationId;

    #endregion

    #region Events

    public event EventHandler? MessageAdded;

    #endregion

    #region Constructor

    public ChatViewModel(
        IDataService dataService,
        INavigationService navigationService)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Messages = new ObservableCollection<ChatMessage>();
        Messages.CollectionChanged += OnMessagesCollectionChanged;
    }

    #endregion

    #region Public Methods

    public async Task SetConversationAsync(string conversationId)
    {
        ArgumentException.ThrowIfNullOrEmpty(conversationId);
        _conversationId = conversationId;

        var conversation = await _dataService.GetConversationAsync(conversationId);
        if (conversation is null)
        {
            return;
        }

        Title = conversation.Title;

        var messages = await _dataService.GetMessagesAsync(conversationId);

        Messages.Clear();

        foreach (var msg in messages)
        {
            msg.IsLoading = false;
            if (!msg.IsComplete && !msg.IsError)
            {
                msg.IsComplete = true;
            }
            Messages.Add(msg);
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task GoBackAsync()
    {
        if (Messages.Count > 0)
        {
            await SaveConversationAsync();
        }

        _navigationService.GoBack();
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputMessage))
            return;

        var userMessage = CreateUserMessage();
        Messages.Add(userMessage);
        await _dataService.SaveMessageAsync(userMessage);

        UpdateTitleFromFirstMessage();
        InputMessage = string.Empty;

        ShowToastMessage("AI 服务已移除，消息已保存");
    }

    [RelayCommand]
    private void EditMessage(ChatMessage? message)
    {
        if (message == null || !message.IsUser)
            return;

        message.BeginEdit();
    }

    [RelayCommand]
    private async Task SaveEditedMessageAsync(ChatMessage? message)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.EditableContent))
            return;

        message.Content = message.EditableContent;
        message.CancelEdit();

        await _dataService.SaveMessageAsync(message);
    }

    [RelayCommand]
    private void CancelEditMessage(ChatMessage? message)
    {
        message?.CancelEdit();
    }

    [RelayCommand]
    private async Task DeleteMessageAsync(ChatMessage? message)
    {
        if (message == null)
            return;

        Messages.Remove(message);
        await _dataService.DeleteMessageAsync(message.Id);

        if (Messages.Count > 0)
        {
            await SaveConversationAsync();
        }
    }

    [RelayCommand]
    private async Task CopyMessageAsync(ChatMessage? message)
    {
        if (message == null || string.IsNullOrEmpty(message.Content))
            return;

        var topLevel = global::Avalonia.Controls.TopLevel.GetTopLevel(
            global::Avalonia.Application.Current?.ApplicationLifetime
                is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null);

        if (topLevel?.Clipboard != null)
        {
            await topLevel.Clipboard.SetTextAsync(message.Content);
            ShowToastMessage("已复制到剪贴板");
        }
    }

    [RelayCommand]
    private void OpenMoreMenu()
    {
        IsMoreMenuOpen = true;
    }

    [RelayCommand]
    private void CloseMoreMenu()
    {
        IsMoreMenuOpen = false;
    }

    [RelayCommand]
    private async Task ClearCurrentConversationAsync()
    {
        if (Messages.Count == 0)
            return;

        Messages.Clear();
        await _dataService.DeleteMessagesAsync(_conversationId);
        ShowToastMessage("会话已清空");
        IsMoreMenuOpen = false;
    }

    [RelayCommand]
    private void RenameConversation()
    {
        IsEditingTitle = true;
        IsMoreMenuOpen = false;
    }

    [RelayCommand]
    private void CancelRenameTitle()
    {
        IsEditingTitle = false;
        EditableTitle = Title;
    }

    [RelayCommand]
    private async Task SaveTitleAsync()
    {
        if (!string.IsNullOrWhiteSpace(EditableTitle))
        {
            Title = EditableTitle;
            await SaveConversationAsync();
        }
        IsEditingTitle = false;
    }

    [RelayCommand]
    private void ExportConversation()
    {
        ShowToastMessage("导出功能开发中...");
        IsMoreMenuOpen = false;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _navigationService.NavigateTo<SettingsViewModel>();
        IsMoreMenuOpen = false;
    }

    #endregion

    #region Private Helper Methods

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasMessages));

        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            MessageAdded?.Invoke(this, EventArgs.Empty);
        }
    }

    private ChatMessage CreateUserMessage()
    {
        return new ChatMessage
        {
            ConversationId = _conversationId,
            Content = InputMessage,
            IsUser = true,
            Timestamp = DateTime.Now
        };
    }

    private void UpdateTitleFromFirstMessage()
    {
        if (Messages.Count != 1 || InputMessage.Length <= 0)
            return;

        Title = InputMessage.Length > MaxTitleLength
            ? InputMessage[..MaxTitleLength] + "..."
            : InputMessage;
    }

    private async Task SaveConversationAsync()
    {
        var lastMessage = Messages.LastOrDefault();
        var preview = GetPreviewText(lastMessage?.Content);

        var conversation = new Conversation
        {
            Id = _conversationId,
            Title = Title,
            Preview = preview,
            ModelName = null,
            UpdatedAt = DateTime.Now
        };

        await _dataService.SaveConversationAsync(conversation);
    }

    private static string GetPreviewText(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return "空会话";

        return content.Length > MaxPreviewLength
            ? content[..MaxPreviewLength] + "..."
            : content;
    }

    private void ShowToastMessage(string message)
    {
        _toastCts?.Cancel();
        _toastCts?.Dispose();
        _toastCts = new System.Threading.CancellationTokenSource();

        ToastMessage = message;
        ShowToast = true;

        _ = HideToastAsync(_toastCts.Token);
    }

    private async Task HideToastAsync(System.Threading.CancellationToken ct)
    {
        try
        {
            await Task.Delay(2000, ct);
            ShowToast = false;
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
    }

    #endregion
}
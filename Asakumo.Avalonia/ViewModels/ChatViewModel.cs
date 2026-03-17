using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// View model for the chat view, managing conversation state and AI interactions.
/// </summary>
public partial class ChatViewModel : ViewModelBase
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;
    private readonly IAIService _aiService;
    private CancellationTokenSource? _responseCts;
    private string _conversationId = Guid.NewGuid().ToString();

    private const int MaxTitleLength = 30;
    private const int MaxPreviewLength = 50;
    private const int SuccessMessageDisplayMs = 3000;
    private const string InterruptedMessage = "[已中断]";

    #region Observable Properties

    [ObservableProperty]
    private string _title = "新会话";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _inputMessage = string.Empty;

    [ObservableProperty]
    private string? _currentModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    private bool _isAiResponding;

    [ObservableProperty]
    private bool _isApiConfigured;

    [ObservableProperty]
    private bool _showErrorDialog;

    [ObservableProperty]
    private bool _showSuccessMessage;

    #endregion

    #region Computed Properties

    public ObservableCollection<ChatMessage> Messages { get; }

    public bool HasMessages => Messages.Count > 0;

    public bool CanSend => !IsAiResponding && !string.IsNullOrWhiteSpace(InputMessage);

    public string ConversationId => _conversationId;

    #endregion

    #region Events

    public event EventHandler? MessageAdded;

    #endregion

    #region Constructor

    public ChatViewModel(
        IDataService dataService,
        INavigationService navigationService,
        IAIService aiService)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));

        Messages = new ObservableCollection<ChatMessage>();
        Messages.CollectionChanged += OnMessagesCollectionChanged;

        _ = InitializeAsync();
    }

    #endregion

    #region Initialization

    private async Task InitializeAsync()
    {
        try
        {
            var settings = await _dataService.GetSettingsAsync();
            UpdateApiConfigStatus(settings);
            UpdateCurrentModelDisplay(settings.CurrentModelId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize: {ex.Message}");
        }
    }

    private void UpdateApiConfigStatus(AppSettings settings)
    {
        IsApiConfigured = !string.IsNullOrEmpty(settings.CurrentProviderId) &&
                          settings.ProviderConfigs.TryGetValue(settings.CurrentProviderId, out var config) &&
                          config.IsValid;
    }

    private void UpdateCurrentModelDisplay(string? modelId)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            CurrentModel = null;
            return;
        }

        var providers = _dataService.GetProviders();
        var model = providers.SelectMany(p => p.Models).FirstOrDefault(m => m.Id == modelId);
        CurrentModel = model?.Name;
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
            _aiService.ClearHistory(conversationId);
            return;
        }

        Title = conversation.Title;
        CurrentModel = conversation.ModelName;

                var messages = await _dataService.GetMessagesAsync(conversationId);

                Messages.Clear();

                RestoreAndAddMessages(messages);

                _aiService.RestoreHistory(conversationId, messages);
    }

    public async Task OnConfigurationCompleteAsync()
    {
        var settings = await _dataService.GetSettingsAsync();
        IsApiConfigured = true;
        ShowSuccessMessage = true;
        UpdateCurrentModelDisplay(settings.CurrentModelId);

        _ = HideSuccessMessageAsync();
    }

    private async Task HideSuccessMessageAsync()
    {
        try
        {
            await Task.Delay(SuccessMessageDisplayMs);
            ShowSuccessMessage = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to hide success message: {ex.Message}");
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task GoBackAsync()
    {
        _responseCts?.Cancel();

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

        if (!IsApiConfigured)
        {
            ShowErrorDialog = true;
            return;
        }

        var userMessage = CreateUserMessage();
        Messages.Add(userMessage);
        await _dataService.SaveMessageAsync(userMessage);

        UpdateTitleFromFirstMessage();
        InputMessage = string.Empty;

        await StreamAiResponseAsync(userMessage.Content);
    }

    [RelayCommand]
    private void StopResponse()
    {
        _responseCts?.Cancel();
        IsAiResponding = false;
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
    private async Task RetryMessageAsync(ChatMessage? message)
    {
        if (message == null || !message.IsError)
            return;

        Messages.Remove(message);

        var lastUserMessage = Messages.LastOrDefault(m => m.IsUser);
        if (lastUserMessage != null)
        {
            await StreamAiResponseAsync(lastUserMessage.Content);
        }
    }

    [RelayCommand]
    private void ConfigureProvider()
    {
        ShowErrorDialog = false;
        _navigationService.NavigateTo<ProviderSelectionViewModel>();
    }

    [RelayCommand]
    private void CloseErrorDialog()
    {
        ShowErrorDialog = false;
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

    private void RestoreAndAddMessages(List<ChatMessage> messages)
    {
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

    private async Task StreamAiResponseAsync(string message)
    {
        IsAiResponding = true;
        ResetCancellationToken();

        var response = CreateAiResponse();
        Messages.Add(response);

        try
        {
            await StreamTokensAsync(response, message);
            await CompleteResponseAsync(response);
        }
        catch (OperationCanceledException)
        {
            HandleCancellation(response);
        }
        catch (Exception ex)
        {
            HandleStreamError(response, ex);
        }
        finally
        {
            FinalizeResponse(response);
        }
    }

    private void ResetCancellationToken()
    {
        _responseCts?.Cancel();
        _responseCts?.Dispose();
        _responseCts = new CancellationTokenSource();
    }

    private ChatMessage CreateAiResponse()
    {
        return new ChatMessage
        {
            ConversationId = _conversationId,
            Content = string.Empty,
            IsUser = false,
            IsLoading = true,
            Timestamp = DateTime.Now
        };
    }

    private async Task StreamTokensAsync(ChatMessage response, string message)
    {
        await foreach (var token in _aiService.StreamChatAsync(_conversationId, message, _responseCts!.Token))
        {
            response.Content += token;
        }
    }

    private async Task CompleteResponseAsync(ChatMessage response)
    {
        response.IsComplete = true;
        await _dataService.SaveMessageAsync(response);
        await SaveConversationAsync();
    }

    private void HandleCancellation(ChatMessage response)
    {
        if (string.IsNullOrEmpty(response.Content))
        {
            response.Content = InterruptedMessage;
        }
        response.IsComplete = true;
    }

    private void HandleStreamError(ChatMessage response, Exception ex)
    {
        response.IsError = true;
        response.Content = ex.Message;
    }

    private void FinalizeResponse(ChatMessage response)
    {
        response.IsLoading = false;
        IsAiResponding = false;
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
            ModelName = CurrentModel,
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

    #endregion
}

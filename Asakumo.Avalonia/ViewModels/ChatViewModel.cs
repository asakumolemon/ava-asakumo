using System;
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

    #region Observable Properties

    /// <summary>
    /// Gets or sets the conversation title.
    /// </summary>
    [ObservableProperty]
    private string _title = "新会话";

    /// <summary>
    /// Gets or sets the input message text.
    /// </summary>
    [ObservableProperty]
    private string _inputMessage = string.Empty;

    /// <summary>
    /// Gets or sets the current model display name.
    /// </summary>
    [ObservableProperty]
    private string? _currentModel;

    /// <summary>
    /// Gets or sets a value indicating whether the AI is currently responding.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    private bool _isAiResponding;

    /// <summary>
    /// Gets or sets a value indicating whether API is configured.
    /// </summary>
    [ObservableProperty]
    private bool _isApiConfigured;

    /// <summary>
    /// Gets or sets a value indicating whether to show the error dialog.
    /// </summary>
    [ObservableProperty]
    private bool _showErrorDialog;

    /// <summary>
    /// Gets or sets a value indicating whether to show the success message.
    /// </summary>
    [ObservableProperty]
    private bool _showSuccessMessage;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the messages in the conversation.
    /// </summary>
    public ObservableCollection<ChatMessage> Messages { get; }

    /// <summary>
    /// Gets a value indicating whether there are any messages.
    /// </summary>
    public bool HasMessages => Messages.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the user can send a message.
    /// </summary>
    public bool CanSend => !IsAiResponding && !string.IsNullOrWhiteSpace(InputMessage);

    /// <summary>
    /// Gets the conversation ID.
    /// </summary>
    public string ConversationId => _conversationId;

    #endregion

    #region Events

    /// <summary>
    /// Event raised when a new message is added (for auto-scroll).
    /// </summary>
    public event EventHandler? MessageAdded;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatViewModel"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="aiService">The AI service.</param>
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
        var settings = await _dataService.GetSettingsAsync();
        UpdateApiConfigStatus(settings);
        UpdateCurrentModelDisplay(settings.CurrentModelId);
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

    /// <summary>
    /// Sets the conversation ID and loads existing messages if any.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    public async Task SetConversationAsync(string conversationId)
    {
        _conversationId = conversationId;

        var conversation = await _dataService.GetConversationAsync(conversationId);

        if (conversation != null)
        {
            Title = conversation.Title;
            CurrentModel = conversation.ModelName;

            var messages = await _dataService.GetMessagesAsync(conversationId);

            Messages.Clear();
            foreach (var msg in messages)
            {
                Messages.Add(msg);
            }

            _aiService.RestoreHistory(conversationId, messages);
        }
        else
        {
            _aiService.ClearHistory(conversationId);
        }
    }

    /// <summary>
    /// Called when API configuration is completed.
    /// </summary>
    public async Task OnConfigurationCompleteAsync()
    {
        var settings = await _dataService.GetSettingsAsync();
        IsApiConfigured = true;
        ShowSuccessMessage = true;
        UpdateCurrentModelDisplay(settings.CurrentModelId);

        // Auto-hide success message after 3 seconds
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            ShowSuccessMessage = false;
        });
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to go back to the previous view.
    /// </summary>
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

    /// <summary>
    /// Command to send the current input message.
    /// </summary>
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

        await SendToAiAsync(userMessage.Content);
    }

    /// <summary>
    /// Command to stop the current AI response.
    /// </summary>
    [RelayCommand]
    private void StopResponse()
    {
        _responseCts?.Cancel();
        IsAiResponding = false;
    }

    /// <summary>
    /// Command to edit a message.
    /// </summary>
    [RelayCommand]
    private void EditMessage(ChatMessage? message)
    {
        if (message == null || !message.IsUser)
            return;

        message.BeginEdit();
    }

    /// <summary>
    /// Command to save an edited message.
    /// </summary>
    [RelayCommand]
    private async Task SaveEditedMessageAsync(ChatMessage? message)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.EditableContent))
            return;

        message.Content = message.EditableContent;
        message.CancelEdit();

        await _dataService.SaveMessageAsync(message);
    }

    /// <summary>
    /// Command to cancel editing a message.
    /// </summary>
    [RelayCommand]
    private void CancelEditMessage(ChatMessage? message)
    {
        message?.CancelEdit();
    }

    /// <summary>
    /// Command to delete a message.
    /// </summary>
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

    /// <summary>
    /// Command to retry a failed message.
    /// </summary>
    [RelayCommand]
    private async Task RetryMessageAsync(ChatMessage? message)
    {
        if (message == null || !message.IsError)
            return;

        Messages.Remove(message);

        var lastUserMessage = Messages.LastOrDefault(m => m.IsUser);
        if (lastUserMessage != null)
        {
            await SendToAiAsync(lastUserMessage.Content);
        }
    }

    /// <summary>
    /// Command to open provider configuration.
    /// </summary>
    [RelayCommand]
    private void ConfigureProvider()
    {
        ShowErrorDialog = false;
        _navigationService.NavigateTo<ProviderSelectionViewModel>();
    }

    /// <summary>
    /// Command to close the error dialog.
    /// </summary>
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

        Title = InputMessage.Length > 30
            ? InputMessage[..30] + "..."
            : InputMessage;
    }

    private async Task SendToAiAsync(string message)
    {
        IsAiResponding = true;

        _responseCts?.Cancel();
        _responseCts?.Dispose();
        _responseCts = new CancellationTokenSource();

        var response = new ChatMessage
        {
            ConversationId = _conversationId,
            Content = string.Empty,
            IsUser = false,
            IsLoading = true,
            Timestamp = DateTime.Now
        };

        Messages.Add(response);

        try
        {
            await foreach (var token in _aiService.StreamChatAsync(_conversationId, message, _responseCts.Token))
            {
                response.Content += token;
            }

            await _dataService.SaveMessageAsync(response);
            await SaveConversationAsync();
        }
        catch (OperationCanceledException)
        {
            if (string.IsNullOrEmpty(response.Content))
            {
                response.Content = "[已中断]";
            }
        }
        catch (Exception ex)
        {
            response.IsError = true;
            response.Content = ex.Message;
        }
        finally
        {
            response.IsLoading = false;
            IsAiResponding = false;
        }
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

        return content.Length > 50
            ? content[..50] + "..."
            : content;
    }

    #endregion
}
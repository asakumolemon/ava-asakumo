using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// View model for the chat view.
/// </summary>
public partial class ChatViewModel : ViewModelBase
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;
    private readonly IAIService _aiService;
    private CancellationTokenSource? _responseCts;
    private string _conversationId = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the conversation title.
    /// </summary>
    [ObservableProperty]
    private string _title = "新会话";

    /// <summary>
    /// Gets or sets the input message.
    /// </summary>
    [ObservableProperty]
    private string _inputMessage = string.Empty;

    /// <summary>
    /// Gets or sets the current model name.
    /// </summary>
    [ObservableProperty]
    private string? _currentModel;

    /// <summary>
    /// Gets or sets a value indicating whether the AI is responding.
    /// </summary>
    [ObservableProperty]
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

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Gets the messages in the conversation.
    /// </summary>
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    /// <summary>
    /// Gets the conversation ID.
    /// </summary>
    public string ConversationId => _conversationId;

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
        _dataService = dataService;
        _navigationService = navigationService;
        _aiService = aiService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var settings = await _dataService.GetSettingsAsync();
        IsApiConfigured = !string.IsNullOrEmpty(settings.CurrentProviderId) &&
                          settings.ProviderConfigs.TryGetValue(settings.CurrentProviderId, out var config) &&
                          config.IsValid;

        if (!string.IsNullOrEmpty(settings.CurrentModelId))
        {
            var providers = _dataService.GetProviders();
            var model = providers
                .SelectMany(p => p.Models)
                .FirstOrDefault(m => m.Id == settings.CurrentModelId);
            CurrentModel = model?.Name;
        }
    }

    /// <summary>
    /// Sets the conversation ID and loads existing messages if any.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    public async Task SetConversationAsync(string conversationId)
    {
        _conversationId = conversationId;

        // Load existing conversation data
        var conversation = await _dataService.GetConversationAsync(conversationId);

        if (conversation != null)
        {
            Title = conversation.Title;
            CurrentModel = conversation.ModelName;

            // Load messages separately
            var messages = await _dataService.GetMessagesAsync(conversationId);
            Messages.Clear();
            foreach (var msg in messages)
            {
                Messages.Add(msg);
            }

            // Restore AI conversation history from loaded messages
            _aiService.RestoreHistory(conversationId, messages);
        }
        else
        {
            // Clear AI history for new conversation
            _aiService.ClearHistory(conversationId);
        }
    }

    /// <summary>
    /// Command to go back.
    /// </summary>
    [RelayCommand]
    private async Task GoBackAsync()
    {
        // Cancel any ongoing response
        _responseCts?.Cancel();

        // Save conversation if it has messages
        if (Messages.Count > 0)
        {
            await SaveConversationAsync();
        }

        _navigationService.GoBack();
    }

    /// <summary>
    /// Command to send a message.
    /// </summary>
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputMessage))
            return;

        if (!IsApiConfigured)
        {
            ShowErrorDialog = true;
            return;
        }

        var userMessage = new ChatMessage
        {
            ConversationId = _conversationId,
            Content = InputMessage,
            IsUser = true,
            Timestamp = DateTime.Now
        };

        Messages.Add(userMessage);

        // Save user message immediately
        await _dataService.SaveMessageAsync(userMessage);

        // Update title from first message
        if (Messages.Count == 1 && InputMessage.Length > 0)
        {
            Title = InputMessage.Length > 30
                ? InputMessage[..30] + "..."
                : InputMessage;
        }

        InputMessage = string.Empty;

        // Send to AI and get response
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
    /// Command to open provider selection.
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

    /// <summary>
    /// Called when API configuration is completed.
    /// </summary>
    public async Task OnConfigurationCompleteAsync()
    {
        var settings = await _dataService.GetSettingsAsync();
        IsApiConfigured = true;
        ShowSuccessMessage = true;

        if (!string.IsNullOrEmpty(settings.CurrentModelId))
        {
            var providers = _dataService.GetProviders();
            var model = providers
                .SelectMany(p => p.Models)
                .FirstOrDefault(m => m.Id == settings.CurrentModelId);
            CurrentModel = model?.Name;
        }

        // Auto-hide success message
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            ShowSuccessMessage = false;
        });
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
            Timestamp = DateTime.Now
        };

        Messages.Add(response);

        try
        {
            await foreach (var token in _aiService.StreamChatAsync(_conversationId, message, _responseCts.Token))
            {
                response.Content += token;
            }

            // Save AI response
            await _dataService.SaveMessageAsync(response);

            // Update conversation metadata
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
            response.Content = $"[错误] {ex.Message}";
        }
        finally
        {
            IsAiResponding = false;
        }
    }

    private async Task SaveConversationAsync()
    {
        // Get preview from last message
        var lastMessage = Messages.LastOrDefault();
        var preview = lastMessage?.Content ?? "空会话";
        if (preview.Length > 50)
        {
            preview = preview[..50] + "...";
        }

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
}

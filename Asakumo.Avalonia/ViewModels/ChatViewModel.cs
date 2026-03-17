using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using ModelDescriptor = Asakumo.Avalonia.Services.ModelDescriptor;
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
    private readonly IModelService _modelService;
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
    private string? _currentModelDisplay;

    [ObservableProperty]
    private string _currentProviderIcon = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    private bool _isAiResponding;

    [ObservableProperty]
    private bool _isApiConfigured;

    [ObservableProperty]
    private bool _showErrorDialog;

    [ObservableProperty]
    private bool _showSuccessMessage;

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

    [ObservableProperty]
    private bool _showModelPicker;

    private CancellationTokenSource? _toastCts;

    #endregion

    #region Computed Properties

    public ObservableCollection<ChatMessage> Messages { get; }

    public bool HasMessages => Messages.Count > 0;

    public bool CanSend => !IsAiResponding && !string.IsNullOrWhiteSpace(InputMessage);

    public string ConversationId => _conversationId;

    /// <summary>
    /// Gets the view model for the model picker popup.
    /// </summary>
    public ModelPickerViewModel? ModelPickerViewModel { get; private set; }

    #endregion

    #region Events

    public event EventHandler? MessageAdded;

    #endregion

    #region Constructor

    public ChatViewModel(
        IDataService dataService,
        INavigationService navigationService,
        IAIService aiService,
        IModelService modelService)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));

        Messages = new ObservableCollection<ChatMessage>();
        Messages.CollectionChanged += OnMessagesCollectionChanged;

        // Subscribe to model changes
        _modelService.CurrentModelChanged += OnCurrentModelChanged;

        _ = InitializeAsync();
    }

    #endregion

    #region Initialization

    private async Task InitializeAsync()
    {
        try
        {
            // Validate current model configuration
            IsApiConfigured = await _modelService.ValidateCurrentModelAsync();

            // Update display with current model
            UpdateCurrentModelDisplay();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize: {ex.Message}");
        }
    }

    private void UpdateCurrentModelDisplay()
    {
        var model = _modelService.CurrentModel;

        if (model == null)
        {
            CurrentModelDisplay = null;
            CurrentProviderIcon = string.Empty;
            return;
        }

        CurrentModelDisplay = model.Name;
        CurrentProviderIcon = GetProviderIcon(model.ProviderName);
    }

    private void OnCurrentModelChanged(object? sender, ModelChangedEventArgs e)
    {
        UpdateCurrentModelDisplay();
        ShowToastMessage($"已切换到: {e.NewModel?.Name}");
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

        var messages = await _dataService.GetMessagesAsync(conversationId);

        Messages.Clear();

        RestoreAndAddMessages(messages);

        _aiService.RestoreHistory(conversationId, messages);
    }

    public async Task OnConfigurationCompleteAsync()
    {
        IsApiConfigured = await _modelService.ValidateCurrentModelAsync();
        ShowSuccessMessage = true;
        UpdateCurrentModelDisplay();

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
        _aiService.ClearHistory(_conversationId);
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
        // TODO: 实现导出功能
        ShowToastMessage("导出功能开发中...");
        IsMoreMenuOpen = false;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _navigationService.NavigateTo<SettingsViewModel>();
        IsMoreMenuOpen = false;
    }

    [RelayCommand]
    private void OpenModelPicker()
    {
        // Create the model picker view model with callbacks
        ModelPickerViewModel = new ModelPickerViewModel(
            _modelService,
            onModelSelected: OnModelSelected,
            onClose: CloseModelPicker);

        ShowModelPicker = true;
    }

    [RelayCommand]
    private void CloseModelPicker()
    {
        ShowModelPicker = false;
        ModelPickerViewModel = null;
    }

    private void OnModelSelected(ModelDescriptor model)
    {
        // Model switch is already handled by the ModelService
        // Just close the picker
        CloseModelPicker();
    }

    /// <summary>
    /// Gets the icon for a provider by name.
    /// </summary>
    private static string GetProviderIcon(string? providerName)
    {
        if (string.IsNullOrEmpty(providerName))
            return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z";

        var lower = providerName.ToLower();
        if (lower.Contains("openai") || lower.Contains("gpt"))
            return "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6Z";
        if (lower.Contains("claude") || lower.Contains("anthropic"))
            return "M12,2L2,7L12,12L22,7L12,2M2,17L12,22L22,17L22,7L12,12L2,7V17Z";
        if (lower.Contains("gemini") || lower.Contains("google"))
            return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4Z";
        if (lower.Contains("deepseek"))
            return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z";
        if (lower.Contains("ollama"))
            return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4Z";

        return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z";
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
            ModelName = CurrentModelDisplay,
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
        _toastCts = new CancellationTokenSource();

        ToastMessage = message;
        ShowToast = true;

        _ = HideToastAsync(_toastCts.Token);
    }

    private async Task HideToastAsync(CancellationToken ct)
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

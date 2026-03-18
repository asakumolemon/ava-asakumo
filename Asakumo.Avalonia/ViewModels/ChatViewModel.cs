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
using Microsoft.Extensions.Logging;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// View model for the chat view, managing conversation state.
/// </summary>
public partial class ChatViewModel : ViewModelBase, INavigationAware
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;
    private readonly IAIService _aiService;
    private readonly ILogger<ChatViewModel> _logger;
    private string _conversationId = Guid.NewGuid().ToString();
    private CancellationTokenSource? _responseCts;

    private const int MaxTitleLength = 30;
    private const int MaxPreviewLength = 50;
    private const int SuccessMessageDisplayMs = 3000;

    #region Observable Properties

    [ObservableProperty]
    private string _title = "新会话";

    [ObservableProperty]
    private string _currentModelName = string.Empty;

    [ObservableProperty]
    private string _currentProviderName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _inputMessage = string.Empty;

    [ObservableProperty]
    private bool _isMoreMenuOpen;

    [ObservableProperty]
    private bool _isModelSwitcherOpen;

    [ObservableProperty]
    private bool _isEditingTitle;

    [ObservableProperty]
    private string _editableTitle = string.Empty;

    [ObservableProperty]
    private string? _toastMessage;

    [ObservableProperty]
    private bool _showToast;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private bool _isAiResponding;

    [ObservableProperty]
    private bool _showConfigPrompt;

    [ObservableProperty]
    private ObservableCollection<ModelGroupItem> _availableModels = new();

    private System.Threading.CancellationTokenSource? _toastCts;

    #endregion

    #region Computed Properties

    public ObservableCollection<ChatMessage> Messages { get; }

    public bool HasMessages => Messages.Count > 0;

    public bool CanSend => !string.IsNullOrWhiteSpace(InputMessage) && !IsAiResponding;

    public string ConversationId => _conversationId;

    #endregion

    #region Events

    public event EventHandler? MessageAdded;

    #endregion

    #region Constructor

    public ChatViewModel(
        IDataService dataService,
        INavigationService navigationService,
        IAIService aiService,
        ILogger<ChatViewModel> logger)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Messages = new ObservableCollection<ChatMessage>();
        Messages.CollectionChanged += OnMessagesCollectionChanged;

        // Initialize current model and provider names
        _currentModelName = _aiService.CurrentModelName ?? _aiService.CurrentModelId ?? string.Empty;
        _currentProviderName = _aiService.CurrentProviderId ?? string.Empty;
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public void OnNavigatedTo(string? conversationId)
    {
        if (!string.IsNullOrEmpty(conversationId))
        {
            _ = SetConversationAsync(conversationId);
        }
    }

    private async Task SetConversationAsync(string conversationId)
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

        // Restore conversation history for AI service
        _aiService.RestoreHistory(conversationId, Messages);
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

        // Check if AI is configured
        if (!_aiService.IsConfigured)
        {
            ShowConfigPrompt = true;
            return;
        }

        // Cancel any existing response before starting a new one
        if (_responseCts != null)
        {
            _responseCts.Cancel();
            _responseCts.Dispose();
            _responseCts = null;
        }

        var userMessage = CreateUserMessage();
        Messages.Add(userMessage);
        await _dataService.SaveMessageAsync(userMessage);

        UpdateTitleFromFirstMessage();
        InputMessage = string.Empty;
        ShowConfigPrompt = false;

        // Send to AI and stream response
        await SendToAiAsync(userMessage.Content);
    }

    [RelayCommand]
    private void StopGeneration()
    {
        _responseCts?.Cancel();
        _responseCts?.Dispose();
        _responseCts = null;
        IsAiResponding = false;
    }

    [RelayCommand]
    private void OpenProviderConfig()
    {
        _navigationService.NavigateTo<ProviderSelectionViewModel>();
        ShowConfigPrompt = false;
    }

    [RelayCommand]
    private void DismissConfigPrompt()
    {
        ShowConfigPrompt = false;
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

    private async Task SendToAiAsync(string userMessage)
    {
        var aiMessage = new ChatMessage
        {
            ConversationId = _conversationId,
            Content = string.Empty,
            IsUser = false,
            IsLoading = true
        };

        Messages.Add(aiMessage);
        IsAiResponding = true;

        _responseCts = new CancellationTokenSource();

        try
        {
            await foreach (var token in _aiService.StreamChatAsync(
                _conversationId,
                userMessage,
                _responseCts.Token))
            {
                aiMessage.Content += token;
            }

            aiMessage.IsLoading = false;
            aiMessage.IsComplete = true;

            await _dataService.SaveMessageAsync(aiMessage);
            await SaveConversationAsync();
        }
        catch (OperationCanceledException)
        {
            aiMessage.IsLoading = false;
            aiMessage.Content += "\n[已中断]";
            aiMessage.IsComplete = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI streaming failed for conversation {ConversationId}", _conversationId);
            aiMessage.IsLoading = false;
            aiMessage.IsError = true;
            aiMessage.Content = $"[错误] {ex.Message}";
        }
        finally
        {
            IsAiResponding = false;
            _responseCts?.Dispose();
            _responseCts = null;
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

    #region Model Switcher Commands

    [RelayCommand]
    private async Task OpenModelSwitcherAsync()
    {
        await LoadAvailableModelsAsync();
        IsModelSwitcherOpen = true;
    }

    [RelayCommand]
    private void CloseModelSwitcher()
    {
        IsModelSwitcherOpen = false;
    }

    [RelayCommand]
    private async Task SwitchModelAsync(ModelItem? model)
    {
        if (model == null)
            return;

        var success = await _aiService.SetCurrentModelAsync(model.ModelId, model.ProviderId);
        if (success)
        {
            CurrentModelName = model.ModelName;
            CurrentProviderName = model.ProviderId;
            ShowToastMessage($"已切换到 {model.ModelName}");
        }
        else
        {
            ShowToastMessage("切换模型失败");
        }

        IsModelSwitcherOpen = false;
    }

    private async Task LoadAvailableModelsAsync()
    {
        AvailableModels.Clear();

        var configs = await _dataService.GetAllProviderConfigsAsync();
        var providers = _dataService.GetProviders();
        var providerMap = providers.ToDictionary(p => p.Id);

        foreach (var config in configs.Where(c => c.HasAvailableModels && c.HasValidCredentials))
        {
            if (!providerMap.TryGetValue(config.ProviderId, out var provider))
                continue;

            var group = new ModelGroupItem
            {
                ProviderName = provider.Name,
                ProviderId = config.ProviderId
            };

            foreach (var modelId in config.AvailableModelIds)
            {
                var model = provider.Models.FirstOrDefault(m => m.Id == modelId);
                if (model != null)
                {
                    group.Models.Add(new ModelItem
                    {
                        ModelId = model.Id,
                        ModelName = model.Name,
                        ProviderId = config.ProviderId,
                        IsSelected = model.Id == _aiService.CurrentModelId && config.ProviderId == _aiService.CurrentProviderId
                    });
                }
            }

            if (group.Models.Count > 0)
            {
                AvailableModels.Add(group);
            }
        }
    }

    #endregion
}

public partial class ModelGroupItem : ObservableObject
{
    [ObservableProperty]
    private string _providerName = string.Empty;

    [ObservableProperty]
    private string _providerId = string.Empty;

    public ObservableCollection<ModelItem> Models { get; } = new();
}

public partial class ModelItem : ObservableObject
{
    [ObservableProperty]
    private string _modelId = string.Empty;

    [ObservableProperty]
    private string _modelName = string.Empty;

    [ObservableProperty]
    private string _providerId = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
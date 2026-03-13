using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    /// Gets the messages in the conversation.
    /// </summary>
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatViewModel"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="navigationService">The navigation service.</param>
    public ChatViewModel(IDataService dataService, INavigationService navigationService)
    {
        _dataService = dataService;
        _navigationService = navigationService;

        var settings = _dataService.GetSettings();
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
    /// Command to go back.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Command to send a message.
    /// </summary>
    [RelayCommand]
    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(InputMessage))
            return;

        if (!IsApiConfigured)
        {
            ShowErrorDialog = true;
            return;
        }

        var message = new ChatMessage
        {
            Content = InputMessage,
            IsUser = true,
            Timestamp = DateTime.Now
        };

        Messages.Add(message);
        InputMessage = string.Empty;

        // Simulate AI response
        _ = SimulateAiResponseAsync();
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
    public void OnConfigurationComplete()
    {
        var settings = _dataService.GetSettings();
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

    private async Task SimulateAiResponseAsync()
    {
        IsAiResponding = true;

        await Task.Delay(500);

        var response = new ChatMessage
        {
            Content = string.Empty,
            IsUser = false,
            Timestamp = DateTime.Now,
            Status = MessageStatus.Streaming
        };

        Messages.Add(response);

        var fullResponse = "你好！我是一个AI助手，可以帮助你回答问题、写作、编程、翻译等各种任务。有什么我可以帮助你的吗？";
        var words = fullResponse.ToCharArray();

        foreach (var word in words)
        {
            response.Content += word;
            await Task.Delay(50);
        }

        response.Status = MessageStatus.Sent;
        IsAiResponding = false;
    }
}

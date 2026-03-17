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
/// View model for the conversation list view.
/// </summary>
public partial class ConversationListViewModel : ViewModelBase
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Gets or sets the conversations grouped by date.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ConversationGroup> _groupedConversations = new();

    /// <summary>
    /// Gets or sets a value indicating whether there are no conversations.
    /// </summary>
    [ObservableProperty]
    private bool _isEmpty;

    /// <summary>
    /// Gets the quick prompt suggestions.
    /// </summary>
    public ObservableCollection<string> QuickPrompts { get; } = new()
    {
        "解释量子力学",
        "写一段代码",
        "翻译"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationListViewModel"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="navigationService">The navigation service.</param>
    public ConversationListViewModel(IDataService dataService, INavigationService navigationService)
    {
        _dataService = dataService;
        _navigationService = navigationService;
    }

    /// <inheritdoc/>
    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        _ = LoadConversationsAsync();
    }

    /// <summary>
    /// Command to create a new conversation.
    /// </summary>
    [RelayCommand]
    private void NewConversation()
    {
        _navigationService.NavigateTo<ChatViewModel>();
    }

    /// <summary>
    /// Command to open settings.
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        _navigationService.NavigateTo<SettingsViewModel>();
    }

    /// <summary>
    /// Command to select a conversation.
    /// </summary>
    /// <param name="conversation">The selected conversation.</param>
    [RelayCommand]
    private void SelectConversation(Conversation conversation)
    {
        // Navigate to chat view with the selected conversation ID
        _navigationService.NavigateTo<ChatViewModel>(conversation.Id);
    }

    /// <summary>
    /// Command to delete a conversation.
    /// </summary>
    /// <param name="conversation">The conversation to delete.</param>
    [RelayCommand]
    private async Task DeleteConversationAsync(Conversation conversation)
    {
        await _dataService.DeleteConversationAsync(conversation.Id);
        await LoadConversationsAsync();
    }

    /// <summary>
    /// Command to pin/unpin a conversation.
    /// </summary>
    /// <param name="conversation">The conversation to pin/unpin.</param>
    [RelayCommand]
    private async Task PinConversationAsync(Conversation conversation)
    {
        conversation.IsPinned = !conversation.IsPinned;
        await _dataService.SaveConversationAsync(conversation);
        await LoadConversationsAsync();
    }

    /// <summary>
    /// Command to use a quick prompt.
    /// </summary>
    /// <param name="prompt">The quick prompt.</param>
    [RelayCommand]
    private void UseQuickPrompt(string prompt)
    {
        // Create new conversation with the prompt
        _navigationService.NavigateTo<ChatViewModel>();
    }

    private async Task LoadConversationsAsync()
    {
        var conversations = await _dataService.GetConversationsAsync();
        IsEmpty = conversations.Count == 0;

        // Sort: Pinned first, then by UpdatedAt descending
        var sortedConversations = conversations
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.UpdatedAt)
            .ToList();

        GroupedConversations.Clear();

        // Separate pinned conversations (always show at top, no date grouping)
        var pinnedConversations = sortedConversations.Where(c => c.IsPinned).ToList();
        var regularConversations = sortedConversations.Where(c => !c.IsPinned).ToList();

        // Add pinned group if any
        if (pinnedConversations.Any())
        {
            GroupedConversations.Add(new ConversationGroup
            {
                DateLabel = "置顶",
                Conversations = new ObservableCollection<Conversation>(pinnedConversations)
            });
        }

        // Group regular conversations by date
        var regularGroups = regularConversations
            .GroupBy(c => GetDateGroup(c.UpdatedAt))
            .Select(g => new ConversationGroup
            {
                DateLabel = g.Key,
                Conversations = new ObservableCollection<Conversation>(g)
            });

        foreach (var group in regularGroups)
        {
            GroupedConversations.Add(group);
        }
    }

    private static string GetDateGroup(DateTime date)
    {
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        if (date.Date == today)
            return "今天";
        if (date.Date == yesterday)
            return "昨天";
        return "更早";
    }
}

/// <summary>
/// Represents a group of conversations by date.
/// </summary>
public class ConversationGroup
{
    /// <summary>
    /// Gets or sets the date label.
    /// </summary>
    public string DateLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversations in this group.
    /// </summary>
    public ObservableCollection<Conversation> Conversations { get; set; } = new();
}

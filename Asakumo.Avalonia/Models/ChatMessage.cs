using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SQLite;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// Represents a version of a message (used for AI message regeneration history).
/// </summary>
public class MessageVersion
{
    /// <summary>
    /// Gets or sets the content of this version.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this version was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets a value indicating whether this version is an error.
    /// </summary>
    public bool IsError { get; set; }
}

/// <summary>
/// Represents a single chat message in a conversation.
/// Implements ObservableObject to support real-time UI updates during streaming.
/// </summary>
[Table("chat_messages")]
public partial class ChatMessage : ObservableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for the message.
    /// </summary>
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the conversation ID this message belongs to.
    /// </summary>
    [Indexed]
    public string ConversationId { get; set; } = string.Empty;

    private string _content = string.Empty;

    /// <summary>
    /// Gets or sets the content of the message.
    /// Manually triggers notifications for computed properties during streaming.
    /// </summary>
    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value))
            {
                // Notify computed properties when content changes
                OnPropertyChanged(nameof(HasContent));
                OnPropertyChanged(nameof(IsThinking));
                OnPropertyChanged(nameof(IsStreaming));
            }
        }
    }

    /// <summary>
    /// Gets or sets the editable content (used during edit mode).
    /// </summary>
    [ObservableProperty]
    private string _editableContent = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the message is in edit mode.
    /// </summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>
    /// Gets or sets a value indicating whether the message is loading (streaming).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStreaming))]
    [NotifyPropertyChangedFor(nameof(HasContent))]
    [NotifyPropertyChangedFor(nameof(IsThinking))]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets a value indicating whether the message has an error.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsComplete))]
    private bool _isError;

    /// <summary>
    /// Gets or sets a value indicating whether the message is complete.
    /// This is persisted to database to correctly restore message state when reopening conversations.
    /// </summary>
    [ObservableProperty]
    private bool _isComplete;

    /// <summary>
    /// Gets a value indicating whether the message has content.
    /// </summary>
    public bool HasContent => !string.IsNullOrEmpty(Content);

    /// <summary>
    /// Gets a value indicating whether the message is thinking (loading without content).
    /// </summary>
    public bool IsThinking => IsLoading && !HasContent;

    /// <summary>
    /// Gets a value indicating whether the message is streaming (loading with content).
    /// </summary>
    public bool IsStreaming => IsLoading && HasContent;

    /// <summary>
    /// Gets or sets a value indicating whether the message is from the user (true) or AI (false).
    /// </summary>
    public bool IsUser { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the message.
    /// </summary>
    [Indexed]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Starts editing this message.
    /// </summary>
    public void BeginEdit()
    {
        EditableContent = Content;
        IsEditing = true;
    }

    /// <summary>
    /// Cancels editing and restores original content.
    /// </summary>
    public void CancelEdit()
    {
        EditableContent = string.Empty;
        IsEditing = false;
    }

    #region Version History

    /// <summary>
    /// Gets or sets the JSON serialized version history.
    /// Not directly used in UI - use VersionHistory property instead.
    /// </summary>
    public string? VersionHistoryJson { get; set; }

    /// <summary>
    /// Gets or sets the index of the currently displayed version.
    /// -1 means displaying the original/current content.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayVersionNumber))]
    [NotifyPropertyChangedFor(nameof(CanGoToPreviousVersion))]
    [NotifyPropertyChangedFor(nameof(CanGoToNextVersion))]
    private int _currentVersionIndex = -1;

    /// <summary>
    /// Gets the list of historical versions for this message.
    /// </summary>
    [Ignore]
    public List<MessageVersion> VersionHistory
    {
        get
        {
            if (string.IsNullOrEmpty(VersionHistoryJson))
                return new List<MessageVersion>();
            try
            {
                return JsonSerializer.Deserialize<List<MessageVersion>>(VersionHistoryJson) ?? new List<MessageVersion>();
            }
            catch
            {
                return new List<MessageVersion>();
            }
        }
    }

    /// <summary>
    /// Gets the total number of versions including the original.
    /// </summary>
    [Ignore]
    public int VersionCount => VersionHistory.Count + 1;

    /// <summary>
    /// Gets a value indicating whether this message has version history.
    /// </summary>
    [Ignore]
    public bool HasVersionHistory => VersionHistory.Count > 0;

    /// <summary>
    /// Gets the current version number for display (1-based).
    /// </summary>
    [Ignore]
    public int DisplayVersionNumber => CurrentVersionIndex == -1 ? VersionCount : CurrentVersionIndex + 1;

    /// <summary>
    /// Saves the current content as a new version before regenerating.
    /// </summary>
    public void SaveCurrentAsVersion()
    {
        var versions = VersionHistory;
        versions.Add(new MessageVersion
        {
            Content = Content,
            Timestamp = Timestamp,
            IsError = IsError
        });
        VersionHistoryJson = JsonSerializer.Serialize(versions);

        // Reset to show the new version (will be created after this)
        CurrentVersionIndex = -1;

        // Notify UI that version-related properties have changed
        OnPropertyChanged(nameof(VersionCount));
        OnPropertyChanged(nameof(HasVersionHistory));
        OnPropertyChanged(nameof(DisplayVersionNumber));
    }

    /// <summary>
    /// Switches to a specific version.
    /// </summary>
    /// <param name="versionIndex">-1 for original, 0+ for history versions.</param>
    public void SwitchToVersion(int versionIndex)
    {
        var versions = VersionHistory;

        if (versionIndex == -1)
        {
            // Switch to original (latest) version
            CurrentVersionIndex = -1;
            OnPropertyChanged(nameof(DisplayVersionNumber));
            return;
        }

        if (versionIndex < 0 || versionIndex >= versions.Count)
            return;

        CurrentVersionIndex = versionIndex;
        var version = versions[versionIndex];
        Content = version.Content;
        Timestamp = version.Timestamp;
        IsError = version.IsError;
        OnPropertyChanged(nameof(DisplayVersionNumber));
    }

    /// <summary>
    /// Navigates to the previous (older) version.
    /// </summary>
    public void GoToPreviousVersion()
    {
        if (!HasVersionHistory) return;
        
        var targetIndex = CurrentVersionIndex == -1 ? VersionHistory.Count - 1 : CurrentVersionIndex - 1;
        if (targetIndex >= 0)
        {
            SwitchToVersion(targetIndex);
        }
    }

    /// <summary>
    /// Navigates to the next (newer) version.
    /// </summary>
    public void GoToNextVersion()
    {
        if (CurrentVersionIndex == -1) return;
        
        var targetIndex = CurrentVersionIndex + 1;
        if (targetIndex >= VersionHistory.Count)
        {
            // Go back to original
            CurrentVersionIndex = -1;
        }
        else
        {
            SwitchToVersion(targetIndex);
        }
    }

    /// <summary>
    /// Gets a value indicating whether there is a previous version available.
    /// </summary>
    [Ignore]
    public bool CanGoToPreviousVersion => HasVersionHistory && CurrentVersionIndex != 0;

    /// <summary>
    /// Gets a value indicating whether there is a next version available.
    /// </summary>
    [Ignore]
    public bool CanGoToNextVersion => CurrentVersionIndex != -1;

    #endregion
}

/// <summary>
/// Represents the status of a message (for UI display only, not persisted).
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// Message is being sent.
    /// </summary>
    Sending,

    /// <summary>
    /// Message has been sent successfully.
    /// </summary>
    Sent,

    /// <summary>
    /// Message delivery failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Message is being streamed.
    /// </summary>
    Streaming
}

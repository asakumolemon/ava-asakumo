using System;
using SQLite;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Asakumo.Avalonia.Models;

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

    /// <summary>
    /// Gets or sets the content of the message.
    /// Uses ObservableProperty to notify UI of changes during streaming.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasContent))]
    [NotifyPropertyChangedFor(nameof(IsStreaming))]
    private string _content = string.Empty;

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

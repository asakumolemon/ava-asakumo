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
    private string _content = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the message is from the user (true) or AI (false).
    /// </summary>
    public bool IsUser { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the message.
    /// </summary>
    [Indexed]
    public DateTime Timestamp { get; set; } = DateTime.Now;
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

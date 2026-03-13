using System;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// Represents a single chat message in a conversation.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the message.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the message is from the user (true) or AI (false).
    /// </summary>
    public bool IsUser { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the message.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the status of the message.
    /// </summary>
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
}

/// <summary>
/// Represents the status of a message.
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

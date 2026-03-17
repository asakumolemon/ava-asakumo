using System;
using SQLite;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// Represents a conversation session.
/// </summary>
[Table("conversations")]
public class Conversation
{
    /// <summary>
    /// Gets or sets the unique identifier for the conversation.
    /// </summary>
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the title of the conversation.
    /// </summary>
    [MaxLength(200)]
    public string Title { get; set; } = "新会话";

    /// <summary>
    /// Gets or sets the preview text (last message or first message).
    /// </summary>
    [MaxLength(500)]
    public string Preview { get; set; } = "空会话";

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    [Indexed]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    [Indexed]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the AI model used for this conversation.
    /// </summary>
    [MaxLength(100)]
    public string? ModelName { get; set; }

    /// <summary>
    /// Gets or sets the AI provider ID.
    /// </summary>
    [MaxLength(50)]
    public string? ProviderId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the conversation is pinned.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the conversation has unread messages.
    /// </summary>
    public bool HasUnread { get; set; }

    /// <summary>
    /// Gets or sets the unread message count.
    /// </summary>
    public int UnreadCount { get; set; }
}

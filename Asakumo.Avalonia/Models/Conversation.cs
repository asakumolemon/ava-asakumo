using System;
using System.Collections.Generic;
using System.Linq;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// Represents a conversation session.
/// </summary>
public class Conversation
{
    /// <summary>
    /// Gets or sets the unique identifier for the conversation.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the title of the conversation.
    /// </summary>
    public string Title { get; set; } = "新会话";

    /// <summary>
    /// Gets or sets the list of messages in the conversation.
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the AI model used for this conversation.
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Gets or sets the AI provider ID.
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Gets the preview text for the conversation (first message or last message preview).
    /// </summary>
    public string Preview => Messages.Count > 0
        ? Messages[0].Content.Length > 50
            ? Messages[0].Content[..50] + "..."
            : Messages[0].Content
        : "空会话";
}

using System;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// Represents an AI model with its capabilities and specifications.
/// </summary>
public class AIModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the model (e.g., "gpt-4o").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the model (e.g., "GPT-4o").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum context length in tokens.
    /// </summary>
    public int MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports vision/image input.
    /// </summary>
    public bool SupportsVision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports function calling.
    /// </summary>
    public bool SupportsFunctionCalling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports streaming output.
    /// </summary>
    public bool SupportsStreaming { get; set; } = true;

    /// <summary>
    /// Gets or sets the provider ID this model belongs to.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional description of the model.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Creates a shallow copy of this model.
    /// </summary>
    public AIModel Clone()
    {
        return new AIModel
        {
            Id = Id,
            Name = Name,
            MaxTokens = MaxTokens,
            SupportsVision = SupportsVision,
            SupportsFunctionCalling = SupportsFunctionCalling,
            SupportsStreaming = SupportsStreaming,
            ProviderId = ProviderId,
            Description = Description
        };
    }
}

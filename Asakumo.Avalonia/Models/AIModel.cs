using System.Collections.Generic;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// Represents an AI model.
/// </summary>
public class AIModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the model.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the model.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the model.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category of the model.
    /// </summary>
    public ModelCategory Category { get; set; }

    /// <summary>
    /// Gets or sets the capability tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether this model is recommended.
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// Gets or sets the provider ID this model belongs to.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;
}

/// <summary>
/// Represents the category of an AI model.
/// </summary>
public enum ModelCategory
{
    /// <summary>
    /// Recommended models.
    /// </summary>
    Recommended,

    /// <summary>
    /// Reasoning models for complex tasks.
    /// </summary>
    Reasoning,

    /// <summary>
    /// Chat/conversation models.
    /// </summary>
    Chat,

    /// <summary>
    /// Vision-enabled models.
    /// </summary>
    Vision
}

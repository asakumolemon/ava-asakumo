using System.Collections.Generic;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// Represents an AI service provider.
/// </summary>
public class AIProvider
{
    /// <summary>
    /// Gets or sets the unique identifier for the provider.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the provider.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider category.
    /// </summary>
    public ProviderCategory Category { get; set; }

    /// <summary>
    /// Gets or sets the description of the provider.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default base URL for the API.
    /// </summary>
    public string DefaultBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of available models.
    /// </summary>
    public List<AIModel> Models { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the provider requires an API key.
    /// </summary>
    public bool RequiresApiKey { get; set; } = true;

    /// <summary>
    /// Gets or sets the icon or logo identifier.
    /// </summary>
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// Represents the category of an AI provider.
/// </summary>
public enum ProviderCategory
{
    /// <summary>
    /// Quick start providers (no configuration needed).
    /// </summary>
    QuickStart,

    /// <summary>
    /// Popular cloud-based providers.
    /// </summary>
    Popular,

    /// <summary>
    /// Local/self-hosted models.
    /// </summary>
    Local
}

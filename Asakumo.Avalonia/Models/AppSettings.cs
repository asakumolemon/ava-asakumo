using System;
using System.Collections.Generic;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// Represents application settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets the current language.
    /// </summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled.
    /// </summary>
    public bool IsDarkMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the currently selected provider ID.
    /// </summary>
    public string? CurrentProviderId { get; set; }

    /// <summary>
    /// Gets or sets the currently selected model ID.
    /// </summary>
    public string? CurrentModelId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the welcome screen has been shown.
    /// </summary>
    public bool HasSeenWelcome { get; set; }

    /// <summary>
    /// Gets or sets the provider configurations (API keys, base URLs, etc.).
    /// </summary>
    public Dictionary<string, ProviderConfig> ProviderConfigs { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of recently used models.
    /// </summary>
    public List<RecentModel> RecentModels { get; set; } = new();
}

/// <summary>
/// Represents configuration for a specific provider.
/// </summary>
public class ProviderConfig
{
    /// <summary>
    /// Gets or sets the API key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the base URL.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the configuration is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the custom display name for this provider.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the last used model for this provider.
    /// </summary>
    public string? LastUsedModelId { get; set; }

    /// <summary>
    /// Gets or sets the list of available models for this provider (cached).
    /// </summary>
    public List<ModelInfo> AvailableModels { get; set; } = new();

    /// <summary>
    /// Gets or sets the last time the model list was updated.
    /// </summary>
    public DateTime? ModelsLastUpdated { get; set; }
}

/// <summary>
/// Represents a recently used model.
/// </summary>
public class RecentModel
{
    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time when this model was last used.
    /// </summary>
    public DateTime UsedAt { get; set; }

    /// <summary>
    /// Gets or sets the custom name for this model (user-defined).
    /// </summary>
    public string? CustomName { get; set; }
}

/// <summary>
/// Represents model information for provider management.
/// </summary>
public class ModelInfo
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
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the provider ID this model belongs to.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owner of the model (from API response).
    /// </summary>
    public string? OwnedBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this model is selected by user.
    /// </summary>
    public bool IsSelected { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this model supports vision/image input.
    /// </summary>
    public bool SupportsVision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this model supports function calling.
    /// </summary>
    public bool SupportsFunctionCalling { get; set; }

    /// <summary>
    /// Gets or sets the context window size (in tokens).
    /// </summary>
    public int? ContextWindow { get; set; }
}

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
}

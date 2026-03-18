using System;
using System.Text.Json.Serialization;

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
    /// Gets or sets a value indicating whether the welcome screen has been shown.
    /// </summary>
    public bool HasSeenWelcome { get; set; }

    #region AI Provider Settings

    /// <summary>
    /// Gets or sets the currently selected AI provider ID.
    /// </summary>
    public string? SelectedProviderId { get; set; }

    /// <summary>
    /// Gets or sets the currently selected model ID.
    /// </summary>
    public string? SelectedModelId { get; set; }

    /// <summary>
    /// Gets a value indicating whether the user has configured any AI provider.
    /// This is a computed property and is not persisted.
    /// </summary>
    [JsonIgnore]
    public bool HasProviderConfigured => !string.IsNullOrWhiteSpace(SelectedProviderId);

    #endregion
}
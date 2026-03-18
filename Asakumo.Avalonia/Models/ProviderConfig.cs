using System;
using System.Collections.Generic;
using System.Text.Json;
using SQLite;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// Represents the configuration for an AI provider, stored in SQLite.
/// </summary>
[Table("provider_configs")]
public class ProviderConfig
{
    /// <summary>
    /// Gets or sets the provider ID (primary key, e.g., "openai", "anthropic").
    /// </summary>
    [PrimaryKey]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key for the provider.
    /// </summary>
    [MaxLength(500)]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the base URL for API requests (optional, for custom endpoints).
    /// </summary>
    [MaxLength(500)]
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the selected model ID for this provider.
    /// </summary>
    [MaxLength(100)]
    public string? SelectedModelId { get; set; }

    /// <summary>
    /// Gets or sets the list of available model IDs for this provider (stored as JSON array).
    /// </summary>
    [MaxLength(2000)]
    public string? AvailableModelIdsJson { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this provider is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this config was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this config was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional configuration as JSON (for future extensibility).
    /// </summary>
    [MaxLength(2000)]
    public string? AdditionalConfig { get; set; }

    /// <summary>
    /// Gets a value indicating whether this config has valid API credentials.
    /// </summary>
    public bool HasValidCredentials => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    /// Gets the list of available model IDs for this provider.
    /// This is a runtime property not stored directly in the database.
    /// </summary>
    [Ignore]
    public List<string> AvailableModelIds
    {
        get
        {
            if (string.IsNullOrEmpty(AvailableModelIdsJson))
                return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(AvailableModelIdsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
        set
        {
            AvailableModelIdsJson = value != null && value.Count > 0
                ? JsonSerializer.Serialize(value)
                : null;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this provider has any available models configured.
    /// </summary>
    public bool HasAvailableModels => AvailableModelIds.Count > 0;

    /// <summary>
    /// Creates a deep copy of this configuration.
    /// </summary>
    public ProviderConfig Clone()
    {
        return new ProviderConfig
        {
            ProviderId = ProviderId,
            ApiKey = ApiKey,
            BaseUrl = BaseUrl,
            SelectedModelId = SelectedModelId,
            AvailableModelIdsJson = AvailableModelIdsJson,
            IsEnabled = IsEnabled,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            AdditionalConfig = AdditionalConfig
        };
    }
}

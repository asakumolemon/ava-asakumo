using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Service for managing multiple AI providers and models.
/// </summary>
public interface IProviderManager
{
    /// <summary>
    /// Event raised when the current model is changed.
    /// </summary>
    event EventHandler<ModelChangedEventArgs>? ModelChanged;

    /// <summary>
    /// Gets all enabled providers.
    /// </summary>
    /// <returns>List of enabled provider information.</returns>
    Task<IEnumerable<ProviderViewModel>> GetEnabledProvidersAsync();

    /// <summary>
    /// Gets all configured providers (including disabled ones).
    /// </summary>
    /// <returns>List of all configured providers.</returns>
    Task<IEnumerable<ProviderViewModel>> GetConfiguredProvidersAsync();

    /// <summary>
    /// Gets available models for a specific provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="forceRefresh">Whether to force refresh from API.</param>
    /// <returns>List of available models.</returns>
    Task<IEnumerable<ModelViewModel>> GetProviderModelsAsync(string providerId, bool forceRefresh = false);

    /// <summary>
    /// Gets all enabled models across all enabled providers.
    /// </summary>
    /// <returns>List of all available models.</returns>
    Task<IEnumerable<ModelViewModel>> GetAllEnabledModelsAsync();

    /// <summary>
    /// Switches to a specific model.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="modelId">The model ID.</param>
    Task SwitchModelAsync(string providerId, string modelId);

    /// <summary>
    /// Gets the currently selected model.
    /// </summary>
    /// <returns>The current model information.</returns>
    Task<ModelViewModel?> GetCurrentModelAsync();

    /// <summary>
    /// Gets recently used models.
    /// </summary>
    /// <param name="count">Maximum number of models to return.</param>
    /// <returns>List of recently used models.</returns>
    Task<IEnumerable<RecentModelViewModel>> GetRecentModelsAsync(int count = 5);

    /// <summary>
    /// Sets whether a provider is enabled.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="enabled">Whether to enable the provider.</param>
    Task SetProviderEnabledAsync(string providerId, bool enabled);

    /// <summary>
    /// Sets whether a specific model is enabled.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="modelId">The model ID.</param>
    /// <param name="enabled">Whether to enable the model.</param>
    Task SetModelEnabledAsync(string providerId, string modelId, bool enabled);

    /// <summary>
    /// Tests a provider connection.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>True if the connection is successful.</returns>
    Task<bool> TestProviderAsync(string providerId);

    /// <summary>
    /// Updates the display name for a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="displayName">The new display name.</param>
    Task SetProviderDisplayNameAsync(string providerId, string? displayName);

    /// <summary>
    /// Refreshes the model list for a provider from the API.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>True if refresh was successful.</returns>
    Task<bool> RefreshProviderModelsAsync(string providerId);
}

/// <summary>
/// Event arguments for model changed event.
/// </summary>
public class ModelChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the old provider ID.
    /// </summary>
    public string? OldProviderId { get; set; }

    /// <summary>
    /// Gets or sets the old model ID.
    /// </summary>
    public string? OldModelId { get; set; }

    /// <summary>
    /// Gets or sets the new provider ID.
    /// </summary>
    public string NewProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new model ID.
    /// </summary>
    public string NewModelId { get; set; } = string.Empty;
}

/// <summary>
/// View model for provider information.
/// </summary>
public class ProviderViewModel
{
    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider category.
    /// </summary>
    public ProviderCategory Category { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this provider is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this provider is configured.
    /// </summary>
    public bool IsConfigured { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this provider is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the number of enabled models.
    /// </summary>
    public int EnabledModelCount { get; set; }

    /// <summary>
    /// Gets or sets the icon path or identifier.
    /// </summary>
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// View model for model information.
/// </summary>
public class ModelViewModel
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this model is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the currently selected model.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Gets or sets the model category.
    /// </summary>
    public ModelCategory Category { get; set; }
}

/// <summary>
/// View model for recently used model information.
/// </summary>
public class RecentModelViewModel
{
    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the custom name.
    /// </summary>
    public string? CustomName { get; set; }

    /// <summary>
    /// Gets or sets the time when this model was last used.
    /// </summary>
    public DateTime UsedAt { get; set; }

    /// <summary>
    /// Gets the display name (custom name or model name).
    /// </summary>
    public string DisplayName => CustomName ?? ModelName;
}

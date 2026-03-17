using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Simplified service for managing AI models and providers.
/// </summary>
public interface IModelService
{
    /// <summary>
    /// Event raised when the current model is changed.
    /// </summary>
    event EventHandler<ModelChangedEventArgs>? CurrentModelChanged;

    /// <summary>
    /// Gets the currently selected model.
    /// </summary>
    ModelDescriptor? CurrentModel { get; }

    /// <summary>
    /// Gets all available providers with their enabled state.
    /// </summary>
    Task<IReadOnlyList<ProviderInfo>> GetProvidersAsync();

    /// <summary>
    /// Gets all enabled models across all providers.
    /// </summary>
    Task<IReadOnlyList<ModelDescriptor>> GetAllModelsAsync();

    /// <summary>
    /// Gets models for a specific provider.
    /// </summary>
    Task<IReadOnlyList<ModelDescriptor>> GetModelsByProviderAsync(string providerId);

    /// <summary>
    /// Gets recently used models.
    /// </summary>
    Task<IReadOnlyList<ModelDescriptor>> GetRecentModelsAsync(int count = 5);

    /// <summary>
    /// Switches to a specific model.
    /// </summary>
    Task SwitchModelAsync(string providerId, string modelId);

    /// <summary>
    /// Validates that the current model is configured and ready to use.
    /// </summary>
    Task<bool> ValidateCurrentModelAsync();

    /// <summary>
    /// Tests a provider connection.
    /// </summary>
    Task<bool> TestProviderAsync(string providerId);

    /// <summary>
    /// Refreshes the model list for a provider from the API.
    /// </summary>
    Task<bool> RefreshProviderModelsAsync(string providerId);

    /// <summary>
    /// Sets the display name for a provider.
    /// </summary>
    Task SetProviderDisplayNameAsync(string providerId, string? displayName);
}

/// <summary>
/// Provider information for model selection.
/// </summary>
public class ProviderInfo
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
    /// Gets or sets the provider icon path.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this provider is properly configured.
    /// </summary>
    public bool IsConfigured { get; set; }

    /// <summary>
    /// Gets or sets the provider color for UI theming.
    /// </summary>
    public string? Color { get; set; }
}

/// <summary>
/// Model descriptor for selection.
/// </summary>
public class ModelDescriptor
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
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider icon.
    /// </summary>
    public string ProviderIcon { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the model capabilities.
    /// </summary>
    public ModelCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when this model was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets the full display name including provider.
    /// </summary>
    public string FullName => $"{ProviderName} / {Name}";
}

/// <summary>
/// Model capabilities flags.
/// </summary>
public class ModelCapabilities
{
    /// <summary>
    /// Supports streaming responses.
    /// </summary>
    public bool Streaming { get; set; } = true;

    /// <summary>
    /// Supports vision/image input.
    /// </summary>
    public bool Vision { get; set; }

    /// <summary>
    /// Supports function calling.
    /// </summary>
    public bool FunctionCalling { get; set; }
}

/// <summary>
/// Event arguments for model changed event.
/// </summary>
public class ModelChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the old model descriptor.
    /// </summary>
    public ModelDescriptor? OldModel { get; set; }

    /// <summary>
    /// Gets or sets the new model descriptor.
    /// </summary>
    public ModelDescriptor? NewModel { get; set; }
}

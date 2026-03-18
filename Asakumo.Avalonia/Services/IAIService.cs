using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Provides AI chat functionality with history management.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Streams chat response tokens.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="message">The user message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of response tokens.</returns>
    IAsyncEnumerable<string> StreamChatAsync(
        string conversationId,
        string message,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a chat request and returns the full response.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="message">The user message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The complete response text.</returns>
    Task<string> ChatAsync(
        string conversationId,
        string message,
        CancellationToken ct = default);

    /// <summary>
    /// Clears the conversation history from memory.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    void ClearHistory(string conversationId);

    /// <summary>
    /// Restores conversation history from persisted messages.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="messages">The historical messages.</param>
    void RestoreHistory(string conversationId, IEnumerable<ChatMessage> messages);

    /// <summary>
    /// Gets the available models for the current provider.
    /// </summary>
    /// <returns>List of available models.</returns>
    Task<IEnumerable<AIModel>> GetAvailableModelsAsync();

    /// <summary>
    /// Validates the current provider configuration.
    /// </summary>
    /// <returns>True if configuration is valid.</returns>
    Task<bool> ValidateConfigurationAsync();

    /// <summary>
    /// Validates a specific provider configuration.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="baseUrl">Optional base URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if configuration is valid.</returns>
    Task<bool> ValidateProviderAsync(
        string providerId,
        string apiKey,
        string? baseUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a value indicating whether a provider is configured.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gets the current provider ID.
    /// </summary>
    string? CurrentProviderId { get; }

    /// <summary>
    /// Gets the current model ID.
    /// </summary>
    string? CurrentModelId { get; }

    /// <summary>
    /// Gets the current model display name.
    /// </summary>
    string? CurrentModelName { get; }

    /// <summary>
    /// Reloads the provider configuration from settings.
    /// </summary>
    Task ReloadConfigurationAsync();

    /// <summary>
    /// Sets the current model for AI chat.
    /// </summary>
    /// <param name="modelId">The model ID to use.</param>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>True if successful.</returns>
    Task<bool> SetCurrentModelAsync(string modelId, string providerId);
}

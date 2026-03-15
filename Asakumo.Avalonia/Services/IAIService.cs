using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Provides AI chat functionality.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Streams a chat response token by token.
    /// </summary>
    /// <param name="conversationId">The conversation ID for context.</param>
    /// <param name="message">The user message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of response tokens.</returns>
    IAsyncEnumerable<string> StreamChatAsync(string conversationId, string message, CancellationToken ct = default);

    /// <summary>
    /// Sends a message and returns the complete response.
    /// </summary>
    /// <param name="conversationId">The conversation ID for context.</param>
    /// <param name="message">The user message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The complete AI response.</returns>
    Task<string> ChatAsync(string conversationId, string message, CancellationToken ct = default);

    /// <summary>
    /// Clears the chat history for a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    void ClearHistory(string conversationId);

    /// <summary>
    /// Restores chat history from a list of messages.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="messages">The messages to restore.</param>
    void RestoreHistory(string conversationId, IEnumerable<ChatMessage> messages);

    /// <summary>
    /// Gets available models for the current provider.
    /// </summary>
    /// <returns>List of available models.</returns>
    Task<IEnumerable<AIModel>> GetAvailableModelsAsync();

    /// <summary>
    /// Validates the current provider configuration.
    /// </summary>
    /// <returns>True if configuration is valid.</returns>
    Task<bool> ValidateConfigurationAsync();

    /// <summary>
    /// Validates a provider configuration without saving it.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="baseUrl">Optional custom base URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the configuration is valid.</returns>
    Task<bool> ValidateProviderAsync(string providerId, string apiKey, string? baseUrl, CancellationToken ct = default);
}

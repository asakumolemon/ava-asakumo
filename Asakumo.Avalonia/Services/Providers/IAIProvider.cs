using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;

namespace Asakumo.Avalonia.Services.Providers;

/// <summary>
/// Represents a message in the provider-agnostic format.
/// </summary>
public record ProviderMessage(string Role, string Content);

/// <summary>
/// Defines the interface for AI provider adapters.
/// </summary>
public interface IAIProvider
{
    /// <summary>
    /// Gets the provider ID.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Streams chat completion tokens.
    /// </summary>
    /// <param name="messages">The conversation messages.</param>
    /// <param name="modelId">The model ID to use.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of response tokens.</returns>
    IAsyncEnumerable<string> StreamChatAsync(
        IEnumerable<ProviderMessage> messages,
        string modelId,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a chat completion request and returns the full response.
    /// </summary>
    /// <param name="messages">The conversation messages.</param>
    /// <param name="modelId">The model ID to use.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The complete response text.</returns>
    Task<string> ChatAsync(
        IEnumerable<ProviderMessage> messages,
        string modelId,
        CancellationToken ct = default);

    /// <summary>
    /// Validates the provider configuration (API key, base URL).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if configuration is valid.</returns>
    Task<bool> ValidateAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the available models for this provider.
    /// </summary>
    /// <returns>List of available models.</returns>
    Task<IEnumerable<AIModel>> GetModelsAsync(CancellationToken ct = default);
}

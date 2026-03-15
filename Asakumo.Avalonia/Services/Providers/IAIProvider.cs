using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;

namespace Asakumo.Avalonia.Services.Providers;

/// <summary>
/// Represents a chat message for AI providers.
/// </summary>
public record ProviderMessage(string Role, string Content);

/// <summary>
/// Interface for AI provider adapters.
/// </summary>
public interface IAIProvider
{
    /// <summary>
    /// Gets the provider ID.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Streams a chat response.
    /// </summary>
    /// <param name="messages">The chat history.</param>
    /// <param name="modelId">The model to use.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of response tokens.</returns>
    IAsyncEnumerable<string> StreamChatAsync(IEnumerable<ProviderMessage> messages, string modelId, CancellationToken ct = default);

    /// <summary>
    /// Gets available models for this provider.
    /// </summary>
    /// <returns>List of available models.</returns>
    Task<IEnumerable<AIModel>> GetModelsAsync();

    /// <summary>
    /// Validates the provider configuration.
    /// </summary>
    /// <returns>True if configuration is valid.</returns>
    Task<bool> ValidateAsync();
}

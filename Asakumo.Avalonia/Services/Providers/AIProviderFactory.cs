using System;
using Asakumo.Avalonia.Models;
using Microsoft.Extensions.Logging;

namespace Asakumo.Avalonia.Services.Providers;

/// <summary>
/// Factory class for creating AI provider instances.
/// </summary>
public class AIProviderFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIProviderFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    public AIProviderFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates an AI provider instance based on the provider type.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="baseUrl">Optional base URL override.</param>
    /// <returns>The provider instance.</returns>
    /// <exception cref="ArgumentException">Thrown when provider type is not supported.</exception>
    public IAIProvider CreateProvider(string providerId, string apiKey, string? baseUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        var provider = GetProviderDefinition(providerId)
            ?? throw new ArgumentException($"Unknown provider: {providerId}");

        return provider.Type switch
        {
            AIProviderType.OpenAICompatible => new OpenAICompatibleProvider(
                providerId,
                apiKey,
                baseUrl ?? provider.DefaultBaseUrl,
                _loggerFactory.CreateLogger<OpenAICompatibleProvider>()),

            AIProviderType.Google => new GeminiProvider(
                apiKey,
                baseUrl,
                _loggerFactory.CreateLogger<GeminiProvider>()),

            AIProviderType.Anthropic => new AnthropicProvider(
                apiKey,
                baseUrl,
                _loggerFactory.CreateLogger<AnthropicProvider>()),

            _ => throw new ArgumentException($"Unsupported provider type: {provider.Type}")
        };
    }

    /// <summary>
    /// Gets the static provider definition.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>The provider definition, or null if not found.</returns>
    public static AIProvider? GetProviderDefinition(string providerId)
    {
        return AIProvider.GetAllProviders()
            .Find(p => p.Id.Equals(providerId, StringComparison.OrdinalIgnoreCase));
    }
}

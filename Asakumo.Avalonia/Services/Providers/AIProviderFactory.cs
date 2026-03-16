using System;
using System.Collections.Generic;

namespace Asakumo.Avalonia.Services.Providers;

/// <summary>
/// Provider type enumeration.
/// </summary>
public enum ProviderType
{
    /// <summary>
    /// OpenAI-compatible API.
    /// </summary>
    OpenAI,

    /// <summary>
    /// Anthropic Claude API.
    /// </summary>
    Anthropic,

    /// <summary>
    /// Google Gemini API.
    /// </summary>
    Gemini
}

/// <summary>
/// Provider information.
/// </summary>
public record ProviderInfo(
    string ProviderId,
    string Name,
    string DefaultBaseUrl,
    ProviderType Type,
    bool RequiresApiKey = true
);

/// <summary>
/// Registry of known AI providers.
/// </summary>
public static class ProviderRegistry
{
    /// <summary>
    /// Gets all known providers.
    /// </summary>
    public static IReadOnlyDictionary<string, ProviderInfo> Providers => new Dictionary<string, ProviderInfo>
    {
        ["openai"] = new("openai", "OpenAI", "https://api.openai.com/v1", ProviderType.OpenAI),
        ["deepseek"] = new("deepseek", "DeepSeek", "https://api.deepseek.com", ProviderType.OpenAI),
        ["ollama"] = new("ollama", "Ollama", "http://localhost:11434/v1", ProviderType.OpenAI, RequiresApiKey: false),
        ["anthropic"] = new("anthropic", "Anthropic", "https://api.anthropic.com", ProviderType.Anthropic),
        ["google"] = new("google", "Google Gemini", "https://generativelanguage.googleapis.com", ProviderType.Gemini),
    };
}

/// <summary>
/// Factory for creating AI provider instances.
/// </summary>
public class AIProviderFactory
{
    /// <summary>
    /// Creates an AI provider based on configuration.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="baseUrl">Optional custom base URL.</param>
    /// <param name="modelId">The model ID to use.</param>
    /// <returns>An AI provider instance.</returns>
    public IAIProvider CreateProvider(string providerId, string? apiKey, string? baseUrl, string modelId)
    {
        if (!ProviderRegistry.Providers.TryGetValue(providerId, out var info))
        {
            throw new NotSupportedException($"不支持的 Provider: {providerId}");
        }

        var effectiveBaseUrl = baseUrl ?? info.DefaultBaseUrl;

        return info.Type switch
        {
            ProviderType.OpenAI => new OpenAICompatibleProvider(
                providerId,
                apiKey ?? "no-key",
                modelId,
                effectiveBaseUrl),
            ProviderType.Anthropic => new AnthropicProvider(
                providerId,
                apiKey ?? string.Empty,
                modelId,
                effectiveBaseUrl),
            ProviderType.Gemini => new GeminiProvider(
                providerId,
                apiKey ?? string.Empty,
                modelId),
            _ => throw new NotSupportedException($"不支持的 Provider 类型: {info.Type}")
        };
    }
}
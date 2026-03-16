using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google.GenAI;
using Google.GenAI.Types;
using Asakumo.Avalonia.Models;

namespace Asakumo.Avalonia.Services.Providers;

/// <summary>
/// Google Gemini provider implementation.
/// </summary>
public class GeminiProvider : IAIProvider
{
    private readonly Client _client;
    private readonly string _providerId;
    private readonly string _defaultModelId;
    private readonly string? _systemPrompt;

    public string ProviderId => _providerId;

    /// <summary>
    /// Initializes a new instance of the Gemini provider.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="modelId">The default model ID.</param>
    /// <param name="baseUrl">Optional custom base URL (not used for Gemini).</param>
    /// <param name="systemPrompt">Optional system prompt.</param>
    public GeminiProvider(
        string providerId,
        string apiKey,
        string modelId,
        string? baseUrl = null,
        string? systemPrompt = null)
    {
        _providerId = providerId;
        _defaultModelId = modelId;
        _systemPrompt = systemPrompt;

        _client = new Client(apiKey: apiKey);
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        IEnumerable<ProviderMessage> messages,
        string modelId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Build Gemini contents
        var contents = new List<Content>();

        foreach (var msg in messages)
        {
            var role = msg.Role.ToLowerInvariant() switch
            {
                "user" => "user",
                "assistant" => "model",
                _ => null
            };

            if (role != null)
            {
                contents.Add(new Content
                {
                    Role = role,
                    Parts = new List<Part> { new() { Text = msg.Content } }
                });
            }
        }

        // Build config with system prompt
        var config = new GenerateContentConfig();
        if (!string.IsNullOrEmpty(_systemPrompt))
        {
            config.SystemInstruction = new Content
            {
                Parts = new List<Part> { new() { Text = _systemPrompt } }
            };
        }

        // Stream response
        await foreach (var chunk in _client.Models.GenerateContentStreamAsync(
            model: modelId,
            contents: contents,
            config: config,
            cancellationToken: ct))
        {
            if (chunk.Candidates != null && chunk.Candidates.Count > 0)
            {
                var candidate = chunk.Candidates[0];
                if (candidate.Content?.Parts != null)
                {
                    foreach (var part in candidate.Content.Parts)
                    {
                        if (!string.IsNullOrEmpty(part.Text))
                        {
                            yield return part.Text;
                        }
                    }
                }
            }
        }
    }

    public Task<IEnumerable<AIModel>> GetModelsAsync()
    {
        // Return empty list - models are defined in ProviderRegistry
        return Task.FromResult<IEnumerable<AIModel>>(Array.Empty<AIModel>());
    }

    public async Task<bool> ValidateAsync()
    {
        try
        {
            // Try a minimal request to validate the API key
            await foreach (var chunk in _client.Models.GenerateContentStreamAsync(
                model: _defaultModelId,
                contents: "Hi"))
            {
                return true; // Success on first chunk
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}

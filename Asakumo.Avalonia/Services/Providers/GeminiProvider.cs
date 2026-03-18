using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;

namespace Asakumo.Avalonia.Services.Providers;

/// <summary>
/// Google Gemini provider implementation using Google.GenAI SDK.
/// </summary>
public class GeminiProvider : IAIProvider
{
    private readonly string _providerId;
    private readonly string _apiKey;
    private readonly ILogger<GeminiProvider> _logger;
    private Client? _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeminiProvider"/> class.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="logger">The logger instance.</param>
    public GeminiProvider(
        string providerId,
        string apiKey,
        ILogger<GeminiProvider> logger)
    {
        _providerId = providerId;
        _apiKey = apiKey;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string ProviderId => _providerId;

    private Client Client => _client ??= new Client(apiKey: _apiKey);

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> StreamChatAsync(
        IEnumerable<ProviderMessage> messages,
        string modelId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var contents = ConvertMessages(messages);

        IAsyncEnumerable<GenerateContentResponse> stream;
        try
        {
            stream = Client.Models.GenerateContentStreamAsync(
                model: modelId,
                contents: contents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Gemini stream for {ModelId}", modelId);
            yield break;
        }

        await foreach (var chunk in stream)
        {
            ct.ThrowIfCancellationRequested();

            var text = chunk.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (!string.IsNullOrEmpty(text))
            {
                yield return text;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<string> ChatAsync(
        IEnumerable<ProviderMessage> messages,
        string modelId,
        CancellationToken ct = default)
    {
        var contents = ConvertMessages(messages);

        try
        {
            var response = await Client.Models.GenerateContentAsync(
                model: modelId,
                contents: contents);

            return response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Gemini chat for {ModelId}", modelId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            // Send a minimal request to validate credentials
            var contents = new List<Content>
            {
                new()
                {
                    Parts = new List<Part> { new() { Text = "Hi" } },
                    Role = "user"
                }
            };

            await Client.Models.GenerateContentAsync(
                model: "gemini-2.0-flash",
                contents: contents);

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Validation timeout for {ProviderId}", _providerId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Validation failed for {ProviderId}", _providerId);
            return false;
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<AIModel>> GetModelsAsync(CancellationToken ct = default)
    {
        var provider = AIProviderFactory.GetProviderDefinition(_providerId);
        return Task.FromResult(provider?.Models ?? Enumerable.Empty<AIModel>());
    }

    private List<Content> ConvertMessages(IEnumerable<ProviderMessage> messages)
    {
        var contents = new List<Content>();

        foreach (var msg in messages)
        {
            contents.Add(new Content
            {
                Parts = new List<Part> { new() { Text = msg.Content } },
                Role = msg.Role.ToLowerInvariant() == "user" ? "user" : "model"
            });
        }

        return contents;
    }
}

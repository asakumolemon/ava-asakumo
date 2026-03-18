using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Microsoft.Extensions.Logging;

namespace Asakumo.Avalonia.Services.Providers;

/// <summary>
/// Anthropic Claude provider implementation using HTTP API.
/// </summary>
public class AnthropicProvider : IAIProvider
{
    private readonly string _providerId;
    private readonly string _apiKey;
    private readonly ILogger<AnthropicProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicProvider"/> class.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="logger">The logger instance.</param>
    public AnthropicProvider(
        string providerId,
        string apiKey,
        ILogger<AnthropicProvider> logger)
    {
        _providerId = providerId;
        _apiKey = apiKey;
        _logger = logger;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.anthropic.com")
        };

        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc/>
    public string ProviderId => _providerId;

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> StreamChatAsync(
        IEnumerable<ProviderMessage> messages,
        string modelId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = CreateMessagesRequest(messages, modelId, stream: true);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("/v1/messages", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Claude stream cancelled for {ModelId}", modelId);
            yield break;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Claude stream for {ModelId}", modelId);
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line.Substring(6);

            ClaudeStreamEvent? streamEvent;
            try
            {
                streamEvent = JsonSerializer.Deserialize<ClaudeStreamEvent>(data, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse stream event: {Data}", data);
                continue;
            }

            if (streamEvent?.Type == "content_block_delta" &&
                streamEvent.Delta?.Type == "text_delta" &&
                streamEvent.Delta.Text != null)
            {
                yield return streamEvent.Delta.Text;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<string> ChatAsync(
        IEnumerable<ProviderMessage> messages,
        string modelId,
        CancellationToken ct = default)
    {
        var request = CreateMessagesRequest(messages, modelId, stream: false);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("/v1/messages", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<ClaudeResponse>(responseJson, _jsonOptions);

            return result?.Content?.FirstOrDefault()?.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Claude chat for {ModelId}", modelId);
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

            var messages = new List<ProviderMessage>
            {
                new("user", "Hi")
            };

            await ChatAsync(messages, "claude-3-haiku-20240307", cts.Token);
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

    private ClaudeRequest CreateMessagesRequest(IEnumerable<ProviderMessage> messages, string modelId, bool stream)
    {
        var request = new ClaudeRequest
        {
            Model = modelId,
            MaxTokens = 4096,
            Stream = stream
        };

        foreach (var msg in messages)
        {
            if (msg.Role.ToLowerInvariant() == "system")
            {
                request.System = msg.Content;
            }
            else
            {
                request.Messages.Add(new ClaudeMessage
                {
                    Role = msg.Role.ToLowerInvariant(),
                    Content = msg.Content
                });
            }
        }

        return request;
    }

    #region API Models

    private class ClaudeRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 4096;

        [JsonPropertyName("messages")]
        public List<ClaudeMessage> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("system")]
        public string? System { get; set; }
    }

    private class ClaudeMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class ClaudeResponse
    {
        [JsonPropertyName("content")]
        public List<ClaudeContent>? Content { get; set; }
    }

    private class ClaudeContent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class ClaudeStreamEvent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("delta")]
        public ClaudeDelta? Delta { get; set; }
    }

    private class ClaudeDelta
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    #endregion
}
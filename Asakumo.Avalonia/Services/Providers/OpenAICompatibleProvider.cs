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
/// OpenAI-compatible provider implementation supporting OpenAI, DeepSeek, Ollama, etc.
/// Uses direct HTTP API for better control and compatibility.
/// </summary>
public class OpenAICompatibleProvider : IAIProvider
{
    private readonly string _providerId;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly ILogger<OpenAICompatibleProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAICompatibleProvider"/> class.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="baseUrl">The base URL for API requests.</param>
    /// <param name="logger">The logger instance.</param>
    public OpenAICompatibleProvider(
        string providerId,
        string apiKey,
        string baseUrl,
        ILogger<OpenAICompatibleProvider> logger)
    {
        _providerId = providerId;
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
        _logger = logger;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl)
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
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
        var request = CreateChatRequest(messages, modelId, stream: true);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("/chat/completions", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Chat stream cancelled for {ProviderId}/{ModelId}", _providerId, modelId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting stream for {ProviderId}/{ModelId}", _providerId, modelId);
            throw;
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line.Substring(6);
            if (data == "[DONE]") break;

            OpenAIStreamResponse? streamResponse;
            try
            {
                streamResponse = JsonSerializer.Deserialize<OpenAIStreamResponse>(data, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse stream event: {Data}", data);
                continue;
            }

            var delta = streamResponse?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(delta))
            {
                yield return delta;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<string> ChatAsync(
        IEnumerable<ProviderMessage> messages,
        string modelId,
        CancellationToken ct = default)
    {
        var request = CreateChatRequest(messages, modelId, stream: false);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("/chat/completions", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<OpenAIResponse>(responseJson, _jsonOptions);

            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat completion for {ProviderId}/{ModelId}", _providerId, modelId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateAsync(CancellationToken ct = default)
    {
        try
        {
            // First, try to list models as a lightweight validation
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            var response = await _httpClient.GetAsync("/models", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Validation successful for {ProviderId} via /models endpoint", _providerId);
                return true;
            }

            // If /models endpoint fails but returns 401/403, credentials are invalid
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Invalid credentials for {ProviderId}: {StatusCode}", _providerId, response.StatusCode);
                return false;
            }

            // For other errors, try a minimal chat request with a known model
            // Use a new cancellation token source for the second phase
            using var chatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            chatCts.CancelAfter(TimeSpan.FromSeconds(10));

            var testModel = GetTestModel();
            var messages = new List<ProviderMessage>
            {
                new("user", "Hi")
            };

            await ChatAsync(messages, testModel, chatCts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Validation timeout for {ProviderId}", _providerId);
            return false;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                                               ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("Invalid credentials for {ProviderId}", _providerId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Validation failed for {ProviderId}", _providerId);
            return false;
        }
    }

    /// <summary>
    /// Gets a test model ID for validation based on the provider.
    /// Uses the first available model from provider definition to avoid hardcoding.
    /// </summary>
    private string GetTestModel()
    {
        // Try to get the first available model from provider definition
        var provider = AIProviderFactory.GetProviderDefinition(_providerId);
        var firstModel = provider?.Models.FirstOrDefault();
        if (firstModel != null)
        {
            return firstModel.Id;
        }

        // Fallback to hardcoded models if provider definition is not available
        return _providerId.ToLowerInvariant() switch
        {
            "openai" => "gpt-3.5-turbo",
            "deepseek" => "deepseek-chat",
            "ollama" => "llama3.2",
            _ => "gpt-3.5-turbo"
        };
    }

    /// <inheritdoc/>
    public Task<IEnumerable<AIModel>> GetModelsAsync(CancellationToken ct = default)
    {
        // Return static models from provider definition
        var provider = AIProviderFactory.GetProviderDefinition(_providerId);
        return Task.FromResult(provider?.Models ?? Enumerable.Empty<AIModel>());
    }

    private OpenAIRequest CreateChatRequest(IEnumerable<ProviderMessage> messages, string modelId, bool stream)
    {
        var request = new OpenAIRequest
        {
            Model = modelId,
            Stream = stream
        };

        foreach (var msg in messages)
        {
            request.Messages.Add(new OpenAIMessage
            {
                Role = msg.Role.ToLowerInvariant(),
                Content = msg.Content
            });
        }

        return request;
    }

    #region API Models

    private class OpenAIRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenAIMessage> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIChoice>? Choices { get; set; }
    }

    private class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public OpenAIMessage? Message { get; set; }
    }

    private class OpenAIStreamResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIStreamChoice>? Choices { get; set; }
    }

    private class OpenAIStreamChoice
    {
        [JsonPropertyName("delta")]
        public OpenAIDelta? Delta { get; set; }
    }

    private class OpenAIDelta
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    #endregion
}
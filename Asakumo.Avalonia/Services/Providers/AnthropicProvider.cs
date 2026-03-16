using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;

namespace Asakumo.Avalonia.Services.Providers;

/// <summary>
/// Anthropic Claude provider implementation using direct HTTP calls.
/// </summary>
public class AnthropicProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _providerId;
    private readonly string _defaultModelId;
    private readonly string? _systemPrompt;
    private readonly string _baseUrl;

    public string ProviderId => _providerId;

    /// <summary>
    /// Initializes a new instance of the Anthropic provider.
    /// </summary>
    public AnthropicProvider(
        string providerId,
        string apiKey,
        string modelId,
        string? baseUrl = null,
        string? systemPrompt = null)
    {
        _providerId = providerId;
        _defaultModelId = modelId;
        _systemPrompt = systemPrompt;
        _baseUrl = baseUrl ?? "https://api.anthropic.com";

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        IEnumerable<ProviderMessage> messages,
        string modelId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Build request body
        var messagesArray = new JsonArray();
        foreach (var msg in messages)
        {
            var role = msg.Role.ToLowerInvariant() switch
            {
                "user" => "user",
                "assistant" => "assistant",
                _ => null
            };

            if (role != null)
            {
                messagesArray.Add(new JsonObject
                {
                    ["role"] = role,
                    ["content"] = msg.Content
                });
            }
        }

        var requestBody = new JsonObject
        {
            ["model"] = modelId,
            ["max_tokens"] = 4096,
            ["messages"] = messagesArray,
            ["stream"] = true
        };

        if (!string.IsNullOrEmpty(_systemPrompt))
        {
            requestBody["system"] = _systemPrompt;
        }

        var content = new StringContent(
            requestBody.ToJsonString(),
            Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/messages")
        {
            Content = content
        };

        var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]")
                break;

            string? textContent = null;
            try
            {
                var json = JsonNode.Parse(data);
                var type = json?["type"]?.ToString();

                if (type == "content_block_delta")
                {
                    textContent = json?["delta"]?["text"]?.ToString();
                }
                else if (type == "content_block_start")
                {
                    textContent = json?["content_block"]?["text"]?.ToString();
                }
            }
            catch (JsonException)
            {
                // Skip malformed JSON - continue to next line
            }

            if (!string.IsNullOrEmpty(textContent))
            {
                yield return textContent;
            }
        }
    }

    public Task<IEnumerable<AIModel>> GetModelsAsync()
    {
        return Task.FromResult<IEnumerable<AIModel>>(Array.Empty<AIModel>());
    }

    public async Task<bool> ValidateAsync()
    {
        try
        {
            var requestBody = new JsonObject
            {
                ["model"] = _defaultModelId,
                ["max_tokens"] = 10,
                ["messages"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = "Hi"
                    }
                }
            };

            var content = new StringContent(
                requestBody.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/v1/messages", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

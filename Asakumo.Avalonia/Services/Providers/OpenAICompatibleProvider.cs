using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Asakumo.Avalonia.Services.Providers;

/// <summary>
/// OpenAI-compatible provider implementation.
/// Supports OpenAI, DeepSeek, Ollama, and other OpenAI-compatible APIs.
/// </summary>
public class OpenAICompatibleProvider : IAIProvider
{
    private readonly OpenAIClient _openAIClient;
    private readonly ChatClient _chatClient;
    private readonly HttpClient _httpClient;
    private readonly string _providerId;
    private readonly string _modelId;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly List<ProviderMessage> _systemPrompt;

    public string ProviderId => _providerId;

    /// <summary>
    /// Initializes a new instance of the OpenAI-compatible provider.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="modelId">The default model ID.</param>
    /// <param name="baseUrl">The base URL (optional, defaults to OpenAI).</param>
    /// <param name="systemPrompt">Optional system prompt.</param>
    public OpenAICompatibleProvider(
        string providerId,
        string apiKey,
        string modelId,
        string? baseUrl = null,
        string? systemPrompt = null)
    {
        _providerId = providerId;
        _modelId = modelId;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://api.openai.com/v1";

        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(baseUrl))
        {
            clientOptions.Endpoint = new Uri(baseUrl);
        }

        var credential = new ApiKeyCredential(apiKey);
        _openAIClient = new OpenAIClient(credential, clientOptions);
        _chatClient = _openAIClient.GetChatClient(_modelId);

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        _systemPrompt = string.IsNullOrEmpty(systemPrompt)
            ? new List<ProviderMessage>()
            : new List<ProviderMessage> { new("system", systemPrompt) };
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        IEnumerable<ProviderMessage> messages,
        string modelId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Build message list
        var chatMessages = new List<OpenAI.Chat.ChatMessage>();

        // Add system prompt if present
        foreach (var msg in _systemPrompt)
        {
            chatMessages.Add(new SystemChatMessage(msg.Content));
        }

        // Add conversation history
        foreach (var msg in messages)
        {
            chatMessages.Add(msg.Role.ToLowerInvariant() switch
            {
                "user" => new UserChatMessage(msg.Content),
                "assistant" => new AssistantChatMessage(msg.Content),
                "system" => new SystemChatMessage(msg.Content),
                _ => throw new ArgumentException($"Unknown role: {msg.Role}")
            });
        }

        // Stream response
        await foreach (var update in _chatClient.CompleteChatStreamingAsync(chatMessages, cancellationToken: ct))
        {
            if (update.ContentUpdate != null)
            {
                foreach (var part in update.ContentUpdate)
                {
                    if (part.Kind == ChatMessageContentPartKind.Text && !string.IsNullOrEmpty(part.Text))
                    {
                        yield return part.Text;
                    }
                }
            }
        }
    }

    public async Task<IEnumerable<AIModel>> GetModelsAsync()
    {
        try
        {
            // Use HTTP client to call /models endpoint
            var response = await _httpClient.GetAsync($"{_baseUrl}/models");
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(jsonContent);

            var models = new List<AIModel>();

            if (json?["data"] is JsonArray dataArray)
            {
                foreach (var item in dataArray)
                {
                    var modelId = item?["id"]?.ToString();
                    if (!string.IsNullOrEmpty(modelId) && ShouldIncludeModel(modelId))
                    {
                        models.Add(new AIModel
                        {
                            Id = modelId,
                            Name = GetDisplayName(modelId),
                            Description = GetModelDescription(modelId) ?? string.Empty,
                            ProviderId = _providerId ?? string.Empty
                        });
                    }
                }
            }

            return models.OrderBy(m => m.Name);
        }
        catch (Exception)
        {
            // If fetching models fails, return empty list
            return Array.Empty<AIModel>();
        }
    }

    private static bool ShouldIncludeModel(string modelId)
    {
        // Include chat models, exclude embeddings, moderation, audio, etc.
        var excludePrefixes = new[]
        {
            "text-embedding", "text-moderation", "whisper", "tts", "dall-e",
            "davinci", "curie", "babbage", "ada" // Old completion models
        };

        foreach (var prefix in excludePrefixes)
        {
            if (modelId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static string GetDisplayName(string modelId)
    {
        // Convert model ID to display name
        return modelId switch
        {
            "gpt-4o" => "GPT-4o",
            "gpt-4o-mini" => "GPT-4o Mini",
            "gpt-4-turbo" => "GPT-4 Turbo",
            "gpt-4" => "GPT-4",
            "gpt-3.5-turbo" => "GPT-3.5 Turbo",
            "o1" => "o1",
            "o1-mini" => "o1 Mini",
            "o1-preview" => "o1 Preview",
            "o3-mini" => "o3 Mini",
            "deepseek-chat" => "DeepSeek Chat",
            "deepseek-reasoner" => "DeepSeek Reasoner",
            _ => modelId
        };
    }

    private static string? GetModelDescription(string modelId)
    {
        return modelId switch
        {
            "gpt-4o" => "最强大的多模态模型",
            "gpt-4o-mini" => "快速且经济",
            "gpt-4-turbo" => "GPT-4 的优化版本",
            "gpt-4" => "强大的推理能力",
            "gpt-3.5-turbo" => "快速且经济实惠",
            "o1" => "高级推理模型",
            "o1-mini" => "快速推理",
            "o3-mini" => "最新推理模型",
            "deepseek-chat" => "通用对话模型",
            "deepseek-reasoner" => "深度推理模型",
            _ => null
        };
    }

    public async Task<bool> ValidateAsync()
    {
        try
        {
            // Try a minimal request to validate the API key
            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                new UserChatMessage("Hi")
            };

            await foreach (var _ in _chatClient.CompleteChatStreamingAsync(messages))
            {
                return true; // Success on first token
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}

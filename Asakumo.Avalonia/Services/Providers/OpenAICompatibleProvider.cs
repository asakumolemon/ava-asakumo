using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    private readonly ChatClient _chatClient;
    private readonly string _providerId;
    private readonly string _modelId;
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

        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(baseUrl))
        {
            clientOptions.Endpoint = new Uri(baseUrl);
        }

        var credential = new ApiKeyCredential(apiKey);
        var client = new OpenAIClient(credential, clientOptions);
        _chatClient = client.GetChatClient(_modelId);

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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services.Providers;
using Microsoft.Extensions.Logging;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Implementation of the AI service.
/// </summary>
public class AIService : IAIService
{
    private readonly IDataService _dataService;
    private readonly AIProviderFactory _providerFactory;
    private readonly ILogger<AIService>? _logger;
    private readonly ConcurrentDictionary<string, List<ProviderMessage>> _conversationHistory = new();
    private IAIProvider? _currentProvider;
    private string? _currentModelId;

    /// <summary>
    /// Initializes a new instance of the AIService.
    /// </summary>
    public AIService(
        IDataService dataService,
        AIProviderFactory providerFactory,
        ILogger<AIService>? logger = null)
    {
        _dataService = dataService;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        string conversationId,
        string message,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Ensure provider is initialized
        if (_currentProvider == null)
        {
            if (!await InitializeProviderAsync())
            {
                yield return "[错误] 请先配置 AI 服务提供商";
                yield break;
            }
        }

        // Get or create conversation history
        var history = _conversationHistory.GetOrAdd(conversationId, _ => new List<ProviderMessage>());

        // Add user message to history
        history.Add(new ProviderMessage("user", message));

        // Limit history length
        TrimHistory(history);

        // Use Channel to enable proper streaming with exception handling
        var channel = Channel.CreateUnbounded<string>();
        var fullResponse = new StringBuilder();

        // Start streaming in background
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var token in _currentProvider!.StreamChatAsync(history, _currentModelId!, ct))
                {
                    fullResponse.Append(token);
                    await channel.Writer.WriteAsync(token, ct);
                }

                // Add assistant response to history on success
                if (fullResponse.Length > 0)
                {
                    history.Add(new ProviderMessage("assistant", fullResponse.ToString()));
                }
            }
            catch (OperationCanceledException)
            {
                await channel.Writer.WriteAsync("\n[已中断]", ct);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Chat request failed");
                await channel.Writer.WriteAsync($"\n{GetErrorMessage(ex)}", ct);
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, ct);

        // Yield tokens from channel
        await foreach (var token in channel.Reader.ReadAllAsync(ct))
        {
            yield return token;
        }
    }

    public async Task<string> ChatAsync(string conversationId, string message, CancellationToken ct = default)
    {
        var response = new StringBuilder();
        await foreach (var token in StreamChatAsync(conversationId, message, ct))
        {
            response.Append(token);
        }
        return response.ToString();
    }

    public void ClearHistory(string conversationId)
    {
        _conversationHistory.TryRemove(conversationId, out _);
    }

    public Task<IEnumerable<AIModel>> GetAvailableModelsAsync()
    {
        if (_currentProvider == null)
        {
            return Task.FromResult(Enumerable.Empty<AIModel>());
        }

        return _currentProvider.GetModelsAsync();
    }

    public async Task<bool> ValidateConfigurationAsync()
    {
        if (_currentProvider == null)
        {
            if (!await InitializeProviderAsync())
            {
                return false;
            }
        }

        return await _currentProvider!.ValidateAsync();
    }

    /// <summary>
    /// Sets the current provider and model.
    /// </summary>
    public async Task SetProviderAsync(string providerId, string modelId)
    {
        var settings = await _dataService.GetSettingsAsync();
        settings.CurrentProviderId = providerId;
        settings.CurrentModelId = modelId;
        await _dataService.SaveSettingsAsync(settings);

        // Reset provider to force reinitialization
        _currentProvider = null;
        _currentModelId = null;
    }

    private async Task<bool> InitializeProviderAsync()
    {
        var settings = await _dataService.GetSettingsAsync();

        if (string.IsNullOrEmpty(settings.CurrentProviderId) ||
            string.IsNullOrEmpty(settings.CurrentModelId))
        {
            _logger?.LogWarning("No provider configured");
            return false;
        }

        if (!settings.ProviderConfigs.TryGetValue(settings.CurrentProviderId, out var config) ||
            !config.IsValid ||
            string.IsNullOrEmpty(config.ApiKey))
        {
            _logger?.LogWarning("Provider {ProviderId} not configured or invalid", settings.CurrentProviderId);
            return false;
        }

        try
        {
            _currentProvider = _providerFactory.CreateProvider(
                settings.CurrentProviderId,
                config.ApiKey,
                config.BaseUrl,
                settings.CurrentModelId);
            _currentModelId = settings.CurrentModelId;
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize provider {ProviderId}", settings.CurrentProviderId);
            return false;
        }
    }

    private static void TrimHistory(List<ProviderMessage> history, int maxMessages = 20)
    {
        if (history.Count > maxMessages)
        {
            var toRemove = history.Count - maxMessages;
            history.RemoveRange(0, toRemove);
        }
    }

    private static string GetErrorMessage(Exception ex)
    {
        return ex switch
        {
            System.Net.Http.HttpRequestException httpEx when httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized
                => "[错误] API Key 无效，请检查配置",
            System.Net.Http.HttpRequestException httpEx when httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                => "[错误] 请求过于频繁，请稍后再试",
            System.Net.Http.HttpRequestException httpEx when httpEx.StatusCode == System.Net.HttpStatusCode.PaymentRequired
                => "[错误] API 额度不足，请充值后重试",
            TaskCanceledException
                => "[错误] 请求超时，请检查网络连接",
            UriFormatException
                => "[错误] Base URL 格式错误",
            _ => $"[错误] 无法连接到 AI 服务: {ex.Message}"
        };
    }
}

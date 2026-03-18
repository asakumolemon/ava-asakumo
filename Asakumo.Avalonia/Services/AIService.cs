using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services.Providers;
using Microsoft.Extensions.Logging;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Implementation of AI service with history management and streaming support.
/// </summary>
public class AIService : IAIService
{
    private readonly IDataService _dataService;
    private readonly AIProviderFactory _providerFactory;
    private readonly ILogger<AIService> _logger;

    private readonly ConcurrentDictionary<string, List<ProviderMessage>> _conversationHistory = new();
    private IAIProvider? _currentProvider;
    private string? _currentModelId;

    private const int MaxHistoryMessages = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIService"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="providerFactory">The provider factory.</param>
    /// <param name="logger">The logger instance.</param>
    public AIService(
        IDataService dataService,
        AIProviderFactory providerFactory,
        ILogger<AIService> logger)
    {
        _dataService = dataService;
        _providerFactory = providerFactory;
        _logger = logger;

        // Initialize configuration asynchronously
        _ = InitializeAsync();
    }

    /// <inheritdoc/>
    public bool IsConfigured => _currentProvider != null && !string.IsNullOrWhiteSpace(_currentModelId);

    /// <inheritdoc/>
    public string? CurrentProviderId => _currentProvider?.ProviderId;

    /// <inheritdoc/>
    public string? CurrentModelId => _currentModelId;

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> StreamChatAsync(
        string conversationId,
        string message,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_currentProvider == null || string.IsNullOrWhiteSpace(_currentModelId))
        {
            throw new InvalidOperationException("AI provider is not configured. Please configure a provider first.");
        }

        // Get or create conversation history
        var history = _conversationHistory.GetOrAdd(conversationId, _ => new List<ProviderMessage>());

        // Add user message to history
        history.Add(new ProviderMessage("user", message));

        // Trim history if needed
        TrimHistory(history);

        // Use Channel pattern for safe streaming
        var channel = System.Threading.Channels.Channel.CreateUnbounded<string>();
        var fullResponse = new StringBuilder();

        // Start streaming in background
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var token in _currentProvider.StreamChatAsync(history, _currentModelId, ct))
                {
                    fullResponse.Append(token);
                    await channel.Writer.WriteAsync(token, ct);
                }

                // Add assistant response to history on success
                if (fullResponse.Length > 0)
                {
                    history.Add(new ProviderMessage("assistant", fullResponse.ToString()));
                    _logger.LogDebug("Added response to history for conversation {ConversationId}", conversationId);
                }
            }
            catch (OperationCanceledException)
            {
                await channel.Writer.WriteAsync("\n[已中断]", ct);
                _logger.LogInformation("Chat cancelled for conversation {ConversationId}", conversationId);
            }
            catch (Exception ex)
            {
                var errorMessage = GetErrorMessage(ex);
                await channel.Writer.WriteAsync($"\n{errorMessage}", ct);
                _logger.LogError(ex, "Error in chat for conversation {ConversationId}", conversationId);
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, ct);

        // Stream tokens to caller
        await foreach (var token in channel.Reader.ReadAllAsync(ct))
        {
            yield return token;
        }
    }

    /// <inheritdoc/>
    public async Task<string> ChatAsync(
        string conversationId,
        string message,
        CancellationToken ct = default)
    {
        if (_currentProvider == null || string.IsNullOrWhiteSpace(_currentModelId))
        {
            throw new InvalidOperationException("AI provider is not configured. Please configure a provider first.");
        }

        var history = _conversationHistory.GetOrAdd(conversationId, _ => new List<ProviderMessage>());
        history.Add(new ProviderMessage("user", message));
        TrimHistory(history);

        try
        {
            var response = await _currentProvider.ChatAsync(history, _currentModelId, ct);
            history.Add(new ProviderMessage("assistant", response));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    /// <inheritdoc/>
    public void ClearHistory(string conversationId)
    {
        _conversationHistory.TryRemove(conversationId, out _);
        _logger.LogDebug("Cleared history for conversation {ConversationId}", conversationId);
    }

    /// <inheritdoc/>
    public void RestoreHistory(string conversationId, IEnumerable<ChatMessage> messages)
    {
        var history = _conversationHistory.GetOrAdd(conversationId, _ => new List<ProviderMessage>());
        history.Clear();

        foreach (var msg in messages.OrderBy(m => m.Timestamp))
        {
            var role = msg.IsUser ? "user" : "assistant";
            history.Add(new ProviderMessage(role, msg.Content));
        }

        TrimHistory(history);
        _logger.LogDebug("Restored {Count} messages for conversation {ConversationId}",
            history.Count, conversationId);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AIModel>> GetAvailableModelsAsync()
    {
        if (_currentProvider == null)
        {
            return Enumerable.Empty<AIModel>();
        }

        return await _currentProvider.GetModelsAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateConfigurationAsync()
    {
        if (_currentProvider == null)
        {
            return false;
        }

        return await _currentProvider.ValidateAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateProviderAsync(
        string providerId,
        string apiKey,
        string? baseUrl,
        CancellationToken ct = default)
    {
        try
        {
            var provider = _providerFactory.CreateProvider(providerId, apiKey, baseUrl);
            return await provider.ValidateAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider validation failed for {ProviderId}", providerId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task ReloadConfigurationAsync()
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var settings = await _dataService.GetSettingsAsync();

            if (string.IsNullOrWhiteSpace(settings.SelectedProviderId))
            {
                _logger.LogInformation("No provider selected in settings");
                return;
            }

            var config = await _dataService.GetProviderConfigAsync(settings.SelectedProviderId);

            if (config == null || !config.HasValidCredentials)
            {
                _logger.LogWarning("Provider {ProviderId} has no valid configuration",
                    settings.SelectedProviderId);
                return;
            }

            var provider = _dataService.GetProvider(settings.SelectedProviderId);
            if (provider == null)
            {
                _logger.LogWarning("Provider {ProviderId} not found in definitions",
                    settings.SelectedProviderId);
                return;
            }

            // Create provider instance
            _currentProvider = _providerFactory.CreateProvider(
                provider.Id,
                config.ApiKey!,
                config.BaseUrl);

            // Set model
            _currentModelId = config.SelectedModelId
                ?? settings.SelectedModelId
                ?? provider.Models.FirstOrDefault()?.Id;

            _logger.LogInformation("Initialized AI service with provider {ProviderId} and model {ModelId}",
                _currentProvider.ProviderId, _currentModelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AI service");
        }
    }

    private static void TrimHistory(List<ProviderMessage> history)
    {
        if (history.Count > MaxHistoryMessages)
        {
            var toRemove = history.Count - MaxHistoryMessages;
            history.RemoveRange(0, toRemove);
        }
    }

    private static string GetErrorMessage(Exception ex)
    {
        return ex switch
        {
            HttpRequestException httpEx when httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized
                => "[错误] API Key 无效，请检查配置",
            HttpRequestException httpEx when httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                => "[错误] 请求过于频繁，请稍后再试",
            HttpRequestException httpEx when httpEx.StatusCode == System.Net.HttpStatusCode.PaymentRequired
                => "[错误] API 额度不足，请充值后重试",
            TaskCanceledException
                => "[错误] 请求超时，请检查网络连接",
            UriFormatException
                => "[错误] Base URL 格式错误",
            InvalidOperationException invEx
                => $"[错误] {invEx.Message}",
            _ => $"[错误] 无法连接到 AI 服务: {ex.Message}"
        };
    }
}

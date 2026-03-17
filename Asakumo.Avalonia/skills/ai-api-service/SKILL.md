---
name: ai-api-service
description: AI API 服务层实现，基于 OpenAI .NET SDK 2.2.0，支持流式响应和聊天上下文管理。用于实现真实的 AI 聊天功能，包括：(1) 调用 OpenAI/DeepSeek/Ollama 等兼容 API，(2) Channel 模式的流式输出处理，(3) 聊天历史管理，(4) 错误处理和重试机制。触发词：API服务、流式输出、AI调用、聊天API。
---

# AI API 服务层实现

基于 **OpenAI .NET SDK 2.2.0** 的 AI 服务层实现。

## 核心接口

```csharp
public interface IAIService
{
    IAsyncEnumerable<string> StreamChatAsync(string conversationId, string message, CancellationToken ct = default);
    Task<string> ChatAsync(string conversationId, string message, CancellationToken ct = default);
    void ClearHistory(string conversationId);
    void RestoreHistory(string conversationId, IEnumerable<ChatMessage> messages);
    Task<IEnumerable<AIModel>> GetAvailableModelsAsync();
    Task<bool> ValidateConfigurationAsync();
    Task<bool> ValidateProviderAsync(string providerId, string apiKey, string? baseUrl, CancellationToken ct = default);
}
```

## 黄金案例 vs 失败案例

### 1. 流式响应处理（使用 Channel 模式）

```csharp
// ✅ 正确：使用 Channel 处理 IAsyncEnumerable 中的异常
public async IAsyncEnumerable<string> StreamChatAsync(
    string conversationId,
    string message,
    [EnumeratorCancellation] CancellationToken ct)
{
    // 使用 Channel 在后台线程处理流式响应
    var channel = Channel.CreateUnbounded<string>();
    var fullResponse = new StringBuilder();

    // 在后台线程启动流式请求
    _ = Task.Run(async () =>
    {
        try
        {
            await foreach (var token in _provider.StreamChatAsync(history, _modelId, ct))
            {
                fullResponse.Append(token);
                await channel.Writer.WriteAsync(token, ct);
            }

            // 成功后将完整响应添加到历史
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
            await channel.Writer.WriteAsync($"\n[错误] {ex.Message}", ct);
        }
        finally
        {
            channel.Writer.Complete();
        }
    }, ct);

    // 从 Channel 读取并返回给调用方
    await foreach (var token in channel.Reader.ReadAllAsync(ct))
    {
        yield return token;
    }
}

// ❌ 错误：直接在 IAsyncEnumerable 中 try-catch 会导致异常无法正确传递
public async IAsyncEnumerable<string> BadStreamAsync(string message)
{
    try
    {
        await foreach (var token in _provider.StreamChatAsync(message))
        {
            yield return token;  // 异常发生在 yield 时难以捕获
        }
    }
    catch (Exception ex)
    {
        yield return $"[错误] {ex.Message}";  // 可能在 UI 层无法正确处理
    }
}
```

### 2. OpenAI SDK 2.2.0 流式实现

```csharp
// ✅ 正确：OpenAI SDK 2.2.0 流式聊天实现
public async IAsyncEnumerable<string> StreamChatAsync(
    IEnumerable<ProviderMessage> messages,
    string modelId,
    [EnumeratorCancellation] CancellationToken ct)
{
    var chatMessages = new List<OpenAI.Chat.ChatMessage>();

    // 转换消息格式
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

    // 获取 ChatClient
    var client = new OpenAIClient(new ApiKeyCredential(_apiKey), _clientOptions);
    var chatClient = client.GetChatClient(modelId);

    // 流式调用
    await foreach (var update in chatClient.CompleteChatStreamingAsync(chatMessages, cancellationToken: ct))
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
```

### 3. 聊天历史管理

```csharp
// ✅ 正确：维护上下文，限制长度
public class AIService : IAIService
{
    private readonly ConcurrentDictionary<string, List<ProviderMessage>> _conversationHistory = new();

    public async IAsyncEnumerable<string> StreamChatAsync(
        string conversationId,
        string message,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // 获取或创建会话历史
        var history = _conversationHistory.GetOrAdd(conversationId, _ => new List<ProviderMessage>());

        // 添加用户消息
        history.Add(new ProviderMessage("user", message));

        // 限制历史长度
        TrimHistory(history);

        // 流式请求...
    }

    private static void TrimHistory(List<ProviderMessage> history, int maxMessages = 20)
    {
        if (history.Count > maxMessages)
        {
            var toRemove = history.Count - maxMessages;
            history.RemoveRange(0, toRemove);
        }
    }

    public void ClearHistory(string conversationId)
    {
        _conversationHistory.TryRemove(conversationId, out _);
    }

    public void RestoreHistory(string conversationId, IEnumerable<ChatMessage> messages)
    {
        var history = _conversationHistory.GetOrAdd(conversationId, _ => new List<ProviderMessage>());
        history.Clear();

        foreach (var msg in messages)
        {
            var role = msg.IsUser ? "user" : "assistant";
            history.Add(new ProviderMessage(role, msg.Content));
        }

        TrimHistory(history);
    }
}

// ❌ 错误：不维护历史或无限制增长
// 每次调用都丢失上下文，AI 无法记住之前的对话
// 或者历史无限增长导致 API 费用爆炸
```

### 4. 错误处理（HTTP 状态码细分）

```csharp
// ✅ 正确：区分错误类型，提供用户友好的反馈
private static string GetErrorMessage(Exception ex)
{
    return ex switch
    {
        HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.Unauthorized
            => "[错误] API Key 无效，请检查配置",
        HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.TooManyRequests
            => "[错误] 请求过于频繁，请稍后再试",
        HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.PaymentRequired
            => "[错误] API 额度不足，请充值后重试",
        TaskCanceledException
            => "[错误] 请求超时，请检查网络连接",
        UriFormatException
            => "[错误] Base URL 格式错误",
        _ => $"[错误] 无法连接到 AI 服务: {ex.Message}"
    };
}

// 使用
_ = Task.Run(async () =>
{
    try
    {
        // ... 流式请求
    }
    catch (Exception ex)
    {
        var errorMessage = GetErrorMessage(ex);
        await channel.Writer.WriteAsync($"\n{errorMessage}", ct);
    }
});
```

### 5. ViewModel 集成（与 CommunityToolkit.Mvvm）

```csharp
public partial class ChatViewModel : ViewModelBase
{
    private readonly IAIService _aiService;
    private CancellationTokenSource? _currentRequestCts;

    [ObservableProperty]
    private string _inputMessage = string.Empty;

    [ObservableProperty]
    private bool _isAiResponding;

    // 绑定到 ItemsSource
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputMessage)) return;

        var userMessage = InputMessage;
        InputMessage = string.Empty;

        // 添加用户消息
        Messages.Add(new ChatMessage { Content = userMessage, IsUser = true });

        // 创建 AI 响应消息（ObservableObject 支持实时更新）
        var aiMessage = new ChatMessage { IsUser = false };
        Messages.Add(aiMessage);

        IsAiResponding = true;
        _currentRequestCts = new CancellationTokenSource();

        try
        {
            await foreach (var token in _aiService.StreamChatAsync(
                CurrentConversationId, userMessage, _currentRequestCts.Token))
            {
                aiMessage.Content += token;  // 自动触发 UI 更新
            }
        }
        catch (OperationCanceledException)
        {
            // 用户取消
        }
        finally
        {
            IsAiResponding = false;
        }
    }

    [RelayCommand]
    private void StopGeneration()
    {
        _currentRequestCts?.Cancel();
    }
}
```

## 服务注册

```csharp
// App.axaml.cs
var services = new ServiceCollection();

services.AddSingleton<IDataService, DataService>();
services.AddSingleton<AIProviderFactory>();
services.AddSingleton<IAIService, AIService>();

// 配置
services.AddLogging(builder => builder.AddDebug());
```

## 验证配置

```csharp
public async Task<bool> ValidateProviderAsync(
    string providerId, 
    string apiKey, 
    string? baseUrl, 
    CancellationToken ct)
{
    try
    {
        // 获取默认模型
        var providers = _dataService.GetProviders();
        var provider = providers.FirstOrDefault(p => p.Id == providerId);
        var defaultModel = provider?.Models.FirstOrDefault()?.Id ?? "gpt-3.5-turbo";

        // 创建临时 Provider 进行验证
        var tempProvider = _providerFactory.CreateProvider(
            providerId, apiKey, baseUrl, defaultModel);

        return await tempProvider.ValidateAsync();
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Provider validation failed");
        return false;
    }
}
```

## 依赖包

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="OpenAI" Version="2.2.0" />
```

## 检查清单

- [ ] 使用 `IAsyncEnumerable` 实现流式响应
- [ ] 使用 `Channel` 模式处理流式异常
- [ ] 维护聊天历史，设置最大长度限制
- [ ] 区分 HTTP 错误类型（401、429、402 等）
- [ ] 支持 `CancellationToken` 取消请求
- [ ] 使用 `ConcurrentDictionary` 管理多会话历史
- [ ] 使用 `StringBuilder` 累积完整响应
- [ ] ViewModel 中正确处理流式取消

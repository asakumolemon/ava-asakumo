---
name: ai-api-service
description: AI API 服务层实现，支持流式响应和聊天上下文管理。用于实现真实的 AI 聊天功能，包括：(1) 调用 OpenAI/Anthropic/DeepSeek 等 API，(2) 流式输出（SSE）处理，(3) 聊天历史管理，(4) 错误处理和重试机制。触发词：API服务、流式输出、AI调用、聊天API。
---

# AI API 服务层实现

## 核心接口

```csharp
public interface IAIService
{
    IAsyncEnumerable<string> StreamChatAsync(string message, CancellationToken ct = default);
    Task<string> ChatAsync(string message, CancellationToken ct = default);
    void ClearHistory();
}
```

## 黄金案例 vs 失败案例

### 1. 流式响应处理

```csharp
// ✅ 正确：使用 IAsyncEnumerable 流式返回
public async IAsyncEnumerable<string> StreamChatAsync(string message, [EnumeratorCancellation] CancellationToken ct)
{
    _chatHistory.Add(new UserChatMessage(message));
    
    var response = _chatClient.CompleteChatStreamingAsync(_chatHistory, ct);
    
    var fullResponse = new StringBuilder();
    
    await foreach (var update in response.WithCancellation(ct))
    {
        if (update.ContentUpdate != null)
        {
            foreach (var part in update.ContentUpdate)
            {
                if (part.Kind == ChatMessageContentPartKind.Text)
                {
                    fullResponse.Append(part.Text);
                    yield return part.Text; // 逐 token 返回
                }
            }
        }
    }
    
    // 完成后添加到历史
    if (fullResponse.Length > 0)
        _chatHistory.Add(new AssistantChatMessage(fullResponse.ToString()));
}

// ❌ 错误：等待完整响应再返回
public async Task<string> ChatAsync(string message)
{
    var response = await _chatClient.CompleteChatAsync(message);
    return response.Content; // 用户看不到打字效果
}
```

### 2. 聊天历史管理

```csharp
// ✅ 正确：维护上下文，限制长度
private readonly List<ChatMessage> _chatHistory = new();

public void AddToHistory(string userMessage, string assistantMessage)
{
    _chatHistory.Add(new UserChatMessage(userMessage));
    _chatHistory.Add(new AssistantChatMessage(assistantMessage));
    
    // 限制历史长度，避免 token 超限
    if (_chatHistory.Count > MaxHistoryMessages)
    {
        // 保留系统消息（如果有）+ 最近的消息
        var messagesToKeep = _chatHistory.TakeLast(MaxHistoryMessages - 1).ToList();
        _chatHistory.Clear();
        _chatHistory.AddRange(messagesToKeep);
    }
}

// ❌ 错误：不维护历史或无限制增长
// 每次调用都丢失上下文，AI 无法记住之前的对话
// 或者历史无限增长导致 API 费用爆炸
```

### 3. 错误处理

```csharp
// ✅ 正确：区分错误类型，提供用户友好的反馈
public async IAsyncEnumerable<string> StreamChatAsync(string message)
{
    try
    {
        await foreach (var token in StreamFromApi(message))
        {
            yield return token;
        }
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
        yield return "[错误] API Key 无效，请检查配置";
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    {
        yield return "[错误] 请求过于频繁，请稍后再试";
    }
    catch (TaskCanceledException)
    {
        yield return "[已中断] 用户取消了请求";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "AI API 调用失败");
        yield return "[错误] 无法连接到 AI 服务";
    }
}

// ❌ 错误：让异常直接抛出，UI 崩溃
// 或者吞掉异常，用户看不到任何反馈
```

## 推荐实现模式

### 服务注册

```csharp
// App.axaml.cs 或 Startup
services.AddHttpClient<IAIService, OpenAIService>();
services.Configure<AIServiceOptions>(configuration.GetSection("AI"));

// 配置类
public class AIServiceOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o-mini";
    public string? BaseUrl { get; set; } // 支持自定义端点
    public int MaxHistoryMessages { get; set; } = 20;
    public int TimeoutSeconds { get; set; } = 60;
}
```

### ViewModel 集成

```csharp
public partial class ChatViewModel : ViewModelBase
{
    private readonly IAIService _aiService;
    private CancellationTokenSource? _currentRequestCts;
    
    [RelayCommand]
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(InputMessage)) return;
        
        var userMessage = InputMessage;
        InputMessage = string.Empty;
        
        // 添加用户消息
        Messages.Add(new ChatMessage { Content = userMessage, IsUser = true });
        
        // 创建 AI 响应消息
        var aiMessage = new ChatMessage { IsUser = false, Status = MessageStatus.Streaming };
        Messages.Add(aiMessage);
        
        _currentRequestCts = new CancellationTokenSource();
        
        try
        {
            await foreach (var token in _aiService.StreamChatAsync(userMessage, _currentRequestCts.Token))
            {
                aiMessage.Content += token;
            }
            aiMessage.Status = MessageStatus.Sent;
        }
        catch (OperationCanceledException)
        {
            aiMessage.Status = MessageStatus.Cancelled;
        }
    }
    
    [RelayCommand]
    private void StopGeneration()
    {
        _currentRequestCts?.Cancel();
    }
}
```

## 检查清单

- [ ] 使用 `IAsyncEnumerable` 实现流式响应
- [ ] 维护聊天历史，设置最大长度限制
- [ ] 区分不同错误类型，提供用户友好反馈
- [ ] 支持 CancellationToken 取消请求
- [ ] 配置超时时间
- [ ] 使用 HttpClient 工厂模式
---
name: multi-provider-adapter
description: 多 AI 服务商适配器实现，基于 OpenAI .NET SDK 2.2.0，支持 OpenAI、DeepSeek、Ollama 等 OpenAI 兼容 API，以及 Anthropic、Google Gemini 等需要适配的服务。触发词：多服务商、Provider适配、API适配器、切换AI服务。
---

# 多 AI 服务商适配器实现

基于 **OpenAI .NET SDK 2.2.0** 的多服务商适配器实现。

## 核心设计：利用 OpenAI 兼容性

大多数 AI 服务商都提供 **OpenAI 兼容 API**：
- **OpenAI**: 原生支持
- **DeepSeek**: 完全兼容，只需改 BaseUrl
- **Ollama**: 完全兼容本地模型
- **Anthropic**: 需要适配器转换格式
- **Google Gemini**: 使用 Google.GenAI SDK

## 黄金案例 vs 失败案例

### 1. 架构设计

```csharp
// ✅ 正确：统一接口 + 适配器模式
public interface IAIProvider
{
    string ProviderId { get; }
    IAsyncEnumerable<string> StreamChatAsync(IEnumerable<ProviderMessage> history, string modelId, CancellationToken ct);
    Task<IEnumerable<AIModel>> GetModelsAsync();
    Task<bool> ValidateAsync();
}

// 统一消息格式
public record ProviderMessage(string Role, string Content);

// OpenAI 兼容（OpenAI、DeepSeek、Ollama）
public class OpenAICompatibleProvider : IAIProvider
{
    private readonly ChatClient _chatClient;
    private readonly string _providerId;

    public OpenAICompatibleProvider(string providerId, string apiKey, string? baseUrl, string modelId)
    {
        _providerId = providerId;

        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(baseUrl))
        {
            clientOptions.Endpoint = new Uri(baseUrl);
        }

        var credential = new ApiKeyCredential(apiKey);
        var client = new OpenAIClient(credential, clientOptions);
        _chatClient = client.GetChatClient(modelId);
    }

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

        // 流式调用
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
        // 模型列表从配置返回
        return Task.FromResult<IEnumerable<AIModel>>(Array.Empty<AIModel>());
    }

    public async Task<bool> ValidateAsync()
    {
        try
        {
            var messages = new List<OpenAI.Chat.ChatMessage> { new UserChatMessage("Hi") };
            await foreach (var _ in _chatClient.CompleteChatStreamingAsync(messages))
            {
                return true; // 成功收到第一个 token
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}

// ❌ 错误：每个 Provider 单独实现，没有统一接口
// 切换服务商需要改很多代码
```

### 2. Provider 工厂

```csharp
// ✅ 正确：工厂模式创建 Provider 实例
public class AIProviderFactory
{
    public IAIProvider CreateProvider(string providerId, string apiKey, string? baseUrl, string modelId)
    {
        return providerId.ToLowerInvariant() switch
        {
            "openai" => new OpenAICompatibleProvider(providerId, apiKey, baseUrl ?? "https://api.openai.com/v1", modelId),
            "deepseek" => new OpenAICompatibleProvider(providerId, apiKey, baseUrl ?? "https://api.deepseek.com", modelId),
            "ollama" => new OpenAICompatibleProvider(providerId, apiKey, baseUrl ?? "http://localhost:11434/v1", modelId),
            "anthropic" => new AnthropicProvider(apiKey, modelId),
            "google" => new GeminiProvider(apiKey, modelId),
            _ => throw new NotSupportedException($"Provider {providerId} not supported")
        };
    }
}

// ❌ 错误：硬编码每个 Provider
// 添加新 Provider 需要修改核心代码
```

### 3. OpenAI SDK 2.2.0 配置详解

```csharp
// ✅ 正确：OpenAI SDK 2.2.0 客户端配置
public class OpenAICompatibleProvider : IAIProvider
{
    private readonly ChatClient _chatClient;

    public OpenAICompatibleProvider(
        string providerId,
        string apiKey,
        string? baseUrl,
        string modelId)
    {
        // 创建客户端选项
        var clientOptions = new OpenAIClientOptions();
        
        // 设置自定义端点（用于 DeepSeek、Ollama、中转服务）
        if (!string.IsNullOrEmpty(baseUrl))
        {
            clientOptions.Endpoint = new Uri(baseUrl);
        }

        // 创建凭证
        var credential = new ApiKeyCredential(apiKey);
        
        // 创建客户端
        var client = new OpenAIClient(credential, clientOptions);
        
        // 获取 ChatClient（指定模型）
        _chatClient = client.GetChatClient(modelId);
    }
}

// 各种服务商的配置
var openai = new OpenAICompatibleProvider("openai", "sk-xxx", null, "gpt-4o");
var deepseek = new OpenAICompatibleProvider("deepseek", "sk-xxx", "https://api.deepseek.com", "deepseek-chat");
var ollama = new OpenAICompatibleProvider("ollama", "ollama", "http://localhost:11434/v1", "llama3.2");
```

### 4. 配置管理

```csharp
// ✅ 正确：统一的 ProviderConfig 支持不同配置
public class ProviderConfig
{
    public string ProviderId { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string? BaseUrl { get; set; } // 自定义端点（中转服务）
    public string? DefaultModel { get; set; }
    public bool IsValid => !string.IsNullOrEmpty(ApiKey);
}

// 存储在设置中
public class AppSettings
{
    public string CurrentProviderId { get; set; } = "";
    public string CurrentModelId { get; set; } = "";
    public Dictionary<string, ProviderConfig> ProviderConfigs { get; set; } = new();
}

// 使用
var config = new ProviderConfig
{
    ProviderId = "deepseek",
    ApiKey = "sk-xxx",
    BaseUrl = "https://custom-proxy.example.com" // 可选：用于中转服务
};
```

### 5. Provider 静态数据

```csharp
// ✅ 正确：集中管理 Provider 和模型信息
public class AIProvider
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? DefaultBaseUrl { get; set; }
    public bool RequiresApiKey { get; set; } = true;
    public string Icon { get; set; } = "";
    public List<AIModel> Models { get; set; } = new();
}

public class AIModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ProviderId { get; set; } = "";
    public bool IsRecommended { get; set; }
    public List<string> Tags { get; set; } = new();
}

// 在服务中提供静态数据
public List<AIProvider> GetProviders()
{
    return new List<AIProvider>
    {
        new()
        {
            Id = "openai",
            Name = "OpenAI (GPT)",
            Description = "GPT-4, GPT-3.5 等模型",
            DefaultBaseUrl = "https://api.openai.com/v1",
            RequiresApiKey = true,
            Models = new()
            {
                new() { Id = "gpt-4o", Name = "GPT-4o", IsRecommended = true },
                new() { Id = "gpt-4o-mini", Name = "GPT-4o-mini", IsRecommended = true },
                new() { Id = "o3-mini", Name = "o3-mini" }
            }
        },
        new()
        {
            Id = "deepseek",
            Name = "DeepSeek",
            Description = "国产大模型，性价比高",
            DefaultBaseUrl = "https://api.deepseek.com",
            RequiresApiKey = true,
            Models = new()
            {
                new() { Id = "deepseek-chat", Name = "DeepSeek Chat", IsRecommended = true },
                new() { Id = "deepseek-reasoner", Name = "DeepSeek Reasoner" }
            }
        },
        new()
        {
            Id = "ollama",
            Name = "Ollama (本地部署)",
            Description = "本地运行开源模型",
            DefaultBaseUrl = "http://localhost:11434/v1",
            RequiresApiKey = false,
            Models = new()
            {
                new() { Id = "llama3.2", Name = "Llama 3.2", IsRecommended = true },
                new() { Id = "qwen2.5", Name = "Qwen 2.5" }
            }
        }
    };
}
```

## 完整服务集成

```csharp
public class AIService : IAIService
{
    private readonly IDataService _dataService;
    private readonly AIProviderFactory _providerFactory;
    private IAIProvider? _currentProvider;
    private string? _currentModelId;

    public AIService(IDataService dataService, AIProviderFactory providerFactory)
    {
        _dataService = dataService;
        _providerFactory = providerFactory;
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        string conversationId,
        string message,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // 确保 Provider 已初始化
        if (_currentProvider == null)
        {
            if (!await InitializeProviderAsync())
            {
                yield return "[错误] 请先配置 AI 服务提供商";
                yield break;
            }
        }

        // 流式对话...
    }

    private async Task<bool> InitializeProviderAsync()
    {
        var settings = await _dataService.GetSettingsAsync();

        if (!settings.ProviderConfigs.TryGetValue(settings.CurrentProviderId, out var config))
            return false;

        _currentProvider = _providerFactory.CreateProvider(
            settings.CurrentProviderId,
            config.ApiKey,
            config.BaseUrl,
            settings.CurrentModelId);
        
        _currentModelId = settings.CurrentModelId;
        return true;
    }
}
```

## OpenAI SDK 2.2.0 关键 API

```csharp
// 客户端创建
var credential = new ApiKeyCredential(apiKey);
var options = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
var client = new OpenAIClient(credential, options);

// 获取 ChatClient（指定模型）
var chatClient = client.GetChatClient(modelId);

// 流式调用
await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, cancellationToken: ct))
{
    foreach (var part in update.ContentUpdate)
    {
        if (part.Kind == ChatMessageContentPartKind.Text)
        {
            yield return part.Text;
        }
    }
}
```

## 服务商兼容性表

| Provider | 兼容类型 | BaseUrl | API Key |
|----------|----------|---------|---------|
| OpenAI | 原生 | https://api.openai.com/v1 | 必需 |
| DeepSeek | OpenAI 兼容 | https://api.deepseek.com | 必需 |
| Ollama | OpenAI 兼容 | http://localhost:11434/v1 | 可选 |
| Anthropic | 需适配 | https://api.anthropic.com | 必需 |
| Google | 需适配 (Google.GenAI) | - | 必需 |

## 依赖包

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="OpenAI" Version="2.2.0" />
<PackageVersion Include="Google.GenAI" Version="1.0.0" />
```

## 检查清单

- [ ] 定义统一的 `IAIProvider` 接口
- [ ] 使用工厂模式创建 Provider 实例
- [ ] 利用 OpenAI SDK 2.2.0 的兼容性减少适配工作
- [ ] 支持自定义 BaseUrl（用于中转服务）
- [ ] 统一的内部消息格式 `ProviderMessage`
- [ ] 集中管理 Provider 和模型静态数据
- [ ] 统一的配置类 `ProviderConfig`
- [ ] 使用 `ChatClient` 进行流式调用
- [ ] 实现 `ValidateAsync` 验证配置有效性

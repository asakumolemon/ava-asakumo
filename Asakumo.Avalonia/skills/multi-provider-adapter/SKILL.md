---
name: multi-provider-adapter
description: 多 AI 服务商适配器实现，支持 OpenAI、Anthropic、DeepSeek 等不同 API 格式。用于实现多服务商适配功能，包括：(1) 统一接口抽象，(2) 各厂商 API 差异处理，(3) 动态切换 Provider，(4) OpenAI 兼容格式利用。触发词：多服务商、Provider适配、API适配器、切换AI服务。
---

# 多 AI 服务商适配器实现

## 核心设计：利用 OpenAI 兼容性

大多数 AI 服务商都提供 **OpenAI 兼容 API**：
- DeepSeek：完全兼容，只需改 BaseUrl
- Anthropic：需要适配器转换格式
- Ollama：完全兼容本地模型
- 其他中转服务：直接兼容

## 黄金案例 vs 失败案例

### 1. 架构设计

```csharp
// ✅ 正确：统一接口 + 适配器模式
public interface IAIProvider
{
    string ProviderId { get; }
    IAsyncEnumerable<string> StreamChatAsync(IEnumerable<ChatMessage> history, CancellationToken ct);
    Task<IEnumerable<ModelInfo>> GetModelsAsync();
}

// OpenAI 兼容（DeepSeek、Ollama、中转服务）
public class OpenAICompatibleProvider : IAIProvider
{
    public OpenAICompatibleProvider(string providerId, string baseUrl, string apiKey)
    {
        _client = new OpenAIClient(apiKey, new OpenAIClientOptions { Endpoint = new Uri(baseUrl) });
    }
    // 直接使用 OpenAI SDK
}

// Anthropic 专用适配器
public class AnthropicProvider : IAIProvider
{
    // 转换消息格式
}

// ❌ 错误：每个 Provider 单独实现，没有统一接口
// 切换服务商需要改很多代码
```

### 2. Provider 注册

```csharp
// ✅ 正确：动态注册，配置驱动
public static class ProviderRegistry
{
    public static IReadOnlyDictionary<string, ProviderInfo> Providers => new Dictionary<string, ProviderInfo>
    {
        ["openai"] = new("openai", "OpenAI", "https://api.openai.com/v1", ProviderType.OpenAI),
        ["deepseek"] = new("deepseek", "DeepSeek", "https://api.deepseek.com/v1", ProviderType.OpenAI),
        ["anthropic"] = new("anthropic", "Anthropic", "https://api.anthropic.com", ProviderType.Anthropic),
        ["ollama"] = new("ollama", "Ollama", "http://localhost:11434/v1", ProviderType.OpenAI),
    };
}

// 服务工厂
public class AIProviderFactory
{
    public IAIProvider CreateProvider(ProviderConfig config)
    {
        var info = ProviderRegistry.Providers[config.ProviderId];
        
        return info.Type switch
        {
            ProviderType.OpenAI => new OpenAICompatibleProvider(info.ProviderId, info.BaseUrl, config.ApiKey),
            ProviderType.Anthropic => new AnthropicProvider(config.ApiKey),
            _ => throw new NotSupportedException($"不支持的 Provider: {config.ProviderId}")
        };
    }
}

// ❌ 错误：硬编码每个 Provider
// 添加新 Provider 需要修改核心代码
```

### 3. 消息格式转换

```csharp
// ✅ 正确：统一内部格式，各适配器转换
public record ChatMessage(string Role, string Content); // 内部统一格式

// OpenAI 适配器
public class OpenAICompatibleProvider : IAIProvider
{
    public async IAsyncEnumerable<string> StreamChatAsync(IEnumerable<ChatMessage> history, [EnumeratorCancellation] CancellationToken ct)
    {
        var openAIMessages = history.Select(m => m.Role switch
        {
            "user" => new UserChatMessage(m.Content),
            "assistant" => new AssistantChatMessage(m.Content),
            "system" => new SystemChatMessage(m.Content),
            _ => throw new ArgumentException($"Unknown role: {m.Role}")
        }).ToList();
        
        await foreach (var token in StreamFromOpenAI(openAIMessages, ct))
            yield return token;
    }
}

// Anthropic 适配器
public class AnthropicProvider : IAIProvider
{
    public async IAsyncEnumerable<string> StreamChatAsync(IEnumerable<ChatMessage> history, [EnumeratorCancellation] CancellationToken ct)
    {
        // Anthropic API 格式转换
        var request = new
        {
            model = "claude-3-5-sonnet-latest",
            messages = history.Select(m => new { role = m.Role, content = m.Content }),
            max_tokens = 4096,
            stream = true
        };
        
        await foreach (var token in StreamFromAnthropic(request, ct))
            yield return token;
    }
}

// ❌ 错误：每种格式都暴露给调用方
// 调用方需要知道每个 API 的细节
```

### 4. 配置管理

```csharp
// ✅ 正确：ProviderConfig 支持不同配置
public class ProviderConfig
{
    public string ProviderId { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string? BaseUrl { get; set; } // 自定义端点（中转服务）
    public string? DefaultModel { get; set; }
    public Dictionary<string, string> ExtraSettings { get; set; } = new(); // Provider 特定配置
    public bool IsValid { get; set; }
}

// 使用
var config = new ProviderConfig
{
    ProviderId = "deepseek",
    ApiKey = "sk-xxx",
    // BaseUrl 可选，用于中转服务
};

// ❌ 错误：每个 Provider 有不同的配置类
// 配置管理复杂，难以统一存储
```

## 推荐实现

### 服务注册

```csharp
// App.axaml.cs
services.AddSingleton<AIProviderFactory>();
services.AddSingleton<IAIService, AIService>();

// AIService 使用工厂创建当前 Provider
public class AIService : IAIService
{
    private readonly AIProviderFactory _factory;
    private readonly IDataService _dataService;
    private IAIProvider? _currentProvider;
    
    public async IAsyncEnumerable<string> StreamChatAsync(string message, [EnumeratorCancellation] CancellationToken ct)
    {
        _currentProvider ??= CreateCurrentProvider();
        var history = await GetChatHistory();
        history.Add(new ChatMessage("user", message));
        
        await foreach (var token in _currentProvider.StreamChatAsync(history, ct))
            yield return token;
    }
    
    private IAIProvider CreateCurrentProvider()
    {
        var settings = _dataService.GetSettings();
        var config = _dataService.GetProviderConfigAsync(settings.CurrentProviderId).Result;
        return _factory.CreateProvider(config!);
    }
}
```

## OpenAI 兼容性利用

```csharp
// DeepSeek、Ollama、大多数中转服务可以直接用 OpenAI SDK
// 只需修改 BaseUrl

// DeepSeek
var deepseekClient = new OpenAIClient(
    "sk-xxx",
    new OpenAIClientOptions { Endpoint = new Uri("https://api.deepseek.com/v1") }
);

// Ollama 本地
var ollamaClient = new OpenAIClient(
    "ollama", // Ollama 不需要真实 key
    new OpenAIClientOptions { Endpoint = new Uri("http://localhost:11434/v1") }
);

// 中转服务
var proxyClient = new OpenAIClient(
    "your-proxy-key",
    new OpenAIClientOptions { Endpoint = new Uri("https://your-proxy.com/v1") }
);
```

## 需要单独适配的 Provider

| Provider | 适配原因 | 适配方式 |
|----------|----------|----------|
| OpenAI | 标准格式 | 直接使用 SDK |
| DeepSeek | 完全兼容 | 改 BaseUrl |
| Ollama | 完全兼容 | 改 BaseUrl |
| Anthropic | API 格式不同 | 写适配器 |
| Google Gemini | API 格式不同 | 写适配器 |

## 检查清单

- [ ] 定义统一的 `IAIProvider` 接口
- [ ] 使用工厂模式创建 Provider 实例
- [ ] 利用 OpenAI 兼容性减少适配工作
- [ ] 配置驱动，支持动态添加 Provider
- [ ] 统一内部消息格式
- [ ] 各适配器负责格式转换
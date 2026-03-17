---
name: data-persistence
description: 本地数据持久化实现，支持会话、消息和设置的存储与加载。用于实现数据持久化功能，包括：(1) SQLite 数据库（复杂查询），(2) JSON 文件存储（配置），(3) 数据迁移，(4) 原子写入。触发词：数据持久化、本地存储、保存会话、SQLite、JSON存储。
---

# 本地数据持久化实现

## 选择方案

| 场景 | 推荐方案 | 理由 |
|------|----------|------|
| 会话/消息存储 | SQLite | 需要查询、分页、关联 |
| 应用设置 | JSON 文件 | 简单键值，易读写 |
| 用户配置 | JSON 文件 | 结构简单，版本兼容好 |

## 黄金案例 vs 失败案例

### 1. 跨平台数据目录获取

```csharp
// ✅ 正确：使用 Environment.GetFolderPath 获取跨平台路径
public class DataService
{
    private readonly string _dbPath;
    private readonly string _settingsPath;

    public DataService()
    {
        // Windows: %AppData%\Asakumo
        // macOS: ~/Library/Application Support/Asakumo
        // Linux: ~/.config/Asakumo
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDir = Path.Combine(appDataPath, "Asakumo");

        // Ensure directory exists
        if (!Directory.Exists(appDir))
        {
            Directory.CreateDirectory(appDir);
        }

        _dbPath = Path.Combine(appDir, "asakumo.db");
        _settingsPath = Path.Combine(appDir, "settings.json");
    }
}

// ❌ 错误：使用 MAUI 特定的 FileSystem API（仅限 MAUI 应用）
// var dbPath = Path.Combine(FileSystem.AppDataDirectory, "asakumo.db");

// ❌ 错误：硬编码路径
// var dbPath = "C:\\MyApp\\data.db";  // Windows only
// var dbPath = "/home/user/.myapp/data.db";  // Linux only
```

### 2. SQLite 初始化

```csharp
// ✅ 正确：异步延迟初始化
public class DataService : IDataService
{
    private SQLiteAsyncConnection? _database;
    private bool _isInitialized;

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        _database = new SQLiteAsyncConnection(_dbPath);
        
        // Create tables
        await _database.CreateTableAsync<Conversation>();
        await _database.CreateTableAsync<ChatMessage>();

        _isInitialized = true;
    }

    public async Task<List<Conversation>> GetConversationsAsync()
    {
        await EnsureInitializedAsync();
        return await _database!.Table<Conversation>().ToListAsync();
    }
}

// ✅ 桌面应用可选：构造函数中同步初始化（可接受）
public DataService()
{
    // 对于桌面应用，这种方式可避免每次调用都检查初始化状态
    _ = InitializeAsync();  // Fire-and-forget
}

// ❌ 错误：同步阻塞初始化
public DataService()
{
    InitializeAsync().Wait();  // 可能导致死锁！
}
```

### 3. 事务处理

```csharp
// ✅ 正确：使用 ExecuteAsync 进行批量操作
public async Task DeleteConversationAsync(string id)
{
    await EnsureInitializedAsync();

    // 先删除消息，再删除会话
    await _database!.ExecuteAsync(
        "DELETE FROM chat_messages WHERE ConversationId = ?", 
        id);
    await _database.ExecuteAsync(
        "DELETE FROM conversations WHERE Id = ?", 
        id);
}

// ✅ 原子操作：使用事务（注意 sqlite-net-pcl 的事务限制）
public async Task SaveConversationWithMessagesAsync(
    Conversation conversation, 
    List<ChatMessage> messages)
{
    await EnsureInitializedAsync();

    await _database!.RunInTransactionAsync(txn =>
    {
        txn.InsertOrReplace(conversation);
        foreach (var msg in messages)
        {
            msg.ConversationId = conversation.Id;
            txn.InsertOrReplace(msg);
        }
    });
}

// ❌ 错误：没有事务，中间失败导致数据不一致
public async Task BadExample(Conversation conv, List<ChatMessage> msgs)
{
    await _database.InsertAsync(conv);      // 成功
    // 如果这里抛出异常...
    foreach (var msg in msgs)
    {
        await _database.InsertAsync(msg);   // ...这些不会执行
    }
    // 结果：会话存在但没有消息，数据损坏
}
```

### 4. JSON 文件原子写入

```csharp
// ✅ 正确：原子写入，异常处理
public class SettingsService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        var tempPath = _settingsPath + ".tmp";
        
        // 先写临时文件
        await File.WriteAllTextAsync(tempPath, json);
        
        // 原子替换原文件
        File.Move(tempPath, _settingsPath, overwrite: true);
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();

        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) 
                ?? new AppSettings();
        }
        catch (JsonException ex)
        {
            // 配置文件损坏，使用默认值
            _logger.LogWarning(ex, "Settings file corrupted, using defaults");
            return new AppSettings();
        }
    }
}

// ❌ 错误：直接覆盖原文件
public async Task BadSaveAsync(AppSettings settings)
{
    var json = JsonSerializer.Serialize(settings);
    await File.WriteAllTextAsync(_settingsPath, json);  // 写入过程中崩溃会导致文件损坏
}
```

### 5. 数据模型设计

```csharp
// ✅ 正确：适当的索引和关系
[Table("conversations")]
public class Conversation
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Title { get; set; } = "";
    
    [Indexed]  // 经常按此字段排序/查询
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    public string? ModelId { get; set; }
}

[Table("chat_messages")]
public class ChatMessage : ObservableObject  // 继承 ObservableObject 支持 UI 实时更新
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Indexed]  // 外键查询需要索引
    public string ConversationId { get; set; } = "";
    
    // 使用 ObservableProperty 支持流式响应时 UI 实时更新
    [ObservableProperty]
    private string _content = "";
    
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; }
}

// ❌ 错误：缺少索引
public class BadConversation
{
    public string Id { get; set; }  // 缺少 [PrimaryKey]
    public DateTime UpdatedAt { get; set; }  // 缺少 [Indexed]，查询慢
}
```

## IDataService 接口设计

```csharp
public interface IDataService
{
    // 会话管理
    Task<List<Conversation>> GetConversationsAsync();
    Task<Conversation?> GetConversationAsync(string id);
    Task SaveConversationAsync(Conversation conversation);
    Task DeleteConversationAsync(string id);
    
    // 消息管理
    Task<List<ChatMessage>> GetMessagesAsync(string conversationId);
    Task SaveMessageAsync(ChatMessage message);
    Task DeleteMessagesAsync(string conversationId);
    
    // 设置管理（带缓存）
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
    
    // Provider 配置
    Task<ProviderConfig?> GetProviderConfigAsync(string providerId);
    Task SaveProviderConfigAsync(string providerId, ProviderConfig config);
}
```

## 设置缓存模式

```csharp
// ✅ 缓存设置避免重复文件读取
public class DataService : IDataService
{
    private AppSettings? _cachedSettings;

    public async Task<AppSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        // 从文件加载...
        _cachedSettings = await LoadFromFileAsync();
        return _cachedSettings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        // 保存到文件...
        await SaveToFileAsync(settings);
        
        // 更新缓存
        _cachedSettings = settings;
    }
}
```

## 推荐的包

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="SQLitePCLRaw.bundle_green" Version="2.1.10" />
<PackageVersion Include="sqlite-net-pcl" Version="1.9.172" />
```

## 检查清单

- [ ] 使用 `Environment.GetFolderPath` 获取跨平台数据目录
- [ ] SQLite 表有必要的索引（PrimaryKey、Indexed）
- [ ] 相关操作使用事务或正确的执行顺序
- [ ] JSON 文件使用原子写入（临时文件 + Move）
- [ ] 处理文件损坏的情况（try-catch + 默认值）
- [ ] 设置类使用缓存避免重复读取
- [ ] 数据模型继承 `ObservableObject` 以支持 UI 实时更新
- [ ] 使用正确的数据类型（DateTime、bool 等）

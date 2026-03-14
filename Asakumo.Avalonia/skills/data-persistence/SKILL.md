---
name: data-persistence
description: 本地数据持久化实现，支持会话、消息和设置的存储与加载。用于实现数据持久化功能，包括：(1) JSON 文件存储（简单场景），(2) SQLite 数据库（复杂查询），(3) 配置管理，(4) 数据迁移。触发词：数据持久化、本地存储、保存会话、SQLite、JSON存储。
---

# 本地数据持久化实现

## 选择方案

| 场景 | 推荐方案 | 理由 |
|------|----------|------|
| 会话/消息存储 | SQLite | 需要查询、分页、关联 |
| 应用设置 | JSON 文件 | 简单键值，易读写 |
| 用户配置 | JSON 文件 | 结构简单，版本兼容好 |

## 黄金案例 vs 失败案例

### 1. 数据存储选择

```csharp
// ✅ 正确：根据场景选择合适的存储方式
// 会话和消息用 SQLite（需要查询）
public class ConversationRepository
{
    private readonly SQLiteConnection _db;
    
    public Task<List<Conversation>> GetRecentAsync(int count) =>
        _db.Table<Conversation>()
           .OrderByDescending(c => c.UpdatedAt)
           .Take(count)
           .ToListAsync();
}

// 设置用 JSON（简单直接）
public class SettingsService
{
    private const string SettingsPath = "settings.json";
    
    public AppSettings Load() =>
        File.Exists(SettingsPath) 
            ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) 
            : new AppSettings();
}

// ❌ 错误：所有数据都用 JSON 文件
// 会话多了后查询慢，加载慢，文件可能损坏
// 或者用 SQLite 存简单配置，过度设计
```

### 2. SQLite 初始化

```csharp
// ✅ 正确：异步初始化，正确处理表创建
public class DatabaseService : IDataService
{
    private SQLiteAsyncConnection? _database;
    
    public async Task InitializeAsync()
    {
        if (_database is not null) return;
        
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "asakumo.db");
        _database = new SQLiteAsyncConnection(dbPath);
        
        // 创建表
        await _database.CreateTableAsync<Conversation>();
        await _database.CreateTableAsync<ChatMessage>();
        await _database.CreateTableAsync<ProviderConfig>();
    }
}

// ❌ 错误：同步阻塞初始化，或每次操作都创建连接
// UI 卡顿，资源浪费
```

### 3. 事务处理

```csharp
// ✅ 正确：相关操作放在事务中
public async Task SaveConversationWithMessagesAsync(Conversation conversation, List<ChatMessage> messages)
{
    await _database.RunInTransactionAsync(async txn =>
    {
        await txn.InsertOrReplaceAsync(conversation);
        foreach (var msg in messages)
        {
            msg.ConversationId = conversation.Id;
            await txn.InsertOrReplaceAsync(msg);
        }
    });
}

// ❌ 错误：没有事务，中间失败导致数据不一致
// 插入了会话但消息失败了，数据损坏
```

### 4. JSON 文件读写

```csharp
// ✅ 正确：原子写入，异常处理
public class JsonFileService
{
    public async Task SaveAsync<T>(string path, T data)
    {
        var tempPath = path + ".tmp";
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        
        // 先写临时文件
        await File.WriteAllTextAsync(tempPath, json);
        
        // 原子替换
        File.Move(tempPath, path, overwrite: true);
    }
    
    public async Task<T?> LoadAsync<T>(string path) where T : new()
    {
        if (!File.Exists(path)) return new T();
        
        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "配置文件损坏，使用默认值");
            return new T();
        }
    }
}

// ❌ 错误：直接覆盖文件
// 写入过程中断电或崩溃，文件损坏，数据丢失
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
    
    [Indexed]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    public string? ModelId { get; set; }
}

[Table("chat_messages")]
public class ChatMessage
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Indexed] // 外键查询需要索引
    public string ConversationId { get; set; } = "";
    
    public string Content { get; set; } = "";
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; }
}

// ❌ 错误：缺少索引
// 按时间排序或按会话查询时很慢
```

## IDataService 接口实现

```csharp
public interface IDataService
{
    // 会话
    Task<List<Conversation>> GetConversationsAsync();
    Task<Conversation?> GetConversationAsync(string id);
    Task SaveConversationAsync(Conversation conversation);
    Task DeleteConversationAsync(string id);
    
    // 消息
    Task<List<ChatMessage>> GetMessagesAsync(string conversationId);
    Task SaveMessageAsync(ChatMessage message);
    
    // 设置
    AppSettings GetSettings();
    Task SaveSettingsAsync(AppSettings settings);
    
    // Provider 配置
    Task<ProviderConfig?> GetProviderConfigAsync(string providerId);
    Task SaveProviderConfigAsync(ProviderConfig config);
}
```

## 推荐的包

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="SQLitePCLRaw.bundle_green" Version="2.1.10" />
<PackageVersion Include="sqlite-net-pcl" Version="1.9.172" />
```

## 检查清单

- [ ] 选择正确的存储方案（JSON vs SQLite）
- [ ] SQLite 表有必要的索引
- [ ] 相关操作使用事务
- [ ] JSON 文件使用原子写入
- [ ] 异步初始化数据库
- [ ] 处理文件损坏的情况
- [ ] 使用正确的数据类型
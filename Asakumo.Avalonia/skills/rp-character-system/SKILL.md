---
name: rp-character-system
description: 为 AI 聊天应用实现 RP（角色扮演）角色系统。包含角色卡片格式设计、角色管理、Prompt 构建、Lorebook/World Info 系统等完整实现指南。触发词："实现 RP 功能"、"角色扮演系统"、"Character Card"
---

# RP 角色扮演系统开发指南

为 AI 聊天客户端实现完整的角色扮演功能，支持 Character Card V2/V3 格式、角色管理、Prompt 构建和 Lorebook 系统。

## 核心设计原则

1. **兼容性优先**: 支持 SillyTavern/Character.AI 的 Character Card V2 格式
2. **可扩展性**: 通过 Extensions 机制支持自定义字段
3. **Token 优化**: 控制角色定义大小，确保足够的对话历史保留空间
4. **渐进增强**: 基础功能完整，高级功能可选

---

## 数据模型设计

### 1. Character (角色卡片)

```csharp
public class Character
{
    // 基础信息
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;  // 角色描述/背景
    public string Personality { get; set; } = string.Empty;  // 性格特征
    public string Scenario { get; set; } = string.Empty;     // 场景设定

    // 对话相关
    public string FirstMessage { get; set; } = string.Empty;  // 首条消息
    public string? AlternateGreetings { get; set; }          // 替代问候语 (JSON 数组)
    public string? ExampleMessages { get; set; }             // 对话示例

    // 高级设置
    public string? SystemPrompt { get; set; }                // 系统提示词
    public string? PostHistoryInstructions { get; set; }     // 历史处理指令

    // 元数据
    public string? Creator { get; set; }
    public string? CreatorNotes { get; set; }
    public string? Tags { get; set; }                       // JSON 数组
    public string? CharacterVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 头像 (Base64 或文件路径)
    public string? AvatarPath { get; set; }

    // Lorebook/World Info (JSON)
    public string? CharacterBook { get; set; }

    // 扩展字段 (JSON)
    public string? Extensions { get; set; }

    // 应用内设置
    public bool IsFavourite { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
```

### 2. CharacterBookEntry (Lorebook 条目)

```csharp
public class CharacterBookEntry
{
    public int Id { get; set; }
    public string CharacterId { get; set; } = string.Empty;

    // 触发机制
    public string Keys { get; set; } = string.Empty;           // 主关键词 (逗号分隔)
    public string? SecondaryKeys { get; set; }                 // 次要关键词
    public bool IsConstant { get; set; }                       // 是否始终注入
    public bool IsSelective { get; set; }                      // 是否选择性注入

    // 内容
    public string Content { get; set; } = string.Empty;

    // 优先级
    public int Order { get; set; } = 100;                      // 注入顺序
    public int Position { get; set; } = 0;                     // 注入位置

    public bool IsEnabled { get; set; } = true;
}
```

### 3. CharacterImportResult (导入结果)

```csharp
public class CharacterImportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Character? Character { get; set; }
    public string OriginalFormat { get; set; } = string.Empty; // "V1", "V2", "V3", "PNG"
}
```

---

## Prompt 构建系统

### 系统提示词模板

```
[System Prompt - 如果设置了]

[角色描述]
{{char}} 是 {{description}}

[性格特征]
{{char}} 的性格: {{personality}}

[场景设定]
当前场景: {{scenario}}

[示例对话]
{{example_messages}}

[角色书/World Info]
{{activated_lorebook_entries}}

[历史处理指令 - 如果设置了]
{{post_history_instructions}}
```

### PromptBuilder 服务

```csharp
public interface ICharacterPromptBuilder
{
    /// <summary>
    /// 构建完整的系统提示词
    /// </summary>
    string BuildSystemPrompt(Character character, BuildPromptOptions? options = null);

    /// <summary>
    /// 构建包含激活的 Lorebook 条目的提示词
    /// </summary>
    string BuildPromptWithLorebook(Character character, string conversationContext, List<ChatMessage> recentMessages);

    /// <summary>
    /// 估算 Token 数量
    /// </summary>
    int EstimateTokenCount(string text);
}

public class BuildPromptOptions
{
    public bool IncludeExampleMessages { get; set; } = true;
    public bool IncludeLorebook { get; set; } = true;
    public int MaxLorebookEntries { get; set; } = 10;
    public int MaxContextTokens { get; set; } = 2000;
}
```

---

## Character Card 导入/导出

### 支持的格式

| 格式 | 扩展名 | 说明 |
|------|--------|------|
| PNG (V2) | .png | 元数据嵌入 PNG |
| JSON (V1/V2/V3) | .json | 纯 JSON 数据 |
| CharX | .charx | ZIP 压缩包 (多文件) |

### 导入服务

```csharp
public interface ICharacterImportService
{
    Task<CharacterImportResult> ImportFromPngAsync(Stream pngStream);
    Task<CharacterImportResult> ImportFromJsonAsync(string json);
    Task<CharacterImportResult> ImportFromCharxAsync(Stream charxStream);
    Task<CharacterImportResult> ImportFromFileAsync(string filePath);
}
```

### V2 格式解析示例

```csharp
public class CharacterCardV2Parser
{
    public Character Parse(JsonDocument doc)
    {
        var root = doc.RootElement;

        // 检查 spec 标识
        var spec = root.GetProperty("spec").GetString();
        if (spec != "chara_card_v2")
            throw new InvalidFormatException("Not a V2 character card");

        var data = root.GetProperty("data");

        return new Character
        {
            Name = data.GetProperty("name").GetString() ?? string.Empty,
            Description = data.GetProperty("description").GetString() ?? string.Empty,
            Personality = data.GetProperty("personality").GetString() ?? string.Empty,
            Scenario = data.GetProperty("scenario").GetString() ?? string.Empty,
            FirstMessage = data.GetProperty("first_mes").GetString() ?? string.Empty,
            ExampleMessages = data.TryGetProperty("mes_example", out var ex) ? ex.GetString() : null,
            SystemPrompt = data.TryGetProperty("system_prompt", out var sys) ? sys.GetString() : null,
            CreatorNotes = data.TryGetProperty("creator_notes", out var notes) ? notes.GetString() : null,
            Creator = data.TryGetProperty("creator", out var creator) ? creator.GetString() : null,
            Tags = data.TryGetProperty("tags", out var tags) ? JsonSerializer.Serialize(tags) : null,
            CharacterVersion = data.TryGetProperty("character_version", out var ver) ? ver.GetString() : null,
            CharacterBook = data.TryGetProperty("character_book", out var book) ? book.GetRawText() : null,
            Extensions = data.TryGetProperty("extensions", out var ext) ? ext.GetRawText() : null
        };
    }
}
```

---

## Lorebook/World Info 系统

### 激活逻辑

```csharp
public class LorebookActivator
{
    /// <summary>
    /// 根据对话上下文激活相关的 Lorebook 条目
    /// </summary>
    public List<CharacterBookEntry> ActivateEntries(
        List<CharacterBookEntry> entries,
        string conversationContext,
        List<ChatMessage> recentMessages)
    {
        var activated = new List<CharacterBookEntry>();
        var contextText = BuildContextText(conversationContext, recentMessages);

        foreach (var entry in entries.Where(e => e.IsEnabled))
        {
            // 始终注入的条目
            if (entry.IsConstant)
            {
                activated.Add(entry);
                continue;
            }

            // 检查关键词匹配
            var keys = entry.Keys.Split(',').Select(k => k.Trim().ToLower());
            var secondaryKeys = entry.SecondaryKeys?.Split(',').Select(k => k.Trim().ToLower()) ?? Enumerable.Empty<string>();

            bool primaryMatch = keys.Any(k => contextText.Contains(k));
            bool secondaryMatch = !entry.IsSelective || secondaryKeys.Any(k => contextText.Contains(k));

            if (primaryMatch && secondaryMatch)
            {
                activated.Add(entry);
            }
        }

        // 按 Order 排序
        return activated.OrderBy(e => e.Order).ToList();
    }
}
```

---

## UI 设计指南

### 角色列表页

- 网格/列表视图切换
- 搜索和标签筛选
- 收藏功能
- 最近使用排序
- 导入/导出按钮

### 角色编辑页

**基础信息 Tab:**
- 名称输入
- 头像上传/预览
- 描述文本框 (支持 Markdown)
- 性格特征标签输入
- 场景设定

**对话设置 Tab:**
- 首条消息编辑器
- 替代问候语列表
- 对话示例编辑器 (带语法高亮)

**高级设置 Tab:**
- 系统提示词
- 历史处理指令
- Token 计数显示

**Lorebook Tab:**
- 条目列表 (可拖拽排序)
- 关键词编辑器
- 内容编辑器
- 启用/禁用开关

### 角色详情页

- 头像展示
- 基本信息展示
- 开始对话按钮
- 编辑/删除/导出按钮
- 使用统计

---

## 常见陷阱与解决方案

### 1. 审讯式循环 (Interrogation Loops)

**问题**: AI 反复追问同一信息

**解决方案**:
- 在 Description 中明确角色应该主动推进剧情
- 添加 Post History Instructions: "主动创造情节发展，不要反复询问相同信息"
- 鼓励多角色设定

### 2. 强制浪漫 (Forced Romance)

**问题**: AI 快速转向浪漫情节

**解决方案**:
- 删除 Description 中的感性/诱惑性描述
- 明确场景设定为非浪漫情境
- 在 Personality 中强调其他特质

### 3. 黏人 NPC (Clingy NPCs)

**问题**: 次要角色反复出现

**解决方案**:
- 背景角色使用通用标签而非名字 ("铁匠"而非"Jerry")
- 在 Scenario 中明确时间/地点变化

### 4. Token 超限

**问题**: 角色定义占用太多上下文

**解决方案**:
- 提供 Token 计数器
- 建议 Description 控制在 500-1000 tokens
- Lorebook 条目按需激活而非全部加载

---

## 实现检查清单

### 数据层
- [ ] Character 模型实现
- [ ] CharacterBookEntry 模型实现
- [ ] SQLite 表结构
- [ ] 导入/导出服务

### 服务层
- [ ] ICharacterService 接口
- [ ] ICharacterPromptBuilder 接口
- [ ] LorebookActivator 实现
- [ ] Token 估算器

### UI 层
- [ ] 角色列表页面
- [ ] 角色编辑页面 (多 Tab)
- [ ] 角色详情页面
- [ ] 导入/导出对话框
- [ ] 头像选择器

### 集成
- [ ] 与 ChatViewModel 集成 (选择角色)
- [ ] 与 AIService 集成 (构建 Prompt)
- [ ] 与 Conversation 关联 (记录使用的角色)

---

## 参考资源

- [SillyTavern Character Design Guide](https://sillytavern.wiki/usage/core-concepts/characterdesign/)
- [Character Card V2 Specification](https://github.com/bradennapier/character-cards-v2)
- [CHub Lorebooks Documentation](https://docs.chub.ai/docs/advanced-setups/lorebooks)

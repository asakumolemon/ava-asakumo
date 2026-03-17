# Asakumo.Avalonia 项目上下文

## 项目概述

**Asakumo** 是一个跨平台 AI 聊天客户端应用，基于 Avalonia UI 框架构建。支持 Windows、macOS、Linux、iOS、Android 和 WebAssembly 平台。

### 核心技术栈

- **框架**: Avalonia UI 11.3.12
- **运行时**: .NET 9.0
- **MVVM**: CommunityToolkit.Mvvm 8.4.0
- **DI**: Microsoft.Extensions.DependencyInjection 9.0.0
- **日志**: Microsoft.Extensions.Logging 9.0.0
- **AI SDK**: OpenAI 2.2.0, Google.GenAI 1.0.0
- **数据存储**: sqlite-net-pcl 1.9.172 + SQLitePCLRaw.bundle_green 2.1.10
- **Markdown**: Markdown.Avalonia 11.0.3-a1
- **主题**: Fluent Theme + Material Design 3 组件

### 架构模式

采用标准的 MVVM 架构：
- **Models**: 数据实体 (ChatMessage, Conversation, AIProvider, AIModel, AppSettings)
- **ViewModels**: 业务逻辑和视图状态管理
- **Views**: AXAML 视图定义
- **Services**: 服务层 (INavigationService, IDataService, IAIService, IThemeService)
- **Providers**: AI 服务商适配器 (IAIProvider, OpenAICompatibleProvider, GeminiProvider, AnthropicProvider)

---

## 项目结构

```
Asakumo.Avalonia/
├── Asakumo.Avalonia/              # 核心项目 (共享代码)
│   ├── Models/                    # 数据模型
│   │   ├── ChatMessage.cs         # 聊天消息 (SQLite 表 + ObservableObject)
│   │   ├── Conversation.cs        # 会话 (SQLite 表)
│   │   ├── AIProvider.cs          # AI 服务提供商 (静态数据)
│   │   ├── AIModel.cs             # AI 模型 (静态数据)
│   │   └── AppSettings.cs         # 应用设置 (JSON 存储)
│   ├── ViewModels/                # 视图模型
│   │   ├── ViewModelBase.cs       # 基类
│   │   ├── MainViewModel.cs       # 主容器
│   │   ├── WelcomeViewModel.cs    # 欢迎页
│   │   ├── ConversationListViewModel.cs  # 会话列表
│   │   ├── ChatViewModel.cs       # 聊天页
│   │   ├── SettingsViewModel.cs   # 设置页
│   │   ├── ProviderSelectionViewModel.cs # 服务商选择
│   │   ├── ApiKeyConfigViewModel.cs      # API 配置
│   │   └── ModelSelectionViewModel.cs    # 模型选择
│   ├── Views/                     # 视图 (AXAML)
│   │   ├── ConversationListView.axaml    # 会话列表 (MD3 导航栏)
│   │   ├── ConversationListView.axaml.cs # 滑动手势处理
│   │   ├── ChatView.axaml                # 聊天界面 (更多菜单/复制)
│   │   └── ... 其他视图
│   ├── Services/                  # 服务层
│   │   ├── IDataService.cs        # 数据服务接口
│   │   ├── DataService.cs         # 数据服务实现 (SQLite + JSON)
│   │   ├── INavigationService.cs  # 导航服务接口
│   │   ├── NavigationService.cs   # 导航服务实现
│   │   ├── IAIService.cs          # AI 服务接口
│   │   ├── AIService.cs           # AI 服务实现
│   │   ├── IThemeService.cs       # 主题服务接口和实现
│   │   └── Providers/             # AI 服务商适配器
│   │       ├── IAIProvider.cs     # 适配器接口
│   │       ├── AIProviderFactory.cs # 工厂类
│   │       ├── OpenAICompatibleProvider.cs # OpenAI 兼容实现
│   │       ├── GeminiProvider.cs  # Google Gemini 实现
│   │       └── AnthropicProvider.cs # Anthropic Claude 实现
│   ├── skills/                    # 项目专属 Skills
│   ├── prototype/                 # UI 原型设计文档
│   └── App.axaml.cs               # 应用入口 + DI 配置
├── Asakumo.Avalonia.Desktop/      # 桌面平台入口
├── Asakumo.Avalonia.Browser/      # WebAssembly 入口
├── Asakumo.Avalonia.Android/      # Android 平台
├── Asakumo.Avalonia.iOS/          # iOS 平台
└── Directory.Packages.props       # 中央包版本管理
```

---

## 构建与运行

### 构建命令

```bash
# 构建核心项目
dotnet build Asakumo.Avalonia/Asakumo.Avalonia.csproj

# 构建桌面应用
dotnet build Asakumo.Avalonia.Desktop/Asakumo.Avalonia.Desktop.csproj

# 运行桌面应用
dotnet run --project Asakumo.Avalonia.Desktop/Asakumo.Avalonia.Desktop.csproj
```

### 平台特定构建

```bash
# Browser (WebAssembly)
dotnet build Asakumo.Avalonia.Browser/Asakumo.Avalonia.Browser.csproj

# Android
dotnet build Asakumo.Avalonia.Android/Asakumo.Avalonia.Android.csproj

# iOS (需要 macOS)
dotnet build Asakumo.Avalonia.iOS/Asakumo.Avalonia.iOS.csproj
```

---

## 数据持久化

### 存储方案

| 数据类型 | 存储方案 | 文件位置 |
|----------|----------|----------|
| 会话 (Conversation) | SQLite | `%AppData%/Asakumo/asakumo.db` |
| 消息 (ChatMessage) | SQLite | 同上 |
| 应用设置 (AppSettings) | JSON | `%AppData%/Asakumo/settings.json` |
| Provider 配置 | JSON (嵌入设置) | 同上 |

### 数据模型特性

```csharp
// Conversation - SQLite 表
[Table("conversations")]
public class Conversation
{
    [PrimaryKey]
    public string Id { get; set; }
    
    [MaxLength(200)]
    public string Title { get; set; }
    
    [MaxLength(500)]
    public string Preview { get; set; }
    
    public bool IsPinned { get; set; }  // 置顶状态
    
    public bool HasUnread { get; set; }
    
    public int UnreadCount { get; set; }
    
    [Indexed]
    public DateTime UpdatedAt { get; set; }
}

// ChatMessage - SQLite 表 + ObservableObject (支持流式响应 UI 更新)
[Table("chat_messages")]
public partial class ChatMessage : ObservableObject
{
    [PrimaryKey]
    public string Id { get; set; }
    
    [Indexed]
    public string ConversationId { get; set; }
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasContent))]
    [NotifyPropertyChangedFor(nameof(IsStreaming))]
    private string _content = string.Empty;
    
    [ObservableProperty]
    private string _editableContent = string.Empty;
    
    [ObservableProperty]
    private bool _isEditing;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStreaming))]
    [NotifyPropertyChangedFor(nameof(IsComplete))]
    private bool _isLoading;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsComplete))]
    private bool _isError;
    
    [ObservableProperty]
    private bool _isComplete;
    
    // 计算属性 - 用于 UI 绑定
    public bool HasContent => !string.IsNullOrEmpty(Content);
    public bool IsStreaming => IsLoading && HasContent;
    public bool IsComplete => !IsLoading && !IsError;
    
    [Indexed]
    public DateTime Timestamp { get; set; }
    
    public bool IsUser { get; set; }
}
```

### JSON 原子写入

应用设置使用原子写入模式，避免写入中断导致文件损坏：

```csharp
public async Task SaveSettingsAsync(AppSettings settings)
{
    var json = JsonSerializer.Serialize(settings, _jsonOptions);
    var tempPath = _settingsPath + ".tmp";
    
    // 先写临时文件
    await File.WriteAllTextAsync(tempPath, json);
    
    // 原子替换
    File.Move(tempPath, _settingsPath, overwrite: true);
}
```

---

## 开发约定

### MVVM 规范

1. **ViewModel 必须继承 ViewModelBase**
2. **使用 `[ObservableProperty]` 自动生成属性**
3. **使用 `[RelayCommand]` 自动生成命令**
4. **ViewModel 不得引用 UI 控件**
5. **异步方法使用 `Async` 后缀**
6. **使用 `#region` 组织代码结构**

```csharp
// 标准 ViewModel 模式
public partial class ExampleViewModel : ViewModelBase
{
    #region Observable Properties
    
    [ObservableProperty]
    private string _title = string.Empty;
    
    #endregion

    #region Commands
    
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        // 异步业务逻辑
    }
    
    #endregion
}
```

### Model 规范 (需要 UI 更新的场景)

对于需要在运行时动态更新并通知 UI 的数据模型（如流式响应消息），Model 类应继承 `ObservableObject`：

```csharp
// 支持实时 UI 更新的 Model
public partial class ChatMessage : ObservableObject
{
    // 使用 ObservableProperty 支持属性变更通知
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasContent))]
    private string _content = string.Empty;
    
    // 计算属性简化 XAML 绑定
    public bool HasContent => !string.IsNullOrEmpty(Content);
    public bool IsStreaming => IsLoading && HasContent;
    public bool IsComplete => !IsLoading && !IsError;
}

// 使用示例：流式响应时实时更新
var response = new ChatMessage { Content = "" };
Messages.Add(response);

await foreach (var token in aiService.StreamChatAsync(...))
{
    response.Content += token;  // 自动触发 UI 更新
}
```

### AXAML 规范

1. **必须使用 `x:DataType` 启用编译绑定**
2. **使用 `{DynamicResource}` 引用主题资源**
3. **使用 `Classes` 属性应用样式**
4. **使用 PathIcon 替代 emoji 图标**
5. **使用简单属性绑定替代复杂 MultiBinding**

```xml
<UserControl x:Class="MyApp.Views.ExampleView"
             x:DataType="vm:ExampleViewModel">
  <!-- 使用 PathIcon -->
  <Button Classes="icon" ToolTip.Tip="返回">
    <PathIcon Data="M20,11H7.83L13.42,5.41L12,4L4,12L12,20L13.41,18.59L7.83,13H20V11Z"
              Width="20" Height="20"/>
  </Button>
  
  <!-- 使用简单绑定 -->
  <StackPanel IsVisible="{Binding IsStreaming}">
    <TextBlock Text="{Binding Content}"/>
    <Border Classes="streaming-cursor"/>
  </StackPanel>
</UserControl>
```

### 命名约定

| 类型 | 约定 | 示例 |
|------|------|------|
| 类 | PascalCase | `ChatViewModel` |
| 私有字段 | _camelCase | `_userName` |
| 属性 | PascalCase | `UserName` |
| 方法 | PascalCase | `SendMessage()` |
| 异步方法 | XxxAsync | `LoadDataAsync()` |
| 命令 | XxxCommand | `SendCommand` |
| 视图 | XxxView | `ChatView.axaml` |

---

## 服务层架构

### 核心接口

```csharp
// 数据服务
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
    Task DeleteMessageAsync(string messageId);
    Task DeleteMessagesAsync(string conversationId);

    // 设置管理
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);

    // Provider 配置
    Task<ProviderConfig?> GetProviderConfigAsync(string providerId);
    Task SaveProviderConfigAsync(string providerId, ProviderConfig config);

    // 静态数据
    List<AIProvider> GetProviders();
}

// 导航服务
public interface INavigationService
{
    ViewModelBase? CurrentView { get; }
    bool CanGoBack { get; }
    void NavigateTo<T>() where T : ViewModelBase;
    void NavigateTo<T>(string parameter) where T : ViewModelBase;
    void GoBack();
    event Action<ViewModelBase>? NavigationChanged;
}

// AI 服务
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

// 主题服务
public interface IThemeService
{
    bool IsDarkMode { get; set; }
    void Initialize(bool isDarkMode);
    event Action<bool>? ThemeChanged;
}
```

### AI 服务商适配器

```csharp
// 统一适配器接口
public interface IAIProvider
{
    string ProviderId { get; }
    IAsyncEnumerable<string> StreamChatAsync(IEnumerable<ProviderMessage> messages, string modelId, CancellationToken ct = default);
    Task<IEnumerable<AIModel>> GetModelsAsync();
    Task<bool> ValidateAsync();
}

// 消息记录
public record ProviderMessage(string Role, string Content);
```

### 依赖注入配置

在 `App.axaml.cs` 中配置：

```csharp
var services = new ServiceCollection();

// 日志
services.AddLogging(builder => builder.AddDebug());

// 服务
services.AddSingleton<IDataService, DataService>();
services.AddSingleton<INavigationService, NavigationService>();
services.AddSingleton<IThemeService, ThemeService>();
services.AddSingleton<AIProviderFactory>();
services.AddSingleton<IAIService, AIService>();

// ViewModels
services.AddTransient<MainViewModel>();
services.AddTransient<WelcomeViewModel>();
services.AddTransient<ConversationListViewModel>();
services.AddTransient<ChatViewModel>();
services.AddTransient<SettingsViewModel>();
services.AddTransient<ProviderSelectionViewModel>();
services.AddTransient<ApiKeyConfigViewModel>();
services.AddTransient<ModelSelectionViewModel>();
```

---

## 支持的 AI 服务商

| Provider | 类型 | 说明 |
|----------|------|------|
| OpenAI | OpenAI 兼容 | GPT-4o, GPT-4o-mini, o3-mini, GPT-3.5-turbo |
| Anthropic | 原生 SDK | Claude 3.5 Sonnet, Claude 3 Opus |
| Google | 原生 SDK | Gemini 2.0 Flash, Gemini 1.5 Pro |
| DeepSeek | OpenAI 兼容 | DeepSeek Chat, DeepSeek Reasoner |
| Ollama | OpenAI 兼容 | Llama 3.2, Qwen 2.5, DeepSeek R1, Code Llama |

**注意**: DeepSeek、Ollama 等使用 OpenAI 兼容 API，只需修改 BaseUrl 即可。

---

## 页面流程

```
欢迎页 (WelcomeView)
    ↓ 开始使用 / 跳过引导
会话列表 (ConversationListView)
    ↓ 点击会话 / 新建会话
聊天页 (ChatView)
    ↓ 未配置时
服务商选择 (ProviderSelectionView)
    ↓ 选择提供商
API 配置 (ApiKeyConfigView)
    ↓ 验证成功
模型选择 (ModelSelectionView)
    ↓ 确认选择
返回聊天页
```

---

## Material Design 3 UI 组件

### 顶部应用栏 (Top App Bar)

- **高度**: 64dp (Small Top App Bar)
- **Logo**: 月亮 emoji 图标 (🌙)
- **右侧**: 设置图标按钮

```xml
<Border Height="64">
  <Grid ColumnDefinitions="Auto, *, Auto" Margin="16,0">
    <!-- Logo -->
    <Border Width="40" Height="40" CornerRadius="20"
            Background="{DynamicResource PrimaryBrush}">
      <TextBlock Text="&#x1F319;" FontSize="20"/>
    </Border>
    <!-- Settings Button -->
    <Button Grid.Column="2" Classes="icon">
      <PathIcon Data="..." Width="24" Height="24"/>
    </Button>
  </Grid>
</Border>
```

### 底部导航栏 (Navigation Bar)

- **高度**: 80dp
- **活动项**: 药丸形指示器 + 主色调图标
- **非活动项**: 灰色图标

```xml
<Border Height="80" Background="{DynamicResource AppSurfaceBrush}">
  <Grid ColumnDefinitions="*, *">
    <!-- Active Tab -->
    <Button Grid.Column="0">
      <StackPanel Spacing="4">
        <!-- Pill Background -->
        <Border Background="{DynamicResource PrimaryBrush}"
                CornerRadius="16" Width="64" Height="32">
          <PathIcon ... Foreground="White"/>
        </Border>
        <TextBlock Text="会话" Foreground="{DynamicResource PrimaryBrush}"/>
      </StackPanel>
    </Button>
    <!-- Inactive Tab -->
    <Button Grid.Column="1">
      <StackPanel Spacing="4">
        <PathIcon ... Foreground="{DynamicResource AppTextSecondaryBrush}"/>
        <TextBlock Text="设置" Foreground="{DynamicResource AppTextSecondaryBrush}"/>
      </StackPanel>
    </Button>
  </Grid>
</Border>
```

### 会话列表项 (List Item)

- **高度**: 72dp
- **布局**: 三栏网格 (Avatar 56dp | 内容 | 时间/徽章)
- **滑动**: 左滑显示置顶，右滑显示删除

```xml
<Grid Height="72" ColumnDefinitions="Auto, *, Auto">
  <!-- Avatar -->
  <Border Width="48" Height="48" CornerRadius="24">
    <TextBlock Text="AI"/>
  </Border>
  <!-- Content -->
  <StackPanel Grid.Column="1" Margin="16,0">
    <TextBlock Text="{Binding Title}" FontSize="16" FontWeight="Medium"/>
    <TextBlock Text="{Binding Preview}" FontSize="14" Opacity="0.6"/>
  </StackPanel>
  <!-- Trailing -->
  <StackPanel Grid.Column="2">
    <TextBlock Text="{Binding UpdatedAt, StringFormat='{}{0:HH:mm}'}"/>
    <Border IsVisible="{Binding HasUnread}">
      <TextBlock Text="{Binding UnreadCount}"/>
    </Border>
  </StackPanel>
</Grid>
```

---

## 聊天界面功能

### 消息显示状态

ChatMessage 模型支持多种显示状态：

| 状态 | 条件 | UI 表现 |
|------|------|----------|
| 加载中 | `IsLoading && !HasContent` | 三个点动画 + "思考中" |
| 流式输出 | `IsLoading && HasContent` | 内容 + 闪烁光标 |
| 完成 | `!IsLoading && !IsError` | Markdown 渲染 + 时间戳 |
| 错误 | `IsError` | 错误图标 + 错误信息 + 重试按钮 |
| 编辑中 | `IsEditing` | 文本框 + 保存/取消按钮 |

### 交互功能

- **消息复制**: 点击复制按钮将消息内容复制到剪贴板
- **消息编辑**: 用户消息支持编辑和重新发送
- **消息删除**: 支持删除单条消息
- **重试发送**: 错误消息可重试
- **停止生成**: 流式输出时可中断
- **自动滚动**: 新消息自动滚动到底部
- **键盘快捷键**: Enter 发送，Shift+Enter 换行

### 更多菜单

点击聊天页右上角更多按钮打开菜单：

| 选项 | 命令 | 说明 |
|------|------|------|
| 重命名 | `RenameConversationCommand` | 修改会话标题 |
| 清空会话 | `ClearCurrentConversationCommand` | 删除所有消息 |
| 导出对话 | `ExportConversationCommand` | 导出为文本文件 |
| 设置 | `NavigateToSettingsCommand` | 跳转到设置页 |

### Toast 提示

全局 Toast 提示系统，用于显示操作反馈：

```csharp
// 显示 Toast
ShowToastMessage("已复制到剪贴板");

// 自动隐藏 (2秒后)
private async void ShowToastMessage(string message)
{
    ToastMessage = message;
    ShowToast = true;
    
    _toastCts?.Cancel();
    _toastCts = new CancellationTokenSource();
    
    try
    {
        await Task.Delay(2000, _toastCts.Token);
        ShowToast = false;
    }
    catch (OperationCanceledException) { }
}
```

### 动画效果

```xml
<!-- 打字指示器动画 -->
<Style Selector="StackPanel.typing-indicator > Border.typing-dot:nth-child(1)">
    <Style.Animations>
        <Animation Duration="0:0:1.2" IterationCount="Infinite">
            <KeyFrame Cue="0%"><Setter Property="Opacity" Value="0.3"/></KeyFrame>
            <KeyFrame Cue="25%"><Setter Property="Opacity" Value="1"/></KeyFrame>
            <KeyFrame Cue="50%"><Setter Property="Opacity" Value="0.3"/></KeyFrame>
        </Animation>
    </Style.Animations>
</Style>

<!-- 流式光标闪烁 -->
<Style Selector="Border.streaming-cursor">
    <Style.Animations>
        <Animation Duration="0:0:0.6" IterationCount="Infinite">
            <KeyFrame Cue="0%"><Setter Property="Opacity" Value="0.3"/></KeyFrame>
            <KeyFrame Cue="50%"><Setter Property="Opacity" Value="1"/></KeyFrame>
            <KeyFrame Cue="100%"><Setter Property="Opacity" Value="0.3"/></KeyFrame>
        </Animation>
    </Style.Animations>
</Style>
```

---

## 滑动手势

### 会话列表滑动操作

实现左滑置顶、右滑删除功能：

```csharp
// ConversationListView.axaml.cs
public partial class ConversationListView : UserControl
{
    private const double SwipeThreshold = 72;
    private Control? _currentSwipeItem;
    private double _startX;
    private bool _isSwiping;

    private void OnSwipePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        
        // Close any previously swiped item
        if (_currentSwipeItem != null && _currentSwipeItem != control)
        {
            ResetSwipe(_currentSwipeItem);
        }
        
        _currentSwipeItem = control;
        _startX = e.GetPosition(control).X;
        _isSwiping = true;
    }

    private void OnSwipePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isSwiping || _currentSwipeItem == null) return;
        
        var currentX = e.GetPosition(_currentSwipeItem).X;
        var deltaX = currentX - _startX;
        
        // Limit swipe distance
        if (deltaX > SwipeThreshold) deltaX = SwipeThreshold;
        if (deltaX < -SwipeThreshold) deltaX = -SwipeThreshold;
        
        // Apply transform
        var transform = TransformOperations.CreateBuilder(1);
        transform.AppendTranslate(deltaX, 0);
        _currentSwipeItem.RenderTransform = transform.Build();
    }

    private void OnSwipePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isSwiping || _currentSwipeItem == null) return;
        
        var currentX = e.GetPosition(_currentSwipeItem).X;
        var deltaX = currentX - _startX;
        
        // Snap to open or closed position
        if (Math.Abs(deltaX) > SwipeThreshold / 2)
        {
            var snapX = deltaX > 0 ? SwipeThreshold : -SwipeThreshold;
            var transform = TransformOperations.CreateBuilder(1);
            transform.AppendTranslate(snapX, 0);
            _currentSwipeItem.RenderTransform = transform.Build();
        }
        else
        {
            ResetSwipe(_currentSwipeItem);
        }
        
        _isSwiping = false;
    }

    private static void ResetSwipe(Control item)
    {
        item.RenderTransform = TransformOperations.Identity;
    }
}
```

---

## 项目 Skills

项目包含多个专属 Skill，位于 `Asakumo.Avalonia/skills/`:

| Skill | 用途 |
|-------|------|
| `avalonia-chat-ui-design` | AI 聊天应用 UI 设计指南 |
| `avalonia-ui-prototype` | ASCII 线框图原型生成 |
| `dotnet-avalonia-quality` | 代码质量和 MVVM 规范 |
| `ai-api-service` | AI API 服务层实现模式 |
| `data-persistence` | 本地数据持久化实现 |
| `multi-provider-adapter` | 多 AI 服务商适配器实现 |
| `agent-loading-effects` | Agent 加载动画效果 |
| `markdown-rendering-optimization` | Markdown 渲染优化 |
| `linus-code-review` | 代码审查规范 |

---

## 中央包版本管理

项目使用 `Directory.Packages.props` 进行集中版本管理。添加新包时：

1. 在 `Directory.Packages.props` 中添加 `<PackageVersion>`
2. 在项目 `.csproj` 中添加 `<PackageReference>` (不带版本号)

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="NewPackage" Version="1.0.0" />

<!-- 项目.csproj -->
<PackageReference Include="NewPackage" />
```

---

## 常见问题

### XAML 编译错误

- 使用 `x:DataType` 启用编译绑定，提前发现错误
- 检查绑定路径是否正确
- 确保命名空间正确引用

### 布局性能

- 避免深层嵌套，保持布局扁平
- 使用虚拟化列表处理大量数据
- 使用 `Grid` 的 `RowDefinitions`/`ColumnDefinitions` 替代嵌套 `StackPanel`

### 深色模式

- 使用 `DynamicResource` 引用系统资源
- 定义 `ThemeDictionaries` 支持深浅主题切换
- 确保文字对比度 >= 4.5:1

### 流式响应异常处理

使用 Channel 模式处理 `IAsyncEnumerable` 中的异常：

```csharp
public async IAsyncEnumerable<string> StreamChatAsync(...)
{
    var channel = Channel.CreateUnbounded<string>();
    
    _ = Task.Run(async () => {
        try {
            await foreach (var token in provider.StreamChatAsync(...))
                await channel.Writer.WriteAsync(token, ct);
        } catch (Exception ex) {
            await channel.Writer.WriteAsync($"[错误] {ex.Message}", ct);
        } finally {
            channel.Writer.Complete();
        }
    }, ct);
    
    await foreach (var token in channel.Reader.ReadAllAsync(ct))
        yield return token;
}
```

### 流式响应 UI 不更新

**问题**: 流式响应时消息内容在更新，但 UI 没有实时显示。

**原因**: Model 类是普通 POCO，属性变更不会触发 `INotifyPropertyChanged`。

**解决方案**: 让 Model 类继承 `ObservableObject`，使用 `[ObservableProperty]`：

```csharp
// 错误: 普通 POCO，UI 不会更新
public class ChatMessage
{
    public string Content { get; set; }  // 无通知
}

// 正确: 继承 ObservableObject
public partial class ChatMessage : ObservableObject
{
    [ObservableProperty]
    private string _content = string.Empty;  // 自动通知 UI
}

// 使用时
response.Content += token;  // 自动触发 PropertyChanged 事件
```

### 复杂 MultiBinding 问题

**问题**: 在 MultiBinding 中使用否定绑定 (`!IsLoading`) 可能无法正确解析。

**解决方案**: 为 Model 添加计算属性，简化绑定：

```csharp
// 添加计算属性
public bool IsComplete => !IsLoading && !IsError;
public bool IsStreaming => IsLoading && HasContent;

// XAML 使用简单绑定
<StackPanel IsVisible="{Binding IsStreaming}">
    <TextBlock Text="{Binding Content}"/>
    <Border Classes="streaming-cursor"/>
</StackPanel>
```

### 异步方法调用

在构造函数中调用异步方法，使用 `GetAwaiter().GetResult()` (桌面应用) 或 `_ = InitializeAsync()` (Fire-and-forget)：

```csharp
// 构造函数中同步等待
public MainViewModel(IDataService dataService)
{
    Settings = dataService.GetSettingsAsync().GetAwaiter().GetResult();
}

// 或使用 Fire-and-forget
public SettingsViewModel(IDataService dataService)
{
    _ = LoadSettingsAsync();
}
```

### 自动滚动实现

在 View 的 code-behind 中处理自动滚动：

```csharp
public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ChatViewModel viewModel)
        {
            viewModel.MessageAdded += (_, _) => ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        _messagesScrollViewer?.ScrollToEnd();
    }
}
```

### Toast 提示冲突

**问题**: Toast 提示显示时，之前设置的隐藏定时器仍在运行。

**解决方案**: 使用 CancellationTokenSource 取消之前的定时器：

```csharp
private CancellationTokenSource? _toastCts;

private async void ShowToastMessage(string message)
{
    ToastMessage = message;
    ShowToast = true;
    
    // 取消之前的定时器
    _toastCts?.Cancel();
    _toastCts = new CancellationTokenSource();
    
    try
    {
        await Task.Delay(2000, _toastCts.Token);
        ShowToast = false;
    }
    catch (OperationCanceledException) 
    { 
        // 被取消时忽略异常
    }
}
```

---

## 最近更新

### UI 优化 (2026-03)

1. **Material Design 3 导航栏**
   - 顶部应用栏: 64dp 高度，月亮 emoji Logo
   - 底部导航栏: 80dp 高度，药丸形活动指示器

2. **会话列表 MD3 重构**
   - 三栏网格布局 (56dp Avatar + 内容 + 时间/徽章)
   - 统一 72dp 高度
   - 时间分组显示 (今天/昨天/更早)
   - 左滑置顶 / 右滑删除手势

3. **聊天界面增强**
   - 消息复制功能
   - 右上角更多菜单 (重命名/清空/导出/设置)
   - Toast 提示系统
   - 标题编辑功能

4. **消息状态优化**
   - 添加 `IsComplete` 持久化属性
   - 支持消息编辑模式
   - 流式光标动画
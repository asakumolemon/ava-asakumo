# Asakumo.Avalonia 项目上下文

## 项目概述

**Asakumo** 是一个跨平台 AI 聊天客户端应用，基于 Avalonia UI 框架构建。支持 Windows、macOS、Linux、iOS、Android 和 WebAssembly 平台。

### 核心技术栈

- **框架**: Avalonia UI 11.3.12
- **运行时**: .NET 9.0
- **MVVM**: CommunityToolkit.Mvvm 8.4.0
- **DI**: Microsoft.Extensions.DependencyInjection 9.0.0
- **主题**: Fluent Theme + Inter 字体

### 架构模式

采用标准的 MVVM 架构：
- **Models**: 数据实体 (ChatMessage, Conversation, AIProvider, AIModel, AppSettings)
- **ViewModels**: 业务逻辑和视图状态管理
- **Views**: AXAML 视图定义
- **Services**: 服务层 (INavigationService, IDataService)

---

## 项目结构

```
Asakumo.Avalonia/
├── Asakumo.Avalonia/              # 核心项目 (共享代码)
│   ├── Models/                    # 数据模型
│   │   ├── ChatMessage.cs         # 聊天消息
│   │   ├── Conversation.cs        # 会话
│   │   ├── AIProvider.cs          # AI 服务提供商
│   │   ├── AIModel.cs             # AI 模型
│   │   └── AppSettings.cs         # 应用设置
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
│   ├── Services/                  # 服务层
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

## 开发约定

### MVVM 规范

1. **ViewModel 必须继承 ViewModelBase**
2. **使用 `[ObservableProperty]` 自动生成属性**
3. **使用 `[RelayCommand]` 自动生成命令**
4. **ViewModel 不得引用 UI 控件**

```csharp
// 标准 ViewModel 模式
public partial class ExampleViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = string.Empty;

    [RelayCommand]
    private void DoSomething()
    {
        // 业务逻辑
    }
}
```

### AXAML 规范

1. **必须使用 `x:DataType` 启用编译绑定**
2. **使用 `{DynamicResource}` 引用主题资源**
3. **使用 `Classes` 属性应用样式**

```xml
<UserControl x:Class="MyApp.Views.ExampleView"
             x:DataType="vm:ExampleViewModel">
  <Button Classes="primary" Command="{Binding DoSomethingCommand}">
    <TextBlock Text="{Binding Title}"/>
  </Button>
</UserControl>
```

### 命名约定

| 类型 | 约定 | 示例 |
|------|------|------|
| 类 | PascalCase | `ChatViewModel` |
| 私有字段 | _camelCase | `_userName` |
| 属性 | PascalCase | `UserName` |
| 方法 | PascalCase | `SendMessage()` |
| 命令 | XxxCommand | `SendCommand` |
| 视图 | XxxView | `ChatView.axaml` |

---

## 页面流程

根据 `prototype/ui-prototype.md` 的设计：

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

## 项目 Skills

项目包含三个专属 Skill，位于 `Asakumo.Avalonia/skills/`:

| Skill | 用途 |
|-------|------|
| `avalonia-chat-ui-design` | AI 聊天应用 UI 设计指南 |
| `avalonia-ui-prototype` | ASCII 线框图原型生成 |
| `dotnet-avalonia-quality` | 代码质量和 MVVM 规范 |

---

## 依赖注入配置

在 `App.axaml.cs` 中配置：

```csharp
var services = new ServiceCollection();

// 服务
services.AddSingleton<IDataService, DataService>();
services.AddSingleton<INavigationService, NavigationService>();

// ViewModels
services.AddTransient<MainViewModel>();
services.AddTransient<WelcomeViewModel>();
// ... 其他 ViewModels
```

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

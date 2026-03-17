# Markdown 渲染优化指南

## 概述

在 Avalonia 应用中使用 Markdown.Avalonia 渲染聊天消息时，常见的 4 个问题及解决方案。

## 问题与解决方案

### 问题 1: Markdown 消息被转化为空

**现象**: 当消息内容为空或 null 时，MarkdownScrollViewer 可能渲染失败或显示空白。

**解决方案**: 添加空内容回退机制

```xml
<!-- 空内容回退：当 Markdown 为空时显示普通文本 -->
<TextBlock Text="{Binding Content}"
           TextWrapping="Wrap" FontSize="14"
           Foreground="{DynamicResource AppTextPrimaryBrush}"
           IsVisible="{Binding !HasContent}"/>
<!-- Markdown 渲染 -->
<markdown:MarkdownScrollViewer Markdown="{Binding Content}"
                               IsVisible="{Binding HasContent}"/>
```

### 问题 2 & 3: 重新进入会话后 Markdown 不渲染 / 全部消息显示为"接收中"

**根本原因**:
- `IsComplete` 原本是计算属性 (`!IsLoading && !IsError`)，不持久化到数据库
- 从数据库加载消息后，属性变更通知可能未正确触发
- 无法区分"已完成的消息"和"新加载的消息"

**解决方案**: 添加 `IsComplete` 持久化字段

```csharp
// ChatMessage.cs
[ObservableProperty]
private bool _isComplete;

// 发送完成后设置
response.IsComplete = true;
await _dataService.SaveMessageAsync(response);

// 加载时兼容旧数据
foreach (var message in messages)
{
    if (!message.IsComplete && !message.IsLoading && !message.IsError)
    {
        message.IsComplete = true;
    }
}
```

### 问题 4: 移动端 Markdown 缩进太长

**现象**: 代码块和列表在移动端有过大的边距和缩进。

**解决方案**: 覆盖默认样式

```xml
<markdown:MarkdownScrollViewer Markdown="{Binding Content}">
  <markdown:MarkdownScrollViewer.Styles>
    <!-- 减小代码块内边距 -->
    <Style Selector="Border.code-block">
      <Setter Property="Margin" Value="0,4"/>
      <Setter Property="Padding" Value="8,6"/>
    </Style>
    <!-- 减小列表缩进 -->
    <Style Selector="StackPanel.list">
      <Setter Property="Margin" Value="0,4"/>
    </Style>
    <Style Selector="StackPanel.list > TextBlock">
      <Setter Property="Margin" Value="0,2"/>
    </Style>
  </markdown:MarkdownScrollViewer.Styles>
</markdown:MarkdownScrollViewer>
```

## 最佳实践

1. **状态持久化**: 聊天消息的 UI 状态（如 IsComplete）需要持久化，不能仅依赖计算属性
2. **空值处理**: Markdown 控件应始终检查空内容并提供回退
3. **移动端适配**: 为移动设备定制 Markdown 样式，减小默认的 20-40px 边距
4. **向后兼容**: 数据库架构变更时，在加载数据时自动修复旧数据

## 相关文件

- `Models/ChatMessage.cs` - 消息模型
- `Views/ChatView.axaml` - 聊天界面
- `Services/DataService.cs` - 数据服务
- `ViewModels/ChatViewModel.cs` - 聊天视图模型

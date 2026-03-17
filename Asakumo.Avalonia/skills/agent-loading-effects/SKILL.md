---
name: agent-loading-effects
description: AI Agent 接收消息时的加载效果设计 Skill。包含流式输出、省略号动画、打字机效果、骨架屏等多种加载状态模式。适用于 Avalonia AI 聊天应用。触发词：加载效果、流式输出、打字机效果、省略号动画、thinking indicator、skeleton loading。
---

# Agent 消息加载效果 Skill

为 AI 聊天应用提供专业的消息加载状态设计方案。

## 加载效果类型概览

| 效果类型 | 适用场景 | 用户体验 | 实现复杂度 |
|----------|----------|----------|------------|
| **流式输出 (Streaming)** | 实时生成内容 | 最佳，用户感知实时性 | 中等 |
| **省略号动画 (Ellipsis)** | 等待响应开始 | 好，明确的等待反馈 | 简单 |
| **打字机效果 (Typewriter)** | 非流式内容展示 | 好，模拟真人打字 | 简单 |
| **骨架屏 (Skeleton)** | 长内容预占位 | 好，减少布局跳动 | 中等 |
| **Thinking 指示器** | 展示推理过程 | 很好，增加透明度 | 中等 |

---

## 1. 流式输出 (Streaming) - 推荐

### 设计原理

流式输出是现代 AI 聊天应用的标准，如 ChatGPT、Claude 均采用此模式。

**优势：**
- 用户立即看到内容开始生成
- 减少感知等待时间
- 可中途停止生成

### Avalonia 实现

```csharp
// ViewModel 中使用 ObservableProperty 支持实时更新
public partial class ChatMessage : ObservableObject
{
    [ObservableProperty]
    private string _content = string.Empty;
    
    [ObservableProperty]
    private bool _isStreaming; // 是否正在流式输出
    
    [ObservableProperty]
    private bool _isComplete;  // 是否已完成
}

// ChatViewModel 中的流式处理
public partial class ChatViewModel : ViewModelBase
{
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        // 添加用户消息
        var userMessage = new ChatMessage 
        { 
            Role = "user", 
            Content = InputMessage 
        };
        Messages.Add(userMessage);
        
        // 创建 AI 响应消息（初始为空）
        var aiMessage = new ChatMessage 
        { 
            Role = "assistant", 
            Content = "",
            IsStreaming = true,
            IsComplete = false
        };
        Messages.Add(aiMessage);
        
        try
        {
            // 流式接收
            await foreach (var token in _aiService.StreamChatAsync(conversationId, InputMessage))
            {
                aiMessage.Content += token; // 自动触发 UI 更新
            }
        }
        finally
        {
            aiMessage.IsStreaming = false;
            aiMessage.IsComplete = true;
        }
    }
}
```

### XAML 布局

```xml
<!-- 流式消息气泡 -->
<Border Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}"
        CornerRadius="4,16,16,16"
        Padding="12,8">
  <StackPanel Spacing="4">
    <!-- 内容 -->
    <TextBlock Text="{Binding Content}" 
               TextWrapping="Wrap"
               Foreground="{DynamicResource SystemControlForegroundBaseBrush}"/>
    
    <!-- 流式指示器（仅在流式中显示） -->
    <StackPanel Orientation="Horizontal" 
                Spacing="4"
                IsVisible="{Binding IsStreaming}">
      <Border Width="6" Height="6" 
              Background="{DynamicResource SystemAccentColor}" 
              CornerRadius="3">
        <Border.Styles>
          <Style Selector="Border">
            <Style.Animations>
              <Animation Duration="0:0:0.6" 
                         IterationCount="INFINITE">
                <KeyFrame Cue="0%">
                  <Setter Property="Opacity" Value="0.3"/>
                </KeyFrame>
                <KeyFrame Cue="50%">
                  <Setter Property="Opacity" Value="1"/>
                </KeyFrame>
                <KeyFrame Cue="100%">
                  <Setter Property="Opacity" Value="0.3"/>
                </KeyFrame>
              </Animation>
            </Style.Animations>
          </Style>
        </Border.Styles>
      </Border>
      <TextBlock Text="生成中..." 
                 FontSize="11"
                 Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"/>
    </StackPanel>
  </StackPanel>
</Border>
```

---

## 2. 省略号动画 (Ellipsis / Typing Indicator)

### 设计原理

三个跳动的点，表示 Agent 正在"输入中"。WhatsApp、iMessage、Slack 都采用此模式。

**适用场景：**
- 等待 AI 开始响应
- 网络延迟时提供反馈
- 非流式 API 的等待状态

### Avalonia 实现

```xml
<!-- 三个跳动的小圆点 -->
<StackPanel Orientation="Horizontal" 
            Spacing="4"
            VerticalAlignment="Center">
  <!-- 圆点 1 -->
  <Border Width="8" Height="8" 
          Background="{DynamicResource SystemControlForegroundBaseMediumBrush}" 
          CornerRadius="4">
    <Border.Styles>
      <Style Selector="Border">
        <Style.Animations>
          <Animation Duration="0:0:1.2" 
                     IterationCount="INFINITE">
            <KeyFrame Cue="0%">
              <Setter Property="Opacity" Value="0.3"/>
              <Setter Property="(LayoutTransform).ScaleX" Value="0.8"/>
              <Setter Property="(LayoutTransform).ScaleY" Value="0.8"/>
            </KeyFrame>
            <KeyFrame Cue="33%">
              <Setter Property="Opacity" Value="1"/>
              <Setter Property="(LayoutTransform).ScaleX" Value="1"/>
              <Setter Property="(LayoutTransform).ScaleY" Value="1"/>
            </KeyFrame>
            <KeyFrame Cue="66%">
              <Setter Property="Opacity" Value="0.3"/>
              <Setter Property="(LayoutTransform).ScaleX" Value="0.8"/>
              <Setter Property="(LayoutTransform).ScaleY" Value="0.8"/>
            </KeyFrame>
          </Animation>
        </Style.Animations>
      </Style>
    </Border.Styles>
  </Border>
  
  <!-- 圆点 2 -->
  <Border Width="8" Height="8" 
          Background="{DynamicResource SystemControlForegroundBaseMediumBrush}" 
          CornerRadius="4">
    <Border.Styles>
      <Style Selector="Border">
        <Style.Animations>
          <Animation Duration="0:0:1.2" 
                     Delay="0:0:0.2"
                     IterationCount="INFINITE">
            <KeyFrame Cue="0%">
              <Setter Property="Opacity" Value="0.3"/>
            </KeyFrame>
            <KeyFrame Cue="33%">
              <Setter Property="Opacity" Value="1"/>
            </KeyFrame>
            <KeyFrame Cue="66%">
              <Setter Property="Opacity" Value="0.3"/>
            </KeyFrame>
          </Animation>
        </Style.Animations>
      </Style>
    </Border.Styles>
  </Border>
  
  <!-- 圆点 3 -->
  <Border Width="8" Height="8" 
          Background="{DynamicResource SystemControlForegroundBaseMediumBrush}" 
          CornerRadius="4">
    <Border.Styles>
      <Style Selector="Border">
        <Style.Animations>
          <Animation Duration="0:0:1.2" 
                     Delay="0:0:0.4"
                     IterationCount="INFINITE">
            <KeyFrame Cue="0%">
              <Setter Property="Opacity" Value="0.3"/>
            </KeyFrame>
            <KeyFrame Cue="33%">
              <Setter Property="Opacity" Value="1"/>
            </KeyFrame>
            <KeyFrame Cue="66%">
              <Setter Property="Opacity" Value="0.3"/>
            </KeyFrame>
          </Animation>
        </Style.Animations>
      </Style>
    </Border.Styles>
  </Border>
</StackPanel>
```

### 简化版（仅 CSS 风格）

```xml
<!-- 文本省略号动画 -->
<TextBlock FontSize="20" 
           Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}">
  <TextBlock.Styles>
    <Style Selector="TextBlock">
      <Style.Animations>
        <Animation Duration="0:0:1.5" 
                   IterationCount="INFINITE">
          <KeyFrame Cue="0%">
            <Setter Property="Text" Value=""/>
          </KeyFrame>
          <KeyFrame Cue="25%">
            <Setter Property="Text" Value="."/>
          </KeyFrame>
          <KeyFrame Cue="50%">
            <Setter Property="Text" Value=".."/>
          </KeyFrame>
          <KeyFrame Cue="75%">
            <Setter Property="Text" Value="..."/>
          </KeyFrame>
        </Animation>
      </Style.Animations>
    </Style>
  </TextBlock.Styles>
</TextBlock>
```

---

## 3. 打字机效果 (Typewriter Effect)

### 设计原理

内容逐字显示，模拟真人打字。适用于非流式 API 的内容展示。

**适用场景：**
- 短内容展示
- 需要强调的内容
- 非流式 API 的渐进式显示

### Avalonia 实现

```csharp
// 打字机效果 ViewModel
public partial class TypewriterMessage : ObservableObject
{
    [ObservableProperty]
    private string _displayedContent = "";
    
    [ObservableProperty]
    private bool _isTyping;
    
    private string _fullContent = "";
    
    public async Task StartTypingAsync(string content, int delayMs = 30)
    {
        _fullContent = content;
        IsTyping = true;
        DisplayedContent = "";
        
        foreach (char c in content)
        {
            DisplayedContent += c;
            await Task.Delay(delayMs);
        }
        
        IsTyping = false;
    }
}

// 使用
var message = new TypewriterMessage();
Messages.Add(message);
await message.StartTypingAsync("这是要显示的内容...");
```

### 带光标闪烁的 XAML

```xml
<StackPanel Orientation="Horizontal">
  <TextBlock Text="{Binding DisplayedContent}"
             TextWrapping="Wrap"
             Foreground="{DynamicResource SystemControlForegroundBaseBrush}"/>
  
  <!-- 光标 -->
  <Border Width="2" 
          Height="16" 
          Background="{DynamicResource SystemAccentColor}"
          IsVisible="{Binding IsTyping}"
          Margin="2,0,0,0">
    <Border.Styles>
      <Style Selector="Border">
        <Style.Animations>
          <Animation Duration="0:0:0.5" 
                     IterationCount="INFINITE">
            <KeyFrame Cue="0%">
              <Setter Property="Opacity" Value="1"/>
            </KeyFrame>
            <KeyFrame Cue="50%">
              <Setter Property="Opacity" Value="0"/>
            </KeyFrame>
            <KeyFrame Cue="100%">
              <Setter Property="Opacity" Value="1"/>
            </KeyFrame>
          </Animation>
        </Style.Animations>
      </Style>
    </Border.Styles>
  </Border>
</StackPanel>
```

---

## 4. Thinking / 推理指示器

### 设计原理

Claude 等先进模型支持展示推理过程，增加透明度。

**设计要点：**
- 显示"Thinking..."或"正在思考..."
- 可展示推理时间
- 可展开查看详细推理过程

### Avalonia 实现

```xml
<!-- Thinking 指示器 -->
<Expander IsExpanded="False"
          Background="Transparent">
  <Expander.Header>
    <StackPanel Orientation="Horizontal" Spacing="8">
      <!-- 思考图标 -->
      <PathIcon Width="16" Height="16"
                Data="M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6M12,8A4,4 0 0,1 16,12A4,4 0 0,1 12,16A4,4 0 0,1 8,12A4,4 0 0,1 12,8Z"
                Foreground="{DynamicResource SystemAccentColor}">
        <PathIcon.Styles>
          <Style Selector="PathIcon">
            <Style.Animations>
              <Animation Duration="0:0:2" 
                         IterationCount="INFINITE">
                <KeyFrame Cue="0%">
                  <Setter Property="RotateTransform.Angle" Value="0"/>
                </KeyFrame>
                <KeyFrame Cue="100%">
                  <Setter Property="RotateTransform.Angle" Value="360"/>
                </KeyFrame>
              </Animation>
            </Style.Animations>
          </Style>
        </PathIcon.Styles>
      </PathIcon>
      
      <TextBlock Text="正在思考..." 
                 Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"/>
      
      <TextBlock Text="{Binding ThinkingTime}" 
                 FontSize="11"
                 Foreground="{DynamicResource SystemControlForegroundBaseLowBrush}"/>
    </StackPanel>
  </Expander.Header>
  
  <!-- 推理过程内容 -->
  <TextBlock Text="{Binding ReasoningContent}"
             TextWrapping="Wrap"
             FontSize="12"
             Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"
             Margin="24,8,0,0"/>
</Expander>
```

---

## 5. 骨架屏 (Skeleton Loading)

### 设计原理

在长内容生成前显示占位结构，减少布局跳动。

**适用场景：**
- 预计生成较长内容
- 需要保持界面稳定
- 渐进式展示

### Avalonia 实现

```xml
<!-- 骨架屏占位 -->
<StackPanel Spacing="8" IsVisible="{Binding IsLoading}">
  <!-- 第一行 -->
  <Border Height="16" 
          Background="{DynamicResource SystemControlBackgroundBaseLowBrush}"
          CornerRadius="4"
          Width="80%">
    <Border.Styles>
      <Style Selector="Border">
        <Style.Animations>
          <Animation Duration="0:0:1.5" 
                     IterationCount="INFINITE">
            <KeyFrame Cue="0%">
              <Setter Property="Opacity" Value="0.3"/>
            </KeyFrame>
            <KeyFrame Cue="50%">
              <Setter Property="Opacity" Value="0.6"/>
            </KeyFrame>
            <KeyFrame Cue="100%">
              <Setter Property="Opacity" Value="0.3"/>
            </KeyFrame>
          </Animation>
        </Style.Animations>
      </Style>
    </Border.Styles>
  </Border>
  
  <!-- 第二行 -->
  <Border Height="16" 
          Background="{DynamicResource SystemControlBackgroundBaseLowBrush}"
          CornerRadius="4"
          Width="95%">
    <Border.Styles>
      <Style Selector="Border">
        <Style.Animations>
          <Animation Duration="0:0:1.5" 
                     Delay="0:0:0.2"
                     IterationCount="INFINITE">
            <KeyFrame Cue="0%">
              <Setter Property="Opacity" Value="0.3"/>
            </KeyFrame>
            <KeyFrame Cue="50%">
              <Setter Property="Opacity" Value="0.6"/>
            </KeyFrame>
            <KeyFrame Cue="100%">
              <Setter Property="Opacity" Value="0.3"/>
            </KeyFrame>
          </Animation>
        </Style.Animations>
      </Style>
    </Border.Styles>
  </Border>
  
  <!-- 第三行（短） -->
  <Border Height="16" 
          Background="{DynamicResource SystemControlBackgroundBaseLowBrush}"
          CornerRadius="4"
          Width="60%">
    <Border.Styles>
      <Style Selector="Border">
        <Style.Animations>
          <Animation Duration="0:0:1.5" 
                     Delay="0:0:0.4"
                     IterationCount="INFINITE">
            <KeyFrame Cue="0%">
              <Setter Property="Opacity" Value="0.3"/>
            </KeyFrame>
            <KeyFrame Cue="50%">
              <Setter Property="Opacity" Value="0.6"/>
            </KeyFrame>
            <KeyFrame Cue="100%">
              <Setter Property="Opacity" Value="0.3"/>
            </KeyFrame>
          </Animation>
        </Style.Animations>
      </Style>
    </Border.Styles>
  </Border>
</StackPanel>
```

---

## 完整组合示例

```xml
<!-- 消息容器 -->
<Border Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}"
        CornerRadius="4,16,16,16"
        Padding="12,8">
  <Grid>
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/> <!-- Thinking -->
      <RowDefinition Height="Auto"/> <!-- 骨架屏 -->
      <RowDefinition Height="Auto"/> <!-- 内容 -->
      <RowDefinition Height="Auto"/> <!-- 流式指示器 -->
    </Grid.RowDefinitions>
    
    <!-- 1. Thinking 指示器 -->
    <ContentControl Grid.Row="0"
                    ContentTemplate="{StaticResource ThinkingIndicator}"
                    IsVisible="{Binding IsThinking}"/>
    
    <!-- 2. 骨架屏（内容加载中） -->
    <ContentControl Grid.Row="1"
                    ContentTemplate="{StaticResource SkeletonLoader}"
                    IsVisible="{Binding IsLoading}"/>
    
    <!-- 3. 实际内容 -->
    <TextBlock Grid.Row="2"
               Text="{Binding Content}"
               TextWrapping="Wrap"
               IsVisible="{Binding HasContent}"/>
    
    <!-- 4. 流式指示器 -->
    <StackPanel Grid.Row="3"
                Orientation="Horizontal"
                Spacing="4"
                IsVisible="{Binding IsStreaming}">
      <Border Width="6" Height="6" 
              Background="{DynamicResource SystemAccentColor}" 
              CornerRadius="3">
        <Border.Styles>
          <Style Selector="Border">
            <Style.Animations>
              <Animation Duration="0:0:0.6" IterationCount="INFINITE">
                <KeyFrame Cue="0%"><Setter Property="Opacity" Value="0.3"/></KeyFrame>
                <KeyFrame Cue="50%"><Setter Property="Opacity" Value="1"/></KeyFrame>
                <KeyFrame Cue="100%"><Setter Property="Opacity" Value="0.3"/></KeyFrame>
              </Animation>
            </Style.Animations>
          </Style>
        </Border.Styles>
      </Border>
      <TextBlock Text="生成中..." 
                 FontSize="11"
                 Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"/>
    </StackPanel>
  </Grid>
</Border>
```

---

## 最佳实践总结

### 选择建议

| 场景 | 推荐效果 | 原因 |
|------|----------|------|
| 流式 API | 流式输出 + 流式指示器 | 最佳用户体验 |
| 非流式 API（短内容） | 省略号动画 | 简洁有效 |
| 非流式 API（长内容） | 骨架屏 + 打字机 | 渐进式展示 |
| 推理模型 | Thinking 指示器 | 增加透明度 |
| 网络延迟高 | 省略号动画 | 明确反馈 |

### 性能优化

1. **动画性能**
   - 使用 `Opacity` 动画而非布局动画
   - 限制动画元素数量
   - 使用 `RenderTransform` 而非 `LayoutTransform`

2. **内存优化**
   - 动画结束后及时清理
   - 使用 `Animation.FillMode="None"` 避免保持最终状态
   - 大量消息时考虑虚拟化

3. **用户体验**
   - 动画时长 0.5-2 秒为宜
   - 提供跳过/停止按钮
   - 支持键盘导航时动画不干扰

### 检查清单

- [ ] 根据 API 类型选择合适的加载效果
- [ ] 使用 `DynamicResource` 支持主题切换
- [ ] 动画性能优化（Opacity、RenderTransform）
- [ ] 提供停止/跳过生成按钮
- [ ] 错误状态处理（网络中断等）
- [ ] 测试深色模式下的效果
- [ ] 考虑无障碍访问（减少动画选项）

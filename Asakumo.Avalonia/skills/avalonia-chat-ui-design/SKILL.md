---
name: avalonia-chat-ui-design
description: AI chat application UI design skill for Avalonia. Based on Fluent Theme and chat app best practices. Provides XAML templates, color systems using DynamicResource, and component patterns for building professional chat interfaces.
---

# Avalonia AI 聊天应用 UI 设计 Skill

基于 **Fluent Theme** 和聊天应用最佳实践，为 Avalonia AI 聊天应用提供专业 UI 设计指导。

## When to Use

使用此 skill 当需要：
1. 设计 AI 聊天客户端界面
2. 创建聊天气泡、会话列表等组件
3. 使用 DynamicResource 支持深色/浅色主题切换
4. 优化现有聊天界面的用户体验
5. 解决 Avalonia UI 开发中的常见问题

## Trigger Keywords

- "聊天界面" / "chat UI"
- "会话列表" / "conversation list"
- "聊天气泡" / "chat bubble"
- "设计风格" / "design style"
- "深色模式" / "dark mode"

---

## 设计原则

### 核心理念

| 原则 | 说明 | 实践方法 |
|------|------|----------|
| **简洁至上** | 聊天界面应专注内容，减少视觉干扰 | 移除不必要的装饰，使用留白分隔内容 |
| **层次分明** | 重要信息突出，次要信息弱化 | 通过颜色、大小、位置建立视觉层级 |
| **主题适配** | 支持浅色和深色模式自动切换 | 使用 DynamicResource 引用系统颜色 |
| **可访问性** | 所有人都能使用 | 对比度 4.5:1+，触摸目标 44x44pt |

### 颜色系统（Fluent Theme）

使用 Avalonia 内置的系统颜色资源，自动适配深浅主题：

```xml
<!-- 主要颜色 -->
{DynamicResource SystemAccentColor}              <!-- 强调色（用户系统设置） -->
{DynamicResource SystemControlBackgroundAccentBrush}

<!-- 背景颜色 -->
{DynamicResource SystemControlBackgroundBaseLowBrush}      <!-- 低层级背景 -->
{DynamicResource SystemControlBackgroundBaseMediumBrush}   <!-- 中层级背景 -->
{DynamicResource SystemControlBackgroundBaseHighBrush}     <!-- 高层级背景 -->
{DynamicResource SystemControlBackgroundChromeMediumBrush} <!-- Chrome 背景 -->

<!-- 文字颜色 -->
{DynamicResource SystemControlForegroundBaseBrush}         <!-- 主要文字 -->
{DynamicResource SystemControlForegroundBaseMediumBrush}   <!-- 次要文字 -->
{DynamicResource SystemControlForegroundBaseLowBrush}      <!-- 辅助文字 -->

<!-- 边框颜色 -->
{DynamicResource SystemControlBorderBaseLowBrush}
{DynamicResource SystemControlBorderBaseMediumBrush}
```

---

## 黄金案例 vs 失败案例

### 1. 聊天气泡设计

#### ✅ 正确做法（使用 DynamicResource）

```xml
<!-- 发送消息 (右侧) -->
<Border Background="{DynamicResource SystemAccentColor}"
        CornerRadius="16,4,16,16"
        Padding="12,8"
        HorizontalAlignment="Right"
        MaxWidth="280">
  <StackPanel Spacing="4">
    <TextBlock Text="{Binding Content}" 
               TextWrapping="Wrap" 
               Foreground="White"/>
    <TextBlock Text="{Binding Timestamp}" 
               FontSize="10" 
               Foreground="#80FFFFFF"
               HorizontalAlignment="Right"/>
  </StackPanel>
</Border>

<!-- 接收消息 (左侧) -->
<Border Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}"
        CornerRadius="4,16,16,16"
        Padding="12,8"
        HorizontalAlignment="Left"
        MaxWidth="280">
  <StackPanel Spacing="4">
    <TextBlock Text="{Binding Content}" 
               TextWrapping="Wrap"
               Foreground="{DynamicResource SystemControlForegroundBaseBrush}"/>
    <TextBlock Text="{Binding Timestamp}" 
               FontSize="10" 
               Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"
               HorizontalAlignment="Right"/>
  </StackPanel>
</Border>
```

#### ❌ 错误做法

```xml
<!-- 不要这样做 -->
<Border Background="Gray" CornerRadius="0">
  <!-- 错误1: 发送和接收用相同颜色 -->
  <!-- 错误2: 直角边框，不友好 -->
  <!-- 错误3: 硬编码颜色，不支持深色模式 -->
</Border>
```

**对比总结：**

| 有效做法 | 无效做法 | 原因 |
|----------|----------|------|
| 使用 `DynamicResource` | 硬编码颜色如 `#10A37F` | 支持深色模式 |
| 发送/接收不同颜色 | 统一灰色 | 快速识别发送者 |
| 圆角 16px (一侧尖角) | 直角 0px | 视觉友好 |
| MaxWidth 限制 | 无宽度限制 | 长消息易读 |

---

### 2. 会话列表设计

#### ✅ 正确做法

```xml
<!-- 会话卡片 -->
<Border Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}"
        CornerRadius="12" 
        Padding="12"
        BorderBrush="{DynamicResource SystemControlBorderBaseLowBrush}"
        BorderThickness="1">
  <Grid ColumnDefinitions="Auto, *, Auto" 
        RowDefinitions="Auto, Auto">
    <!-- 头像 -->
    <Border Grid.RowSpan="2" 
            Width="48" Height="48" 
            CornerRadius="24" 
            Margin="0,0,12,0"
            Background="{DynamicResource SystemControlBackgroundBaseLowBrush}">
      <Image Source="{Binding Avatar}"/>
    </Border>
    
    <!-- 标题 -->
    <TextBlock Grid.Column="1" 
               Text="{Binding Title}" 
               FontWeight="Medium"
               Foreground="{DynamicResource SystemControlForegroundBaseBrush}"
               TextTrimming="CharacterEllipsis"/>
    
    <!-- 预览 -->
    <TextBlock Grid.Row="1" Grid.Column="1" 
               Text="{Binding Preview}"
               FontSize="12" 
               Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"
               TextTrimming="CharacterEllipsis"/>
    
    <!-- 时间和未读 -->
    <StackPanel Grid.Column="2" VerticalAlignment="Top">
      <TextBlock Text="{Binding Time}" 
                 FontSize="11"
                 Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"/>
      <Border Background="{DynamicResource SystemAccentColor}"
              CornerRadius="10" 
              Padding="6,2"
              IsVisible="{Binding HasUnread}">
        <TextBlock Text="{Binding UnreadCount}" 
                   FontSize="11" 
                   Foreground="White"/>
      </Border>
    </StackPanel>
  </Grid>
</Border>
```

#### ❌ 错误做法

```xml
<!-- 不要这样做 -->
<StackPanel Orientation="Horizontal">
  <!-- 错误1: 头像方形，不美观 -->
  <!-- 错误2: 没有预览，信息不足 -->
  <!-- 错误3: 没有未读标记，用户可能错过消息 -->
  <!-- 错误4: 背景硬编码 -->
</StackPanel>
```

---

### 3. 输入框设计

#### ✅ 正确做法

```xml
<Border Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}"
        CornerRadius="24" 
        Padding="16,10"
        BorderBrush="{DynamicResource SystemControlBorderBaseLowBrush}"
        BorderThickness="1">
  <Grid ColumnDefinitions="Auto, *, Auto">
    <!-- 附件按钮 -->
    <Button Background="Transparent" 
            BorderThickness="0" 
            Padding="8">
      <PathIcon Data="M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M13.5,16V19H10.5V16H8L12,12L16,16H13.5M13,9V3.5L18.5,9H13Z"
                Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"/>
    </Button>
    
    <!-- 输入框 -->
    <TextBox Grid.Column="1"
             Text="{Binding InputMessage}"
             Watermark="输入消息..."
             AcceptsReturn="True"
             TextWrapping="Wrap"
             MaxHeight="120"
             Background="Transparent"
             BorderThickness="0"
             Foreground="{DynamicResource SystemControlForegroundBaseBrush}"/>
    
    <!-- 发送按钮 -->
    <Button Grid.Column="2"
            Command="{Binding SendMessageCommand}"
            Background="{DynamicResource SystemAccentColor}"
            CornerRadius="20" 
            Padding="16,8">
      <TextBlock Text="发送" 
                 Foreground="White" 
                 FontWeight="Medium"/>
    </Button>
  </Grid>
</Border>
```

---

### 4. 空状态设计

#### ✅ 正确做法

```xml
<StackPanel VerticalAlignment="Center" Spacing="20">
  <!-- 图标 -->
  <PathIcon Width="64" Height="64"
            Data="M20,2H4A2,2 0 0,0 2,4V22L6,18H20A2,2 0 0,0 22,16V4A2,2 0 0,0 20,2M20,16H6L4,18V4H20V16Z"
            Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"/>
  
  <!-- 标题 -->
  <TextBlock Text="还没有会话" 
             FontSize="18" FontWeight="Medium"
             HorizontalAlignment="Center"
             Foreground="{DynamicResource SystemControlForegroundBaseBrush}"/>
  
  <!-- 说明 -->
  <TextBlock Text="点击下方按钮开始新的对话"
             HorizontalAlignment="Center"
             Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"/>
  
  <!-- 操作按钮 -->
  <Button Content="开始新对话"
          Command="{Binding NewConversationCommand}"
          Background="{DynamicResource SystemAccentColor}"
          Foreground="White"
          HorizontalAlignment="Center"
          Padding="24,12"
          CornerRadius="8"/>
</StackPanel>
```

---

## 主题适配示例

### App.axaml 配置

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Asakumo.Avalonia.App">
  
  <Application.Styles>
    <!-- Fluent Theme -->
    <FluentTheme />
  </Application.Styles>
  
  <Application.Resources>
    <!-- 自定义颜色（基于系统颜色） -->
    <Color x:Key="CustomBrandColor">{DynamicResource SystemAccentColor}</Color>
    
    <SolidColorBrush x:Key="BrandBrush" 
                     Color="{DynamicResource SystemAccentColor}"/>
    
    <SolidColorBrush x:Key="SurfaceBrush" 
                     Color="{DynamicResource SystemControlBackgroundChromeMediumColor}"/>
  </Application.Resources>
</Application>
```

### 深浅主题切换

```xml
<!-- 在 App.axaml 或 Theme 中定义 -->
<ResourceDictionary.ThemeDictionaries>
  <ResourceDictionary x:Key="Light">
    <SolidColorBrush x:Key="ChatBubbleUserBrush" 
                     Color="{DynamicResource SystemAccentColor}"/>
    <SolidColorBrush x:Key="ChatBubbleAiBrush" 
                     Color="#F3F4F6"/>
  </ResourceDictionary>
  
  <ResourceDictionary x:Key="Dark">
    <SolidColorBrush x:Key="ChatBubbleUserBrush" 
                     Color="{DynamicResource SystemAccentColor}"/>
    <SolidColorBrush x:Key="ChatBubbleAiBrush" 
                     Color="{DynamicResource SystemControlBackgroundChromeMediumColor}"/>
  </ResourceDictionary>
</ResourceDictionary.ThemeDictionaries>
```

---

## 组件样式库

### 1. 主按钮样式

```xml
<Style Selector="Button.primary">
  <Setter Property="Background" 
          Value="{DynamicResource SystemAccentColor}"/>
  <Setter Property="Foreground" Value="White"/>
  <Setter Property="FontWeight" Value="SemiBold"/>
  <Setter Property="CornerRadius" Value="8"/>
  <Setter Property="Padding" Value="24,12"/>
</Style>

<Style Selector="Button.primary:pointerover">
  <Setter Property="Opacity" Value="0.9"/>
</Style>
```

### 2. 卡片样式

```xml
<Style Selector="Border.card">
  <Setter Property="Background" 
          Value="{DynamicResource SystemControlBackgroundChromeMediumBrush}"/>
  <Setter Property="CornerRadius" Value="12"/>
  <Setter Property="Padding" Value="16"/>
  <Setter Property="BorderBrush" 
          Value="{DynamicResource SystemControlBorderBaseLowBrush}"/>
  <Setter Property="BorderThickness" Value="1"/>
</Style>
```

---

## 常见问题与解决方案

### 问题 1: 深色模式下颜色不对

**症状**: 硬编码颜色在深色模式下显示异常

**解决方案**:
```xml
<!-- ❌ 错误：硬编码 -->
<Border Background="#FFFFFF"/>

<!-- ✅ 正确：使用 DynamicResource -->
<Border Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}"/>
```

### 问题 2: XAML 编译错误难以定位

**症状**: 一个小的 XAML 错误导致大量编译错误

**解决方案**:
```xml
<!-- 使用 x:DataType 启用编译绑定，提前发现错误 -->
<UserControl x:Class="MyApp.Views.ChatView"
             xmlns:vm="using:MyApp.ViewModels"
             x:DataType="vm:ChatViewModel">
  <!-- 编译时会检查绑定路径 -->
  <TextBlock Text="{Binding Content}"/>
</UserControl>
```

### 问题 3: 布局性能问题

**症状**: 复杂界面卡顿

**解决方案**:
```xml
<!-- 错误: 深层嵌套 -->
<Grid>
  <Grid>
    <Grid>
      <StackPanel>
        <Border><!-- 内容 --></Border>
      </StackPanel>
    </Grid>
  </Grid>
</Grid>

<!-- 正确: 扁平布局 -->
<Grid RowDefinitions="Auto, *, Auto">
  <Header Grid.Row="0"/>
  <Content Grid.Row="1"/>
  <Footer Grid.Row="2"/>
</Grid>
```

---

## 推荐的 UI 库

| 库名 | 特点 | 适用场景 |
|------|------|----------|
| **Fluent Theme** | 系统原生风格 | 本项目使用 |
| **FluentAvalonia** | 现代化 Fluent Design | 更现代的视觉效果 |
| **Semi.Avalonia** | 抖音 Semi Design | 中文应用 |

---

## 设计检查清单

开发前确认：

- [ ] 是否使用了 `DynamicResource` 而非硬编码颜色？
- [ ] 是否启用了 `x:DataType` 编译绑定？
- [ ] 发送/接收消息是否有视觉区分？
- [ ] 聊天气泡是否设置了 MaxWidth？
- [ ] 是否有空状态设计？
- [ ] 输入框是否支持多行？
- [ ] 是否有加载/错误状态？
- [ ] 触摸目标是否 >= 44x44pt？
- [ ] 文字对比度是否 >= 4.5:1？
- [ ] 是否测试了深色模式？
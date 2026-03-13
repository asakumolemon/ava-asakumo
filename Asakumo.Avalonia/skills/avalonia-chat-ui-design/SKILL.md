---
name: avalonia-chat-ui-design
description: AI chat application UI design skill for Avalonia. Based on SukiUI design patterns and chat app best practices. Provides XAML templates, color systems, and component patterns for building professional chat interfaces.
---

# Avalonia AI 聊天应用 UI 设计 Skill

基于 SukiUI 设计模式和聊天应用最佳实践，为 Avalonia AI 聊天应用提供专业 UI 设计指导。

## When to Use

使用此 skill 当需要：
1. 设计 AI 聊天客户端界面
2. 创建聊天气泡、会话列表等组件
3. 选择合适的配色方案和设计风格
4. 优化现有聊天界面的用户体验
5. 解决 Avalonia UI 开发中的常见问题

## Trigger Keywords

- "聊天界面" / "chat UI"
- "会话列表" / "conversation list"
- "聊天气泡" / "chat bubble"
- "设计风格" / "design style"
- "配色方案" / "color scheme"

---

## 设计原则

### 核心理念

| 原则 | 说明 | 实践方法 |
|------|------|----------|
| **简洁至上** | 聊天界面应专注内容，减少视觉干扰 | 移除不必要的装饰，使用留白分隔内容 |
| **层次分明** | 重要信息突出，次要信息弱化 | 通过颜色、大小、位置建立视觉层级 |
| **一致性** | 全应用保持统一的设计语言 | 使用统一的组件、颜色、间距 |
| **可访问性** | 所有人都能使用 | 对比度 4.5:1+，触摸目标 44x44pt |

---

## 黄金案例 vs 失败案例

### 1. 聊天气泡设计

#### ✅ 正确做法

```xml
<!-- 发送消息 (右侧) -->
<Border Background="#10A37F"
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
    <TextBlock Text="{Binding Content}" TextWrapping="Wrap"/>
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
  <!-- 错误3: 没有最大宽度，长消息撑爆屏幕 -->
</Border>
```

**对比总结：**

| 有效做法 | 无效做法 | 原因 |
|----------|----------|------|
| 发送/接收不同颜色 | 统一灰色 | 无法快速识别发送者 |
| 圆角 16px (一侧尖角) | 直角 0px | 视觉不友好 |
| MaxWidth 限制 | 无宽度限制 | 长消息难以阅读 |
| 时间戳右对齐 | 时间戳左对齐 | 视觉层级混乱 |

---

### 2. 会话列表设计

#### ✅ 正确做法

```xml
<!-- 会话卡片 -->
<Border Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}"
        CornerRadius="12" Padding="12">
  <Grid ColumnDefinitions="Auto, *, Auto" RowDefinitions="Auto, Auto">
    <!-- 头像 -->
    <Border Grid.RowSpan="2" Width="48" Height="48" 
            CornerRadius="24" Margin="0,0,12,0">
      <Image Source="{Binding Avatar}"/>
    </Border>
    
    <!-- 标题和预览 -->
    <TextBlock Grid.Column="1" Text="{Binding Title}" 
               FontWeight="Medium" TextTrimming="CharacterEllipsis"/>
    <TextBlock Grid.Row="1" Grid.Column="1" 
               Text="{Binding Preview}"
               FontSize="12" 
               Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"
               TextTrimming="CharacterEllipsis"/>
    
    <!-- 时间和未读 -->
    <StackPanel Grid.Column="2" VerticalAlignment="Top">
      <TextBlock Text="{Binding Time}" FontSize="11"
                 Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"/>
      <Border Background="{DynamicResource SystemAccentColor}"
              CornerRadius="10" Padding="6,2"
              IsVisible="{Binding HasUnread}">
        <TextBlock Text="{Binding UnreadCount}" 
                   FontSize="11" Foreground="White"/>
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
</StackPanel>
```

**对比总结：**

| 有效做法 | 无效做法 | 原因 |
|----------|----------|------|
| 圆形头像 (48x48) | 方形头像 | 现代、友好 |
| 消息预览 | 仅标题 | 用户需要上下文 |
| 未读计数徽章 | 无未读提示 | 用户可能错过消息 |
| 时间戳显示 | 无时间信息 | 缺少时间上下文 |
| 卡片背景 | 纯文本列表 | 缺少视觉分隔 |

---

### 3. 输入框设计

#### ✅ 正确做法

```xml
<Border Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}"
        CornerRadius="24" Padding="16,10">
  <Grid ColumnDefinitions="Auto, *, Auto">
    <!-- 附件按钮 -->
    <Button Background="Transparent" BorderThickness="0" Padding="8">
      <PathIcon Data="M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M13.5,16V19H10.5V16H8L12,12L16,16H13.5M13,9V3.5L18.5,9H13Z"/>
    </Button>
    
    <!-- 输入框 -->
    <TextBox Grid.Column="1"
             Text="{Binding InputMessage}"
             Watermark="输入消息..."
             AcceptsReturn="True"
             TextWrapping="Wrap"
             MaxHeight="120"
             Background="Transparent"
             BorderThickness="0"/>
    
    <!-- 发送按钮 -->
    <Button Grid.Column="2"
            Command="{Binding SendMessageCommand}"
            Background="{DynamicResource SystemAccentColor}"
            CornerRadius="20" Padding="16,8">
      <TextBlock Text="发送" Foreground="White" FontWeight="Medium"/>
    </Button>
  </Grid>
</Border>
```

#### ❌ 错误做法

```xml
<!-- 不要这样做 -->
<StackPanel Orientation="Horizontal">
  <TextBox Width="200"/>
  <Button Content="发送"/>
  <!-- 错误1: 输入框太窄 -->
  <!-- 错误2: 按钮样式单调 -->
  <!-- 错误3: 没有附件功能 -->
</StackPanel>
```

**对比总结：**

| 有效做法 | 无效做法 | 原因 |
|----------|----------|------|
| 圆角容器 (24px) | 无圆角 | 现代感 |
| 多行支持 (MaxHeight) | 单行输入 | 长消息体验差 |
| 附件按钮 | 无附件入口 | 功能缺失 |
| 发送按钮醒目 | 发送按钮普通 | 主要操作应突出 |

---

### 4. 空状态设计

#### ✅ 正确做法

```xml
<StackPanel VerticalAlignment="Center" Spacing="20">
  <!-- 插图 -->
  <PathIcon Width="64" Height="64"
            Data="M20,2H4A2,2 0 0,0 2,4V22L6,18H20A2,2 0 0,0 22,16V4A2,2 0 0,0 20,2M20,16H6L4,18V4H20V16Z"
            Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"/>
  
  <!-- 标题 -->
  <TextBlock Text="还没有会话" 
             FontSize="18" FontWeight="Medium"
             HorizontalAlignment="Center"/>
  
  <!-- 说明 -->
  <TextBlock Text="点击下方按钮开始新的对话"
             HorizontalAlignment="Center"
             Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"/>
  
  <!-- 操作按钮 -->
  <Button Content="开始新对话"
          Command="{Binding NewConversationCommand}"
          Classes="primary"/>
</StackPanel>
```

#### ❌ 错误做法

```xml
<!-- 不要这样做 -->
<TextBlock Text="暂无数据"/>
<!-- 错误1: 只有文字，没有引导 -->
<!-- 错误2: 没有操作按钮 -->
<!-- 错误3: 没有视觉吸引力 -->
```

---

## 配色方案

### AI 聊天应用推荐配色

#### 方案 1: OpenAI 风格 (专业、科技)

```xml
<!-- App.axaml Resources -->
<Color x:Key="BrandPrimaryColor">#10A37F</Color>
<Color x:Key="BrandSecondaryColor">#0D8A6A</Color>
<Color x:Key="BrandAccentColor">#1A7F64</Color>

<SolidColorBrush x:Key="PrimaryBrush" Color="#10A37F"/>
<SolidColorBrush x:Key="PrimaryHoverBrush" Color="#0D8A6A"/>
<SolidColorBrush x:Key="PrimaryPressedBrush" Color="#1A7F64"/>

<!-- 文字颜色 -->
<SolidColorBrush x:Key="TextPrimaryBrush" Color="#1F2937"/>
<SolidColorBrush x:Key="TextSecondaryBrush" Color="#6B7280"/>
<SolidColorBrush x:Key="TextOnPrimaryBrush" Color="#FFFFFF"/>

<!-- 表面颜色 -->
<SolidColorBrush x:Key="SurfaceBrush" Color="#FFFFFF"/>
<SolidColorBrush x:Key="SurfaceVariantBrush" Color="#F3F4F6"/>
```

#### 方案 2: SukiUI 风格 (现代、优雅)

```xml
<!-- 浅色主题 -->
<Color x:Key="SukiLightColor">#FAFAFA</Color>
<Color x:Key="SukiAccentColor">#6200EE</Color>
<Color x:Key="SukiPrimaryColor">#3700B3</Color>

<!-- 深色主题 -->
<Color x:Key="SukiDarkColor">#1A1A1A</Color>
<Color x:Key="SukiDarkSurfaceColor">#2D2D2D</Color>
```

#### 方案 3: 渐变风格 (活力、现代)

```xml
<!-- 主渐变 -->
<LinearGradientBrush x:Key="BrandGradientBrush" 
                     StartPoint="0%,0%" EndPoint="100%,100%">
  <GradientStop Color="#667EEA" Offset="0"/>
  <GradientStop Color="#764BA2" Offset="1"/>
</LinearGradientBrush>

<!-- 按钮渐变 -->
<LinearGradientBrush x:Key="ButtonGradientBrush"
                     StartPoint="0%,50%" EndPoint="100%,50%">
  <GradientStop Color="#10A37F" Offset="0"/>
  <GradientStop Color="#0D8A6A" Offset="1"/>
</LinearGradientBrush>

<!-- 背景渐变 -->
<LinearGradientBrush x:Key="BackgroundGradientBrush"
                     StartPoint="0%,0%" EndPoint="100%,100%">
  <GradientStop Color="#1A1A2E" Offset="0"/>
  <GradientStop Color="#16213E" Offset="0.5"/>
  <GradientStop Color="#0F3460" Offset="1"/>
</LinearGradientBrush>
```

---

## 组件模板库

### 1. 主按钮样式

```xml
<Style Selector="Button.primary">
  <Setter Property="Background" Value="{StaticResource BrandGradientBrush}"/>
  <Setter Property="Foreground" Value="White"/>
  <Setter Property="FontWeight" Value="SemiBold"/>
  <Setter Property="CornerRadius" Value="25"/>
  <Setter Property="Padding" Value="24,14"/>
  <Setter Property="MinWidth" Value="180"/>
  <Setter Property="Transitions">
    <Transitions>
      <DoubleTransition Property="Opacity" Duration="0:0:0.15"/>
    </Transitions>
  </Setter>
</Style>
<Style Selector="Button.primary:pointerover">
  <Setter Property="Opacity" Value="0.9"/>
</Style>
<Style Selector="Button.primary:pressed">
  <Setter Property="Opacity" Value="0.8"/>
</Style>
```

### 2. 卡片样式

```xml
<Style Selector="Border.card">
  <Setter Property="Background" Value="{StaticResource SurfaceBrush}"/>
  <Setter Property="CornerRadius" Value="16"/>
  <Setter Property="Padding" Value="16"/>
  <Setter Property="BoxShadow" Value="0 4 20 0 #10000000"/>
</Style>
```

### 3. 玻璃效果

```xml
<Style Selector="Border.glass">
  <Setter Property="Background" Value="#20FFFFFF"/>
  <Setter Property="CornerRadius" Value="20"/>
  <Setter Property="BorderBrush" Value="#30FFFFFF"/>
  <Setter Property="BorderThickness" Value="1"/>
  <Setter Property="BoxShadow" Value="0 8 32 0 #20000000"/>
</Style>
```

---

## 常见问题与解决方案

### 问题 1: XAML 编译错误难以定位

**症状**: 一个小的 XAML 错误导致大量编译错误

**解决方案**:
```xml
<!-- 使用 x:DataType 启用编译绑定，提前发现错误 -->
<UserControl x:Class="MyApp.Views.ChatView"
             x:DataType="vm:ChatViewModel">
  <!-- 编译时会检查绑定路径 -->
  <TextBlock Text="{Binding Content}"/>
</UserControl>
```

### 问题 2: 布局性能问题

**症状**: 复杂界面卡顿

**解决方案**:
```xml
<!-- 错误: 深层嵌套 -->
<Grid>
  <Grid>
    <Grid>
      <StackPanel>
        <Border>
          <!-- 内容 -->
        </Border>
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

### 问题 3: 深色模式适配

**症状**: 深色模式下对比度不足

**解决方案**:
```xml
<!-- 使用 DynamicResource 自动适配 -->
<TextBlock Foreground="{DynamicResource SystemControlForegroundBaseBrush}"/>

<!-- 或定义深浅主题 -->
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.ThemeDictionaries>
      <ResourceDictionary x:Key="Light">
        <SolidColorBrush x:Key="CardBrush" Color="#FFFFFF"/>
      </ResourceDictionary>
      <ResourceDictionary x:Key="Dark">
        <SolidColorBrush x:Key="CardBrush" Color="#2D2D2D"/>
      </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

---

## 推荐的 UI 库

| 库名 | 特点 | 适用场景 |
|------|------|----------|
| **SukiUI** | 扁平设计，现代化 | 通用应用 |
| **FluentAvalonia** | 微软 Fluent Design | 桌面应用 |
| **Semi.Avalonia** | 抖音 Semi Design | 中文应用 |
| **Material.Avalonia** | Google Material Design | Android 风格 |

---

## 设计检查清单

开发前确认：

- [ ] 是否定义了统一的颜色系统？
- [ ] 发送/接收消息是否有视觉区分？
- [ ] 聊天气泡是否设置了 MaxWidth？
- [ ] 是否有空状态设计？
- [ ] 输入框是否支持多行？
- [ ] 是否有加载/错误状态？
- [ ] 触摸目标是否 >= 44x44pt？
- [ ] 文字对比度是否 >= 4.5:1？
- [ ] 是否测试了深色模式？
- [ ] 是否使用了 Compiled Bindings？

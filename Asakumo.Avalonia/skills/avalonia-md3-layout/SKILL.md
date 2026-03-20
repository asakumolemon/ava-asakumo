---
name: avalonia-md3-layout
description: |
  Avalonia UI 中的 Material Design 3 组件与布局指导。
  专注于组件结构、间距排版、圆角阴影、布局节奏和视觉层次。
  不包含主题色修改指导，仅提供组件与布局规范。
---

# Avalonia MD3 组件与布局指导

为 Avalonia UI 应用提供 Material Design 3 风格的组件设计、样式规范和布局指导。

## 核心理念

**组件是骨架，间距是呼吸，阴影是层次。**

- **组件结构优先**：先做对结构，再美化外观
- **间距定义节奏**：4dp 基准单位，保持一致的视觉韵律
- **阴影表达层级**：MD3 简化为 6 级 elevation，克制使用
- **Avalonia 原生实现**：使用 Avalonia 的样式系统、资源字典、模板机制

---

## 适用场景

| 场景 | 使用本 Skill |
|------|-------------|
| 创建新的 MD3 风格页面 | 获取组件选型、布局结构、间距规范 |
| 设计通用组件库 | 获取样式组织方式、资源字典结构、模板规范 |
| 重构现有 UI | 对比 MD3 规范，识别间距/阴影/层级问题 |
| 保持视觉一致性 | 检查组件组合方式、间距节奏、状态处理 |

**不适用场景：**
- 需要修改主题色方案（使用主题设计 Skill）
- 需要动画和过渡效果设计（使用动效设计 Skill）
- 需要图标和图形设计（使用视觉设计 Skill）

---

## 设计原则

### 1. 组件结构 > 视觉装饰

```
✅ 正确：先确定使用什么组件（Card、List、NavigationBar）
✅ 正确：先定义组件的层级关系（Grid、StackPanel、Border）
❌ 错误：一上来就定义颜色和渐变
```

### 2. 4dp 基准单位

所有间距、尺寸必须是 **4 的倍数**：
```
4dp   - 最小单位（图标与文字）
8dp   - 小组件内边距
12dp  - 中等间距（列表项内容）
16dp  - 标准内边距（卡片、容器）
24dp  - 大容器内边距、组件间距
32dp  - 页面级间距、分区
```

### 3. 阴影克制使用

MD3 将 elevation 简化为 6 级：
```
Level 0 - 背景、基础容器
Level 1 - 卡片、按钮悬停
Level 2 - 导航栏、对话框、菜单
Level 3 - FAB、悬浮元素
Level 4 - 高优先级悬浮
Level 5 - 模态弹窗
```

### 4. Avalonia 原生实现

```
✅ 使用 Styles 和 Classes 分离样式
✅ 使用 DynamicResource 引用主题资源
✅ 使用 ControlTemplate 定义组件结构
✅ 使用 BoxShadow 定义 elevation
```

---

## Avalonia 实现建议

### 1. 样式系统使用

**使用 Classes 属性，避免内联样式：**

```xml
<!-- ❌ 错误：内联样式 -->
<Border Background="#11182B" CornerRadius="24" Padding="16" Margin="16"/>

<!-- ✅ 正确：使用 Classes -->
<Border Classes="card" Margin="16"/>

<!-- 样式定义 -->
<Style Selector="Border.card">
    <Setter Property="Background" Value="{DynamicResource SurfaceBrush}" />
    <Setter Property="CornerRadius" Value="24" />
    <Setter Property="Padding" Value="16" />
</Style>
```

**选择器从通用到具体：**

```xml
<Style Selector="Button">
    <!-- 所有按钮的通用样式 -->
</Style>

<Style Selector="Button.primary">
    <!-- 主按钮样式 -->
</Style>

<Style Selector="Button.primary:pointerover">
    <!-- 悬停状态 -->
</Style>
```

### 2. 资源字典组织

**推荐的项目结构：**

```
Styles/
├── Themes/
│   ├── DarkTheme.axaml      # 深色主题颜色定义
│   └── LightTheme.axaml     # 浅色主题颜色定义
├── MD3/
│   ├── Elevation.axaml      # MD3 elevation tokens
│   ├── Spacing.axaml        # 间距规范
│   ├── Components.axaml     # 组件样式
│   └── Layout.axaml         # 布局规范
└── AppStyles.axaml          # 全局样式
```

**Elevation Tokens 定义：**

```xml
<Application.Resources>
    <ResourceDictionary>
        <!-- MD3 Elevation Level 1 -->
        <BoxShadow x:Key="Elevation1">
            <BoxShadow.Shadow>
                <DropShadow BlurRadius="4" Direction="270" 
                            ShadowDepth="1" Color="#1f000000"/>
                <DropShadow BlurRadius="8" Direction="270" 
                            ShadowDepth="2" Color="#1f000000"/>
            </BoxShadow.Shadow>
        </BoxShadow>
        
        <!-- MD3 Elevation Level 2 -->
        <BoxShadow x:Key="Elevation2">
            <BoxShadow.Shadow>
                <DropShadow BlurRadius="6" Direction="270" 
                            ShadowDepth="2" Color="#1f000000"/>
                <DropShadow BlurRadius="10" Direction="270" 
                            ShadowDepth="4" Color="#1f000000"/>
            </BoxShadow.Shadow>
        </BoxShadow>
        
        <!-- MD3 Elevation Level 3 -->
        <BoxShadow x:Key="Elevation3">
            <BoxShadow.Shadow>
                <DropShadow BlurRadius="8" Direction="270" 
                            ShadowDepth="3" Color="#1f000000"/>
                <DropShadow BlurRadius="14" Direction="270" 
                            ShadowDepth="6" Color="#1f000000"/>
            </BoxShadow.Shadow>
        </BoxShadow>
    </ResourceDictionary>
</Application.Resources>
```

### 3. 模板使用原则

**仅在需要改变视觉结构时使用 ControlTemplate：**

```xml
<!-- ✅ 仅颜色变化：使用 Style -->
<Style Selector="Button.primary">
    <Setter Property="Background" Value="{DynamicResource PrimaryBrush}" />
    <Setter Property="Foreground" Value="{DynamicResource OnPrimaryBrush}" />
</Style>

<!-- ✅ 需要不同结构：使用 ControlTemplate -->
<Style Selector="Button.pill">
    <Setter Property="Template">
        <ControlTemplate>
            <Border Background="{TemplateBinding Background}"
                    CornerRadius="999"
                    Padding="{TemplateBinding Padding}"
                    BoxShadow="{DynamicResource Elevation1}">
                <ContentPresenter Content="{TemplateBinding Content}" 
                                  Margin="{TemplateBinding Padding}"
                                  HorizontalAlignment="Center"
                                  VerticalAlignment="Center"/>
            </Border>
        </ControlTemplate>
    </Setter>
</Style>
```

**TemplateBinding 必须绑定的属性：**

```xml
<ControlTemplate TargetType="Button">
    <Border Background="{TemplateBinding Background}"
            BorderBrush="{TemplateBinding BorderBrush}"
            BorderThickness="{TemplateBinding BorderThickness}"
            CornerRadius="{TemplateBinding CornerRadius}"
            Padding="{TemplateBinding Padding}">
        <ContentPresenter Content="{TemplateBinding Content}"
                          ContentTemplate="{TemplateBinding ContentTemplate}"/>
    </Border>
</ControlTemplate>
```

---

## 常用组件指导

### 1. 顶部应用栏 (Top App Bar)

**MD3 规范：**
- 高度：64dp (Small Top App Bar)
- 左右边距：16dp
- 图标间距：16dp

**Avalonia 实现：**

```xml
<Border Height="64" Background="{DynamicResource SurfaceBrush}">
    <Grid ColumnDefinitions="Auto, *, Auto" Margin="16,0">
        <!-- Logo / 标题 -->
        <StackPanel Grid.Column="0" Orientation="Horizontal" Spacing="12">
            <Border Width="40" Height="40" CornerRadius="20"
                    Background="{DynamicResource PrimaryBrush}">
                <TextBlock Text="🌙" FontSize="20" 
                           HorizontalAlignment="Center" 
                           VerticalAlignment="Center"/>
            </Border>
            <TextBlock Text="Asakumo" FontSize="20" FontWeight="Medium"
                       VerticalAlignment="Center"/>
        </StackPanel>
        
        <!-- 右侧操作 -->
        <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="8">
            <Button Classes="icon" ToolTip.Tip="设置">
                <PathIcon Data="M19.14,12.94C19.16,12.78 19.16,12.61 19.16,12.44C19.16,12.27 19.16,12.11 19.14,11.94L21.41,10.17C21.61,10 21.66,9.71 21.53,9.49L19.39,5.78C19.26,5.56 18.98,5.48 18.74,5.59L16.06,6.67C15.51,6.24 14.92,5.89 14.29,5.61L13.88,2.74C13.84,2.49 13.63,2.31 13.38,2.31H9.09C8.84,2.31 8.63,2.49 8.59,2.74L8.18,5.61C7.55,5.88 6.96,6.23 6.41,6.66L3.73,5.58C3.49,5.47 3.21,5.55 3.08,5.77L0.94,9.48C0.81,9.7 0.86,9.99 1.06,10.16L3.33,11.93C3.31,12.09 3.3,12.26 3.3,12.43C3.3,12.6 3.31,12.77 3.33,12.93L1.06,14.7C0.86,14.87 0.81,15.16 0.94,15.38L3.08,19.09C3.21,19.31 3.49,19.39 3.73,19.28L6.41,18.2C6.96,18.63 7.55,18.98 8.18,19.25L8.59,22.12C8.63,22.37 8.84,22.55 9.09,22.55H13.38C13.63,22.55 13.84,22.37 13.88,22.12L14.29,19.25C14.92,18.97 15.51,18.62 16.06,18.19L18.74,19.27C18.98,19.38 19.26,19.3 19.39,19.08L21.53,15.37C21.66,15.15 21.61,14.86 21.41,14.69L19.14,12.94M11.43,15.54C9.87,15.54 8.6,14.27 8.6,12.71C8.6,11.15 9.87,9.88 11.43,9.88C12.99,9.88 14.26,11.15 14.26,12.71C14.26,14.27 12.99,15.54 11.43,15.54Z"
                          Width="24" Height="24"/>
            </Button>
        </StackPanel>
    </Grid>
</Border>
```

### 2. 底部导航栏 (Navigation Bar)

**MD3 规范：**
- 高度：80dp
- 图标间距：32dp (图标之间)
- 标签间距：4dp (图标与文字)
- 活动项：药丸形指示器

**Avalonia 实现：**

```xml
<Border Height="80" Background="{DynamicResource SurfaceBrush}"
        BoxShadow="{DynamicResource Elevation2}">
    <Grid ColumnDefinitions="*, *" Margin="16,0">
        <!-- 活动项 -->
        <Button Classes="nav-item-active">
            <StackPanel Spacing="4">
                <!-- 药丸形背景 -->
                <Border Background="{DynamicResource PrimaryBrush}"
                        CornerRadius="16" Width="64" Height="32">
                    <PathIcon Data="M10,20V6H14V20H10M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"
                              Width="20" Height="20"
                              Foreground="{DynamicResource OnPrimaryBrush}"/>
                </Border>
                <TextBlock Text="会话" FontSize="12"
                           Foreground="{DynamicResource PrimaryBrush}"
                           HorizontalAlignment="Center"/>
            </StackPanel>
        </Button>
        
        <!-- 非活动项 -->
        <Button Grid.Column="1" Classes="nav-item">
            <StackPanel Spacing="4">
                <PathIcon Data="M19.14,12.94C19.16,12.78 19.16,12.61 19.16,12.44C19.16,12.27 19.16,12.11 19.14,11.94L21.41,10.17C21.61,10 21.66,9.71 21.53,9.49L19.39,5.78C19.26,5.56 18.98,5.48 18.74,5.59L16.06,6.67C15.51,6.24 14.92,5.89 14.29,5.61L13.88,2.74C13.84,2.49 13.63,2.31 13.38,2.31H9.09C8.84,2.31 8.63,2.49 8.59,2.74L8.18,5.61C7.55,5.88 6.96,6.23 6.41,6.66L3.73,5.58C3.49,5.47 3.21,5.55 3.08,5.77L0.94,9.48C0.81,9.7 0.86,9.99 1.06,10.16L3.33,11.93C3.31,12.09 3.3,12.26 3.3,12.43C3.3,12.6 3.31,12.77 3.33,12.93L1.06,14.7C0.86,14.87 0.81,15.16 0.94,15.38L3.08,19.09C3.21,19.31 3.49,19.39 3.73,19.28L6.41,18.2C6.96,18.63 7.55,18.98 8.18,19.25L8.59,22.12C8.63,22.37 8.84,22.55 9.09,22.55H13.38C13.63,22.55 13.84,22.37 13.88,22.12L14.29,19.25C14.92,18.97 15.51,18.62 16.06,18.19L18.74,19.27C18.98,19.38 19.26,19.3 19.39,19.08L21.53,15.37C21.66,15.15 21.61,14.86 21.41,14.69L19.14,12.94Z"
                          Width="24" Height="24"
                          Foreground="{DynamicResource TextSecondaryBrush}"/>
                <TextBlock Text="设置" FontSize="12"
                           Foreground="{DynamicResource TextSecondaryBrush}"
                           HorizontalAlignment="Center"/>
            </StackPanel>
        </Button>
    </Grid>
</Border>
```

**样式定义：**

```xml
<Style Selector="Button.nav-item">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Padding" Value="16,8" />
    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="Background" Duration="0:0:0.15" />
        </Transitions>
    </Setter>
</Style>

<Style Selector="Button.nav-item:pointerover">
    <Setter Property="Background" Value="{DynamicResource SurfaceVariantBrush}" />
</Style>
```

### 3. 列表项 (List Item)

**MD3 规范：**
- 高度：72dp
- 左右内边距：16dp
- 头像与内容间距：16dp
- 内容行间距：4dp

**Avalonia 实现：**

```xml
<Grid Height="72" ColumnDefinitions="Auto, *, Auto" Margin="16,0">
    <!-- Avatar -->
    <Border Width="48" Height="48" CornerRadius="24"
            Background="{DynamicResource SurfaceVariantBrush}">
        <TextBlock Text="AI" FontSize="18" FontWeight="Medium"
                   Foreground="{DynamicResource OnSurfaceVariantBrush}"
                   HorizontalAlignment="Center" 
                   VerticalAlignment="Center"/>
    </Border>
    
    <!-- 内容 -->
    <StackPanel Grid.Column="1" Margin="16,0" VerticalAlignment="Center">
        <TextBlock Text="{Binding Title}" FontSize="16" FontWeight="Medium"
                   Foreground="{DynamicResource OnSurfaceBrush}"
                   TextTrimming="CharacterEllipsis"/>
        <TextBlock Text="{Binding Preview}" FontSize="14"
                   Foreground="{DynamicResource OnSurfaceVariantBrush}"
                   TextTrimming="CharacterEllipsis"
                   Margin="0,4,0,0"/>
    </StackPanel>
    
    <!-- 尾部信息 -->
    <StackPanel Grid.Column="2" HorizontalAlignment="Right" VerticalAlignment="Center" Spacing="4">
        <TextBlock Text="{Binding UpdatedAt, StringFormat='{}{0:HH:mm}'}" 
                   FontSize="12"
                   Foreground="{DynamicResource OnSurfaceVariantBrush}"/>
        <Border IsVisible="{Binding HasUnread}"
                Background="{DynamicResource PrimaryBrush}"
                CornerRadius="10" MinWidth="20" Height="20"
                Padding="6,0" HorizontalAlignment="Right">
            <TextBlock Text="{Binding UnreadCount}" FontSize="12" FontWeight="Medium"
                       Foreground="{DynamicResource OnPrimaryBrush}"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center"/>
        </Border>
    </StackPanel>
</Grid>
```

### 4. 卡片 (Card)

**MD3 规范：**
- 内边距：16dp
- 圆角：12dp (Filled Card) / 24dp (Elevated Card)
- Elevation: Level 0 (Filled/Outlined) / Level 1 (Elevated)

**Avalonia 实现：**

```xml
<!-- Filled Card -->
<Border Classes="card-filled" Padding="16">
    <StackPanel Spacing="8">
        <TextBlock Text="卡片标题" FontSize="16" FontWeight="Medium"
                   Foreground="{DynamicResource OnSurfaceBrush}"/>
        <TextBlock Text="卡片内容描述文字..." FontSize="14"
                   Foreground="{DynamicResource OnSurfaceVariantBrush}"/>
    </StackPanel>
</Border>

<!-- Elevated Card -->
<Border Classes="card-elevated" Padding="16">
    <!-- 内容同上 -->
</Border>

<!-- 样式定义 -->
<Style Selector="Border.card-filled">
    <Setter Property="Background" Value="{DynamicResource SurfaceVariantBrush}" />
    <Setter Property="CornerRadius" Value="12" />
</Style>

<Style Selector="Border.card-elevated">
    <Setter Property="Background" Value="{DynamicResource SurfaceBrush}" />
    <Setter Property="CornerRadius" Value="12" />
    <Setter Property="BoxShadow" Value="{DynamicResource Elevation1}" />
</Style>
```

### 5. 按钮 (Button)

**MD3 规范：**
- 高度：40dp (Filled Button)
- 内边距：16dp (左右)
- 圆角：20dp (Pill shape)
- 图标与文字间距：8dp

**Avalonia 实现：**

```xml
<!-- Filled Button -->
<Button Classes="btn-filled" Content="保存" />

<!-- Outlined Button -->
<Button Classes="btn-outlined" Content="取消" />

<!-- Text Button -->
<Button Classes="btn-text" Content="跳过" />

<!-- 样式定义 -->
<Style Selector="Button.btn-filled">
    <Setter Property="Background" Value="{DynamicResource PrimaryBrush}" />
    <Setter Property="Foreground" Value="{DynamicResource OnPrimaryBrush}" />
    <Setter Property="CornerRadius" Value="20" />
    <Setter Property="Padding" Value="24,12" />
    <Setter Property="Height" Value="40" />
    <Setter Property="FontWeight" Value="Medium" />
</Style>

<Style Selector="Button.btn-filled:pointerover">
    <Setter Property="Background" Value="{DynamicResource PrimaryHoverBrush}" />
</Style>

<Style Selector="Button.btn-outlined">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="{DynamicResource PrimaryBrush}" />
    <Setter Property="BorderBrush" Value="{DynamicResource OutlineBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="20" />
    <Setter Property="Padding" Value="24,12" />
    <Setter Property="Height" Value="40" />
    <Setter Property="FontWeight" Value="Medium" />
</Style>

<Style Selector="Button.btn-text">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="{DynamicResource PrimaryBrush}" />
    <Setter Property="CornerRadius" Value="20" />
    <Setter Property="Padding" Value="12,12" />
    <Setter Property="Height" Value="40" />
    <Setter Property="FontWeight" Value="Medium" />
</Style>
```

### 6. 输入框 (TextField)

**MD3 规范：**
- 高度：56dp (Filled TextField)
- 内边距：16dp
- 标签与输入间距：4dp
- 圆角：4dp (顶部)

**Avalonia 实现：**

```xml
<Border Classes="text-field-filled">
    <Grid RowDefinitions="Auto, *" Margin="16,0">
        <TextBlock Text="标签文字" FontSize="12"
                   Foreground="{DynamicResource OnSurfaceVariantBrush}"
                   Margin="16,12,16,4"/>
        <TextBox Grid.Row="1" 
                 Classes="md3-filled"
                 Watermark="请输入内容..."
                 Padding="16,8"/>
    </Grid>
</Border>

<!-- 样式定义 -->
<Style Selector="TextBox.md3-filled">
    <Setter Property="Background" Value="{DynamicResource SurfaceVariantBrush}" />
    <Setter Property="Foreground" Value="{DynamicResource OnSurfaceBrush}" />
    <Setter Property="CaretBrush" Value="{DynamicResource PrimaryBrush}" />
    <Setter Property="CornerRadius" Value="4,4,0,0" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="Padding" Value="16,12" />
    <Setter Property="FontSize" Value="16" />
</Style>

<Style Selector="TextBox.md3-filled:focus">
    <Setter Property="Background" Value="{DynamicResource SurfaceBrush}" />
    <Setter Property="BorderBrush" Value="{DynamicResource PrimaryBrush}" />
    <Setter Property="BorderThickness" Value="0,0,0,2" />
</Style>
```

---

## 布局与间距规范

### 1. 页面级布局

**标准页面结构：**

```xml
<Grid RowDefinitions="Auto, *, Auto">
    <!-- 顶部应用栏：64dp -->
    <Border Grid.Row="0" Height="64" Classes="top-app-bar">
        <!-- 内容 -->
    </Border>
    
    <!-- 主内容区：自适应 -->
    <ScrollViewer Grid.Row="1">
        <StackPanel Margin="16" Spacing="16">
            <!-- 内容组件 -->
        </StackPanel>
    </ScrollViewer>
    
    <!-- 底部导航栏：80dp -->
    <Border Grid.Row="2" Height="80" Classes="nav-bar">
        <!-- 内容 -->
    </Border>
</Grid>
```

### 2. 组件间距速查表

| 组件关系 | 间距值 | 说明 |
|----------|--------|------|
| 页面边缘到内容 | 16dp | 标准页边距 |
| 卡片之间 | 8dp | 列表中的卡片 |
| 按钮之间 | 12dp | 水平排列的按钮 |
| 图标与文字 | 8dp | 按钮/列表项内 |
| 标题与正文 | 4dp | 同组内容内 |
| 分区之间 | 24dp | 不同内容区块 |
| 输入框之间 | 16dp | 表单字段 |

### 3. 布局密度

| 密度 | 间距倍数 | 适用场景 |
|------|----------|----------|
| 舒适 (Comfortable) | 16dp | 桌面应用、平板 |
| 紧凑 (Compact) | 8-12dp | 手机应用、信息密集 |
| 宽松 (Spacious) | 24dp+ | 展示型页面、阅读模式 |

**在 Avalonia 中实现密度切换：**

```xml
<!-- 在 App.axaml 中定义密度资源 -->
<Application.Resources>
    <ResourceDictionary>
        <!-- 舒适密度 -->
        <sys:Double x:Key="DensitySpacingS">8</sys:Double>
        <sys:Double x:Key="DensitySpacingM">16</sys:Double>
        <sys:Double x:Key="DensitySpacingL">24</sys:Double>
        
        <!-- 紧凑密度 (通过主题切换) -->
        <ResourceDictionary x:Key="CompactDensity">
            <sys:Double x:Key="DensitySpacingS">4</sys:Double>
            <sys:Double x:Key="DensitySpacingM">12</sys:Double>
            <sys:Double x:Key="DensitySpacingL">16</sys:Double>
        </ResourceDictionary>
    </ResourceDictionary>
</Application.Resources>
```

---

## 样式组织方式

### 1. 文件结构

```
Asakumo.Avalonia/
├── App.axaml                    # 引用全局样式
├── App.axaml.cs                 # 应用入口
└── Styles/
    ├── Global.axaml             # 全局样式（字体、排版）
    ├── MD3/
    │   ├── Elevation.axaml      # Elevation tokens
    │   ├── Spacing.axaml        # 间距 tokens
    │   ├── Colors.axaml         # 颜色 tokens
    │   └── Components.axaml     # 组件样式
    └── Themes/
        ├── Dark.axaml           # 深色主题
        └── Light.axaml          # 浅色主题
```

### 2. App.axaml 引用方式

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Asakumo.Avalonia.App">
    <Application.Styles>
        <FluentTheme />
        <StyleInclude Source="avares://Asakumo.Avalonia/Styles/Global.axaml" />
        <StyleInclude Source="avares://Asakumo.Avalonia/Styles/MD3/Components.axaml" />
    </Application.Styles>
    
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="avares://Asakumo.Avalonia/Styles/MD3/Elevation.axaml" />
                <ResourceDictionary Source="avares://Asakumo.Avalonia/Styles/MD3/Spacing.axaml" />
            </ResourceDictionary.MergedDictionaries>
            
            <!-- 主题字典 -->
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Dark">
                    <ResourceDictionary.MergedDictionaries>
                        <ResourceDictionary Source="avares://Asakumo.Avalonia/Styles/Themes/Dark.axaml" />
                    </ResourceDictionary.MergedDictionaries>
                </ResourceDictionary>
                <ResourceDictionary x:Key="Light">
                    <ResourceDictionary.MergedDictionaries>
                        <ResourceDictionary Source="avares://Asakumo.Avalonia/Styles/Themes/Light.axaml" />
                    </ResourceDictionary.MergedDictionaries>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### 3. 样式命名规范

| 类型 | 命名方式 | 示例 |
|------|----------|------|
| 样式类 | kebab-case | `btn-filled`, `card-elevated` |
| 资源键 | PascalCase + Brush/Color | `PrimaryBrush`, `SurfaceColor` |
| 模板部件 | PART_ 前缀 | `PART_Background`, `PART_ContentPresenter` |

---

## 常见错误与避免方式

### 1. 间距错误

| ❌ 错误做法 | ✅ 正确做法 | 原因 |
|------------|------------|------|
| 使用 3dp、5dp 等非 4 倍数 | 所有间距使用 4 的倍数 | 保持视觉节奏一致 |
| 内边距和外边距混用 | 统一使用 Margin 或 Padding | 保持一致性，便于维护 |
| 页面级间距用 16dp | 页面级用 24dp+ | 区分组件间距和页面间距 |

### 2. 阴影滥用

| ❌ 错误做法 | ✅ 正确做法 | 原因 |
|------------|------------|------|
| 所有卡片都加阴影 | 仅 Elevated Card 用阴影 | MD3 减少阴影使用 |
| 使用过重的阴影 | 使用 MD3 Level 1-2 | 保持界面扁平 |
| 阴影颜色用纯黑 | 使用 #1f000000 (12% 黑) | 更柔和自然 |

### 3. 样式组织错误

| ❌ 错误做法 | ✅ 正确做法 | 原因 |
|------------|------------|------|
| 在视图中写内联样式 | 使用 Classes 分离样式 | 视图与样式分离 |
| 在通用样式中定义颜色 | 在主题文件中定义颜色 | 支持主题切换 |
| 使用硬编码颜色值 | 使用 DynamicResource | 响应主题变化 |

### 4. 选择器错误

| ❌ 错误做法 | ✅ 正确做法 | 原因 |
|------------|------------|------|
| `Selector="*"` 全局应用 | 明确指定控件类型 | 避免性能问题和意外覆盖 |
| 选择器缺少元素类型 | `Selector="Button.primary"` | 提高选择器性能 |
| 使用 WPF 式 TargetType | 使用 CSS 风格选择器 | Avalonia 不支持 TargetType |

### 5. 组件使用错误

| ❌ 错误做法 | ✅ 正确做法 | 原因 |
|------------|------------|------|
| 用 StackPanel 做复杂布局 | 使用 Grid | Grid 性能更好，布局更精确 |
| 所有按钮用相同高度 | 区分 Filled/Outlined/Text | MD3 有不同按钮类型 |
| 导航栏用 Fixed Tab | 用 Navigation Bar | MD3 有专门的 Navigation Bar 组件 |

### 6. 资源引用错误

| ❌ 错误做法 | ✅ 正确做法 | 原因 |
|------------|------------|------|
| 使用 StaticResource 引用主题色 | 使用 DynamicResource | StaticResource 不响应主题切换 |
| 在样式中直接引用颜色 | 引用画笔资源 | 画笔已封装颜色和透明度 |
| 资源键命名不一致 | 统一使用 PascalCase + 类型 | 提高可读性和可维护性 |

---

## 示例 Prompt / 示例用法

### 场景 1：创建新的 MD3 风格页面

```
请帮我创建一个 MD3 风格的设置页面，包含：
- 顶部应用栏（64dp 高度）
- 设置项列表（使用 List Item 组件）
- 底部无导航栏

要求：
- 使用 4dp 基准单位的间距
- 列表项高度 72dp
- 使用 DynamicResource 引用主题颜色
```

### 场景 2：设计通用按钮组件

```
请帮我设计一套 MD3 风格的按钮组件，包括：
- Filled Button
- Outlined Button
- Text Button

要求：
- 高度 40dp，圆角 20dp
- 包含 pointerover 状态
- 支持图标 + 文字组合
- 提供 Avalonia AXAML 实现代码
```

### 场景 3：重构现有 UI 为 MD3 风格

```
请帮我检查以下 XAML 代码，指出不符合 MD3 规范的地方：
- 间距是否使用 4dp 倍数
- 阴影使用是否合理
- 组件选型是否正确
- 样式组织是否符合最佳实践

[粘贴 XAML 代码]
```

### 场景 4：创建样式资源文件

```
请帮我创建一个 MD3 Elevation Tokens 资源文件，包括：
- Level 0 到 Level 5 的阴影定义
- 使用 DropShadow 实现
- 符合 MD3 规范的颜色和模糊值
- 可直接放入 App.axaml 使用
```

### 场景 5：布局密度调整

```
我的应用需要在桌面端使用舒适密度 (16dp)，在手机端使用紧凑密度 (8dp)。
请帮我设计一套密度切换方案，使用 Avalonia 的资源系统实现。
```

---

## 质量检查清单

在提交 UI 代码之前，检查以下项目：

**间距与布局：**
- [ ] 所有间距是 4 的倍数吗？
- [ ] 页面级间距使用 24dp 吗？
- [ ] 组件间距使用 16dp 吗？
- [ ] 使用了 Grid 而非深层嵌套的 StackPanel 吗？

**阴影与层级：**
- [ ] 阴影使用 MD3 Level 1-3 吗？
- [ ] 没有滥用阴影吗？
- [ ] 阴影颜色使用 #1f000000 吗？

**样式组织：**
- [ ] 使用了 Classes 而非内联样式吗？
- [ ] 使用了 DynamicResource 引用主题色吗？
- [ ] 样式文件按功能拆分了吗？
- [ ] 选择器包含元素类型吗？

**组件规范：**
- [ ] 顶部应用栏高度 64dp 吗？
- [ ] 底部导航栏高度 80dp 吗？
- [ ] 列表项高度 72dp 吗？
- [ ] 按钮高度 40dp 吗？

**可维护性：**
- [ ] 样式命名使用 kebab-case 吗？
- [ ] 资源键命名一致吗？
- [ ] 模板部件使用 PART_ 前缀吗？
- [ ] 代码在明/暗主题下都测试过吗？

---

## 快速参考表

### Elevation Tokens

| Level | 阴影定义 | 使用场景 |
|-------|----------|----------|
| Level 0 | 无阴影 | 背景、基础容器 |
| Level 1 | 1px + 2px, #1f000000 | 卡片、按钮悬停 |
| Level 2 | 2px + 4px, #1f000000 | 导航栏、对话框 |
| Level 3 | 3px + 6px, #1f000000 | FAB、悬浮元素 |

### 间距 Tokens

| Token | 值 | 使用场景 |
|-------|-----|----------|
| Spacing XS | 4dp | 图标与文字 |
| Spacing S | 8dp | 小组件内边距 |
| Spacing M | 16dp | 标准内边距 |
| Spacing L | 24dp | 大容器、页面间距 |
| Spacing XL | 32dp | 页面级分区 |

### 组件尺寸

| 组件 | 尺寸 |
|------|------|
| Top App Bar | 64dp 高 |
| Navigation Bar | 80dp 高 |
| List Item | 72dp 高 |
| Button | 40dp 高 |
| TextField | 56dp 高 |
| Card Padding | 16dp |

---

**记住：好的布局是看不见的，它让用户专注于内容，而不是样式本身。**

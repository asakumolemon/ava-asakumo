# Rani 组件规范

## 设计原则

- **冷色主导**：以深蓝、银蓝为主
- **低饱和**：避免鲜艳刺眼的颜色
- **深色背景**：以深色为主，营造夜空感
- **柔和边界**：使用半透明边框，避免硬边缘
- **轻发光**：关键元素带微弱光晕
- **大圆角**：圆角 16-24px，营造柔和感

## 按钮系统

### 主按钮 (Primary)
- 背景：主渐变 `linear-gradient(135deg, #B8C8FF 0%, #8FA8FF 45%, #A792E8 100%)`
- 文字：`#08101F`
- Hover：亮度提升 6%-8%
- 阴影：`0 10px 28px rgba(143,168,255,.28)`

```xml
<Style Selector="Button.primary">
    <Setter Property="Background" Value="#8FA8FF"/>
    <Setter Property="Foreground" Value="#08101F"/>
    <Setter Property="CornerRadius" Value="16"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="BoxShadow" Value="0 10px 28px rgba(143,168,255,.28)"/>
</Style>
```

### 次要按钮 (Secondary)
- 背景：`rgba(255,255,255,.04)` 或透明
- 边框：`rgba(184,200,255,.18)`
- 文字：`#F3F6FF`

```xml
<Style Selector="Button.secondary">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="Foreground" Value="#F3F6FF"/>
    <Setter Property="BorderBrush" Value="rgba(184,200,255,.18)"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="16"/>
</Style>
```

### 危险按钮 (Danger)
- 背景：`rgba(242,138,168,.14)`
- 文字：`#FFC7D5`
- 边框：`rgba(242,138,168,.24)`

## 导航栏 / 侧边栏

- 背景：`rgba(17,24,43,.88)` 带毛玻璃效果
- 边框：`rgba(184,200,255,.12)`
- 激活项背景：`rgba(143,168,255,.14)`
- 激活项文字：`#F3F6FF`
- 普通项文字：`#B8C1D9`
- 圆角：18-24px

## 卡片 / 面板

- 背景：`#11182B`
- 悬停背景：`#161F38`
- 描边：`rgba(184,200,255,.12)`
- 阴影：`0 12px 36px rgba(0,0,0,.35)`
- 圆角：20px

```xml
<Style Selector="Border.card">
    <Setter Property="Background" Value="#11182B"/>
    <Setter Property="BorderBrush" Value="rgba(184,200,255,.12)"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="20"/>
    <Setter Property="BoxShadow" Value="0 12px 36px rgba(0,0,0,.35)"/>
</Style>
```

## 输入框

- 背景：`rgba(255,255,255,.03)`
- 边框：`rgba(184,200,255,.14)`
- 文本：`#F3F6FF`
- placeholder：`#7E8AAC`
- focus 边框：`rgba(142,219,255,.42)`
- focus 光晕：`0 0 0 4px rgba(142,219,255,.08)`

```xml
<Style Selector="TextBox">
    <Setter Property="Background" Value="rgba(255,255,255,.03)"/>
    <Setter Property="Foreground" Value="#F3F6FF"/>
    <Setter Property="BorderBrush" Value="rgba(184,200,255,.14)"/>
    <Setter Property="CornerRadius" Value="12"/>
</Style>
```

## 对话气泡

### AI 气泡
- 背景：`rgba(142,219,255,.07)`
- 边框：`rgba(142,219,255,.18)`
- 文字：`#EAF3FF`

### 用户气泡
- 背景：`rgba(143,168,255,.12)`
- 边框：`rgba(143,168,255,.22)`
- 文字：`#F3F6FF`

### 系统提示气泡
- 背景：`rgba(167,146,232,.10)`
- 边框：`rgba(167,146,232,.22)`

## 标签 / Badge / Chip

### 默认标签
- 背景：`rgba(143,168,255,.10)`
- 边框：`rgba(143,168,255,.18)`
- 文字：`#DDE7FF`

### 魔法标签 (Violet)
- 背景：`rgba(167,146,232,.12)`
- 文字：`#E6DDFF`

### 信息标签 (Cyan)
- 背景：`rgba(142,219,255,.10)`
- 文字：`#DDF6FF`

```xml
<Style Selector="Border.chip">
    <Setter Property="Background" Value="rgba(143,168,255,.10)"/>
    <Setter Property="BorderBrush" Value="rgba(143,168,255,.18)"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="999"/>
    <Setter Property="Padding" Value="12,6"/>
</Style>
```

## 图标规范

- 默认：`#C8D2EA`
- 激活：`#B8C8FF`
- 特殊高亮：`#8EDBFF`

风格：细线条、古典魔法感，可融入月相、星芒、法阵圆环、王冠弧线、雪花结晶感

## 动效规范

- **关键词**：缓慢、浮动、雾化、月光闪烁、神秘但克制
- **Hover**：颜色轻微变亮 + 阴影轻增强
- **弹窗**：淡入 + 上浮 8px
- **加载**：星点呼吸 / 月光扫过
- **页面切换**：220ms-320ms
- **禁止**：强弹跳、快速旋转、高饱和闪烁霓虹、粒子爆炸特效

---
name: rani-theme-design
description: |
  Rani（菈妮）主题设计规范 - 冷月、魔法、神秘、高贵、苍蓝银辉风格。
  
  适用于 AI 助手、桌面应用、启动器、笔记软件等深色主题界面设计。
  
  关键词：冷月、幽蓝、银辉、魔法感、神秘、高贵、静谧、宿命感、深色主题、暗色模式、月之公主、艾尔登法环。
  
  触发场景：
  - 设计深色主题界面或暗色模式
  - 创建神秘、魔法感、高贵气质的 UI
  - 聊天应用、AI 助手界面设计
  - 需要冷色调配色方案
  - 设计启动页、对话页、设置页等界面
  - 按钮样式、输入框、卡片组件设计
---

# Rani 主题设计规范

基于《艾尔登法环》中"月之公主菈妮"气质的 UI 设计系统。

## 设计定位

**风格目标**：深夜天幕 + 冷蓝月光 + 星尘雾气 + 古典魔法银饰

**设计关键词转译**：
- 冷色主导、低饱和、深色背景
- 柔和边界、轻发光、大圆角
- 雾化渐变、古典神秘感、极少暖色、状态色克制

## 核心色彩

| 角色 | 名称 | 色值 |
|------|------|------|
| 背景 | Night Abyss | `#090C14` |
| 内容背景 | Moon Void | `#0D1220` |
| 卡片 | Deep Astral | `#11182B` |
| 主色 | Lunar Blue | `#8FA8FF` |
| 高光 | Moon Glow | `#B8C8FF` |
| 辅助 | Astral Violet | `#A792E8` |
| 强调 | Frost Cyan | `#8EDBFF` |

**主渐变**：`linear-gradient(135deg, #B8C8FF 0%, #8FA8FF 45%, #A792E8 100%)`

## 组件规范速查

| 组件 | 背景色 | 边框 | 圆角 |
|------|--------|------|------|
| 主按钮 | 主渐变 | 无 | 16px |
| 次要按钮 | 透明 | `rgba(184,200,255,.18)` | 16px |
| 卡片 | `#11182B` | `rgba(184,200,255,.12)` | 20px |
| 输入框 | `rgba(255,255,255,.03)` | `rgba(184,200,255,.14)` | 12px |
| AI 气泡 | `rgba(142,219,255,.07)` | `rgba(142,219,255,.18)` | 18px |
| 用户气泡 | `rgba(143,168,255,.12)` | `rgba(143,168,255,.22)` | 18px |

## 详细参考

- **完整色彩系统**：见 [references/color-system.md](references/color-system.md)
- **组件规范**：见 [references/components.md](references/components.md)

## Avalonia XAML 资源定义模板

```xml
<Application.Resources>
    <ResourceDictionary>
        <!-- 背景 -->
        <SolidColorBrush x:Key="AppBackgroundBrush" Color="#090C14"/>
        <SolidColorBrush x:Key="AppSurfaceBrush" Color="#0D1220"/>
        <SolidColorBrush x:Key="AppCardBrush" Color="#11182B"/>
        
        <!-- 品牌色 -->
        <SolidColorBrush x:Key="PrimaryBrush" Color="#8FA8FF"/>
        <SolidColorBrush x:Key="PrimaryHoverBrush" Color="#B8C8FF"/>
        <SolidColorBrush x:Key="PrimaryActiveBrush" Color="#6E84D8"/>
        
        <!-- 辅助色 -->
        <SolidColorBrush x:Key="AccentVioletBrush" Color="#A792E8"/>
        <SolidColorBrush x:Key="AccentCyanBrush" Color="#8EDBFF"/>
        
        <!-- 文字 -->
        <SolidColorBrush x:Key="AppTextPrimaryBrush" Color="#F3F6FF"/>
        <SolidColorBrush x:Key="AppTextSecondaryBrush" Color="#B8C1D9"/>
        
        <!-- 边框 -->
        <SolidColorBrush x:Key="AppBorderBrush" Color="#1E2A4A"/>
    </ResourceDictionary>
</Application.Resources>
```

## 动效原则

- 缓慢、浮动、雾化、月光闪烁
- Hover：颜色变亮 + 阴影增强
- 过渡时长：220ms-320ms
- 禁止：强弹跳、快速旋转、高饱和闪烁
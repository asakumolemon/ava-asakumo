# Rani 色彩系统

## 核心色板

### 背景色
| 名称 | 色值 | 用途 |
|------|------|------|
| Night Abyss | `#090C14` | 应用最外层背景 |
| Moon Void | `#0D1220` | 主内容区背景 |
| Deep Astral Blue | `#11182B` | 卡片层背景 |

### 品牌主色
| 名称 | 色值 | 用途 |
|------|------|------|
| Lunar Blue | `#8FA8FF` | 主题核心色，代表"冷月魔法" |
| Moon Glow | `#B8C8FF` | hover、高光边缘、激活态辉光 |
| Royal Moon | `#6E84D8` | 按钮按下态、选中背景 |

### 辅助魔法色
| 名称 | 色值 | 用途 |
|------|------|------|
| Astral Violet | `#A792E8` | 标签、辅助按钮、特殊模块 |
| Frost Cyan | `#8EDBFF` | 强调信息、AI 状态、焦点态 |
| Silver Mist | `#C8D2EA` | 图标、细描边、次级说明 |

### 中性色
| 名称 | 色值 | 用途 |
|------|------|------|
| Text Primary | `#F3F6FF` | 主要文字 |
| Text Secondary | `#B8C1D9` | 次要文字 |
| Text Tertiary | `#7E8AAC` | 辅助说明文字 |
| Surface 1 | `#11182B` | 卡片背景 |
| Surface 2 | `#161F38` | 悬停背景 |
| Surface 3 | `#1B2644` | 更高层级背景 |

### 边框色
| 名称 | 色值 | 用途 |
|------|------|------|
| Border Soft | `rgba(184,200,255,0.12)` | 普通边框 |
| Border Strong | `rgba(184,200,255,0.24)` | 强调边框 |

### 状态色
| 名称 | 色值 | 用途 |
|------|------|------|
| Success | `#6FD7B8` | 成功状态 |
| Warning | `#E8C27A` | 警告状态 |
| Error | `#F28AA8` | 错误状态 |
| Info | `#8EDBFF` | 信息状态 |

## 渐变系统

### 主渐变
```
linear-gradient(135deg, #B8C8FF 0%, #8FA8FF 45%, #A792E8 100%)
```
用途：主按钮、Logo 光晕、激活卡片描边

### 魔法渐变
```
linear-gradient(135deg, rgba(142,219,255,.9), rgba(143,168,255,.9), rgba(167,146,232,.85))
```
用途：AI 思考状态、加载动画

### 背景雾化渐变
```
radial-gradient(circle at 20% 20%, rgba(143,168,255,.16), transparent 28%),
radial-gradient(circle at 80% 10%, rgba(167,146,232,.12), transparent 24%),
radial-gradient(circle at 60% 80%, rgba(142,219,255,.08), transparent 28%)
```
用途：整页背景、启动页、Hero 区

## Avalonia XAML 色彩定义

```xml
<!-- 背景色 -->
<SolidColorBrush x:Key="NightAbyssBrush" Color="#090C14"/>
<SolidColorBrush x:Key="MoonVoidBrush" Color="#0D1220"/>
<SolidColorBrush x:Key="DeepAstralBrush" Color="#11182B"/>

<!-- 品牌色 -->
<SolidColorBrush x:Key="LunarBlueBrush" Color="#8FA8FF"/>
<SolidColorBrush x:Key="MoonGlowBrush" Color="#B8C8FF"/>
<SolidColorBrush x:Key="RoyalMoonBrush" Color="#6E84D8"/>

<!-- 辅助色 -->
<SolidColorBrush x:Key="AstralVioletBrush" Color="#A792E8"/>
<SolidColorBrush x:Key="FrostCyanBrush" Color="#8EDBFF"/>
<SolidColorBrush x:Key="SilverMistBrush" Color="#C8D2EA"/>

<!-- 文字色 -->
<SolidColorBrush x:Key="TextPrimaryBrush" Color="#F3F6FF"/>
<SolidColorBrush x:Key="TextSecondaryBrush" Color="#B8C1D9"/>
<SolidColorBrush x:Key="TextTertiaryBrush" Color="#7E8AAC"/>

<!-- 状态色 -->
<SolidColorBrush x:Key="SuccessBrush" Color="#6FD7B8"/>
<SolidColorBrush x:Key="WarningBrush" Color="#E8C27A"/>
<SolidColorBrush x:Key="ErrorBrush" Color="#F28AA8"/>
<SolidColorBrush x:Key="InfoBrush" Color="#8EDBFF"/>
```

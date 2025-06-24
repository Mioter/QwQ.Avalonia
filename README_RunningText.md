# RunningText 滚动文字控件

一个优雅的 Avalonia 滚动文字控件，支持多种滚动模式和方向，提供丰富的自定义选项。

## ✨ 特性

- 🎯 **多种滚动方向**：支持水平（左→右、右→左）和垂直（上→下、下→上）滚动
- 🔄 **灵活的滚动模式**：循环滚动和往返滚动两种模式
- 🎛️ **智能滚动行为**：自动、强制、暂停三种行为模式
- 📝 **占位符支持**：当文本为空时显示占位符文本
- 🎨 **丰富的样式**：支持所有标准控件样式属性
- ⚡ **高性能**：优化的动画性能和内存使用
- 🎪 **现代化设计**：优雅的视觉效果和交互反馈

## 🚀 快速开始

### 基本用法

```xml
<!-- 基础横向滚动 -->
<controls:RunningText Text="这是一个滚动文本示例" 
                     Height="32" 
                     Speed="100" />

<!-- 垂直滚动 -->
<controls:RunningText Text="垂直滚动文本" 
                     Direction="BottomToTop" 
                     Height="60" 
                     Speed="80" />
```

### 高级用法

```xml
<!-- 往返滚动模式 -->
<controls:RunningText Text="往返滚动文本" 
                     Mode="Bounce" 
                     Height="32" 
                     Speed="100" />

<!-- 强制滚动行为 -->
<controls:RunningText Text="短文本" 
                     Behavior="Always" 
                     Height="32" />

<!-- 带占位符 -->
<controls:RunningText Text="" 
                     PlaceholderText="请输入内容..." 
                     Width="200" 
                     Height="32" />
```

## 📋 属性说明

### 核心属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Text` | `string` | `""` | 要滚动的文本内容 |
| `PlaceholderText` | `string` | `""` | 占位符文本，Text为空时显示 |
| `Direction` | `RunningDirection` | `RightToLeft` | 滚动方向 |
| `Mode` | `RunningMode` | `Cycle` | 滚动模式 |
| `Behavior` | `RunningBehavior` | `Auto` | 滚动行为 |
| `Speed` | `double` | `120` | 滚动速度（像素/秒） |
| `Space` | `double` | `NaN` | 文本间距，NaN为自动计算 |

### 样式属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `TextAlignment` | `TextAlignment` | `Left` | 文本对齐方式 |
| `FontSize` | `double` | `14` | 字体大小 |
| `FontFamily` | `FontFamily` | `Segoe UI` | 字体族 |
| `FontWeight` | `FontWeight` | `Normal` | 字体粗细 |
| `FontStyle` | `FontStyle` | `Normal` | 字体样式 |
| `CornerRadius` | `CornerRadius` | `4` | 圆角半径 |

## 🎮 枚举类型

### RunningDirection（滚动方向）

```csharp
public enum RunningDirection
{
    RightToLeft,    // 从右往左
    LeftToRight,    // 从左往右
    BottomToTop,    // 从下往上
    TopToBottom     // 从上往下
}
```

### RunningMode（滚动模式）

```csharp
public enum RunningMode
{
    Cycle,          // 循环滚动：无缝衔接
    Bounce          // 往返滚动：有停顿效果
}
```

### RunningBehavior（滚动行为）

```csharp
public enum RunningBehavior
{
    Auto,           // 自动：文本超出时滚动，否则静态显示
    Always,         // 强制：无论文本是否超出都滚动
    Pause           // 暂停：不执行滚动动画
}
```

## 🎨 样式定制

### 基础样式

```xml
<Style Selector="controls|RunningText">
    <Setter Property="Padding" Value="8 4" />
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="CornerRadius" Value="4" />
    <Setter Property="FontSize" Value="14" />
</Style>
```

### 占位符样式

```xml
<Style Selector="controls|RunningText:placeholder">
    <Setter Property="Foreground" Value="{DynamicResource TextBlockTertiaryForeground}" />
    <Setter Property="Opacity" Value="0.6" />
    <Setter Property="FontStyle" Value="Italic" />
</Style>
```

### 交互状态

```xml
<!-- 悬停效果 -->
<Style Selector="controls|RunningText:pointerover">
    <Setter Property="Background" Value="{DynamicResource ControlBackgroundPointerOver}" />
    <Setter Property="BorderBrush" Value="{DynamicResource ControlBorderBrushPointerOver}" />
</Style>

<!-- 焦点效果 -->
<Style Selector="controls|RunningText:focus">
    <Setter Property="BorderBrush" Value="{DynamicResource FocusBorderBrush}" />
    <Setter Property="BorderThickness" Value="1" />
</Style>
```

## 💡 使用场景

### 1. 新闻滚动条
```xml
<controls:RunningText Text="最新消息：欢迎使用 RunningText 控件！" 
                     Height="32" 
                     Speed="80" 
                     Background="LightBlue" 
                     Padding="12 6" />
```

### 2. 状态提示
```xml
<controls:RunningText Text="系统正在处理中，请稍候..." 
                     Behavior="Always" 
                     Height="24" 
                     Speed="60" 
                     Foreground="Orange" />
```

### 3. 公告栏
```xml
<controls:RunningText Text="重要公告：系统将于今晚进行维护升级" 
                     Mode="Bounce" 
                     Height="40" 
                     FontWeight="Bold" 
                     Background="LightYellow" 
                     BorderBrush="Orange" 
                     BorderThickness="1" />
```

### 4. 垂直滚动标签
```xml
<controls:RunningText Text="垂直滚动标签" 
                     Direction="TopToBottom" 
                     Height="80" 
                     Width="32" 
                     TextAlignment="Center" />
```

## 🔧 性能优化

### 代码优化

- **事件管理**：优化了事件订阅和取消订阅逻辑
- **动画控制**：使用 CancellationToken 精确控制动画生命周期
- **内存管理**：及时清理动画资源，避免内存泄漏
- **延迟更新**：使用延迟更新机制避免频繁重绘

### 使用建议

1. **合理设置速度**：根据文本长度和用户体验需求调整滚动速度
2. **避免过度使用**：在同一个页面中不要使用过多的滚动文本
3. **响应式设计**：考虑在不同屏幕尺寸下的显示效果
4. **无障碍支持**：为重要的滚动文本提供静态替代方案

## 🐛 常见问题

### Q: 文本不滚动怎么办？
A: 检查以下几点：
- 确保 `Behavior` 不是 `Pause`
- 检查文本长度是否超过容器宽度/高度
- 验证 `Speed` 值是否合理

### Q: 如何让短文本也滚动？
A: 设置 `Behavior="Always"` 可以强制短文本滚动。

### Q: 如何自定义占位符样式？
A: 使用 `:placeholder` 伪类选择器：
```xml
<Style Selector="controls|RunningText:placeholder">
    <Setter Property="Foreground" Value="Gray" />
    <Setter Property="Opacity" Value="0.6" />
</Style>
```

### Q: 如何实现鼠标悬停暂停滚动？
A: 可以通过绑定 `Behavior` 属性来实现：
```xml
<controls:RunningText Behavior="{Binding IsHovered, Converter={x:Static BoolToBehaviorConverter.Instance}}" />
```

## 📝 更新日志

### v2.0.0 (当前版本)
- ✨ 重构代码结构，提高可维护性
- 🎨 优化样式系统，支持更多自定义选项
- ⚡ 提升性能和内存使用效率
- 📚 完善文档和示例
- 🐛 修复已知问题和边界情况

### v1.0.0
- 🎉 初始版本发布
- 支持基本的滚动功能
- 提供多种滚动模式和方向

## 🤝 贡献

欢迎提交 Issue 和 Pull Request 来改进这个控件！

## �� 许可证

MIT License 
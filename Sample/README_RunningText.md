# RunningText 控件使用说明

## 概述

`RunningText` 是一个用于显示滚动文本的 Avalonia 控件，支持四个方向的滚动动画。该控件特别适用于需要展示动态文本信息的场景，如新闻滚动、状态提示等。

## 主要特性

- **多方向滚动**: 支持从左到右、从右到左、从上到下、从下到上四个方向
- **智能滚动检测**: 自动检测文本是否需要滚动，文本能完整显示时静态显示
- **占位符功能**: 支持设置占位符文本，当主文本为空时显示
- **多种滚动模式**: 支持循环滚动和往返滚动两种模式
- **灵活行为控制**: 提供自动、强制、暂停三种滚动行为
- **文本对齐支持**: 支持水平和垂直方向的文本对齐
- **可调节速度**: 支持自定义滚动速度
- **灵活间距**: 可设置文本间距，支持自动计算
- **性能优化**: 当控件不可见时自动停止动画
- **样式支持**: 支持所有标准的 Control 样式属性

## 基本用法

### XAML 示例

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:controls="using:QwQ.Avalonia.Control">
    
    <!-- 基本用法 -->
    <controls:RunningText Text="这是一个滚动的文本示例"
                          Direction="RightToLeft"
                          Speed="120" />
    
    <!-- 带占位符的用法 -->
    <controls:RunningText Text=""
                          PlaceholderText="请输入文本内容..."
                          Direction="RightToLeft"
                          Speed="120" />
    
    <!-- 带样式的用法 -->
    <controls:RunningText Text="🎉 欢迎使用 RunningText 控件！🎊"
                          Direction="LeftToRight"
                          Speed="150"
                          Space="50"
                          Background="LightBlue"
                          Foreground="DarkBlue"
                          FontSize="16"
                          FontWeight="Bold"
                          Padding="10,5" />
    
    <!-- 垂直滚动 -->
    <controls:RunningText Text="这是第一行文本&#x0a;这是第二行文本&#x0a;这是第三行文本"
                          Direction="BottomToTop"
                          Speed="80"
                          Height="100" />
    
    <!-- 往返滚动模式 -->
    <controls:RunningText Text="往返滚动的文本内容"
                          Mode="Bounce"
                          Direction="RightToLeft"
                          Speed="100" />
    
    <!-- 自动行为控制 -->
    <controls:RunningText Text="短文本"
                          Behavior="Auto"
                          TextAlignment="Center" />
    
</Window>
```

### C# 代码示例

```csharp
using QwQ.Avalonia.Control;

// 创建控件
var runningText = new RunningText
{
    Text = "动态设置的文本内容",
    PlaceholderText = "请输入内容...",
    Direction = RunningDirection.RightToLeft,
    Mode = RunningMode.Cycle,
    Behavior = RunningBehavior.Auto,
    TextAlignment = TextAlignment.Center,
    Speed = 120,
    Space = 30
};

// 动态更改属性
runningText.Text = "新的文本内容";
runningText.Speed = 200;
runningText.Direction = RunningDirection.LeftToRight;
runningText.Mode = RunningMode.Bounce;
```

## 属性说明

### 核心属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Text` | `string` | `""` | 要滚动的文本内容 |
| `PlaceholderText` | `string` | `""` | 占位符文本，当Text为空时显示 |
| `Direction` | `RunningDirection` | `RightToLeft` | 滚动方向 |
| `Mode` | `RunningMode` | `Cycle` | 滚动模式 |
| `Behavior` | `RunningBehavior` | `Auto` | 滚动行为 |
| `TextAlignment` | `TextAlignment` | `Left` | 文本对齐方式 |
| `Speed` | `double` | `120` | 滚动速度（像素/秒） |
| `Space` | `double` | `double.NaN` | 文本间距，NaN表示自动计算 |

### 滚动方向枚举

```csharp
public enum RunningDirection
{
    /// <summary>
    /// 从右往左
    /// </summary>
    RightToLeft,
    
    /// <summary>
    /// 从左往右
    /// </summary>
    LeftToRight,
    
    /// <summary>
    /// 从下往上
    /// </summary>
    BottomToTop,
    
    /// <summary>
    /// 从上往下
    /// </summary>
    TopToBottom
}
```

### 滚动模式枚举

```csharp
public enum RunningMode
{
    /// <summary>
    /// 循环滚动：文本连续循环滚动
    /// </summary>
    Cycle,
    
    /// <summary>
    /// 往返滚动：文本在两端之间往返滚动，便于阅读
    /// </summary>
    Bounce
}
```

### 滚动行为枚举

```csharp
public enum RunningBehavior
{
    /// <summary>
    /// 自动选择：文本超出时滚动，否则静态显示
    /// </summary>
    Auto,
    
    /// <summary>
    /// 强制滚动：无论文本是否超出都滚动
    /// </summary>
    Always,
    
    /// <summary>
    /// 暂停滚动：不执行滚动动画
    /// </summary>
    Pause
}
```

### 样式属性

控件继承自 `TemplatedControl`，支持所有标准的样式属性：

- `Background` - 背景色
- `Foreground` - 前景色（文本颜色）
- `FontSize` - 字体大小
- `FontWeight` - 字体粗细
- `FontFamily` - 字体族
- `Padding` - 内边距
- `BorderBrush` - 边框颜色
- `BorderThickness` - 边框粗细
- `CornerRadius` - 圆角半径

## 新功能详解

### 1. 占位符功能

当 `Text` 属性为空时，控件会显示 `PlaceholderText` 作为占位符：

```xml
<controls:RunningText Text=""
                      PlaceholderText="请输入文本内容..."
                      Height="30" />
```

可以通过样式选择器自定义占位符样式：

```xml
<Style Selector="RunningText:placeholder">
    <Setter Property="Foreground" Value="Gray"/>
    <Setter Property="Opacity" Value="0.6"/>
    <Setter Property="FontStyle" Value="Italic"/>
</Style>
```

### 2. 智能滚动检测

控件会自动检测文本是否需要滚动：

- **水平方向文本**：当文本宽度超过控件宽度时滚动
- **垂直方向文本**：当文本高度超过控件高度时滚动

```xml
<!-- 短文本，自动静态显示 -->
<controls:RunningText Text="短文本" 
                      Behavior="Auto" 
                      TextAlignment="Center" />

<!-- 长文本，自动滚动 -->
<controls:RunningText Text="这是一个很长的文本内容..." 
                      Behavior="Auto" />
```

### 3. 滚动模式

#### Cycle 模式（循环滚动）
文本连续循环滚动，适合持续展示信息：

```xml
<controls:RunningText Text="循环滚动的文本内容"
                      Mode="Cycle"
                      Direction="RightToLeft"
                      Speed="100" />
```

#### Bounce 模式（往返滚动）
文本在两端之间往返滚动，在两端有短暂停留，便于阅读：

```xml
<controls:RunningText Text="往返滚动的文本内容"
                      Mode="Bounce"
                      Direction="RightToLeft"
                      Speed="100" />
```

### 4. 滚动行为控制

#### Auto 模式（自动）
智能判断是否需要滚动，文本超出时滚动，否则静态显示：

```xml
<controls:RunningText Text="短文本"
                      Behavior="Auto"
                      TextAlignment="Center" />
```

#### Always 模式（强制滚动）
无论文本是否超出都执行滚动动画：

```xml
<controls:RunningText Text="短文本"
                      Behavior="Always"
                      Direction="RightToLeft"
                      Speed="100" />
```

#### Pause 模式（暂停滚动）
不执行任何滚动动画，文本静态显示：

```xml
<controls:RunningText Text="长文本内容，但暂停滚动"
                      Behavior="Pause"
                      TextAlignment="Center" />
```

### 5. 文本对齐

`TextAlignment` 属性在文本无需滚动时生效：

#### 水平方向文本对齐
```xml
<!-- 左对齐 -->
<controls:RunningText Text="短文本" 
                      Behavior="Auto" 
                      TextAlignment="Left" />

<!-- 居中对齐 -->
<controls:RunningText Text="短文本" 
                      Behavior="Auto" 
                      TextAlignment="Center" />

<!-- 右对齐 -->
<controls:RunningText Text="短文本" 
                      Behavior="Auto" 
                      TextAlignment="Right" />
```

#### 垂直方向文本对齐
```xml
<controls:RunningText Text="垂直文本"
                      Direction="TopToBottom"
                      Behavior="Auto"
                      TextAlignment="Center"
                      Width="30"
                      Height="80" />
```

## 使用场景

### 1. 新闻滚动条

```xml
<controls:RunningText Text="最新消息：Avalonia UI 发布了新版本，带来了更多功能和性能改进！"
                      Direction="RightToLeft"
                      Mode="Cycle"
                      Speed="100"
                      Background="#FF6B6B"
                      Foreground="White"
                      FontWeight="Bold"
                      Height="30" />
```

### 2. 状态提示

```xml
<controls:RunningText Text="系统正在处理您的请求，请稍候..."
                      Direction="LeftToRight"
                      Mode="Bounce"
                      Speed="80"
                      Background="LightYellow"
                      Foreground="DarkOrange"
                      Padding="10,5" />
```

### 3. 输入框占位符

```xml
<controls:RunningText Text=""
                      PlaceholderText="请输入您的用户名..."
                      Behavior="Pause"
                      TextAlignment="Left"
                      Background="White"
                      BorderBrush="Gray"
                      BorderThickness="1"
                      Padding="10,5" />
```

### 4. 垂直滚动信息

```xml
<controls:RunningText Text="公告1：系统维护通知&#x0a;公告2：新功能上线&#x0a;公告3：用户反馈处理"
                      Direction="BottomToTop"
                      Mode="Cycle"
                      Speed="60"
                      Height="80"
                      Background="LightGray"
                      Padding="15,10" />
```

### 5. 智能显示

```xml
<!-- 根据文本长度自动选择是否滚动 -->
<controls:RunningText Text="{Binding StatusMessage}"
                      Behavior="Auto"
                      TextAlignment="Center"
                      Background="LightBlue"
                      Padding="10,5" />
```

## 性能优化

### 自动性能管理

- 当控件不可见时，动画会自动停止以节省性能
- 当属性发生变化时，动画会自动重新开始
- 智能滚动检测避免不必要的动画
- 使用高效的动画系统，确保流畅的滚动效果

### 最佳实践

1. **合理设置速度**: 建议速度范围在 50-300 像素/秒之间
2. **使用自动行为**: 大多数情况下使用 `Behavior = Auto` 即可
3. **控制文本长度**: 过长的文本可能影响性能，建议适当分段
4. **使用自动间距**: 大多数情况下使用 `Space = double.NaN` 即可
5. **避免频繁更新**: 避免频繁更改 Text 属性
6. **合理选择模式**: 根据使用场景选择合适的滚动模式

## 注意事项

1. **文本换行**: 垂直滚动时，文本会自动换行以适应控件宽度
2. **动画连续性**: 控件使用两个文本块实现无缝滚动效果
3. **响应式设计**: 控件会根据容器大小自动调整
4. **主题支持**: 完全支持 Avalonia 的主题系统
5. **占位符样式**: 占位符样式可通过 `:placeholder` 伪类自定义
6. **文本对齐**: 文本对齐仅在文本无需滚动时生效

## 故障排除

### 常见问题

1. **文本不滚动**
   - 检查控件是否可见
   - 确认文本内容不为空
   - 验证 `Behavior` 属性设置
   - 检查文本是否超出控件边界

2. **滚动方向不正确**
   - 检查 `Direction` 属性设置
   - 确认模板是否正确应用

3. **占位符不显示**
   - 确认 `Text` 属性为空
   - 检查 `PlaceholderText` 是否设置
   - 验证样式选择器是否正确

4. **性能问题**
   - 降低滚动速度
   - 缩短文本长度
   - 检查是否有多个控件同时运行
   - 使用 `Behavior = Auto` 避免不必要的滚动

## 更新日志

### v0.2.0
- 新增占位符功能
- 新增智能滚动检测
- 新增滚动模式（Cycle/Bounce）
- 新增滚动行为控制（Auto/Always/Pause）
- 新增文本对齐支持
- 优化性能和用户体验

### v0.1.6-beta2
- 初始版本发布
- 支持四个方向的滚动
- 支持自定义速度和间距
- 性能优化和自动管理

## 许可证

本项目采用 MIT 许可证，详见 LICENSE 文件。 
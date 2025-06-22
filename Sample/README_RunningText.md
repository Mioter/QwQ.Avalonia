# RunningText 控件使用说明

## 概述

`RunningText` 是一个用于显示滚动文本的 Avalonia 控件，支持四个方向的滚动动画。该控件特别适用于需要展示动态文本信息的场景，如新闻滚动、状态提示等。

## 主要特性

- **多方向滚动**: 支持从左到右、从右到左、从上到下、从下到上四个方向
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
    
</Window>
```

### C# 代码示例

```csharp
using QwQ.Avalonia.Control;

// 创建控件
var runningText = new RunningText
{
    Text = "动态设置的文本内容",
    Direction = RunningDirection.RightToLeft,
    Speed = 120,
    Space = 30
};

// 动态更改属性
runningText.Text = "新的文本内容";
runningText.Speed = 200;
runningText.Direction = RunningDirection.LeftToRight;
```

## 属性说明

### 核心属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Text` | `string` | `""` | 要滚动的文本内容 |
| `Direction` | `RunningDirection` | `RightToLeft` | 滚动方向 |
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

## 使用场景

### 1. 新闻滚动条

```xml
<controls:RunningText Text="最新消息：Avalonia UI 发布了新版本，带来了更多功能和性能改进！"
                      Direction="RightToLeft"
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
                      Speed="80"
                      Background="LightYellow"
                      Foreground="DarkOrange"
                      Padding="10,5" />
```

### 3. 垂直滚动信息

```xml
<controls:RunningText Text="公告1：系统维护通知&#x0a;公告2：新功能上线&#x0a;公告3：用户反馈处理"
                      Direction="BottomToTop"
                      Speed="60"
                      Height="80"
                      Background="LightGray"
                      Padding="15,10" />
```

## 性能优化

### 自动性能管理

- 当控件不可见时，动画会自动停止以节省性能
- 当属性发生变化时，动画会自动重新开始
- 使用高效的动画系统，确保流畅的滚动效果

### 最佳实践

1. **合理设置速度**: 建议速度范围在 50-300 像素/秒之间
2. **控制文本长度**: 过长的文本可能影响性能，建议适当分段
3. **使用自动间距**: 大多数情况下使用 `Space = double.NaN` 即可
4. **避免频繁更新**: 避免频繁更改 Text 属性

## 注意事项

1. **文本换行**: 垂直滚动时，文本会自动换行以适应控件宽度
2. **动画连续性**: 控件使用两个文本块实现无缝滚动效果
3. **响应式设计**: 控件会根据容器大小自动调整
4. **主题支持**: 完全支持 Avalonia 的主题系统

## 故障排除

### 常见问题

1. **文本不滚动**
   - 检查控件是否可见
   - 确认文本内容不为空
   - 验证速度设置是否合理

2. **滚动方向不正确**
   - 检查 `Direction` 属性设置
   - 确认模板是否正确应用

3. **性能问题**
   - 降低滚动速度
   - 缩短文本长度
   - 检查是否有多个控件同时运行

## 更新日志

### v0.1.6-beta2
- 初始版本发布
- 支持四个方向的滚动
- 支持自定义速度和间距
- 性能优化和自动管理

## 许可证

本项目采用 MIT 许可证，详见 LICENSE 文件。 
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace QwQ.Avalonia.Control;

/// <summary>
/// 条件切换控件（类似 if-else 逻辑结构），支持无限嵌套和属性继承
/// 设计要点：
/// 1. 通过继承机制实现多层条件嵌套时的属性穿透
/// 2. 优化绑定性能避免不必要的布局更新
/// 3. 支持复杂数据类型绑定（包括 MVVM 模式）
/// </summary>
public class SwitchControl : ContentControl
{
    //------------------------ 依赖属性定义 ------------------------//

    /// <summary>
    /// 条件值（双向绑定）
    /// 注意：当使用 MultiBinding 时，建议配合 OnlyWhenConverter 等特殊转换器
    /// </summary>
    public static readonly StyledProperty<bool> ConditionProperty =
        AvaloniaProperty.Register<SwitchControl, bool>(
            nameof(Condition),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// 条件为真时显示的内容（支持继承）
    /// 继承规则：当子 SwitchControl 未显式设置时，会继承父级的值
    /// </summary>
    public static readonly StyledProperty<object?> TrueContentProperty =
        AvaloniaProperty.Register<SwitchControl, object?>(
            nameof(TrueContent),
            inherits: true);

    /// <summary>
    /// 条件为假时显示的内容（支持继承）
    /// </summary>
    public static readonly StyledProperty<object?> FalseContentProperty =
        AvaloniaProperty.Register<SwitchControl, object?>(
            nameof(FalseContent),
            inherits: true);
    
    /// <summary>
    /// 页面切换动画效果
    /// </summary>
    public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
        AvaloniaProperty.Register<IndexControl, IPageTransition?>(nameof(PageTransition));

    private TransitioningContentControl? _transitioningContent;

    //------------------------ 构造函数 ------------------------//
    public SwitchControl()
    {
        // 只监听本地值变化（排除继承值的变化）
        ConditionProperty.Changed.AddClassHandler<SwitchControl>(
            (x, e) => x.OnConditionChanged(e));

        // 优化：仅在本地值变化时更新（避免继承值变化触发多次更新）
        TrueContentProperty.Changed.AddClassHandler<SwitchControl>(
            (x, e) => x.OnContentChanged(e));

        FalseContentProperty.Changed.AddClassHandler<SwitchControl>(
            (x, e) => x.OnContentChanged(e));
    }
    
    //------------------------ 模板应用 ------------------------//
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // 从模板中获取切换动画容器
        _transitioningContent = e.NameScope.Find<TransitioningContentControl>("PART_TransitioningContent");

        // 绑定页面切换动画属性
        _transitioningContent?.Bind(
            TransitioningContentControl.PageTransitionProperty,
            this.GetObservable(PageTransitionProperty)
        );

        UpdateContent();
    }

    //------------------------ 属性访问器 ------------------------//
    /// <summary>
    /// 获取或设置条件值
    /// </summary>
    public bool Condition
    {
        get => GetValue(ConditionProperty);
        set => SetValue(ConditionProperty, value);
    }

    /// <summary>
    /// 条件为真时显示的内容（优先使用本地值，其次继承值）
    /// </summary>
    public object? TrueContent
    {
        get => GetValue(TrueContentProperty);
        set => SetValue(TrueContentProperty, value);
    }

    /// <summary>
    /// 条件为假时显示的内容（优先使用本地值，其次继承值）
    /// </summary>
    public object? FalseContent
    {
        get => GetValue(FalseContentProperty);
        set => SetValue(FalseContentProperty, value);
    }
    
    /// <summary>
    /// 获取或设置页面切换动画
    /// </summary>
    public IPageTransition? PageTransition
    {
        get => GetValue(PageTransitionProperty);
        set => SetValue(PageTransitionProperty, value);
    }

    //------------------------ 核心逻辑 ------------------------//
    private void OnConditionChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // 仅当本地值变化时更新内容
        if (e.Priority == BindingPriority.LocalValue)
        {
            UpdateContent();
        }
    }

    private void OnContentChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // 当 TrueContent/FalseContent 本地值变化时更新
        if (e.Priority == BindingPriority.LocalValue)
        {
            UpdateContent();
        }
    }

    /// <summary>
    /// 更新显示内容（智能继承处理）
    /// </summary>
    private void UpdateContent()
    {
        // 通过 Avalonia 属性系统自动处理继承
        // GetCurrentValue 方法会按照优先级获取值（本地值 > 继承值）
        var trueContent = GetCurrentValue(TrueContentProperty);
        var falseContent = GetCurrentValue(FalseContentProperty);

        var newContent =  Condition ? trueContent : falseContent;
        
        // 设置当前内容（自动触发内容模板的更新）
        if (_transitioningContent != null)
        {
            _transitioningContent.Content = newContent; 
        }
        else
        {
            Content = newContent; // 直接更新。
        }
    }

    //------------------------ 辅助方法 ------------------------//
    /// <summary>
    /// 安全获取当前优先级的值（AOT 兼容）
    /// </summary>
    private object? GetCurrentValue(StyledProperty<object?> property)
    {
        // 直接访问属性系统，确保继承值正确传递
        return GetValue(property);
    }
}
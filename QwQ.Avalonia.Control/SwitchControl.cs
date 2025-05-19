using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace QwQ.Avalonia.Control;

/// <summary>
/// 条件切换控件（类似 if-else 逻辑结构），支持无限嵌套和属性继承
/// </summary>
public class SwitchControl : ContentControl
{
    //------------------------ 依赖属性定义 ------------------------//

    /// <summary>
    /// 条件值（双向绑定）
    /// 注意：当使用 MultiBinding 时，建议配合 OnlyWhenConverter 等特殊转换器
    /// </summary>
    public static readonly StyledProperty<bool> ConditionProperty = AvaloniaProperty.Register<
        SwitchControl,
        bool
    >(nameof(Condition), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// 条件为真时显示的内容（支持继承）
    /// 继承规则：当子 SwitchControl 未显式设置时，会继承父级的值
    /// </summary>
    public static readonly StyledProperty<object?> TrueContentProperty = AvaloniaProperty.Register<
        SwitchControl,
        object?
    >(nameof(TrueContent), inherits: true);

    /// <summary>
    /// 条件为假时显示的内容（支持继承）
    /// </summary>
    public static readonly StyledProperty<object?> FalseContentProperty = AvaloniaProperty.Register<
        SwitchControl,
        object?
    >(nameof(FalseContent), inherits: true);

    /// <summary>
    /// 页面切换动画效果
    /// </summary>
    public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
        AvaloniaProperty.Register<SwitchControl, IPageTransition?>(nameof(PageTransition));

    private TransitioningContentControl? _transitioningContent;

    //------------------------ 构造函数 ------------------------//
    public SwitchControl()
    {
        ConditionProperty.Changed.AddClassHandler<SwitchControl>((x, e) => x.UpdateContent());

        TrueContentProperty.Changed.AddClassHandler<SwitchControl>((x, e) => x.UpdateContent());

        FalseContentProperty.Changed.AddClassHandler<SwitchControl>((x, e) => x.UpdateContent());
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
    /// 页面切换动画
    /// </summary>
    public IPageTransition? PageTransition
    {
        get => GetValue(PageTransitionProperty);
        set => SetValue(PageTransitionProperty, value);
    }

    //------------------------ 模板应用 ------------------------//
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // 从模板中获取切换动画容器
        _transitioningContent = e.NameScope.Find<TransitioningContentControl>(
            "PART_TransitioningContent"
        );

        // 绑定页面切换动画属性
        _transitioningContent?.Bind(
            TransitioningContentControl.PageTransitionProperty,
            this.GetObservable(PageTransitionProperty)
        );

        UpdateContent();
    }

    //------------------------ 核心逻辑 ------------------------//

    /// <summary>
    /// 更新显示内容（智能继承处理）
    /// </summary>
    private void UpdateContent()
    {
        if (_transitioningContent == null)
        {
            // 直接更新内容（无动画）
            Content = Condition ? TrueContent : FalseContent;
            return;
        }

        // 通过 TransitioningContentControl 实现动画切换
        object? newContent = Condition ? TrueContent : FalseContent;

        // 仅当内容实际变化时才触发动画
        if (!Equals(_transitioningContent.Content, newContent))
        {
            _transitioningContent.Content = newContent;
        }
    }
}

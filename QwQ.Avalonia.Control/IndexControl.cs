using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace QwQ.Avalonia.Control;

/// <summary>
/// 支持按索引切换内容的控件，支持虚拟化和 AOT 编译
/// 通过 DataTemplate 延迟创建实际内容，避免不必要的实例化
/// </summary>
public class IndexControl : ItemsControl
{
    //------------------------ 依赖属性 ------------------------//

    /// <summary>
    /// 当前显示的内容索引（双向绑定）
    /// </summary>
    public static readonly StyledProperty<int> IndexProperty =
        AvaloniaProperty.Register<IndexControl, int>(
            nameof(Index),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// 当索引无效时显示的默认内容
    /// </summary>
    public static readonly StyledProperty<object?> DefaultContentProperty =
        AvaloniaProperty.Register<IndexControl, object?>(nameof(DefaultContent));

    /// <summary>
    /// 页面切换动画效果
    /// </summary>
    public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
        AvaloniaProperty.Register<IndexControl, IPageTransition?>(nameof(PageTransition));

    private TransitioningContentControl? _transitioningContent;

    //------------------------ 构造函数 ------------------------//
    public IndexControl()
    {
        // 监听索引变化事件
        IndexProperty.Changed.AddClassHandler<IndexControl>((x, _) => x.UpdateContent());
    }

    //------------------------ 属性访问器 ------------------------//
    /// <summary>
    /// 获取或设置当前显示的内容索引
    /// </summary>
    public int Index
    {
        get => GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    /// <summary>
    /// 获取或设置默认回退内容
    /// </summary>
    public object? DefaultContent
    {
        get => GetValue(DefaultContentProperty);
        set => SetValue(DefaultContentProperty, value);
    }

    /// <summary>
    /// 获取或设置页面切换动画
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
        _transitioningContent = e.NameScope.Find<TransitioningContentControl>("PART_TransitioningContent");

        // 绑定页面切换动画属性
        _transitioningContent?.Bind(
            TransitioningContentControl.PageTransitionProperty,
            this.GetObservable(PageTransitionProperty)
        );

        UpdateContent();
    }

    //------------------------ 核心逻辑 ------------------------//
    private void UpdateContent()
    {
        if (_transitioningContent == null) return;

        // 获取数据项集合（可使用 ItemsSource 绑定数据模型）
        var items = Items.Cast<object>().ToList();
        int targetIndex = Index;

        object? newContent;

        // 根据索引选择内容（通过 DataTemplate 延迟实例化）
        if (targetIndex >= 0 && targetIndex < items.Count)
        {
            newContent = items[targetIndex]; // 这里传递数据模型，由 DataTemplate 生成实际控件
        }
        else
        {
            newContent = DefaultContent ?? CreateDefaultFallback();
        }

        // 更新内容触发动画
        _transitioningContent.Content = newContent;
    }

    //------------------------ 辅助方法 ------------------------//
    /// <summary>
    /// 创建默认错误提示内容（AOT 兼容的静态实例）
    /// </summary>
    private static TextBlock CreateDefaultFallback() => new()
    {
        Text = "Invalid selection",
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Foreground = Brushes.Red,
    };
}
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using QwQ.Avalonia.Helper;

namespace QwQ.Avalonia.Control;

/// <summary>
/// 支持按名称选择内容的控件，提供类型安全的值匹配和切换动画
/// </summary>
public class ChoiceControl : ItemsControl
{
    //------------------------ 依赖属性定义 ------------------------//

    /// <summary>
    /// 定义 SelectName 附加属性（用于标记子项对应的选择键）
    /// </summary>
    public static readonly AttachedProperty<object?> SelectNameProperty =
        AvaloniaProperty.RegisterAttached<ChoiceControl, AvaloniaObject, object?>(
            "SelectName",
            defaultBindingMode: BindingMode.OneWay
        );

    /// <summary>
    /// 当前选中的键值（与 SelectName 匹配）
    /// </summary>
    public static readonly StyledProperty<object?> SelectedProperty = AvaloniaProperty.Register<
        ChoiceControl,
        object?
    >(nameof(Selected), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// 目标值类型（用于类型转换校验）
    /// </summary>
    public static readonly StyledProperty<Type?> TargetTypeProperty = AvaloniaProperty.Register<
        ChoiceControl,
        Type?
    >(nameof(TargetType));

    /// <summary>
    /// 当未找到匹配目标显示的默认内容
    /// </summary>
    public static readonly StyledProperty<object?> DefaultContentProperty =
        AvaloniaProperty.Register<ChoiceControl, object?>(nameof(DefaultContent));

    /// <summary>
    /// 页面切换动画效果
    /// </summary>
    public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
        AvaloniaProperty.Register<ChoiceControl, IPageTransition?>(nameof(PageTransition));

    private TransitioningContentControl? _transitioningContent;
    private readonly Dictionary<global::Avalonia.Controls.Control, IDisposable> _subscriptions =
        new();

    //------------------------ 构造函数 ------------------------//
    public ChoiceControl()
    {
        // 监听子项集合变化
        Items.CollectionChanged += OnItemsCollectionChanged;
        // 监听 Selected 属性变化
        this.GetObservable(SelectedProperty).Subscribe(_ => UpdateContent());
    }

    //------------------------ 属性访问器 ------------------------//
    /// <summary>
    /// 当前选中的键值
    /// </summary>
    public object? Selected
    {
        get => GetValue(SelectedProperty);
        set => SetValue(SelectedProperty, value);
    }

    /// <summary>
    /// 目标值类型（自动推断或手动指定）
    /// </summary>
    public Type? TargetType
    {
        get => GetValue(TargetTypeProperty);
        set => SetValue(TargetTypeProperty, value);
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
    /// 页面切换动画
    /// </summary>
    public IPageTransition? PageTransition
    {
        get => GetValue(PageTransitionProperty);
        set => SetValue(PageTransitionProperty, value);
    }

    //------------------------ 附加属性访问器 ------------------------//
    public static object? GetSelectName(global::Avalonia.Controls.Control obj) =>
        obj.GetValue(SelectNameProperty);

    public static void SetSelectName(global::Avalonia.Controls.Control obj, object? value) =>
        obj.SetValue(SelectNameProperty, value);

    //------------------------ 模板应用 ------------------------//
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // 获取切换动画容器
        _transitioningContent = e.NameScope.Find<TransitioningContentControl>(
            "PART_TransitioningContent"
        );

        // 绑定动画属性
        _transitioningContent?.Bind(
            TransitioningContentControl.PageTransitionProperty,
            this.GetObservable(PageTransitionProperty)
        );

        UpdateContent();
    }

    //------------------------ 核心逻辑 ------------------------//
    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        bool needsUpdate = false;

        // 处理新增项
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<global::Avalonia.Controls.Control>())
            {
                // 监听 SelectName 属性变化
                var sub = item.GetObservable(SelectNameProperty).Subscribe(_ => UpdateContent());
                _subscriptions[item] = sub;
            }
            needsUpdate = true;
        }

        // 处理移除项
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<global::Avalonia.Controls.Control>())
            {
                if (!_subscriptions.TryGetValue(item, out var sub))
                    continue;

                sub.Dispose();
                _subscriptions.Remove(item);
            }
            needsUpdate = true;
        }

        // 只在必要时更新内容
        if (needsUpdate)
        {
            UpdateContent();
        }
    }

    /// <summary>
    /// 更新显示内容（核心匹配逻辑）
    /// </summary>
    private void UpdateContent()
    {
        if (_transitioningContent == null)
            return;

        // 自动推断目标类型（如果未显式设置）
        var effectiveTargetType = TargetType ?? Selected?.GetType();

        // 转换目标值类型
        object? targetValue = SafeConvertValue(Selected, effectiveTargetType);

        // 遍历子项寻找匹配项
        foreach (var item in Items.OfType<global::Avalonia.Controls.Control>())
        {
            object? itemValue = SafeConvertValue(GetSelectName(item), effectiveTargetType);

            if (!IsValueMatch(targetValue, itemValue))
                continue;
            _transitioningContent.Content = item;
            return;
        }

        // 无匹配时提供默认内容
        _transitioningContent.Content = DefaultContent ?? CreateDefaultFallback();
    }

    //------------------------ 辅助方法 ------------------------//
    /// <summary>
    /// 安全类型转换方法（AOT 兼容）
    /// </summary>
    private static object? SafeConvertValue(object? value, Type? targetType)
    {
        if (targetType == null || value == null)
            return value;

        // 类型匹配直接返回
        if (targetType.IsInstanceOfType(value))
            return value;

        // 字符串转换处理
        if (value is not string strValue)
            return value; // 无法转换时返回原值

        // 枚举类型处理
        if (targetType.IsEnum && Enum.TryParse(targetType, strValue, true, out object? enumValue))
            return enumValue;

        // 基础类型处理
        if (targetType == typeof(int) && int.TryParse(strValue, out int intVal))
            return intVal;
        if (targetType == typeof(long) && long.TryParse(strValue, out long longVal))
            return longVal;
        if (targetType == typeof(float) && float.TryParse(strValue, out float floatVal))
            return floatVal;
        if (targetType == typeof(double) && double.TryParse(strValue, out double doubleVal))
            return doubleVal;
        if (targetType == typeof(decimal) && decimal.TryParse(strValue, out decimal decimalVal))
            return decimalVal;
        if (targetType == typeof(bool) && bool.TryParse(strValue, out bool boolVal))
            return boolVal;
        if (targetType == typeof(Guid) && Guid.TryParse(strValue, out Guid guidVal))
            return guidVal;
        if (targetType == typeof(DateTime) && DateTime.TryParse(strValue, out DateTime dateVal))
            return dateVal;

        return value; // 无法转换时返回原值
    }

    /// <summary>
    /// 值匹配逻辑（支持 null 比较）
    /// </summary>
    private static bool IsValueMatch(object? a, object? b)
    {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;
        return a.Equals(b);
    }

    /// <summary>
    /// 创建默认错误提示内容（AOT 兼容的静态实例）
    /// </summary>
    private static TextBlock CreateDefaultFallback() =>
        new()
        {
            Text = "Invalid selection",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.Red,
        };

    //------------------------ 清理资源 ------------------------//
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        // 清理所有订阅
        foreach (var sub in _subscriptions.Values)
        {
            sub.Dispose();
        }
        _subscriptions.Clear();

        base.OnUnloaded(e);
    }
}

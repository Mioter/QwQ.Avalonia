using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia.Media;

namespace QwQ.Avalonia.Control;

/// <summary>
/// 通用分组滚动控件，支持任意分组Key和动态导航栏。
/// </summary>
public class UniversalGroupScrollViewer : TemplatedControl
{
    // 分组信息
    public class GroupInfo(object key, string display, IReadOnlyList<object> items) : ObservableObject
    {
        public object Key { get; set; } = key;
        public string Display { get; set; } = display;
        public IReadOnlyList<object> Items { get; set; } = items;
        public bool IsCurrent
        {
            get;
            set => SetProperty(ref field, value);
        }
    }

    // 数据源
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<UniversalGroupScrollViewer, IEnumerable?>(nameof(ItemsSource));
    [Content]
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    // 分组Key选择器
    public static readonly StyledProperty<Func<object, object>?> GroupKeySelectorProperty =
        AvaloniaProperty.Register<UniversalGroupScrollViewer, Func<object, object>?>(nameof(GroupKeySelector));
    public Func<object, object>? GroupKeySelector
    {
        get => GetValue(GroupKeySelectorProperty);
        set => SetValue(GroupKeySelectorProperty, value);
    }

    // 分组Key显示文本选择器
    public static readonly StyledProperty<Func<object, string>?> GroupKeyDisplaySelectorProperty =
        AvaloniaProperty.Register<UniversalGroupScrollViewer, Func<object, string>?>(nameof(GroupKeyDisplaySelector));
    public Func<object, string>? GroupKeyDisplaySelector
    {
        get => GetValue(GroupKeyDisplaySelectorProperty);
        set => SetValue(GroupKeyDisplaySelectorProperty, value);
    }

    // 分组Key排序器
    public static readonly StyledProperty<IComparer<object>?> GroupKeyComparerProperty =
        AvaloniaProperty.Register<UniversalGroupScrollViewer, IComparer<object>?>(nameof(GroupKeyComparer));
    public IComparer<object>? GroupKeyComparer
    {
        get => GetValue(GroupKeyComparerProperty);
        set => SetValue(GroupKeyComparerProperty, value);
    }

    // 分组后的数据
    public static readonly StyledProperty<IReadOnlyList<GroupInfo>> GroupedItemsProperty =
        AvaloniaProperty.Register<UniversalGroupScrollViewer, IReadOnlyList<GroupInfo>>(nameof(GroupedItems), new List<GroupInfo>());
    public IReadOnlyList<GroupInfo> GroupedItems
    {
        get => GetValue(GroupedItemsProperty);
        private set => SetValue(GroupedItemsProperty, value);
    }

    // 导航栏分组Key集合
    public static readonly StyledProperty<IReadOnlyList<GroupInfo>> NavigationGroupsProperty =
        AvaloniaProperty.Register<UniversalGroupScrollViewer, IReadOnlyList<GroupInfo>>(nameof(NavigationGroups), new List<GroupInfo>());
    public IReadOnlyList<GroupInfo> NavigationGroups
    {
        get => GetValue(NavigationGroupsProperty);
        private set => SetValue(NavigationGroupsProperty, value);
    }

    // Item模板
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<UniversalGroupScrollViewer, IDataTemplate?>(nameof(ItemTemplate));
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    // 滚动到分组命令
    public ICommand ScrollToGroupCommand { get; }

    // StickyHeader相关属性
    public static readonly StyledProperty<bool> StickyHeaderIsVisibleProperty =
        AvaloniaProperty.Register<UniversalGroupScrollViewer, bool>(nameof(StickyHeaderIsVisible));
    public bool StickyHeaderIsVisible
    {
        get => GetValue(StickyHeaderIsVisibleProperty);
        set => SetValue(StickyHeaderIsVisibleProperty, value);
    }

    public static readonly StyledProperty<string> StickyHeaderTextProperty =
        AvaloniaProperty.Register<UniversalGroupScrollViewer, string>(nameof(StickyHeaderText));
    public string StickyHeaderText
    {
        get => GetValue(StickyHeaderTextProperty);
        set => SetValue(StickyHeaderTextProperty, value);
    }

    public static readonly StyledProperty<object?> CurrentGroupKeyProperty =
        AvaloniaProperty.Register<UniversalGroupScrollViewer, object?>(nameof(CurrentGroupKey));
    public object? CurrentGroupKey
    {
        get => GetValue(CurrentGroupKeyProperty);
        set => SetValue(CurrentGroupKeyProperty, value);
    }

    private ScrollViewer? _scrollViewer;
    private ItemsControl? _itemsControl;
    private Border? _stickyHeader;
    private TranslateTransform? _stickyHeaderTransform;

    static UniversalGroupScrollViewer()
    {
        ItemsSourceProperty.Changed.AddClassHandler<UniversalGroupScrollViewer>((x, e) => x.OnItemsSourceChanged(e));
        GroupKeySelectorProperty.Changed.AddClassHandler<UniversalGroupScrollViewer>((x, e) => x.OnGroupKeySelectorChanged(e));
        GroupKeyDisplaySelectorProperty.Changed.AddClassHandler<UniversalGroupScrollViewer>((x, e) => x.OnGroupKeyDisplaySelectorChanged(e));
        GroupKeyComparerProperty.Changed.AddClassHandler<UniversalGroupScrollViewer>((x, e) => x.OnGroupKeyComparerChanged(e));
    }

    public UniversalGroupScrollViewer()
    {
        ScrollToGroupCommand = new RelayCommand<object?>(ScrollToGroup);
    }

    private void OnItemsSourceChanged(AvaloniaPropertyChangedEventArgs e) => GroupItems();
    private void OnGroupKeySelectorChanged(AvaloniaPropertyChangedEventArgs e) => GroupItems();
    private void OnGroupKeyDisplaySelectorChanged(AvaloniaPropertyChangedEventArgs e) => GroupItems();
    private void OnGroupKeyComparerChanged(AvaloniaPropertyChangedEventArgs e) => GroupItems();

    /// <summary>
    /// 分组核心逻辑
    /// </summary>
    private void GroupItems()
    {
        if (ItemsSource == null || GroupKeySelector == null)
        {
            GroupedItems = new List<GroupInfo>();
            NavigationGroups = new List<GroupInfo>();
            return;
        }
        var items = ItemsSource.Cast<object>().ToList();
        var groups = items.GroupBy(GroupKeySelector)
            .Select(g => new GroupInfo(
                g.Key!,
                GroupKeyDisplaySelector?.Invoke(g.Key!) ?? g.Key?.ToString() ?? string.Empty,
                g.ToList()
            ));
        groups = GroupKeyComparer != null ? groups.OrderBy(g => g.Key, GroupKeyComparer) : groups.OrderBy(g => g.Display);
        var groupList = groups.ToList();
        GroupedItems = groupList;
        NavigationGroups = groupList;
    }

    /// <summary>
    /// 滚动到指定分组（需配合模板实现）
    /// </summary>
    private void ScrollToGroup(object? groupKey)
    {
        if (groupKey == null || _itemsControl == null || _scrollViewer == null) return;
        var group = GroupedItems.FirstOrDefault(g => Equals(g.Key, groupKey));
        if (group == null) return;
        if (_itemsControl.ContainerFromItem(group) is not Visual container) return;
        var point = container.TranslatePoint(default, _scrollViewer);
        if (point.HasValue)
        {
            double targetOffsetY = _scrollViewer.Offset.Y + point.Value.Y;
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, targetOffsetY);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _itemsControl = e.NameScope.Find<ItemsControl>("PART_ItemsControl");
        _stickyHeader = e.NameScope.Find<Border>("PART_StickyHeader");
        if (_stickyHeader != null)
        {
            _stickyHeaderTransform = new TranslateTransform();
            _stickyHeader.RenderTransform = _stickyHeaderTransform;
        }
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_scrollViewer == null || _itemsControl == null || _stickyHeader == null || _stickyHeaderTransform == null || !GroupedItems.Any())
        {
            if (_stickyHeader != null) _stickyHeader.IsVisible = false;
            return;
        }
        var containers = GroupedItems
            .Select(g => new { Group = g, Container = _itemsControl.ContainerFromItem(g) as Visual })
            .Where(x => x.Container != null)
            .Select(x => new
            {
                x.Group,
                Container = x.Container!,
                x.Container!.Bounds,
                Position = x.Container.TranslatePoint(default, _scrollViewer),
            })
            .Where(x => x.Position.HasValue)
            .ToList();
        var currentGroupInfo = containers
            .Where(x => x.Position!.Value.Y <= 0)
            .OrderByDescending(x => x.Position!.Value.Y)
            .FirstOrDefault();
        if (currentGroupInfo == null)
        {
            _stickyHeader.IsVisible = false;
            return;
        }
        _stickyHeader.IsVisible = true;
        StickyHeaderText = currentGroupInfo.Group.Display;
        CurrentGroupKey = currentGroupInfo.Group.Key;
        // 设置导航栏高亮
        foreach (var nav in NavigationGroups)
        {
            nav.IsCurrent = Equals(nav.Key, currentGroupInfo.Group.Key);
        }
        var nextGroupInfo = containers
            .Where(x => x.Position!.Value.Y > 0)
            .OrderBy(x => x.Position!.Value.Y)
            .FirstOrDefault();
        if (nextGroupInfo != null)
        {
            double nextHeaderTop = nextGroupInfo.Position!.Value.Y;
            double stickyHeaderHeight = _stickyHeader.Bounds.Height;
            if (nextHeaderTop < stickyHeaderHeight)
            {
                _stickyHeaderTransform.Y = nextHeaderTop - stickyHeaderHeight;
            }
            else
            {
                _stickyHeaderTransform.Y = 0;
            }
        }
        else
        {
            _stickyHeaderTransform.Y = 0;
        }
    }

    // 简单RelayCommand实现
    private class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => canExecute?.Invoke((T?)parameter) ?? true;
        public void Execute(object? parameter) => execute((T?)parameter);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
} 
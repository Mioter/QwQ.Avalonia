using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Controls.Primitives;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Metadata;
using Avalonia.Input;
using Avalonia.Media;
using System.Collections;
using System.Collections.Specialized;

namespace QwQ.Avalonia.Control;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

internal class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
{

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => execute((T?)parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class LetterGroup(char key, IReadOnlyList<object> items)
{
    public char Key { get; } = key;
    public IReadOnlyList<object> Items { get; } = items;
    public bool HasContent => Items.Count > 0;
    public bool IsSpecial { get; set; }
}

public class AlphabetLetterViewModel(char letter) : ObservableObject
{
    public char Letter { get; } = letter;

    public bool IsEnabled
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsCurrent
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsSpecial
    {
        get;
        set => SetProperty(ref field, value);
    }
}

public class AlphabeticalScrollViewer : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<AlphabeticalScrollViewer, IEnumerable?>(nameof(ItemsSource));

    [Content]
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly StyledProperty<Func<object, char>?> LetterSelectorProperty =
        AvaloniaProperty.Register<AlphabeticalScrollViewer, Func<object, char>?>(nameof(LetterSelector));

    public Func<object, char>? LetterSelector
    {
        get => GetValue(LetterSelectorProperty);
        set => SetValue(LetterSelectorProperty, value);
    }

    public static readonly StyledProperty<IReadOnlyList<LetterGroup>> GroupedItemsProperty =
        AvaloniaProperty.Register<AlphabeticalScrollViewer, IReadOnlyList<LetterGroup>>(nameof(GroupedItems),
            new List<LetterGroup>());

    public IReadOnlyList<LetterGroup> GroupedItems
    {
        get => GetValue(GroupedItemsProperty);
        private set => SetValue(GroupedItemsProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<AlphabeticalScrollViewer, IDataTemplate?>(nameof(ItemTemplate));

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly StyledProperty<bool> StickyHeaderIsVisibleProperty =
        AvaloniaProperty.Register<AlphabeticalScrollViewer, bool>(nameof(StickyHeaderIsVisible));

    public bool StickyHeaderIsVisible
    {
        get => GetValue(StickyHeaderIsVisibleProperty);
        set => SetValue(StickyHeaderIsVisibleProperty, value);
    }

    public static readonly StyledProperty<string> StickyHeaderTextProperty =
        AvaloniaProperty.Register<AlphabeticalScrollViewer, string>(nameof(StickyHeaderText));

    public string StickyHeaderText
    {
        get => GetValue(StickyHeaderTextProperty);
        set => SetValue(StickyHeaderTextProperty, value);
    }

    public static readonly StyledProperty<char> CurrentLetterProperty =
        AvaloniaProperty.Register<AlphabeticalScrollViewer, char>(nameof(CurrentLetter));

    public char CurrentLetter
    {
        get => GetValue(CurrentLetterProperty);
        set => SetValue(CurrentLetterProperty, value);
    }

    public static readonly StyledProperty<IReadOnlyList<AlphabetLetterViewModel>> AlphabetProperty =
        AvaloniaProperty.Register<AlphabeticalScrollViewer, IReadOnlyList<AlphabetLetterViewModel>>(nameof(Alphabet));

    public IReadOnlyList<AlphabetLetterViewModel> Alphabet
    {
        get => GetValue(AlphabetProperty);
        private set => SetValue(AlphabetProperty, value);
    }

    public static readonly StyledProperty<IEnumerable?> FavoritesProperty =
        AvaloniaProperty.Register<AlphabeticalScrollViewer, IEnumerable?>(nameof(Favorites));

    public IEnumerable? Favorites
    {
        get => GetValue(FavoritesProperty);
        set => SetValue(FavoritesProperty, value);
    }

    public ICommand ScrollToLetterCommand { get; }

    private ScrollViewer? _scrollViewer;
    private ItemsControl? _itemsControl;
    private InputElement? _stickyHeader;
    private TranslateTransform? _stickyHeaderTransform;
    private InputElement? _indexBar;
    private bool _isDraggingIndexBar;
    
    private readonly Dictionary<Type, Func<object, char>> _letterSelectors = new();

    static AlphabeticalScrollViewer()
    {
        CurrentLetterProperty.Changed.AddClassHandler<AlphabeticalScrollViewer>((x, e) => x.OnCurrentLetterChanged(e));
        ItemsSourceProperty.Changed.AddClassHandler<AlphabeticalScrollViewer>((x, e) => x.OnItemsSourceChanged(e));
        LetterSelectorProperty.Changed.AddClassHandler<AlphabeticalScrollViewer>((x, e) => x.OnLetterSelectorChanged(e));
        FavoritesProperty.Changed.AddClassHandler<AlphabeticalScrollViewer>((x, e) => x.OnFavoritesChanged(e));
    }

    private void OnFavoritesChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldList)
        {
            oldList.CollectionChanged -= OnFavoritesCollectionChanged;
        }
        if (e.NewValue is INotifyCollectionChanged newList)
        {
            newList.CollectionChanged += OnFavoritesCollectionChanged;
        }
        GroupItems();
    }

    private void OnFavoritesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        GroupItems();
    }

    private void OnLetterSelectorChanged(AvaloniaPropertyChangedEventArgs e)
    {
        GroupItems();
    }

    private void OnItemsSourceChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldList)
        {
            oldList.CollectionChanged -= OnItemsCollectionChanged;
        }
        if (e.NewValue is INotifyCollectionChanged newList)
        {
            newList.CollectionChanged += OnItemsCollectionChanged;
        }
        GroupItems();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        GroupItems();
    }

    private void OnCurrentLetterChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is char oldChar)
        {
            var oldVm = Alphabet.FirstOrDefault(vm => vm.Letter == oldChar);
            if (oldVm != null) oldVm.IsCurrent = false;
        }
        if (e.NewValue is char newChar)
        {
            var newVm = Alphabet.FirstOrDefault(vm => vm.Letter == newChar);
            if (newVm != null) newVm.IsCurrent = true;
        }
    }

    public AlphabeticalScrollViewer()
    {
        // 创建包含收藏按钮、A-Z字母和#按钮的完整字母导航栏
        var alphabetList = new List<AlphabetLetterViewModel>();
        
        // 添加收藏按钮
        alphabetList.Add(new AlphabetLetterViewModel('♥') { IsSpecial = true });
        
        // 添加A-Z字母
        alphabetList.AddRange(Enumerable.Range('A', 26).Select(i => new AlphabetLetterViewModel((char)i)));
        
        // 添加#按钮
        alphabetList.Add(new AlphabetLetterViewModel('#') { IsSpecial = true });
        
        Alphabet = alphabetList;
        ScrollToLetterCommand = new RelayCommand<char?>(ScrollToLetter);
    }

    private void UpdateAlphabetState()
    {
        var availableLetters = new HashSet<char>(GroupedItems.Select(g => g.Key));
        bool hasFavorites = Favorites != null && Favorites.Cast<object>().Any();
        
        foreach (var letterVm in Alphabet)
        {
            if (letterVm.IsSpecial)
            {
                // 特殊按钮的启用状态
                if (letterVm.Letter == '♥')
                {
                    letterVm.IsEnabled = hasFavorites;
                }
                else if (letterVm.Letter == '#')
                {
                    letterVm.IsEnabled = availableLetters.Contains('#');
                }
            }
            else
            {
                // 普通字母按钮
                letterVm.IsEnabled = availableLetters.Contains(letterVm.Letter);
            }
        }
    }

    private void ScrollToLetter(char? letter)
    {
        if (!letter.HasValue || _itemsControl == null || _scrollViewer == null) return;
        
        if (letter.Value == '♥')
        {
            // 滚动到收藏区域
            ScrollToFavorites();
            return;
        }
        
        var group = GroupedItems.FirstOrDefault(g => g.Key == letter.Value);
        if (group == null) return;

        if (_itemsControl.ContainerFromItem(group) is not Visual container) return;

        // 计算分组项相对于ScrollViewer的Y坐标
        var point = container.TranslatePoint(default, _scrollViewer);
        if (point.HasValue)
        {
            // 让分组项的顶部对齐到ScrollViewer的顶部
            double targetOffsetY = _scrollViewer.Offset.Y + point.Value.Y;
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, targetOffsetY);
        }
    }

    private void ScrollToFavorites()
    {
        // 滚动到列表顶部，因为收藏项会显示在最前面
        if (_scrollViewer != null)
        {
            _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, 0);
        }
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        GroupItems();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _itemsControl = e.NameScope.Find<ItemsControl>("PART_ItemsControl");
        _stickyHeader = e.NameScope.Find<InputElement>("PART_StickyHeader");
        _indexBar = e.NameScope.Find<InputElement>("PART_IndexBar");

        if (_stickyHeader is not null)
        {
            _stickyHeaderTransform = new TranslateTransform();
            _stickyHeader.RenderTransform = _stickyHeaderTransform;
        }

        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }
        
        if (_indexBar is not null)
        {
            _indexBar.PointerPressed += IndexBar_PointerPressed;
            _indexBar.PointerMoved += IndexBar_PointerMoved;
            _indexBar.PointerReleased += IndexBar_PointerReleased;
            _indexBar.PointerWheelChanged += IndexBar_PointerWheelChanged;
        }
    }

    private void IndexBar_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_scrollViewer is null) return;
        var newOffset = _scrollViewer.Offset + new Vector(0, -e.Delta.Y * 50);
        _scrollViewer.Offset = newOffset;
    }

    private void IndexBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDraggingIndexBar = true;
        e.Pointer.Capture(_indexBar);
        UpdateScrollFromPointer(e.GetPosition(_indexBar));
    }
    
    private void IndexBar_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDraggingIndexBar)
        {
            UpdateScrollFromPointer(e.GetPosition(_indexBar));
        }
    }

    private void IndexBar_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDraggingIndexBar = false;
        e.Pointer.Capture(null);
    }
    
    private void UpdateScrollFromPointer(Point p)
    {
        if (_indexBar is null) return;
        double percent = Math.Clamp(p.Y / _indexBar.Bounds.Height, 0, 1);
        int letterIndex = (int)(percent * Alphabet.Count);
        
        if (letterIndex >= 0 && letterIndex < Alphabet.Count)
        {
            var letter = Alphabet[letterIndex];
            if(letter.IsEnabled)
                ScrollToLetter(letter.Letter);
        }
    }
    
    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_scrollViewer is null || _itemsControl is null || _stickyHeader is null || _stickyHeaderTransform is null || !GroupedItems.Any())
        {
            if(_stickyHeader is not null) _stickyHeader.IsVisible = false;
            return;
        }

        var containers = GroupedItems
            .Select(g => new { Group = g, Container = _itemsControl.ContainerFromItem(g) as Visual })
            .Where(x => x.Container is not null)
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

        if (currentGroupInfo is null)
        {
            _stickyHeader.IsVisible = false;
            return;
        }

        _stickyHeader.IsVisible = true;
        StickyHeaderText = currentGroupInfo.Group.Key.ToString();
        CurrentLetter = currentGroupInfo.Group.Key;

        var nextGroupInfo = containers
            .Where(x => x.Position!.Value.Y > 0)
            .OrderBy(x => x.Position!.Value.Y)
            .FirstOrDefault();

        if (nextGroupInfo is not null)
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
    
    private char GetLetterFromItem(object item)
    {
        if (LetterSelector != null)
            return LetterSelector(item);
        return '#';
    }

    private void GroupItems()
    {
        if (ItemsSource is null || LetterSelector == null)
        {
            GroupedItems = new List<LetterGroup>();
            UpdateAlphabetState();
            return;
        }

        try
        {
            var controls = ItemsSource.Cast<object>().ToList();
            var groupedItems = new List<LetterGroup>();
            
            // 如果有收藏项，先添加收藏分组
            if (Favorites != null)
            {
                var favoritesList = Favorites.Cast<object>().ToList();
                if (favoritesList.Any())
                {
                    groupedItems.Add(new LetterGroup('♥', favoritesList) { IsSpecial = true });
                }
            }
            
            // 按字母分组其他项目
            var regularGroups = controls
                .GroupBy(GetLetterFromItem)
                .Select(g => new LetterGroup(g.Key, g.ToList()))
                .OrderBy(g => g.Key == '#' ? 'Z' + 1 : g.Key) // 让#分组排在所有字母之后
                .ToList();
            
            groupedItems.AddRange(regularGroups);
            
            GroupedItems = groupedItems;
        }
        catch (Exception)
        {
            GroupedItems = new List<LetterGroup>();
        }
        
        UpdateAlphabetState();
    }
}
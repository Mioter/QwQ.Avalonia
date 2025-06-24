using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace QwQ.Avalonia.Control;

/// <summary>
/// 滚动方向
/// </summary>
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
    TopToBottom,
}

/// <summary>
/// 滚动模式
/// </summary>
public enum RunningMode
{
    /// <summary>
    /// 循环滚动
    /// </summary>
    Cycle,

    /// <summary>
    /// 往返滚动
    /// </summary>
    Bounce
}

/// <summary>
/// 滚动行为
/// </summary>
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

/// <summary>
/// 滚动文字控件
/// <para>支持水平、垂直方向的文本滚动，提供循环和往返两种滚动模式</para>
/// </summary>
[TemplatePart("PART_Canvas", typeof(Canvas))]
[TemplatePart("PART_Txt1", typeof(TextBlock))]
[TemplatePart("PART_Txt2", typeof(TextBlock))]
public class RunningText : TemplatedControl
{
    #region StyledProperties

    /// <summary>
    /// 要滚动的文本内容
    /// </summary>
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<RunningText, string>(nameof(Text));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// 文本间距，NaN为自动计算
    /// </summary>
    public static readonly StyledProperty<double> SpaceProperty =
        AvaloniaProperty.Register<RunningText, double>(nameof(Space), double.NaN);

    public double Space
    {
        get => GetValue(SpaceProperty);
        set => SetValue(SpaceProperty, value);
    }

    /// <summary>
    /// 滚动速度（像素/秒）
    /// </summary>
    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<RunningText, double>(nameof(Speed), 120d);

    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    /// <summary>
    /// 滚动方向
    /// </summary>
    public static readonly StyledProperty<RunningDirection> DirectionProperty =
        AvaloniaProperty.Register<RunningText, RunningDirection>(nameof(Direction));

    public RunningDirection Direction
    {
        get => GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    /// <summary>
    /// 滚动模式
    /// </summary>
    public static readonly StyledProperty<RunningMode> ModeProperty =
        AvaloniaProperty.Register<RunningText, RunningMode>(nameof(Mode));

    public RunningMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    /// <summary>
    /// 文本对齐方式
    /// </summary>
    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
        AvaloniaProperty.Register<RunningText, TextAlignment>(nameof(TextAlignment));

    public TextAlignment TextAlignment
    {
        get => GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    /// <summary>
    /// 滚动行为
    /// </summary>
    public static readonly StyledProperty<RunningBehavior> BehaviorProperty =
        AvaloniaProperty.Register<RunningText, RunningBehavior>(nameof(Behavior));

    public RunningBehavior Behavior
    {
        get => GetValue(BehaviorProperty);
        set => SetValue(BehaviorProperty, value);
    }

    /// <summary>
    /// 占位符文本，当Text为空时显示
    /// </summary>
    public static readonly StyledProperty<string> PlaceholderTextProperty =
        AvaloniaProperty.Register<RunningText, string>(nameof(PlaceholderText), string.Empty);

    public string PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    #endregion

    static RunningText()
    {
        AffectsMeasure<RunningText>(TextProperty, SpaceProperty, SpeedProperty, DirectionProperty, PlaceholderTextProperty, ModeProperty, TextAlignmentProperty, BehaviorProperty);
        IsVisibleProperty.Changed.AddClassHandler<RunningText>((x, _) => x.BeginUpdate());
    }

    #region Methods

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // 清理旧的事件订阅
        UnsubscribeFromEvents();

        // 获取模板部件
        _canvas = e.NameScope.Find<Canvas>("PART_Canvas");
        _txt1 = e.NameScope.Find<TextBlock>("PART_Txt1");
        _txt2 = e.NameScope.Find<TextBlock>("PART_Txt2");

        // 验证模板部件
        if (!ValidateTemplateParts())
            return;

        // 订阅事件
        SubscribeToEvents();
        
        // 开始更新
        BeginUpdate();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        // 检查是否需要更新
        if (IsPropertyAffectingUpdate(change.Property))
        {
            BeginUpdate();
        }
    }
        
    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (_ignoreSizeChanges)
            return;
        
        BeginUpdate();
    }

    private CancellationTokenSource? _delayUpdateCts;
    
    /// <summary>
    /// 延迟更新，避免多个事件同时触发时重复执行
    /// </summary>
    private void BeginUpdate()
    {
        _delayUpdateCts?.Cancel();
        _delayUpdateCts = new CancellationTokenSource();
        var token = _delayUpdateCts.Token;
        
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!token.IsCancellationRequested)
                Update();
        }, DispatcherPriority.Loaded);
    }

    private CancellationTokenSource? _animationCts;
    
    /// <summary>
    /// 执行更新逻辑
    /// </summary>
    private void Update()
    {
        // 停止当前动画
        StopCurrentAnimation();
        
        // 重置标志
        _ignoreSizeChanges = false;

        // 验证控件状态
        if (!IsValidForUpdate())
            return;
        
        // 重置文本块属性
        ResetTextBlockProperties();

        // 处理空文本情况
        if (string.IsNullOrEmpty(Text))
        {
            HandleEmptyText();
            return;
        }

        // 设置文本内容
        SetTextContent();

        // 判断是否需要滚动
        bool willScroll = ShouldScroll();

        if (!willScroll)
        {
            HandleStaticDisplay();
            return;
        }
        
        // 执行滚动动画
        ExecuteScrollAnimation();
    }

    /// <summary>
    /// 处理空文本情况
    /// </summary>
    private void HandleEmptyText()
    {
        if (!string.IsNullOrEmpty(PlaceholderText))
        {
            _txt1!.Text = PlaceholderText;
            PseudoClasses.Add(":placeholder");
        }
        else
        {
            _txt1!.Text = string.Empty;
            PseudoClasses.Remove(":placeholder");
        }

        _txt2!.Text = string.Empty;
        _txt2.IsVisible = false;

        // 重置位置
        ResetTextPosition();
    }

    /// <summary>
    /// 设置文本内容
    /// </summary>
    private void SetTextContent()
    {
        _txt1!.Text = Text;
        _txt2!.Text = Text;
        PseudoClasses.Remove(":placeholder");
    }

    /// <summary>
    /// 判断是否需要滚动
    /// </summary>
    private bool ShouldScroll()
    {
        bool shouldScroll = IsHorizontalDirection() 
            ? _txt1!.DesiredSize.Width > _canvas!.Bounds.Width
            : _txt1!.DesiredSize.Height > _canvas!.Bounds.Height;

        return Behavior switch
        {
            RunningBehavior.Auto => shouldScroll,
            RunningBehavior.Always => true,
            RunningBehavior.Pause => false,
            _ => shouldScroll
        };
    }

    /// <summary>
    /// 处理静态显示
    /// </summary>
    private void HandleStaticDisplay()
    {
        StopCurrentAnimation();
        _txt2!.IsVisible = false;

        if (IsHorizontalDirection())
        {
            HandleHorizontalStaticDisplay();
        }
        else
        {
            HandleVerticalStaticDisplay();
        }
    }

    /// <summary>
    /// 处理水平方向静态显示
    /// </summary>
    private void HandleHorizontalStaticDisplay()
    {
        if (_txt1!.DesiredSize.Width < _canvas!.Bounds.Width && _txt1.Bounds.Width > 0)
        {
            _ignoreSizeChanges = true;
            _txt1.Width = _canvas.Bounds.Width;
            _txt1.TextAlignment = TextAlignment;
        }
        
        // 垂直居中
        double top = (_canvas.Bounds.Height - _txt1.Bounds.Height) / 2;
        Canvas.SetTop(_txt1, Math.Max(0, top));
        Canvas.SetLeft(_txt1, 0);
    }

    /// <summary>
    /// 处理垂直方向静态显示
    /// </summary>
    private void HandleVerticalStaticDisplay()
    {
        if (_txt1!.Bounds.Height < _canvas!.Bounds.Height && _txt1.Bounds.Height > 0)
        {
            _txt1.Height = _canvas.Bounds.Height;
            _txt1.TextAlignment = TextAlignment;
        }

        // 水平居中
        double left = (_canvas.Bounds.Width - _txt1.Bounds.Width) / 2;
        Canvas.SetLeft(_txt1, Math.Max(0, left));
        Canvas.SetTop(_txt1, 0);
    }

    /// <summary>
    /// 执行滚动动画
    /// </summary>
    private void ExecuteScrollAnimation()
    {
        _txt2!.IsVisible = true;

        switch (Direction)
        {
            case RunningDirection.RightToLeft:
                CreateHorizontalAnimation(true);
                break;
            case RunningDirection.LeftToRight:
                CreateHorizontalAnimation(false);
                break;
            case RunningDirection.BottomToTop:
                CreateVerticalAnimation(true);
                break;
            case RunningDirection.TopToBottom:
                CreateVerticalAnimation(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Direction));
        }
    }

    /// <summary>
    /// 创建水平方向动画
    /// </summary>
    private void CreateHorizontalAnimation(bool isRightToLeft)
    {
        var (from, to, length) = CalculateHorizontalParameters(isRightToLeft);
        
        if (Mode == RunningMode.Bounce)
        {
            CreateBounceAnimation(Canvas.LeftProperty, from, to);
        }
        else
        {
            CreateCycleAnimation(Canvas.LeftProperty, from, to, length);
        }
    }

    /// <summary>
    /// 创建垂直方向动画
    /// </summary>
    private void CreateVerticalAnimation(bool isBottomToTop)
    {
        var (from, to, length) = CalculateVerticalParameters(isBottomToTop);
        
        if (Mode == RunningMode.Bounce)
        {
            CreateBounceAnimation(Canvas.TopProperty, from, to);
        }
        else
        {
            CreateCycleAnimation(Canvas.TopProperty, from, to, length);
        }
    }

    /// <summary>
    /// 计算水平方向参数
    /// </summary>
    private (double from, double to, double length) CalculateHorizontalParameters(bool isRightToLeft)
    {
        double canvasWidth = _canvas!.Bounds.Width;
        double textWidth = _txt1!.Bounds.Width;

        double from = -textWidth;
        double to = canvasWidth;

        if (isRightToLeft)
        {
            (from, to) = (to, from);
        }

        double length = double.IsNaN(Space) || Space < 0
            ? textWidth < canvasWidth ? canvasWidth : textWidth + canvasWidth
            : textWidth < canvasWidth - Space ? canvasWidth : textWidth + Space;

        return (from, to, length);
    }

    /// <summary>
    /// 计算垂直方向参数
    /// </summary>
    private (double from, double to, double length) CalculateVerticalParameters(bool isBottomToTop)
    {
        double canvasHeight = _canvas!.Bounds.Height;
        double textHeight = _txt1!.Bounds.Height;

        double from = -textHeight;
        double to = canvasHeight;

        if (isBottomToTop)
        {
            (from, to) = (to, from);
        }

        double length = double.IsNaN(Space) || Space < 0
            ? textHeight < canvasHeight ? canvasHeight : textHeight + canvasHeight
            : textHeight < canvasHeight - Space ? canvasHeight : textHeight + Space;

        return (from, to, length);
    }

    /// <summary>
    /// 创建往返动画
    /// </summary>
    private void CreateBounceAnimation(AvaloniaProperty property, double from, double to)
    {
        _txt2!.IsVisible = false;
        
        double moveDuration = Math.Abs(to - from) / Speed;
        const int pauseDuration = 1; // 1秒暂停
        double totalDuration = moveDuration + pauseDuration;
        double movePercent = moveDuration / totalDuration;

        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(totalDuration),
            IterationCount = IterationCount.Infinite,
            PlaybackDirection = PlaybackDirection.Alternate,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(property, from) } },
                new KeyFrame { Cue = new Cue(movePercent), Setters = { new Setter(property, to) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(property, to) } },
            },
        };
        
        animation.RunAsync(_txt1!, _animationCts!.Token);
    }

    /// <summary>
    /// 创建循环动画
    /// </summary>
    private void CreateCycleAnimation(AvaloniaProperty property, double from, double to, double length)
    {
        _txt2!.IsVisible = true;
        
        // 设置初始位置
        if (property == Canvas.LeftProperty)
        {
            Canvas.SetLeft(_txt1!, from);
            Canvas.SetLeft(_txt2!, from);
        }
        else if (property == Canvas.TopProperty)
        {
            Canvas.SetTop(_txt1!, from);
            Canvas.SetTop(_txt2!, from);
        }

        // 计算动画时间
        var beginTime = TimeSpan.FromSeconds(length / Speed);
        var duration = TimeSpan.FromSeconds(Math.Abs(to - from) / Speed);
        var totalTime = beginTime + beginTime;

        // 创建两个错开的动画
        var animation1 = CreateKeyFrameAnimation(property, from, to, totalTime, duration / totalTime);
        var animation2 = CreateKeyFrameAnimation(property, from, to, totalTime, duration / totalTime, beginTime);

        animation1.RunAsync(_txt1!, _animationCts!.Token);
        animation2.RunAsync(_txt2!, _animationCts.Token);
    }

    /// <summary>
    /// 创建关键帧动画
    /// </summary>
    private static Animation CreateKeyFrameAnimation(AvaloniaProperty property, double from, double to, TimeSpan duration, double movePercent, TimeSpan? delay = null)
    {
        var animation = new Animation
        {
            Duration = duration,
            IterationCount = IterationCount.Infinite,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(property, from) } },
                new KeyFrame { Cue = new Cue(movePercent), Setters = { new Setter(property, to) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(property, to) } },
            },
        };

        if (delay.HasValue)
        {
            animation.Delay = delay.Value;
        }

        return animation;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (_canvas != null)
            _canvas.SizeChanged -= OnSizeChanged;

        if (_txt1 != null)
            _txt1.SizeChanged -= OnSizeChanged;
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    private void SubscribeToEvents()
    {
        _txt1!.SizeChanged += OnSizeChanged;
        _canvas!.SizeChanged += OnSizeChanged;
    }

    /// <summary>
    /// 验证模板部件
    /// </summary>
    private bool ValidateTemplateParts()
    {
        return _canvas != null && _txt1 != null && _txt2 != null;
    }

    /// <summary>
    /// 检查属性是否影响更新
    /// </summary>
    private static bool IsPropertyAffectingUpdate(AvaloniaProperty? property)
    {
        return property == TextProperty ||
               property == SpaceProperty ||
               property == SpeedProperty ||
               property == DirectionProperty ||
               property == PlaceholderTextProperty ||
               property == ModeProperty ||
               property == TextAlignmentProperty ||
               property == BehaviorProperty;
    }

    /// <summary>
    /// 停止当前动画
    /// </summary>
    private void StopCurrentAnimation()
    {
        _animationCts?.Cancel();
        _animationCts = new CancellationTokenSource();
    }

    /// <summary>
    /// 验证控件是否可以更新
    /// </summary>
    private bool IsValidForUpdate()
    {
        return _canvas != null && _txt1 != null && _txt2 != null && IsVisible;
    }

    /// <summary>
    /// 重置文本块属性
    /// </summary>
    private void ResetTextBlockProperties()
    {
        _txt1!.ClearValue(WidthProperty);
        _txt1.ClearValue(HeightProperty);
        _txt1.ClearValue(TextAlignmentProperty);
    }

    /// <summary>
    /// 重置文本位置
    /// </summary>
    private void ResetTextPosition()
    {
        Canvas.SetLeft(_txt1!, 0);
        Canvas.SetTop(_txt1!, 0);
    }

    /// <summary>
    /// 判断是否为水平方向
    /// </summary>
    private bool IsHorizontalDirection()
    {
        return Direction is RunningDirection.LeftToRight or RunningDirection.RightToLeft;
    }

    #endregion

    #region Fields

    private Canvas? _canvas;
    private TextBlock? _txt1;
    private TextBlock? _txt2;
    private bool _ignoreSizeChanges;

    #endregion
}
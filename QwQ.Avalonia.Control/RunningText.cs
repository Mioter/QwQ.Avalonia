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
/// 滚动文字
/// </summary>
[TemplatePart("PART_Canvas", typeof(Canvas))]
[TemplatePart("PART_Txt1", typeof(TextBlock))]
[TemplatePart("PART_Txt2", typeof(TextBlock))]
public class RunningText : TemplatedControl
{
    #region StyledProperties

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<RunningText, string>(nameof(Text));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<double> SpaceProperty =
        AvaloniaProperty.Register<RunningText, double>(nameof(Space), double.NaN);

    public double Space
    {
        get => GetValue(SpaceProperty);
        set => SetValue(SpaceProperty, value);
    }

    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<RunningText, double>(nameof(Speed), 120d);

    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    public static readonly StyledProperty<RunningDirection> DirectionProperty =
        AvaloniaProperty.Register<RunningText, RunningDirection>(nameof(Direction));

    public RunningDirection Direction
    {
        get => GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    public static readonly StyledProperty<RunningMode> ModeProperty =
        AvaloniaProperty.Register<RunningText, RunningMode>(nameof(Mode));

    public RunningMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
        AvaloniaProperty.Register<RunningText, TextAlignment>(nameof(TextAlignment));

    public TextAlignment TextAlignment
    {
        get => GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    public static readonly StyledProperty<RunningBehavior> BehaviorProperty =
        AvaloniaProperty.Register<RunningText, RunningBehavior>(nameof(Behavior));

    public RunningBehavior Behavior
    {
        get => GetValue(BehaviorProperty);
        set => SetValue(BehaviorProperty, value);
    }

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

        if (_canvas != null)
            _canvas.SizeChanged -= OnSizeChanged;

        if (_txt1 != null)
            _txt1.SizeChanged -= OnSizeChanged;

        _canvas = e.NameScope.Find<Canvas>("PART_Canvas");
        _txt1 = e.NameScope.Find<TextBlock>("PART_Txt1");
        _txt2 = e.NameScope.Find<TextBlock>("PART_Txt2");

        if (_canvas == null || _txt1 == null || _txt2 == null)
            return;

        _txt1.SizeChanged += OnSizeChanged;
        _canvas.SizeChanged += OnSizeChanged;
        BeginUpdate();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty ||
            change.Property == SpaceProperty ||
            change.Property == SpeedProperty ||
            change.Property == DirectionProperty ||
            change.Property == PlaceholderTextProperty ||
            change.Property == ModeProperty ||
            change.Property == TextAlignmentProperty ||
            change.Property == BehaviorProperty)
        {
            BeginUpdate();
        }
    }
        
    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        BeginUpdate();
    }

    private CancellationTokenSource? _delayUpdateCts;
    /// <summary>
    /// 多个事件同时触发时，仅执行一次
    /// </summary>
    private void BeginUpdate()
    {
        _delayUpdateCts?.Cancel();
        _delayUpdateCts = new CancellationTokenSource();
        var token = _delayUpdateCts.Token;
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if(!token.IsCancellationRequested)
                Update();
        }, DispatcherPriority.Loaded);
    }

    private CancellationTokenSource? _animationCts;
    private void Update()
    {
        // 停用动画
        _animationCts?.Cancel();
        _animationCts = new CancellationTokenSource();

        if (_canvas == null || _txt1 == null || _txt2 == null || !IsVisible)
            return;
        
        _txt1.ClearValue(WidthProperty);
        _txt1.ClearValue(HeightProperty);
        _txt1.ClearValue(TextAlignmentProperty);

        if (string.IsNullOrEmpty(Text))
        {
            if (!string.IsNullOrEmpty(PlaceholderText))
            {
                _txt1.Text = PlaceholderText;
                PseudoClasses.Add(":placeholder");
            }
            else
            {
                _txt1.Text = string.Empty;
                PseudoClasses.Remove(":placeholder");
            }

            _txt2.Text = string.Empty;
            _txt2.IsVisible = false;

            Canvas.SetLeft(_txt1, 0);
            Canvas.SetTop(_txt1, 0);
            return;
        }

        _txt1.Text = Text;
        _txt2.Text = Text;
        PseudoClasses.Remove(":placeholder");

        bool shouldScroll;
        if (Direction is RunningDirection.LeftToRight or RunningDirection.RightToLeft)
            shouldScroll = _txt1.Bounds.Width > _canvas.Bounds.Width;
        else
            shouldScroll = _txt1.Bounds.Height > _canvas.Bounds.Height;

        // 根据行为决定是否滚动
        bool willScroll = Behavior switch
        {
            RunningBehavior.Auto => shouldScroll,
            RunningBehavior.Always => true,
            RunningBehavior.Pause => false,
            _ => shouldScroll
        };

        if (!willScroll)
        {
            _animationCts.Cancel();
            _txt2.IsVisible = false;

            // 设置文本对齐
            if (Direction is RunningDirection.LeftToRight or RunningDirection.RightToLeft)
            {
                // 水平方向文本
                _txt1.Width = _canvas.Bounds.Width;
                _txt1.TextAlignment = TextAlignment;
                
                // 垂直居中
                double t = (_canvas.Bounds.Height - _txt1.Bounds.Height) / 2;
                Canvas.SetTop(_txt1, t > 0 ? t : 0);
                Canvas.SetLeft(_txt1, 0);
            }
            else
            {
                // 垂直方向文本
                _txt1.Height = _canvas.Bounds.Height;
                _txt1.TextAlignment = TextAlignment;
                
                // 水平居中
                double l = (_canvas.Bounds.Width - _txt1.Bounds.Width) / 2;
                Canvas.SetLeft(_txt1, l > 0 ? l : 0);
                Canvas.SetTop(_txt1, 0);
            }
            return;
        }
        
        _txt2.IsVisible = true;

        switch (Direction)
        {
            case RunningDirection.RightToLeft:
                UpdateRightToLeft();
                break;
            case RunningDirection.LeftToRight:
                UpdateLeftToRight();
                break;
            case RunningDirection.BottomToTop:
                UpdateBottomToTop();
                break;
            case RunningDirection.TopToBottom:
                UpdateTopToBottom();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void UpdateRightToLeft()
    {
        GetHorizontal(out double to, out double from, out double len);
        UpdateHorizontal(from, to, len);
    }

    private void UpdateLeftToRight()
    {
        GetHorizontal(out double from, out double to, out double len);
        UpdateHorizontal(from, to, len);
    }

    private void UpdateBottomToTop()
    {
        GetVertical(out double to, out double from, out double len);
        UpdateVertical(from, to, len);
    }

    private void UpdateTopToBottom()
    {
        GetVertical(out double from, out double to, out double len);
        UpdateVertical(from, to, len);
    }

    private void GetHorizontal(out double from, out double to, out double len)
    {
        double widthCanvas = _canvas!.Bounds.Width;
        double widthTxt = _txt1!.Bounds.Width;

        from = -widthTxt;
        to = widthCanvas;

        if (double.IsNaN(Space) || Space < 0)
            len = widthTxt < widthCanvas ? widthCanvas : widthTxt + widthCanvas;
        else
            len = widthTxt < widthCanvas - Space ? widthCanvas : widthTxt + Space;
    }

    private void UpdateHorizontal(double from, double to, double len)
    {
        if (_txt1 == null || _txt2 == null || _animationCts == null || _canvas == null)
            return;

        if (Mode == RunningMode.Bounce)
        {
            _txt2.IsVisible = false;
            double newFrom = 0d;
            double newTo = _canvas.Bounds.Width - _txt1.Bounds.Width;

            if (Direction == RunningDirection.LeftToRight)
            {
                (newFrom, newTo) = (newTo, newFrom);
            }

            double moveDuration = Math.Abs(newTo - newFrom) / Speed;
            int pauseDuration = 1; // 1 second pause
            double totalDuration = moveDuration + pauseDuration;
            double movePercent = moveDuration / totalDuration;

            var animation = new Animation
            {
                Duration = TimeSpan.FromSeconds(totalDuration),
                IterationCount = IterationCount.Infinite,
                PlaybackDirection = PlaybackDirection.Alternate,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Canvas.LeftProperty, newFrom) } },
                    new KeyFrame { Cue = new Cue(movePercent), Setters = { new Setter(Canvas.LeftProperty, newTo) } },
                    new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Canvas.LeftProperty, newTo) } },
                },
            };
            animation.RunAsync(_txt1, _animationCts.Token);
        }
        else
        {
            _txt2.IsVisible = true;
            Canvas.SetLeft(_txt1!, from);
            Canvas.SetLeft(_txt2!, from);

            var begin = TimeSpan.FromSeconds(len / Speed);
            var duration = TimeSpan.FromSeconds(Math.Abs(to - from) / Speed);
            var total = begin + begin;

            var animation1 = new Animation
            {
                Duration = total,
                IterationCount = IterationCount.Infinite,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters = { new Setter(Canvas.LeftProperty, from) },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(duration / total),
                        Setters = { new Setter(Canvas.LeftProperty, to) },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters = { new Setter(Canvas.LeftProperty, to) },
                    },
                },
            };
            var animation2 = new Animation
            {
                Duration = total,
                IterationCount = IterationCount.Infinite,
                Delay = begin,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters = { new Setter(Canvas.LeftProperty, from) },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(duration / total),
                        Setters = { new Setter(Canvas.LeftProperty, to) },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters = { new Setter(Canvas.LeftProperty, to) },
                    },
                },
            };

            animation1.RunAsync(_txt1, _animationCts.Token);
            animation2.RunAsync(_txt2, _animationCts.Token);
        }
    }
        
    private void GetVertical(out double from, out double to, out double len)
    {
        double heightCanvas = _canvas!.Bounds.Height;
        double heightTxt = _txt1!.Bounds.Height;

        from = -heightTxt;
        to = heightCanvas;

        if (double.IsNaN(Space) || Space < 0)
            len = heightTxt < heightCanvas ? heightCanvas : heightTxt + heightCanvas;
        else
            len = heightTxt < heightCanvas - Space ? heightCanvas : heightTxt + Space;
    }

    private void UpdateVertical(double from, double to, double len)
    {
        if (_txt1 == null || _txt2 == null || _animationCts == null || _canvas == null)
            return;

        if (Mode == RunningMode.Bounce)
        {
            _txt2.IsVisible = false;
            double newFrom = 0d;
            double newTo = _canvas.Bounds.Height - _txt1.Bounds.Height;

            if (Direction == RunningDirection.TopToBottom)
            {
                (newFrom, newTo) = (newTo, newFrom);
            }

            double moveDuration = Math.Abs(newTo - newFrom) / Speed;
            int pauseDuration = 1; // 1 second pause
            double totalDuration = moveDuration + pauseDuration;
            double movePercent = moveDuration / totalDuration;

            var animation = new Animation
            {
                Duration = TimeSpan.FromSeconds(totalDuration),
                IterationCount = IterationCount.Infinite,
                PlaybackDirection = PlaybackDirection.Alternate,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Canvas.TopProperty, newFrom) } },
                    new KeyFrame { Cue = new Cue(movePercent), Setters = { new Setter(Canvas.TopProperty, newTo) } },
                    new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Canvas.TopProperty, newTo) } },
                },
            };
            animation.RunAsync(_txt1, _animationCts.Token);
        }
        else
        {
            _txt2.IsVisible = true;
            Canvas.SetTop(_txt1!, from);
            Canvas.SetTop(_txt2!, from);

            var begin = TimeSpan.FromSeconds(len / Speed);
            var duration = TimeSpan.FromSeconds(Math.Abs(to - from) / Speed);
            var total = begin + begin;

            var animation1 = new Animation
            {
                Duration = total,
                IterationCount = IterationCount.Infinite,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters = { new Setter(Canvas.TopProperty, from) },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(duration / total),
                        Setters = { new Setter(Canvas.TopProperty, to) },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters = { new Setter(Canvas.TopProperty, to) },
                    },
                },
            };
            var animation2 = new Animation
            {
                Duration = total,
                IterationCount = IterationCount.Infinite,
                Delay = begin,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters = { new Setter(Canvas.TopProperty, from) },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(duration / total),
                        Setters = { new Setter(Canvas.TopProperty, to) },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters = { new Setter(Canvas.TopProperty, to) },
                    },
                },
            };

            animation1.RunAsync(_txt1, _animationCts.Token);
            animation2.RunAsync(_txt2, _animationCts.Token);
        }
    }

    #endregion

    #region Fields

    private Canvas? _canvas;
    private TextBlock? _txt1;
    private TextBlock? _txt2;

    #endregion
}
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
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

    #endregion

    static RunningText()
    {
        AffectsMeasure<RunningText>(TextProperty, SpaceProperty, SpeedProperty, DirectionProperty);
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
            change.Property == DirectionProperty)
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

        if (_canvas == null || _txt1 == null || _txt2 == null || string.IsNullOrEmpty(Text) || !IsVisible)
            return;

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
        if(_txt1 == null ||  _txt2 == null || _animationCts == null)
            return;
        
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
                    Cue = new Cue(duration/total),
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
                    Cue = new Cue(duration/total),
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
        if(_txt1 == null ||  _txt2 == null || _animationCts == null)
            return;
        
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
                    Cue = new Cue(duration/total),
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
                    Cue = new Cue(duration/total),
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

    #endregion

    #region Fields

    private Canvas? _canvas;
    private TextBlock? _txt1;
    private TextBlock? _txt2;

    #endregion
}
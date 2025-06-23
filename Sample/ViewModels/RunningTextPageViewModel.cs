using System.Collections.ObjectModel;
using System.Windows.Input;
using QwQ.Avalonia.Control;

namespace Sample.ViewModels;

public class RunningTextPageViewModel : ViewModelBase
{
    private string _sampleText = "这是一个滚动的文本示例，展示了RunningText控件的各种功能。";
    private double _speed = 120;
    private double _space = double.NaN;
    private RunningDirection _direction = RunningDirection.RightToLeft;
    private RunningMode _mode = RunningMode.Cycle;
    private RunningBehavior _behavior = RunningBehavior.Auto;
    private bool _isRunning = true;

    public RunningTextPageViewModel()
    {
        SetAutoSpaceCommand = new RelayCommand(() => Space = double.NaN);
    }

    public string SampleText
    {
        get => _sampleText;
        set => SetProperty(ref _sampleText, value);
    }

    public double Speed
    {
        get => _speed;
        set => SetProperty(ref _speed, value);
    }

    public double Space
    {
        get => _space;
        set => SetProperty(ref _space, value);
    }

    public RunningDirection Direction
    {
        get => _direction;
        set => SetProperty(ref _direction, value);
    }

    public RunningMode Mode
    {
        get => _mode;
        set => SetProperty(ref _mode, value);
    }

    public RunningBehavior Behavior
    {
        get => _behavior;
        set => SetProperty(ref _behavior, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    public ICommand SetAutoSpaceCommand { get; }

    public ObservableCollection<RunningDirection> Directions { get; } = new()
    {
        RunningDirection.RightToLeft,
        RunningDirection.LeftToRight,
        RunningDirection.BottomToTop,
        RunningDirection.TopToBottom,
    };

    public ObservableCollection<RunningMode> Modes { get; } = new()
    {
        RunningMode.Cycle,
        RunningMode.Bounce,
    };

    public ObservableCollection<RunningBehavior> Behaviors { get; } = new()
    {
        RunningBehavior.Auto,
        RunningBehavior.Always,
        RunningBehavior.Pause,
    };

    public ObservableCollection<string> SampleTexts { get; } = new()
    {
        "这是一个滚动的文本示例，展示了RunningText控件的各种功能。",
        "欢迎使用QwQ.Avalonia.Control控件库！",
        "这是一个很长的文本，用来测试滚动效果是否正常。文本会持续滚动，直到被停止。",
        "🚀 Avalonia UI 是一个跨平台的 .NET UI 框架 🎨",
        "RunningText控件支持四个方向的滚动：从左到右、从右到左、从上到下、从下到上。",
        "短文本",
        "这是一个测试占位符功能的示例",
    };
}
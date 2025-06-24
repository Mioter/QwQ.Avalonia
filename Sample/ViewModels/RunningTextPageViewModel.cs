using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sample.ViewModels;

public partial class RunningTextPageViewModel : ViewModelBase
{

    [ObservableProperty]
    public partial double Space { get; set; } = double.NaN;

    public ObservableCollection<string> SampleTexts { get; } =
    [
        "这是一个滚动的文本示例，展示了RunningText控件的各种功能。",
        "欢迎使用QwQ.Avalonia.Control控件库！",
        "这是一个很长的文本，用来测试滚动效果是否正常。文本会持续滚动，直到被停止。",
        "🚀 Avalonia UI 是一个跨平台的 .NET UI 框架 🎨",
        "RunningText控件支持四个方向的滚动：从左到右、从右到左、从上到下、从下到上。",
        "短文本",
        "这是一个测试占位符功能的示例",
    ];

    public string? DyText { get; set; }
    
    private int _index;

    [RelayCommand]
    private void ToggleText()
    {
        if (_index == SampleTexts.Count - 1)
        {
            _index = 0;
        }
        else
        {
            _index++;
        }

        DyText = SampleTexts[_index];
        OnPropertyChanged(nameof(DyText));
    }
    
}
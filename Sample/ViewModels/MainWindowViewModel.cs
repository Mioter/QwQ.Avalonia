using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sample.Enums;
using Sample.Examples;

namespace Sample.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [RelayCommand]
    private static async Task RunMessageBusExample()
    {
        await MessageBusExample.RunExample();
    }

    [RelayCommand]
    private static async Task RunRunMessageBusExample()
    {
        await TaskManagerExample.RunExamples();
    }

    [RelayCommand]
    private void TogglePlayMode()
    {
        // 循环切换播放模式
        PlayMode = (PlayMode)(((int)PlayMode + 1) % 3);
    }

    [RelayCommand]
    private void TogglePlaying()
    {
        IsPlaying = !IsPlaying;
    }

    [ObservableProperty]
    public partial PlayMode PlayMode { get; set; } = PlayMode.Sequential;

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }    
    
    [ObservableProperty]
    public partial bool IsEnabled { get; set; }
    
    public int[] CountItems { get; set; } = new int[50];
}

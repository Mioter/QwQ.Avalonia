using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sample.Enums;

namespace Sample.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{

    [RelayCommand]
    private void TogglePlayMode()
    {
        // 循环切换播放模式
        PlayMode = (PlayMode)(((int)PlayMode + 1) % 3);
    }

    [ObservableProperty] public partial PlayMode PlayMode { get; set; } = PlayMode.Sequential;
}

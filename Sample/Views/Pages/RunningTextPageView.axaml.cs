using Avalonia.Controls;

namespace Sample.Views.Pages;

public partial class RunningTextPageView : UserControl
{
    public RunningTextPageView()
    {
        InitializeComponent();
        DataContext = new ViewModels.RunningTextPageViewModel();
    }
}
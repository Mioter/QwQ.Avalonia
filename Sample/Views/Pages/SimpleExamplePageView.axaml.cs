using Avalonia.Controls;
using Sample.ViewModels;

namespace Sample.Views.Pages;

public partial class SimpleExamplePageView : UserControl
{
    public SimpleExamplePageView()
    {
        InitializeComponent();
        DataContext = new SimpleExamplePageViewModel();
    }
}

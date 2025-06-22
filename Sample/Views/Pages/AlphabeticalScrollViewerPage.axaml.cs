using Avalonia.Controls;
using Sample.ViewModels;

namespace Sample.Views.Pages;

public partial class AlphabeticalScrollViewerPage : UserControl
{
    public AlphabeticalScrollViewerPage()
    {
        InitializeComponent();
        DataContext = new AlphabeticalScrollViewerPageViewModel();
    }
}


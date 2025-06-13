using Avalonia.Controls;
using Sample.ViewModels;

namespace Sample.Views.Pages;

public partial class ImageCropperPageView : UserControl
{
    public ImageCropperPageView()
    {
        InitializeComponent();
        DataContext = new ImageCroppingPageViewModel();
    }
}

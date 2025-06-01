using Avalonia.Controls;
using Sample.ViewModels;

namespace Sample.Views;

public partial class ImageCropperSample : UserControl
{
    public ImageCropperSample()
    {
        InitializeComponent();
        DataContext = new ImageCroppingViewModel();
    }
}

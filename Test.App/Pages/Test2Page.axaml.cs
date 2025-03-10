using Avalonia.Controls;
using Test.App.Models;

namespace Test.App.Pages;

public partial class Test2Page : UserControl
{
    public Test2Page()
    {
        InitializeComponent();
        DataContext = new Test2PageModel();
    }
}


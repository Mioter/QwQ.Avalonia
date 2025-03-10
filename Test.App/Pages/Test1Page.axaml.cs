using Avalonia.Controls;
using Test.App.Models;

namespace Test.App.Pages;

public partial class Test1Page : UserControl
{
    public Test1Page()
    {
        InitializeComponent();
        DataContext = new Test1PageModel();
    }
}


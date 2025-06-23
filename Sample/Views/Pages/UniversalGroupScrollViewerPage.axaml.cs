using Avalonia.Controls;
using QwQ.Avalonia.Control;
using Sample.Models;
using Sample.ViewModels;

namespace Sample.Views.Pages;

public partial class UniversalGroupScrollViewerPage : UserControl
{
    public UniversalGroupScrollViewerPage()
    {
        InitializeComponent();
        DataContext = new AlphabeticalScrollViewerPageViewModel();
        // 设置分组方式：按Department分组
        this.FindControl<UniversalGroupScrollViewer>("UniversalGroupScrollViewer")!.GroupKeySelector = obj =>
        {
            var contact = obj as ContactItem;
            return contact?.Department ?? "";
        };
        this.FindControl<UniversalGroupScrollViewer>("UniversalGroupScrollViewer")!.GroupKeyDisplaySelector = key => key.ToString() ?? "";
    }
} 
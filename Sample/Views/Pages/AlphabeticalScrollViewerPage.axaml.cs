using Avalonia.Controls;
using Sample.ViewModels;

namespace Sample.Views.Pages;

public partial class AlphabeticalScrollViewerPage : UserControl
{
    public AlphabeticalScrollViewerPage()
    {
        InitializeComponent();
        DataContext = new AlphabeticalScrollViewerPageViewModel();
        // 设置LetterSelector委托
        this.FindControl<QwQ.Avalonia.Control.AlphabeticalScrollViewer>("AlphabeticalScrollViewer")!.LetterSelector = obj => {
            if (obj is not Models.ContactItem contact || string.IsNullOrEmpty(contact.Name)) 
                return '#';
            
            char c = char.ToUpper(contact.Name[0]);
            return c is >= 'A' and <= 'Z' ? c : '#';
        };
    }
}


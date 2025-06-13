using Avalonia;
using Avalonia.Controls;

namespace QwQ.Avalonia.Control.Docking;

public class DockDocument : ContentControl
{
    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<
        DockDocument,
        string
    >(nameof(Title));

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public DockDocument()
    {
        Title = string.Empty;
    }
}
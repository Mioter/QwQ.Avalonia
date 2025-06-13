using Avalonia;
using Avalonia.Controls;

namespace QwQ.Avalonia.Control.Docking;

public class LayoutRoot : ContentControl, ILayoutRoot
{
    public static readonly StyledProperty<DockPane> RootPaneProperty =
        AvaloniaProperty.Register<LayoutRoot, DockPane>(nameof(RootPane));

    public DockPane RootPane
    {
        get => GetValue(RootPaneProperty);
        set => SetValue(RootPaneProperty, value);
    }

    public LayoutRoot()
    {
        RootPane = new DockPane();
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace QwQ.Avalonia.Control.Docking;

/// <summary>
/// 未实现，请勿使用
/// </summary>
public class DockingManager : ItemsControl
{
    public static readonly StyledProperty<ILayoutRoot> LayoutProperty =
        AvaloniaProperty.Register<DockingManager, ILayoutRoot>(nameof(Layout));

    public ILayoutRoot Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<DockingManager, Orientation>(nameof(Orientation));

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    private List<DockPane> _panes = [];
    private List<DockDocument> _documents = [];

    public DockingManager()
    {
        Layout = new LayoutRoot();
    }

    public void AddPane(DockPane pane)
    {
        if (!_panes.Contains(pane))
        {
            _panes.Add(pane);
            Layout.RootPane?.AddPane(pane);
        }
    }

    public void RemovePane(DockPane pane)
    {
        if (_panes.Contains(pane))
        {
            _panes.Remove(pane);
            Layout.RootPane?.RemovePane(pane);
        }
    }

    public void AddDocument(DockDocument document)
    {
        if (!_documents.Contains(document))
        {
            _documents.Add(document);
            Layout.RootPane?.AddDocument(document);
        }
    }

    public void RemoveDocument(DockDocument document)
    {
        if (_documents.Contains(document))
        {
            _documents.Remove(document);
            Layout.RootPane?.RemoveDocument(document);
        }
    }
}
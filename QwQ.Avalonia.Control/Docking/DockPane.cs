using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace QwQ.Avalonia.Control.Docking;

public class DockPane : ItemsControl
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<DockPane, Orientation>(nameof(Orientation));

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    private List<DockPane> _childPanes = [];
    private List<DockDocument> _documents = [];

    public DockPane()
    {
        Orientation = Orientation.Horizontal;
    }

    public void AddPane(DockPane pane)
    {
        if (!_childPanes.Contains(pane))
        {
            _childPanes.Add(pane);
        }
    }

    public void RemovePane(DockPane pane)
    {
        if (_childPanes.Contains(pane))
        {
            _childPanes.Remove(pane);
        }
    }

    public void AddDocument(DockDocument document)
    {
        if (!_documents.Contains(document))
        {
            _documents.Add(document);
        }
    }

    public void RemoveDocument(DockDocument document)
    {
        if (_documents.Contains(document))
        {
            _documents.Remove(document);
        }
    }
}
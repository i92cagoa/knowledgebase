using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using KnowledgeBase.Desktop.Models;
using KnowledgeBase.Desktop.ViewModels;

namespace KnowledgeBase.Desktop.Controls;

public sealed class GraphView : Control
{
    private readonly List<GraphLayoutNode> _hitNodes = [];
    private double _offsetX;
    private double _offsetY;
    private double _zoom = 1;
    private Point _lastPointer;
    private bool _panning;

    public static readonly StyledProperty<GraphViewModel> GraphProperty =
        AvaloniaProperty.Register<GraphView, GraphViewModel>(nameof(Graph));

    public GraphViewModel Graph
    {
        get => GetValue(GraphProperty);
        set => SetValue(GraphProperty, value);
    }

    static GraphView()
    {
        AffectsRender<GraphView>(GraphProperty);
        AffectsMeasure<GraphView>(GraphProperty);
    }

    public GraphView()
    {
        ClipToBounds = true;
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Graph is null)
        {
            return;
        }

        var w = Bounds.Width;
        var h = Bounds.Height;
        var cx = w / 2 + _offsetX;
        var cy = h / 2 + _offsetY;
        var scale = Math.Min(w, h) * 0.42 * _zoom;

        _hitNodes.Clear();

        // edges
        var edgePen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(90, 110, 130)), 1);
        foreach (var edge in Graph.Edges)
        {
            var a = Graph.Nodes.FirstOrDefault(n => n.Id == edge.SourceNoteId);
            var b = Graph.Nodes.FirstOrDefault(n => n.Id == edge.TargetNoteId);
            if (a is null || b is null)
            {
                continue;
            }

            var p1 = ToScreen(a, cx, cy, scale);
            var p2 = ToScreen(b, cx, cy, scale);
            context.DrawLine(edgePen, p1, p2);
        }

        // nodes
        foreach (var node in Graph.Nodes)
        {
            var center = ToScreen(node, cx, cy, scale);
            var radius = node.Radius * _zoom;
            var brush = node.Selected
                ? new ImmutableSolidColorBrush(Color.FromRgb(255, 208, 120))
                : new ImmutableSolidColorBrush(Color.FromRgb(240, 196, 80));
            var pen = new ImmutablePen(Brushes.Black, 1);

            context.DrawEllipse(brush, pen, new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2));
            _hitNodes.Add(node);
        }
    }

    private static Point ToScreen(GraphLayoutNode node, double cx, double cy, double scale) =>
        new(cx + node.X * scale, cy + node.Y * scale);

    private static Point FromScreen(Point p, double cx, double cy, double scale) =>
        new((p.X - cx) / scale, (p.Y - cy) / scale);

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var position = e.GetPosition(this);
        _lastPointer = position;

        var hit = HitTest(position);
        if (hit is not null)
        {
            Graph.SelectNode(hit.Id);
            e.Handled = true;
        }
        else if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _panning = true;
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var position = e.GetPosition(this);

        if (_panning)
        {
            _offsetX += position.X - _lastPointer.X;
            _offsetY += position.Y - _lastPointer.Y;
            _lastPointer = position;
            InvalidateVisual();
        }

        _lastPointer = position;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _panning = false;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var position = e.GetPosition(this);
        var w = Bounds.Width / 2 + _offsetX;
        var h = Bounds.Height / 2 + _offsetY;
        var before = FromScreen(position, w, h, Math.Min(Bounds.Width, Bounds.Height) * 0.42 * _zoom);

        _zoom = Math.Clamp(_zoom + (e.Delta.Y > 0 ? 0.1 : -0.1), 0.2, 4.0);

        var after = FromScreen(position, w, h, Math.Min(Bounds.Width, Bounds.Height) * 0.42 * _zoom);
        _offsetX += (before.X - after.X) * Math.Min(Bounds.Width, Bounds.Height) * 0.42;
        _offsetY += (before.Y - after.Y) * Math.Min(Bounds.Width, Bounds.Height) * 0.42;

        InvalidateVisual();
        e.Handled = true;
    }

    private GraphLayoutNode? HitTest(Point position)
    {
        var w = Bounds.Width / 2 + _offsetX;
        var h = Bounds.Height / 2 + _offsetY;
        var scale = Math.Min(Bounds.Width, Bounds.Height) * 0.42 * _zoom;

        foreach (var node in Graph.Nodes)
        {
            var center = ToScreen(node, w, h, scale);
            var radius = node.Radius * _zoom + 2;
            if (Math.Abs(position.X - center.X) <= radius && Math.Abs(position.Y - center.Y) <= radius)
            {
                return node;
            }
        }

        return null;
    }
}
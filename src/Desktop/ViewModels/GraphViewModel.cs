using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KnowledgeBase.Desktop.Models;
using KnowledgeBase.Desktop.Services;

namespace KnowledgeBase.Desktop.ViewModels;

public sealed record GraphLayoutNode(
    Guid Id,
    string Title,
    double X,
    double Y,
    int Degree,
    bool Selected)
{
    public double Radius => 8 + Math.Min(Degree, 12);
}

public sealed partial class GraphViewModel : ViewModelBase
{
    private readonly IKnowledgeBaseApiClient _api;

    public GraphViewModel(IKnowledgeBaseApiClient api)
    {
        _api = api;
    }

    public ObservableCollection<GraphLayoutNode> Nodes { get; } = new();
    public ObservableCollection<GraphEdge> Edges { get; } = new();

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public event Action<Guid>? NodeSelected;

    public async Task LoadAsync()
    {
        StatusMessage = "Loading graph...";
        try
        {
            var graph = await _api.GetGraphAsync();
            if (graph is null)
            {
                StatusMessage = "Could not load graph.";
                return;
            }

            Layout(graph);
            StatusMessage = $"{graph.Nodes.Count} notes, {graph.Edges.Count} connections";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load graph: {ex.Message}";
        }
    }

    private void Layout(GraphData graph)
    {
        var byId = graph.Nodes.ToDictionary(n => n.Id);

        if (byId.Count == 0)
        {
            Nodes.Clear();
            Edges.Clear();
            return;
        }

        var degree = graph.Nodes.ToDictionary(n => n.Id, _ => 0);
        foreach (var edge in graph.Edges)
        {
            if (degree.ContainsKey(edge.SourceNoteId)) degree[edge.SourceNoteId]++;
            if (degree.ContainsKey(edge.TargetNoteId)) degree[edge.TargetNoteId]++;
        }

        var nodes = graph.Nodes
            .Select(n => new LayoutPoint(n.Id))
            .ToList();
        var index = new Dictionary<Guid, LayoutPoint>();
        foreach (var n in nodes) index[n.Id] = n;

        var edges = graph.Edges
            .Where(e => index.ContainsKey(e.SourceNoteId) && index.ContainsKey(e.TargetNoteId))
            .Select(e => (A: index[e.SourceNoteId], B: index[e.TargetNoteId]))
            .ToList();

        // simple force-directed relaxation in symmetric [-1,1] space
        var rand = new Random(42);
        foreach (var n in nodes)
        {
            n.X = (rand.NextDouble() * 2) - 1;
            n.Y = (rand.NextDouble() * 2) - 1;
        }

        const int iterations = 60;
        const double repulsion = 0.9;
        const double attraction = 0.015;

        for (var k = 0; k < iterations; k++)
        {
            foreach (var a in nodes)
            {
                foreach (var b in nodes)
                {
                    if (a == b) continue;
                    var dx = a.X - b.X;
                    var dy = a.Y - b.Y;
                    var distSq = dx * dx + dy * dy + 1e-9;
                    var force = repulsion / distSq;
                    var dist = Math.Sqrt(distSq);
                    a.X += (dx / dist) * force;
                    a.Y += (dy / dist) * force;
                }
            }

            foreach (var (a, b) in edges)
            {
                var dx = b.X - a.X;
                var dy = b.Y - a.Y;
                a.X += dx * attraction;
                a.Y += dy * attraction;
                b.X -= dx * attraction;
                b.Y -= dy * attraction;
            }

            // center + scale in
            var cx = nodes.Average(n => n.X);
            var cy = nodes.Average(n => n.Y);
            foreach (var n in nodes)
            {
                n.X = (n.X - cx) * 0.95;
                n.Y = (n.Y - cy) * 0.95;
            }
        }

        // finalize into [-1,1] normalized bounds
        var minX = nodes.Min(n => n.X);
        var maxX = nodes.Max(n => n.X);
        var minY = nodes.Min(n => n.Y);
        var maxY = nodes.Max(n => n.Y);
        var range = Math.Max(maxX - minX, maxY - minY);
        if (range == 0) range = 1;

        Nodes.Clear();
        Edges.Clear();

        foreach (var n in nodes)
        {
            var note = byId[n.Id];
            Nodes.Add(new GraphLayoutNode(
                note.Id,
                note.Title,
                (n.X - minX) / range * 2 - 1,
                (n.Y - minY) / range * 2 - 1,
                degree[n.Id],
                Selected: false));
        }

        foreach (var edge in graph.Edges)
        {
            if (byId.ContainsKey(edge.SourceNoteId) && byId.ContainsKey(edge.TargetNoteId))
            {
                Edges.Add(edge);
            }
        }
    }

    public void SelectNode(Guid id)
    {
        NodeSelected?.Invoke(id);
    }

    private sealed class LayoutPoint
    {
        public Guid Id { get; }
        public double X;
        public double Y;

        public LayoutPoint(Guid id) => Id = id;
    }
}
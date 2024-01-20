using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Routing
{
    public class DijkstraResult<T>
    {
        private readonly Graph<T> _graph;
        private readonly GraphOverloader<T> _overloader;
        private readonly Dictionary<int, VertexData<T>> _dynamicData;
        private readonly Stopwatch _stopwatch;

        public VertexData<T> Source { get; set; }
        public VertexData<T> Target { get; private set; }
        public double ElapsedTimeMs { get; set; }
        public TerminationType Termination { get; set; }

        public int Tries { get; set; }
        public int AboveMaxCost { get; set; }

        public DijkstraResult(Graph<T> graph, GraphOverloader<T> overloader = null)
        {
            _graph = graph;
            _overloader = overloader;
            overloader?.Build(graph);
            _dynamicData = new Dictionary<int, VertexData<T>>();
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public VertexData<T> GetVertexData(int id)
        {
            if (_dynamicData.TryGetValue(id, out var vd)) return vd;
            vd = new VertexData<T>(GetVertex(id));
            _dynamicData.Add(id, vd);
            return vd;
        }

        public DijkstraResult<T> Finish(VertexData<T> target = null, TerminationType termination = TerminationType.Error)
        {
            _stopwatch.Stop();
            _dynamicData.Clear();
            ElapsedTimeMs = _stopwatch.ElapsedMilliseconds;
            Termination = termination;
            Target = target;
            return this;
        }

        public bool HasVisitedVertex(int id)
        {
            return _dynamicData.ContainsKey(id);
        }

        public IEnumerable<VertexData<T>> GetInternalData()
        {
            return _dynamicData.Values;
        }

        private Vertex GetVertex(int id)
        {
            if (_overloader != null && _overloader.TryGetVertex(id, out var v)) return v;
            return _graph.Vertices[id];
        }

        public Edge<T> GetEdge(int startVertexId, int endVertexId)
        {
            if (_overloader != null && _overloader.TryGetEdge(startVertexId, endVertexId, out var e)) return e;
            return _graph.GetEdge(startVertexId, endVertexId);
        }
    }

    public enum TerminationType
    {
        Error,
        ReachedTarget,
        TimedOut
    }
}
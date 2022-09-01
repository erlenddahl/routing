using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Routing
{
    public class DijkstraResult
    {
        private readonly Graph _graph;
        private readonly Dictionary<int, VertexData> _dynamicData;
        private readonly Stopwatch _stopwatch;

        public VertexData Source { get; set; }
        public VertexData Target { get; private set; }
        public TimeSpan ElapsedTime { get; set; }

        public int Tries { get; set; }

        public DijkstraResult(Graph graph)
        {
            _graph = graph; 
            _dynamicData = new Dictionary<int, VertexData>();
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public VertexData GetVertexData(int id)
        {
            if (_dynamicData.TryGetValue(id, out var vd)) return vd;
            vd = new VertexData(_graph.Vertices[id]);
            _dynamicData.Add(id, vd);
            return vd;
        }

        public DijkstraResult Finish(VertexData target = null)
        {
            _stopwatch.Stop();
            ElapsedTime = _stopwatch.Elapsed;
            Target = target;
            return this;
        }

        public bool HasVisitedVertex(int id)
        {
            return _dynamicData.ContainsKey(id);
        }

        public IEnumerable<VertexData> GetInternalData()
        {
            return _dynamicData.Values;
        }
    }
}
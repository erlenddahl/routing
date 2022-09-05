using System.Collections.Generic;
using System.Linq;

namespace Routing
{
    public class GraphOverloader
    {
        private class OverloadVertex
        {
            public int Id;
            public int VertexA;
            public int VertexB;
            public double CostFactor;
            public Vertex Vertex;
            public Dictionary<int, Edge> Edges = new Dictionary<int, Edge>();

            public void CheckAddEdge(Graph graph, int sourceVertex, int targetVertex, double costFactor)
            {
                if (graph.TryGetEdge(sourceVertex, targetVertex, out var edge))
                {
                    Vertex.NeighbourIds.Add(targetVertex);

                    edge = edge.Clone();
                    edge.Cost *= costFactor;
                    edge.IsOverload = true;
                    Edges.Add(targetVertex, edge);
                }
            }
        }

        private Dictionary<int, OverloadVertex> _sourceOverloads = new Dictionary<int, OverloadVertex>();
        private Dictionary<int[], Vertex> _edges = new Dictionary<int[], Vertex>();
        private Graph _graph;

        public void AddSourceOverload(int id, int toVertexA, int toVertexB, double costFactor)
        {
            _sourceOverloads.Add(id, new OverloadVertex()
            {
                Id = id,
                VertexA = toVertexA,
                VertexB = toVertexB,
                CostFactor = costFactor
            });
        }

        public void AddTargetOverload(int id, int fromVertexA, int fromVertexB)
        {

        }

        public void Build(Graph graph)
        {
            _graph = graph;

            foreach (var so in _sourceOverloads.Values)
            {
                so.Vertex = new Vertex() { Id = so.Id };
                so.CheckAddEdge(graph, so.VertexA, so.VertexB, so.CostFactor);
                so.CheckAddEdge(graph, so.VertexB, so.VertexA, 1 - so.CostFactor);
            }
        }

        public bool TryGetVertex(int id, out Vertex vertex)
        {
            if (_sourceOverloads.TryGetValue(id, out var ov))
            {
                vertex = ov.Vertex;
                return true;
            }

            vertex = null;
            return false;
        }

        public bool TryGetEdge(int startVertexId, int endVertexId, out Edge edge)
        {
            if (_sourceOverloads.TryGetValue(startVertexId, out var ov) && ov.Edges.TryGetValue(endVertexId, out edge))
                return true;

            edge = null;
            return false;
        }
    }
}
using System;
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
            public Dictionary<int, Edge> Edges = new();

            public Edge CheckAddEdge(Graph graph, int sourceVertex, int targetVertex, double costFactor)
            {
                if (graph.TryGetEdge(sourceVertex, targetVertex, out var edge))
                {
                    Vertex?.NeighbourIds.Add(targetVertex);

                    edge = edge.Clone();
                    edge.Cost *= costFactor;
                    edge.IsOverload = true;
                    Edges.Add(targetVertex, edge);

                    return edge;
                }

                return null;
            }
        }

        private Dictionary<int, OverloadVertex> _sourceOverloads = new();
        private Dictionary<int, OverloadVertex> _targetOverloads = new();
        private Dictionary<(int From, int To), OverloadVertex> _targetEdges = new();

        public int AddSourceOverload(int id, int toVertexA, int toVertexB, double costFactor)
        {
            if (costFactor < 0 || costFactor > 1) throw new Exception($"Cost factor must be between 0 and 1 (was {costFactor:n5})");

            if (costFactor == 0)
                return toVertexA;
            if(Math.Abs(costFactor - 1) < 0.1)
                return toVertexB;

            _sourceOverloads.Add(id, new OverloadVertex()
            {
                Id = id,
                VertexA = toVertexA,
                VertexB = toVertexB,
                CostFactor = costFactor
            });

            return id;
        }

        public int AddTargetOverload(int id, int fromVertexA, int fromVertexB, double costFactor)
        {
            if (costFactor < 0 || costFactor > 1) throw new Exception($"Cost factor must be between 0 and 1 (was {costFactor:n5})");

            if (costFactor == 0)
                return fromVertexA;
            if (Math.Abs(costFactor - 1) < 0.1)
                return fromVertexB;

            _targetOverloads.Add(id, new OverloadVertex()
            {
                Id = id,
                VertexA = fromVertexA,
                VertexB = fromVertexB,
                CostFactor = costFactor
            });

            return id;
        }

        public void Build(Graph graph)
        {
            foreach (var so in _sourceOverloads.Values)
            {
                so.Vertex = new Vertex() { Id = so.Id };
                so.CheckAddEdge(graph, so.VertexA, so.VertexB, so.CostFactor);
                so.CheckAddEdge(graph, so.VertexB, so.VertexA, 1 - so.CostFactor);
            }

            foreach (var so in _targetOverloads.Values)
            {
                var vertexA = graph.Vertices[so.VertexA];
                var vertexB = graph.Vertices[so.VertexB];

                var edgeAb = so.CheckAddEdge(graph, so.VertexA, so.VertexB, so.CostFactor);
                var edgeBa = so.CheckAddEdge(graph, so.VertexB, so.VertexA, 1 - so.CostFactor);

                if (edgeAb != null)
                {
                    var ov = new OverloadVertex()
                    {
                        Vertex = vertexA.Clone()
                    };
                    ov.Vertex.NeighbourIds.Add(so.Id);
                    ov.Edges.Add(so.Id, edgeAb);
                    _sourceOverloads.Add(so.VertexA, ov);
                }

                if (edgeBa != null)
                {
                    var ov = new OverloadVertex()
                    {
                        Vertex = vertexB.Clone()
                    };
                    ov.Vertex.NeighbourIds.Add(so.Id);
                    ov.Edges.Add(so.Id, edgeBa);
                    _sourceOverloads.Add(so.VertexB, ov);
                }

                so.Vertex = new Vertex() { Id = so.Id };
                _sourceOverloads.Add(so.Id, so);
            }
        }

        public bool TryGetVertex(int id, out Vertex vertex)
        {
            if (_sourceOverloads.TryGetValue(id, out var ovS))
            {
                vertex = ovS.Vertex;
                return true;
            }

            vertex = null;
            return false;
        }

        public bool TryGetEdge(int startVertexId, int endVertexId, out Edge edge)
        {
            if (_sourceOverloads.TryGetValue(startVertexId, out var ovS) && ovS.Edges.TryGetValue(endVertexId, out edge))
                return true;

            edge = null;
            return false;
        }
    }
}
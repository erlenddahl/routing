using System;
using System.Collections.Generic;
using System.Linq;

namespace Routing
{
    /// <summary>
    /// Keeps track of overload vertices at the entry and exit points of a graph search.
    /// </summary>
    /// <remarks>
    /// When performing a routing in a road network, we are usually given a start and end coordinate.
    /// These coordinates could be mapped to the nearest vertex, but then we could lose a part of a road link.
    /// For example if the coordinates are 500 meters into a 1100 meter road link from A to B -- then this road
    /// link would be ignored completely, because the starting vertex would be the A, and those 500 meters
    /// driving from the start point to A would be lost.
    /// Therefore, the router starts by identifying the nearest link instead of the nearest vertex.
    /// Since we don't know which direction along the link we should drive before the routing has completed,
    /// we add an overload vertex O at the start point, which split the nearest link in two -- one part from A to O,
    /// and one part from O to B. The AO and OB links have costs based on the cost on the original link AB, and the
    /// distances from O to A and B. For example, if O is 300 meters from A, but 700 meters from B, the cost on
    /// the edge AO would be 30% of the original cost, while the cost on OB would be 70%.
    /// By doing it this way, the routing algorithm itself will choose which direction to start driving from
    /// the entry point, thus indirectly choosing the most reasonable "nearest vertex".
    /// </remarks>
    public class GraphOverloader<T>
    {
        private class OverloadVertex
        {
            /// <summary>
            /// The vertex Id of this overload vertex. Must be unique within the graph.
            /// Fake vertices usually have negative Ids to separate them from the real vertices.
            /// </summary>
            public int Id;

            /// <summary>
            /// The ID of the start vertex on the edge that this overload vertex cuts in two.
            /// Used to extract any existing edges between them to use as a base for the fake edges.
            /// </summary>
            public int VertexA;

            /// <summary>
            /// The ID of the end vertex on the edge that this overload vertex cuts in two.
            /// Used to extract any existing edges between them to use as a base for the fake edges.
            /// </summary>
            public int VertexB;

            /// <summary>
            /// The cost factor used to create costs for edges out from this overload vertex.
            /// </summary>
            public double CostFactor;

            /// <summary>
            /// The fake/overload vertex, which acts as a entry or exit point into/out from the
            /// full graph.
            /// </summary>
            public Vertex Vertex;
            public Dictionary<int, Edge<T>> Edges = new();

            /// <summary>
            /// Adds a fake edge from this overload-vertex to the given targetVertex.
            /// The sourceVertex and targetVertex are used to find any existing (real) edges
            /// between those, which are used as a base for the fake edge.
            /// </summary>
            /// <param name="graph"></param>
            /// <param name="sourceVertex"></param>
            /// <param name="targetVertex"></param>
            /// <param name="costFactor"></param>
            /// <returns></returns>
            public Edge<T> CheckAddEdge(Graph<T> graph, int sourceVertex, int targetVertex, double costFactor)
            {
                // Find the edge we are "replacing" with two sub-edges
                // If the existing edge doesn't exist (for example if this is a one-way street), do nothing.
                if (!graph.TryGetEdge(sourceVertex, targetVertex, out var edge)) return null;

                // Add the target vertex to the list of neighbors on the overload-vertex.
                Vertex?.NeighbourIds.Add(targetVertex);

                // Create a copy of the existing edge, with a cost that is a part of
                // the original cost (depending on the cost factor, which in practice
                // tells us how large part of the edge we are representing).
                edge = edge.Clone();
                edge.Cost *= costFactor;
                edge.IsOverload = true;
                Edges.Add(targetVertex, edge);

                return edge;

            }
        }

        private readonly Dictionary<int, OverloadVertex> _sourceOverloads = new();
        private readonly Dictionary<int, OverloadVertex> _targetOverloads = new();
        private bool _built;

        /// <summary>
        /// Adds an overload vertex O that has directional edges from the overload vertex to
        /// the two given vertices A and B, with costs calculated using the original edge
        /// from A to B and the cost factor (AO gets the originalCost * costFactor, while OB
        /// gets the originalCost * (1 - costFactor)).
        /// </summary>
        /// <param name="id"></param>
        /// <param name="toVertexA"></param>
        /// <param name="toVertexB"></param>
        /// <param name="costFactor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int AddSourceOverload(int id, int toVertexA, int toVertexB, double costFactor)
        {
            if (_built) throw new Exception("This graph overloader has already been built, and cannot be modified.");
            if (costFactor < 0 || costFactor > 1) throw new Exception($"Cost factor must be between 0 and 1 (was {costFactor:n5})");

            if (costFactor < 0.00001)
                return toVertexA;
            if(Math.Abs(costFactor - 1) < 0.00001)
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

        /// <summary>
        /// Adds an overload vertex O that has directional edges from the two given vertices A
        /// and B to the overload vertex, with costs calculated using the original edge from A
        /// to B and the cost factor (AO gets the originalCost * costFactor, while OB gets the
        /// originalCost * (1 - costFactor)).
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fromVertexA"></param>
        /// <param name="fromVertexB"></param>
        /// <param name="costFactor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int AddTargetOverload(int id, int fromVertexA, int fromVertexB, double costFactor)
        {
            if (_built) throw new Exception("This graph overloader has already been built, and cannot be modified.");
            if (costFactor < 0 || costFactor > 1) throw new Exception($"Cost factor must be between 0 and 1 (was {costFactor:n5})");

            if (costFactor < 0.00001)
                return fromVertexA;
            if (Math.Abs(costFactor - 1) < 0.00001)
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

        public void Build(Graph<T> graph)
        {
            if (_built) return;
            _built = true;

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

        public bool TryGetEdge(int startVertexId, int endVertexId, out Edge<T> edge)
        {
            if (_sourceOverloads.TryGetValue(startVertexId, out var ovS) && ovS.Edges.TryGetValue(endVertexId, out edge))
                return true;

            edge = null;
            return false;
        }
    }
}
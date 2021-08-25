using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Routing
{
    public class GraphAnalysis
    {
        public int Vertices { get; set; }
        public int Edges { get; set; }
        public int TotalNumberOfGroups { get; set; }
        public Dictionary<int, int> VertexIdGroup { get; set; }

        public GraphAnalysis(Graph graph)
        {
            var groupIx = 0;
            var groups = new Dictionary<int, int>();
            foreach (var v in graph.Vertices.Values)
            {
                if (groups.ContainsKey(v.Id)) continue;
                
                Recurse(groups, groupIx, v);

                groupIx++;
            }

            TotalNumberOfGroups = groupIx;
            VertexIdGroup = groups;
            Vertices = graph.Vertices.Count;
            Edges = graph.EdgeCount;

            //foreach (var g in groups.GroupBy(p => p.Value).Select(p => new {GroupId = p.Key, Count = p.Count()}).OrderByDescending(p => p.Count))
            //    Debug.WriteLine(g.GroupId + ", " + g.Count);
        }

        private void Recurse(Dictionary<int, int> groups, int groupIx, Vertex vertex)
        {
            var vertices = new Queue<Vertex>();
            vertices.Enqueue(vertex);
            while(vertices.Any())
            {
                var v = vertices.Dequeue();
                if (groups.ContainsKey(v.Id)) continue;
                groups.Add(v.Id, groupIx);
                foreach (var n in v.Neighbours)
                    vertices.Enqueue(n);
            }
        }
    }
}
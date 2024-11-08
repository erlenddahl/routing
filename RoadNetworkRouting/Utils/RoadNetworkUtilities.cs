using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnergyModule.Geometry.SimpleStructures;
using Extensions.IEnumerableExtensions;

namespace RoadNetworkRouting.Utils
{
    public static class RoadNetworkUtilities
    {
        private struct Node
        {
            public readonly int Id;
            public readonly Point3D Location;

            public Node(Point3D location, int id)
            {
                Location = location;
                Id = id;
            }
        }

        public static FixMissingNodeResult FixMissingNodeIds(RoadNetworkRouter router, double maxDistance = 1)
        {
            // Keep track of how many we fixed
            var res = new FixMissingNodeResult();
            res.ManhattanMaxDistance = 2 * maxDistance;

            // Create a list containing all nodes with their IDs and locations, then store it as a
            // Y-separated dictionary of lists.
            var nodesByY = router.Links
                .SelectMany(p => new[]
                {
                    new Node(p.Value.Geometry.First(), p.Value.FromNodeId),
                    new Node(p.Value.Geometry.Last(), p.Value.ToNodeId)
                })
                .Where(p => p.Id > int.MinValue)
                .GroupBy(p => (int)p.Location.Y)
                .ToDictionary(k => k.Key, v => v.ToList());

            res.NodesByYGroups = nodesByY.Count;
            res.ExistingValidNodes = nodesByY.Sum(p => p.Value.Count);

            // Locate the max node ID, so that we can continue creating nodes with IDs higher than this.
            var id = (int)nodesByY.SelectMany(p => p.Value).SafeMax(p => p.Id, -1) + 1;

            List<Node> FindRelevant(Point3D loc)
            {
                var relevantNodes = new List<Node>();
                if (nodesByY.TryGetValue((int)loc.Y, out var nodes)) relevantNodes.AddRange(nodes);
                if (nodesByY.TryGetValue((int)loc.Y - 1, out var nodesBelow)) relevantNodes.AddRange(nodesBelow);
                if (nodesByY.TryGetValue((int)loc.Y + 1, out var nodesAbove)) relevantNodes.AddRange(nodesAbove);
                return relevantNodes;
            }

            void AddNode(Node node)
            {
                if (!nodesByY.TryGetValue((int)node.Location.Y, out var list))
                    nodesByY.Add((int)node.Location.Y, list = new List<Node>());

                list.Add(node);
            }

            Node FindMatchingNode(Point3D location, string source)
            {
                // Next, retrieve any nodes that could be relevant by doing a simple dictionary lookup.
                var relevantNodes = FindRelevant(location);
                res.RelevantNodesFound += relevantNodes.Count;

                // Finally, find any nodes within 1 meter from this location.
                // (Using ManhattanDistance as a filter first, as the Sqrt calculation in the actual calculation is expensive.)
                var match = relevantNodes.FirstOrDefault(p => p.Location.ManhattanDistanceTo2D(location) < res.ManhattanMaxDistance && p.Location.DistanceTo2D(location) <= maxDistance);
                //Debug.WriteLine($"{source}: Found matching node at {match.Location}");
                
                // If there was no match (only the Location object will be null because it's a struct),
                // create a new node at this location. Make sure to increment the next available ID,
                // as well as adding the new node to the list of nodes.
                if (match.Location == null)
                {
                    match = new Node(location, id++);
                    AddNode(match);
                    //Debug.WriteLine($"{source}: Created new node {match.Id} at {match.Location}");
                    res.NewNodesCreated++;
                }
                else
                {
                    res.MatchingNodesFound++;
                }

                res.FixedNodes++;

                return match;
            }

            // Go through each link and find links with missing From/To node IDs.
            // (Missing is defined as equal to int.MinValue, and must be set as this
            // in the corresponding reader function.)
            foreach (var link in router.Links.Values)
            {
                // If FromNodeId is missing, fix it.
                if (link.FromNodeId == int.MinValue)
                {
                    // First, find its position (the first point in the geometry)
                    var location = link.Geometry.First();

                    // Set FromNodeId to the matching node (either one we found, or one we created).
                    link.FromNodeId = FindMatchingNode(location, "FromNodeId, link " + link.LinkId).Id;
                    res.FromNodesFixed++;
                }
                else
                {
                    res.FromNodesAlreadyOk++;
                }

                // Repeat for ToNodeId
                if (link.ToNodeId == int.MinValue)
                {
                    // First, find its position (the first point in the geometry)
                    var location = link.Geometry.Last();

                    // Set FromNodeId to the matching node (either one we found, or one we created).
                    link.ToNodeId = FindMatchingNode(location, "ToNodeId, link " + link.LinkId).Id;
                    res.ToNodesFixed++;
                }
                else
                {
                    res.ToNodesAlreadyOk++;
                }
            }

            return res;
        }
    }

    public class FixMissingNodeResult
    {
        public int FixedNodes { get; set; }
        public double ManhattanMaxDistance { get; set; }
        public int NodesByYGroups { get; set; }
        public int ExistingValidNodes { get; set; }
        public int RelevantNodesFound { get; set; }
        public int MatchingNodesFound { get; set; }
        public int NewNodesCreated { get; set; }
        public int FromNodesAlreadyOk { get; set; }
        public int ToNodesAlreadyOk { get; set; }
        public int FromNodesFixed { get; set; }
        public int ToNodesFixed { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using Extensions.DictionaryExtensions;
using Extensions.IEnumerableExtensions;
using RoadNetworkRouting.Network;

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

        /// <summary>
        /// If the end of one link hits another link (within the given tolerance),
        /// the other link will be split at this point, creating a proper intersection.
        /// </summary>
        /// <param name="router"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static SplitLinksResult SplitLinksAtIntersections(RoadNetworkRouter router, NetworkBuildConfig config)
        {
            config.StateReporter?.Invoke("SplitLinksAtIntersections");

            var newLinks = new List<RoadLink>();
            var res = new SplitLinksResult();

            var maxLinkId = router.Links.Keys.Max() + 1;
            var maxDistance = config.MaxDistanceLinkSplit;

            config.StateReporter?.Invoke("    > Building nearby-link-lookup");
            var lookup = NearbyBoundsCache<RoadLink>.FromBounds(router.Links.Values, p => p.Bounds, 1000);

            config.StateReporter?.Invoke("    > Processing links");

            // For each link ...
            var processed = 0;
            foreach (var link in router.Links.Values)
            {
                var partsOfThisLink = new List<Point3D[]>(new[] { link.Geometry });

                var nearbyLinks = lookup.GetNearbyItems(link.Bounds.Center, (int)Math.Max(link.Bounds.Height, link.Bounds.Width)).ToArray();
                res.NearbyLinksFound += nearbyLinks.Length;
                res.LinkChecksSaved += router.Links.Count - 1 - nearbyLinks.Length;

                // ... check if any other links "hit" it
                foreach (var otherLink in nearbyLinks)
                {
                    if (link.LinkId == otherLink.LinkId) continue;

                    if (!otherLink.Bounds.Overlaps(link.Bounds, maxDistance))
                    {
                        res.LinksSkippedByBounds++;
                        continue;
                    }

                    var newParts = new List<Point3D[]>();
                    foreach (var part in partsOfThisLink)
                    {
                        var nearestPointStart = LineTools.FindNearestPoint(part, otherLink.Geometry[0]);
                        var nearestPointEnd = LineTools.FindNearestPoint(part, otherLink.Geometry[^1]);

                        var partLength = LineTools.CalculateLength(part);

                        var startInside = nearestPointStart.DistanceFromLine <= maxDistance && nearestPointStart.Distance > maxDistance && nearestPointStart.Distance < partLength - maxDistance;
                        var endInside = nearestPointEnd.DistanceFromLine <= maxDistance && Math.Abs(nearestPointStart.Distance - nearestPointEnd.Distance) > maxDistance && nearestPointEnd.Distance < partLength - maxDistance && nearestPointEnd.Distance > maxDistance;

                        if (startInside && endInside)
                        {
                            var firstSplit = LineTools.Split(part, nearestPointStart.Distance);

                            var theOneToSplit = nearestPointEnd.Distance < nearestPointStart.Distance ? firstSplit.Before : firstSplit.After;
                            var splitAt = nearestPointEnd.Distance < nearestPointStart.Distance ? nearestPointEnd.Distance : nearestPointEnd.Distance - nearestPointStart.Distance;
                            var secondSplit = LineTools.Split(theOneToSplit, splitAt);

                            var theOther = nearestPointEnd.Distance < nearestPointStart.Distance ? firstSplit.After : firstSplit.Before;

                            res.SplitTwice++;
                            newParts.Add(secondSplit.Before);
                            newParts.Add(secondSplit.After);
                            newParts.Add(theOther);
                        }
                        else if (startInside)
                        {
                            var (before, after) = LineTools.Split(part, nearestPointStart.Distance);

                            res.SplitByStart++;
                            newParts.Add(before);
                            newParts.Add(after);
                        }
                        else if (endInside)
                        {
                            var (before, after) = LineTools.Split(part, nearestPointEnd.Distance);

                            res.SplitByEnd++;
                            newParts.Add(before);
                            newParts.Add(after);
                        }
                        else
                        {
                            res.NotWithinTolerance++;
                            newParts.Add(part);
                        }
                    }

                    partsOfThisLink = newParts;

                    res.LinksChecked++;
                }

                newLinks.Add(link.Clone(partsOfThisLink[0]));
                newLinks.AddRange(partsOfThisLink.Skip(1).Select(p=>link.Clone(p,maxLinkId++)));
                res.LinksSplitInto.Increment(partsOfThisLink.Count - 1);

                config?.ProgressReporter?.Invoke((++processed) / (double)router.Links.Count);
            }

            router.Links = newLinks.ToDictionary(k => k.LinkId, v => v);
            return res;
        }

        public static FixMissingNodeResult FixMissingNodeIds(RoadNetworkRouter router, NetworkBuildConfig config)
        {
            config.StateReporter?.Invoke("FixMissingNodeIds");

            // Keep track of how many we fixed
            var res = new FixMissingNodeResult();
            var maxDistance = config.MaxDistanceNodeConnection;
            res.ManhattanMaxDistance = 2 * maxDistance;

            config.StateReporter?.Invoke("    > Creating nodesByY");

            // Create a list containing all nodes with their IDs and locations, then store it as a
            // Y-separated dictionary of lists.
            var processed = 0;
            var nodesByY = router.Links
                .SelectMany(p =>
                {
                    config?.ProgressReporter?.Invoke((++processed) / (double)router.Links.Count);
                    return new[]
                    {
                        new Node(p.Value.Geometry.First(), p.Value.FromNodeId),
                        new Node(p.Value.Geometry.Last(), p.Value.ToNodeId)
                    };
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

            config.StateReporter?.Invoke("    > Processing links");

            // Go through each link and find links with missing From/To node IDs.
            // (Missing is defined as equal to int.MinValue, and must be set as this
            // in the corresponding reader function.)
            processed = 0;
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

                config?.ProgressReporter?.Invoke((++processed) / (double)router.Links.Count);
            }

            return res;
        }
    }

    public class SplitLinksResult
    {
        public int LinksChecked { get; set; }
        public int LinksSkippedByBounds { get; set; }
        public Dictionary<int, int> LinksSplitInto { get; set; } = new();
        public int NotWithinTolerance { get; set; }
        public int SplitTwice { get; set; }
        public int SplitByStart { get; set; }
        public int SplitByEnd { get; set; }
        public int NearbyLinksFound { get; set; }
        public long LinkChecksSaved { get; set; }
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
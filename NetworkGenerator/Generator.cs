using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUtilities.ConsoleInfoPanel;
using Newtonsoft.Json;
using EnergyModule.Geometry.SimpleStructures;

namespace NetworkGenerator
{
    public class Generator
    {
        /*public void Generate(string gdbPath, string outputBinPath, int tolerance = 1)
        {
            using (var cip = new ConsoleInformationPanel("Creating network topology ..."))
            {
                var links = new Dictionary<string, GdbRoadLinkData>();
                using (var pb = cip.SetUnknownProgress("Loading links"))
                {
                    foreach (var link in GdbGraphBuilder.ProcessNewTable(gdbPath))
                        links.Add(link.Reference, link);
                }

                foreach (var link in links.Values)
                {
                    link.FromNodeId = -1;
                    link.ToNodeId = -1;
                }

                var nodes = new Dictionary<int, NetworkNode>();

                cip.Set("Link count", links.Count);

                var nodeId = 0;

                bool HasNearbyNode(NetworkNode node)
                {
                    var max = 1000;
                    foreach (var n in nodes.Values)
                    {
                        var dx = n.X - node.X;
                        var dy = n.Y - node.Y;
                        if (n != node && Math.Abs(dy) < max && Math.Abs(dx) < max && Math.Sqrt(dx * dx + dy * dy) < max)
                            return true;
                    }

                    return false;
                }

                foreach (var link in links.Values)
                {
                    if (link.FromNodeId >= 0 && link.FromNodeLocked != true)
                    {
                        if (nodes[link.FromNodeId].Edges < 2)
                        {
                            if (!link.FromNodeLocked.HasValue && !HasNearbyNode(nodes[link.FromNodeId]))
                            {
                                link.FromNodeLocked = true;
                                cip.Increment("Locked FromNodes");
                            }
                            else
                            {
                                link.FromNodeLocked = false;
                                nodes.Remove(link.FromNodeId);
                                link.FromNodeId = -1;
                                link.FromNodeConnectionTolerance = -1;
                                cip.Increment("Reset nodes (" + tolerance + ")");
                            }
                        }
                    }

                    if (link.ToNodeId >= 0 && link.ToNodeLocked != true)
                    {
                        if (nodes[link.ToNodeId].Edges < 2)
                        {
                            if (!link.ToNodeLocked.HasValue && !HasNearbyNode(nodes[link.ToNodeId]))
                            {
                                link.ToNodeLocked = true;
                                cip.Increment("Locked ToNodes");
                            }
                            else
                            {
                                link.ToNodeLocked = false;
                                nodes.Remove(link.ToNodeId);
                                link.ToNodeId = -1;
                                link.ToNodeConnectionTolerance = -1;
                                cip.Increment("Reset nodes (" + tolerance + ")");
                            }
                        }
                    }
                }

                var nodeLookup = new Dictionary<(int, int), List<NetworkNode>>();

                void AddToNodeLookup(NetworkNode n)
                {
                    var key = ((int) n.X, (int) n.Y);
                    if (nodeLookup.TryGetValue(key, out var list))
                        list.Add(n);
                    else
                        nodeLookup.Add(key, new List<NetworkNode>() {n});
                }

                NetworkNode FindOrCreateNode(Point3D p)
                {
                    var r = tolerance + 1;
                    for(var x = p.X - r; x <= p.X + r; x++)
                    for (var y = p.Y - r; y <= p.Y + r; y++)
                    {
                        if (!nodeLookup.TryGetValue(((int) x, (int) y), out var nearbyNodes)) continue;

                        foreach (var n in nearbyNodes)
                        {
                            if (Math.Abs(p.Y - n.Y) <= tolerance && Math.Abs(p.X - n.X) <= tolerance && p.DistanceTo2D(n.X, n.Y) <= tolerance)
                            {
                                n.Edges++;
                                return n;
                            }
                        }
                    }

                    var newNode = new NetworkNode(p.X, p.Y, nodeId++);
                    nodes.Add(newNode.Id, newNode);
                    AddToNodeLookup(newNode);
                    cip.Set("Node count (" + tolerance + ")", nodes.Count);
                    return newNode;
                }

                foreach (var link in cip.Run("Finding nodes (" + tolerance + ")", links.Values))
                {
                    if (link.FromNodeId < 0)
                    {
                        link.FromNodeId = FindOrCreateNode(link.FirstPoint).Id;
                        link.FromNodeConnectionTolerance = tolerance;
                        cip.Increment("Set FromNode (" + tolerance + ")");
                    }

                    if (link.ToNodeId < 0)
                    {
                        link.ToNodeId = FindOrCreateNode(link.LastPoint).Id;
                        link.ToNodeConnectionTolerance = tolerance;
                        cip.Increment("Set ToNode (" + tolerance + ")");
                    }
                }

                RoadNetworkRouter.SaveToLight(outputBinPath, nodes, links, GdbGraphBuilder.ProcessTable(gdbPath));
            }
        }*/
    }
}

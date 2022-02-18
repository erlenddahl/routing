using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ConsoleUtilities;
using ConsoleUtilities.ConsoleInfoPanel;
using DataflowUtilities.ProducerConsumer;
using Newtonsoft.Json;
using RoadNetworkRouting;
using Routing;

namespace RoutingCli
{
    class Program
    {
        static void Main(string[] args)
        {
            args = new[] { @"C:\Code\Routing\test.json" };

            new ConsoleConfigHelper(args)
                .AutoResize()
                .Run("Processing coordinates", ParseConfigFile)
                .PrintSummary();
        }

        private static IRunnable ParseConfigFile(string path)
        {
            return JsonConvert.DeserializeObject<Searcher>(System.IO.File.ReadAllText(path));
        }
    }

    public class Searcher : IRunnable
    {
        public int ThreadCount { get; set; } = Environment.ProcessorCount;
        public string ResultsPath { get; set; }
        public string NetworkPath { get; set; }
        public CoordinateSearch[] Searches { get; set; }

        public void Run()
        {
            using var cip = new ConsoleInformationPanel();

            RoadNetworkRouter router;
            int largestNetworkSegment;
            using (var _ = cip.SetUnknownProgress("Loading router"))
            {
                for (var i = 0; i < Searches.Length; i++)
                    Searches[i].Index = i;

                router = RoadNetworkRouter.LoadFrom(NetworkPath);

                var analysis = GetGraphAnalysis(router, cip);
                router.SetVertexGroups(analysis);
                largestNetworkSegment = analysis.VertexIdGroup
                    .GroupBy(p => p.Value)
                    .Select(p => new { GroupId = p.Key, Count = p.Count() })
                    .OrderByDescending(p => p.Count)
                    .First()
                    .GroupId;
            }

            var results = new SearchResult[Searches.Length];
            var pbStateGeneration = cip.SetProgress("Creating road network graphs", max: ThreadCount);
            using (var pbTotal = cip.SetProgress("Finding shortest routes", max: Searches.Length, started:false))
            {
                var consumers = new StateConsumerCollection<CoordinateSearch, Graph>()
                {
                    ConsumerCount = ThreadCount,
                    MaxBufferItemsPerConsumer = 50,
                    StateGenerator = () =>
                    {
                        var g = router.GetGraph();
                        pbStateGeneration.Increment();
                        if (pbStateGeneration.Current == pbStateGeneration.Max)
                        {
                            pbStateGeneration.Finish();
                            pbTotal.Start();
                        }
                        return g;
                    },
                    ConsumeAction = (search, graph) =>
                    {
                        var source = router.GetNearestVertex(largestNetworkSegment, search.Source[0], search.Source[1]);
                        var target = router.GetNearestVertex(largestNetworkSegment, search.Target[0], search.Target[1]);
                        var res = graph.GetShortestPathQuickly(source.vertex, target.vertex);
                        var links = router.GetLinkReferences(res.Items);

                        results[search.Index] = new SearchResult()
                        {
                            DistanceToSourceVertex = source.distance,
                            DistanceToTargetVertex = target.distance,
                            RouteDistance = links.Sum(p => p.Geometry.Length)
                        };

                        pbTotal.Increment();
                    },
                    OnException = (calc, ex) =>
                    {
                        cip.Increment("Error (" + ex.Message + ")");
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                };

                foreach (var search in Searches)
                    consumers.Post(search);

                consumers.Complete();
            }

            System.IO.File.WriteAllText(ResultsPath, JsonConvert.SerializeObject(results, Formatting.None));
        }

        private static GraphAnalysis GetGraphAnalysis(RoadNetworkRouter router, ConsoleInformationPanel cip)
        {
            using var _ = cip.SetUnknownProgress("Analysing network");

            var graph = router.GetGraph();
            return graph.Analyze();
        }
    }

    public class SearchResult
    {
        public double DistanceToSourceVertex { get; set; }
        public double DistanceToTargetVertex { get; set; }
        public double RouteDistance { get; set; }
        public double TotalDistance => DistanceToSourceVertex + RouteDistance + DistanceToTargetVertex;
    }

    public class CoordinateSearch
    {
        public int Index { get; set; }
        public double[] Source { get; set; }
        public double[] Target { get; set; }
    }
}

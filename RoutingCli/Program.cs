using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ConsoleUtilities;
using ConsoleUtilities.ConsoleInfoPanel;
using DataflowUtilities.ProducerConsumer;
using EnergyModule.Geometry.SimpleStructures;
using Newtonsoft.Json;
using RoadNetworkRouting;
using RoadNetworkRouting.Network;
using Routing;

namespace RoutingCli
{
    class Program
    {
        static void Main(string[] args)
        {
            //args = new[] { @"C:\Code\Routing\test.json" };

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

            cip.Set("Processing threads", ThreadCount);
            cip.Set("Road network", Path.GetFileName(NetworkPath));

            RoadNetworkRouter router;
            using (var _ = cip.SetUnknownProgress("Loading router"))
            {
                for (var i = 0; i < Searches.Length; i++)
                    Searches[i].Index = i;

                router = RoadNetworkRouter.LoadFrom(NetworkPath);
            }

            var results = new RoadNetworkRoutingResult[Searches.Length];
            using (var pbTotal = cip.SetProgress("Finding shortest routes", max: Searches.Length, started:false))
            {
                var consumers = new ConsumerCollection<CoordinateSearch>()
                {
                    ConsumerCount = ThreadCount,
                    MaxBufferItemsPerConsumer = 50,
                    ConsumeAction = search =>
                    {
                        results[search.Index] = router.Search(new Point3D(search.Source[0], search.Source[1]), new Point3D(search.Target[0], search.Target[1]));
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

            File.WriteAllText(ResultsPath, JsonConvert.SerializeObject(results, Formatting.None));
        }
    }

    public class CoordinateSearch
    {
        public int Index { get; set; }
        public double[] Source { get; set; }
        public double[] Target { get; set; }
    }
}

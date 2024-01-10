using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using ConsoleUtilities.ConsoleInfoPanel;
using ConsoleUtilities.ConsoleInfoPanel.Items;
using Extensions.IntExtensions;
using RoadNetworkRouting.Network;

namespace RoadNetworkRouting.Utils
{
    public class RoadInfoEnricher
    {
        private readonly IEnumerable<RoadLink> _links;

        public RoadInfoEnricher(IEnumerable<RoadLink> links)
        {
            _links = links;
        }

        public Task Run(ConsoleInformationPanel cip = null)
        {
            cip ??= new ConsoleInformationPanel();

            var buffer = new BufferBlock<RoadLink>();
            var pb = cip.SetProgress("Reading link data");
            var pbDownload = cip.SetProgress("Downloading lane codes");
            var pbParse = cip.SetProgress("Parsing data");
            var pbUpdate = cip.SetProgress("Storing to database");

            var consumers = 0.To(Environment.ProcessorCount).Select(p => Run(p, cip, pbDownload, pbParse, pbUpdate, buffer)).ToArray();

            foreach (var link in _links)
            {
                pb.Max++;
                buffer.Post(link);
                pb.Current++;
            }

            pbDownload.Max = pbParse.Max = pbUpdate.Max = pb.Max;
            pb.Finish();

            buffer.Complete();

            return Task.WhenAll(consumers);
        }

        private static async Task Run(int id, ConsoleInformationPanel cip, ProgressInfoItem pbDownload, ProgressInfoItem pbParse, ProgressInfoItem pbUpdate, BufferBlock<RoadLink> buffer)
        {
            var limit = new Random((int)DateTime.Now.Ticks + id).Next(500, 1500);
            var segments = new List<RoadLink>();
            using var client = new HttpClient();
            while (await buffer.OutputAvailableAsync())
            {
                while (buffer.TryReceive(out var item))
                {
                    var url = $"http://visveginfo-static.opentns.org/RoadInfoService/GetRoadDataAtNVDBReference?nvdbLinkID={item.LinkId}&linkRelLen={item.FromRelativeLength.ToString(CultureInfo.InvariantCulture)}";
                    pbDownload.Increment();

                    for (var i = 0; i < 3; i++)
                    {
                        try
                        {
                            var xmlString = client.GetStringAsync(url).Result;
                            var xml = XElement.Parse(xmlString.Replace(" xmlns=", " whocares="));

                            var items = xml.Element("RoadDataItems")?.Elements("RoadDataItem").ToArray();

                            item.LaneCode = items?.Select(p => p.Element("RoadReferenceAtLocation")?.Element("LaneCode")?.Value).FirstOrDefault(p => p != null) ?? "";
                            cip.Increment("Success");
                            break;
                        }
                        catch (Exception ex)
                        {
                            cip.Increment("Failure " + i + " (" + ex.Message + ")");
                            Thread.Sleep(3000);
                        }
                    }

                    segments.Add(item);
                    pbParse.Increment();

                    if (segments.Count >= limit)
                    {
                        var c = segments.Count;
                        pbUpdate.Increment(c);
                    }
                }
            }
        }
    }
}

using System.Collections.Generic;
using DotSpatial.Topology.Operation.Buffer;
using Newtonsoft.Json.Linq;

namespace RoadNetworkRouting.Network
{
    public enum RoadLinkDirection
    {
        BothWays,
        AlongGeometry,
        AgainstGeometry,
        None
    }
}
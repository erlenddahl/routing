using System.Threading.Tasks;
using RoadNetworkRouting.Network;

namespace RoadNetworkRouting;

public interface ILinkDataLoader
{
    public RoadLink Load(RoadNetworkRouter router, RoadLink id);
}
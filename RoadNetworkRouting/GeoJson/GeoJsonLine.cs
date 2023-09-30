namespace RoadNetworkRouting.GeoJson;

public class GeoJsonLine : GeoJsonObject
{
    public double[][] Coordinates { get; set; }

    public GeoJsonLine() : base("LineString")
    {

    }

    public override string ToString()
    {
        return GeoJsonCollection.Serialize(this);
    }
}
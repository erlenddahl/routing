namespace RoadNetworkRouting.GeoJson;

public class GeoJsonPoint : GeoJsonObject
{
    public double[] Coordinates { get; set; }

    public GeoJsonPoint() : base("Point")
    {

    }

    public override string ToString()
    {
        return GeoJsonCollection.Serialize(this);
    }
}
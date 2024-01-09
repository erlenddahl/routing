namespace RoadNetworkRouting.GeoJson;

public class GeoJsonPolygon : GeoJsonObject
{
    public double[][][] Coordinates { get; set; }

    public GeoJsonPolygon() : base("Polygon")
    {

    }

    public override string ToString()
    {
        return GeoJsonCollection.Serialize(this);
    }
}
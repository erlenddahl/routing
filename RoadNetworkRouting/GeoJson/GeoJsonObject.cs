using System.Text.Json.Serialization;

namespace RoadNetworkRouting.GeoJson;

[JsonDerivedType(typeof(GeoJsonCollection))]
[JsonDerivedType(typeof(GeoJsonFeature))]
[JsonDerivedType(typeof(GeoJsonLine))]
[JsonDerivedType(typeof(GeoJsonPolygon))]
[JsonDerivedType(typeof(GeoJsonPoint))]
public abstract class GeoJsonObject
{
    public string Type { get; set; }

    protected GeoJsonObject(string type)
    {
        Type = type;
    }
}
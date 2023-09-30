using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnergyModule.Geometry.SimpleStructures;

namespace RoadNetworkRouting.GeoJson;

public class GeoJsonCollection : GeoJsonObject
{
    public GeoJsonFeature[] Features { get; set; }

    public GeoJsonCollection() : base("FeatureCollection")
    {
    }

    public override string ToString()
    {
        return Serialize(this);
    }

    public static string Serialize(object o)
    {
        return JsonSerializer.Serialize(o, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public static GeoJsonCollection From(IEnumerable<Point3D> points, int sourceSrid)
    {
        return From(points.Select(p => GeoJsonFeature.Point(p, sourceSrid)));
    }

    public static GeoJsonCollection From(IEnumerable<GeoJsonFeature> features)
    {
        return new GeoJsonCollection()
        {
            Features = features.ToArray()
        };
    }

    public void WriteTo(string path)
    {
        System.IO.File.WriteAllText(path, ToString());
    }
}
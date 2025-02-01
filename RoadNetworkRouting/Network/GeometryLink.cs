using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using EnergyModule.Road;
using RoadNetworkRouting.Geometry;
using EnergyModule;

namespace RoadNetworkRouting.Network;

public abstract class GeometryLink : ILinkPartGenerator
{
    private IQueryPointInfo _pointInfoQuerier;
    private Point3D[] _geometry;

    private IQueryPointInfo PointCache => _pointInfoQuerier ??= CreatePointInfoQuerier(Geometry);

    public static IQueryPointInfo CreatePointInfoQuerier(Point3D[] geometry)
    {
        return new CachedLineTools(geometry);
    }
    
    /// <summary>
    /// The 3D length of the road link, calculated (on the first call) from its Geometry.
    /// </summary>
    public double LengthM => PointCache.LengthM;

    public Point3D[] Geometry
    {
        get => _geometry;
        set
        {
            _geometry = value;
            _pointInfoQuerier = null;
        }
    }

    public abstract LinkPart[] GenerateLinkParts(double segmentLength = 20);

    public PointInfo GetGeometricData(double metersFromA)
    {
        return PointCache.QueryPointInfo(metersFromA);
    }

    public abstract GeometryLink Clone(Point3D[] newGeometry = null);

    public virtual GeometryLink ConvertCoordinates(CoordinateConverter converter)
    {
        var link = Clone(Geometry.Select(converter.Forward).ToArray());
        return link;
    }
}
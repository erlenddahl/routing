using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using EnergyModule.Road;
using RoadNetworkRouting.Geometry;
using EnergyModule;
using EnergyModule.Results;

namespace RoadNetworkRouting.Network;

public abstract class GeometryLink : ILinkPartGenerator, IQueryPointInfo
{
    private IQueryPointInfo _pointInfoQuerier;
    private Point3D[] _geometry;

    private IQueryPointInfo PointCache => _pointInfoQuerier ??= new CachedLineTools(Geometry, ignoreFirstAndLastAngles: SpeedLimitKmH > 70);

    /// <summary>
    /// The 3D length of the road link, calculated (on the first call) from its Geometry.
    /// </summary>
    public double LengthM => PointCache.LengthM;

    /// <inheritdoc cref="TransportLink.SpeedLimitKmH"/>
    public byte SpeedLimitKmH { get; set; }

    public Point3D[] Geometry
    {
        get => _geometry;
        set
        {
            _geometry = value;
            _pointInfoQuerier = null;
        }
    }

    public abstract LinkData GenerateLinkParts(double segmentLength = 20, SegmentGenerationWarningData continueWarnings = null);

    public PointInfo QueryPointInfo(double atDistance)
    {
        return PointCache.QueryPointInfo(atDistance);
    }

    public abstract GeometryLink Clone(Point3D[] newGeometry = null);

    public virtual GeometryLink ConvertCoordinates(CoordinateConverter converter)
    {
        var link = Clone(Geometry.Select(converter.Forward).ToArray());
        return link;
    }
}
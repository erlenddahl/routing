﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using EnergyModule.Network;
using EnergyModule.Road;
using RoadNetworkRouting.GeoJson;
using RoadNetworkRouting.Geometry;

namespace RoadNetworkRouting.Network;

public class RoadLink : ILinkPartGenerator
{
    private LinkReference _reference;
    private CachedLineTools _pointCache;
    private double? _length;
    private double _fromRelativeLength;
    private double _toRelativeLength;
    private RoadLinkDirection _direction;
    private int _linkId;

    public LinkReference Reference => _reference ??= new LinkReference(LinkId.ToString(), FromRelativeLength, ToRelativeLength, Direction);
    public byte RoadClass { get; set; }

    public int LinkId
    {
        get => _linkId;
        set
        {
            _linkId = value;
            _reference = null;
        }
    }

    public double FromRelativeLength
    {
        get => _fromRelativeLength;
        set
        {
            _fromRelativeLength = value;
            _reference = null;
        }
    }

    public double ToRelativeLength
    {
        get => _toRelativeLength;
        set
        {
            _toRelativeLength = value;
            _reference = null;
        }
    }

    public int FromNodeId { get; set; }
    public int ToNodeId { get; set; }
    public byte SpeedLimit { get; set; }
    public byte SpeedLimitReversed { get; set; }
    public Point3D[] Geometry { get; set; }
    public string LaneCode { get; set; } = "";
    public float Cost { get; set; }
    public float ReverseCost { get; set; }

    public RoadLinkDirection Direction

    {
        get => _direction;
        set {
            _direction = value;
            _reference = null;
        }
    }

    public BoundingBox2D Bounds { get; set; }

    /// <summary>
    /// Which interconnected part of the road network this link belongs to. If this is a connected/complete graph, all links will be in group 0.
    /// If the graph is disconnected, all links in each sub group will share NetworkGroup.
    /// </summary>
    public int NetworkGroup { get; set; } = -1;

    /// <summary>
    /// The 3D length of the road link, calculated (on the first call) from its Geometry.
    /// </summary>
    public double Length => _length ??= LineTools.CalculateLength(Geometry);

    public RoadLink Clone(Point3D[] newGeometry = null)
    {
        return new RoadLink()
        {
            RoadClass = RoadClass,
            LinkId = LinkId,
            FromRelativeLength = FromRelativeLength,
            ToRelativeLength = ToRelativeLength,
            FromNodeId = FromNodeId,
            ToNodeId = ToNodeId,
            Direction = Direction,
            SpeedLimit = SpeedLimit,
            SpeedLimitReversed = SpeedLimitReversed,
            Geometry = newGeometry ?? Geometry,
            LaneCode = LaneCode,
            Cost = Cost,
            ReverseCost = ReverseCost,
            Bounds = Bounds,
            NetworkGroup =  NetworkGroup,
            _reference = _reference
        };
    }

    public override string ToString()
    {
        var c = Math.Abs(Cost - double.MaxValue) < 0.000001 ? "INF" : Cost.ToString("n2");
        var rc = Math.Abs(ReverseCost - double.MaxValue) < 0.000001 ? "INF" : ReverseCost.ToString("n2");
        return $"Id={LinkId}, Cost={c} / {rc}";
    }

    public PointInfo GetGeometricData(double metersFromA)
    {
        if (_pointCache == null)
            _pointCache = new CachedLineTools(Geometry);
        return _pointCache.QueryPointInfo(metersFromA);
    }

    public LinkPart[] GenerateLinkParts(double segmentLength = TransportLink.StandardSegmentLength)
    {
        var partIndex = 0;

        // Tangent at start of segment:
        var start = GetGeometricData(0);
        var lastKnownHeight = 0.0;
        var linkParts = new LinkPart[(int)Math.Ceiling(_pointCache.Length / segmentLength)];

        for (var posStart = 0d; posStart < _pointCache.Length; posStart += segmentLength)
        {
            //Create the new TransportLinkPart
            var tlp = new LinkPart
            {
                LinkId = LinkId,
                Width = 8, //TODO: Fix!
                LaneInfo = LaneReader.Parse("1#2"), //TODO: Fix!
                NodeA = FromNodeId,
                NodeB = ToNodeId,
                IsFerry = false, //TODO: Fix!
                IsRoundabout = false, //TODO: Fix!
                SpeedLimitKmH = SpeedLimit,
                VehiclesPerHour = 0,
                RoadType = 'r', //TODO: Fix!
                PartIndex = partIndex++
            };

            // Calculate how far into the TransportLink this part will end
            var posEnd = Math.Min(posStart + segmentLength, _pointCache.Length);
            var end = GetGeometricData(posEnd);

            // Check height values
            TransportLink.CheckHeightValue(tlp, start, ref lastKnownHeight);
            TransportLink.CheckHeightValue(tlp, end, ref lastKnownHeight);

            // Update and store list element:   
            tlp.HorizontalRadius = TransportLink.FindRadius(segmentLength, start.Angle, end.Angle);
            tlp.GradientPercent = 100 * (end.Z - start.Z) / segmentLength;
            tlp.SegmentLengthM = posEnd - posStart;
            tlp.Z1 = start.Z;
            tlp.Z2 = end.Z;
            tlp.X1 = start.X;
            tlp.X2 = end.X;
            tlp.Y1 = start.Y;
            tlp.Y2 = end.Y;
            tlp.EndsAt = posEnd; // Used locally. Will later be replaced by length out value relative entire route!

            linkParts[tlp.PartIndex] = tlp;

            // Prepare data for next linkPart:
            start = end;
        }

        return linkParts;
    }

    public static RoadLinkDirection DirectionFromString(string direction)
    {
        switch (direction.ToLower())
        {
            case "b":
                return RoadLinkDirection.BothWays;
            case "ft":
                return RoadLinkDirection.AlongGeometry;
            case "tf":
                return RoadLinkDirection.AgainstGeometry;
            case "n":
                return RoadLinkDirection.None;
        }

        throw new Exception("Unknown road link direction: '" + direction + "' (must be B, FT, TF, or N).");
    }

    public void WriteTo(BinaryWriter writer, Dictionary<string, int> strings, bool writePoints)
    {
        writer.Write(LinkId);

        writer.Write(writePoints ? Geometry.Length : 0);

        writer.Write((byte)Direction);
        writer.Write(RoadClass);
        writer.Write(FromRelativeLength);
        writer.Write(ToRelativeLength);
        writer.Write(FromNodeId);
        writer.Write(ToNodeId);
        writer.Write(NetworkGroup);
        writer.Write(SpeedLimit);
        writer.Write(SpeedLimitReversed);
        writer.Write(Cost);
        writer.Write(ReverseCost);
        writer.Write(strings[LaneCode ?? ""]);

        if (!writePoints) return;
        foreach (var p in Geometry)
        {
            writer.Write(p.X);
            writer.Write(p.Y);
            writer.Write(p.Z);
        }
    }

    public void ReadFrom(BinaryReader reader, Dictionary<int, string> strings, byte[] buffer)
    {
        LinkId = reader.ReadInt32();

        // Fetch the point count, and calculate the length of the rest of this link object, and read it all in at the same time
        var pointCount = reader.ReadInt32();

        var itemSize = CalculateItemSize(pointCount);

        reader.Read(buffer, 0, itemSize);

        Direction = (RoadLinkDirection)buffer[0];
        RoadClass = buffer[1];
        FromRelativeLength = BitConverter.ToDouble(buffer, 2);
        ToRelativeLength = BitConverter.ToDouble(buffer, 10);
        FromNodeId = BitConverter.ToInt32(buffer, 18);
        ToNodeId = BitConverter.ToInt32(buffer, 22);
        NetworkGroup = BitConverter.ToInt32(buffer, 26);
        SpeedLimit = buffer[30];
        SpeedLimitReversed = buffer[31];
        Cost = BitConverter.ToSingle(buffer, 32);
        ReverseCost = BitConverter.ToSingle(buffer, 36);
        LaneCode = strings[BitConverter.ToInt32(buffer, 40)];

        // Update the position to the end of the normal properties, and read all points
        var pos = 44;
        Geometry = new Point3D[pointCount];
        Bounds = BoundingBox2D.Empty();
        for (var j = 0; j < pointCount; j++)
        {
            var point = new Point3D(BitConverter.ToDouble(buffer, pos), BitConverter.ToDouble(buffer, pos + 8), BitConverter.ToDouble(buffer, pos + 16));
            Geometry[j] = point;
            Bounds.ExtendSelf(point);
            pos += 24;
        }
    }

    public static int CalculateItemSize(int pointCount)
    {
        return 1 + 1 + 8 + 8 + 4 + 4 + 4 + 1 + 1 + 4 + 4 + 4 + pointCount * (8 + 8 + 8);
    }

    public RoadLink ConvertCoordinates(CoordinateConverter converter)
    {
        var link = Clone(Geometry.Select(converter.Forward).ToArray());
        link.Bounds = null;
        return link;
    }

    public string ToCsharp()
    {
        return @"new RoadLink()
        {
            RoadClass = " + RoadClass + @",
            LinkId = " + LinkId + @",
            FromRelativeLength = " + FromRelativeLength.ToString(CultureInfo.InvariantCulture) + @",
            ToRelativeLength = " + ToRelativeLength.ToString(CultureInfo.InvariantCulture) + @",
            FromNodeId = " + FromNodeId + @",
            ToNodeId = " + ToNodeId + @",
            Direction = RoadLinkDirection." + Direction.ToString() + @",
            SpeedLimit = " + SpeedLimit + @",
            SpeedLimitReversed = " + SpeedLimitReversed + @",
            Geometry = new[]{" + string.Join(", ", Geometry.Select(ToCsharp)) + @"},
            LaneCode = """ + LaneCode + @""",
            Cost = " + Cost.ToString(CultureInfo.InvariantCulture) + @",
            NetworkGroup = " + NetworkGroup + @",
            ReverseCost = " + ReverseCost.ToString(CultureInfo.InvariantCulture) + @"
        };";
    }

    public GeoJsonFeature ToGeoJsonFeature()
    {
        return GeoJsonFeature.LineString(Geometry, 32633, new
        {
            RoadClass,
            Length,
            Cost,
            ReverseCost,
            Direction,
            FromNodeId,
            ToNodeId,
            FromRelativeLength,
            ToRelativeLength,
            LinkId,
            Reference.Id,
            NetworkGroup
        });
    }

    private string ToCsharp(Point3D p)
    {
        return $"new Point3D({p.X.ToString(CultureInfo.InvariantCulture)}, {p.Y.ToString(CultureInfo.InvariantCulture)}, {p.Z.ToString(CultureInfo.InvariantCulture)})";
    }
}
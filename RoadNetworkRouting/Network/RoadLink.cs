﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using EnergyModule.Geometry.SimpleStructures;
using EnergyModule.Network;
using EnergyModule.Road;
using EnergyModule.Utils;
using RoadNetworkRouting.GeoJson;
using RoadNetworkRouting.Geometry;

namespace RoadNetworkRouting.Network;

public class RoadLink : GeometryLink
{
    private LinkReference _reference;
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
    public byte SpeedLimitKmHReversed { get; set; }
    public string LaneCode { get; set; } = "";
    public float Cost { get; set; }
    public float ReverseCost { get; set; }
    public float RoadWidth { get; set; }
    public bool IsFerry { get; set; }
    public bool IsRoundabout { get; set; }
    public bool IsBridge { get; set; }
    public bool IsTunnel { get; set; }

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

    public RoadLink Clone(Point3D[] newGeometry, int newLinkId)
    {
        var clone = Clone(newGeometry);
        clone.LinkId = newLinkId;
        return clone;
    }

    public override RoadLink Clone(Point3D[] newGeometry = null)
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
            SpeedLimitKmH = SpeedLimitKmH,
            SpeedLimitKmHReversed = SpeedLimitKmHReversed,
            Geometry = newGeometry ?? Geometry,
            LaneCode = LaneCode,
            Cost = Cost,
            ReverseCost = ReverseCost,
            Bounds = Bounds,
            NetworkGroup =  NetworkGroup,
            _reference = _reference,
            IsFerry = IsFerry,
            IsRoundabout = IsRoundabout,
            RoadWidth = RoadWidth
        };
    }

    public override string ToString()
    {
        var c = Math.Abs(Cost - double.MaxValue) < 0.000001 ? "INF" : Cost.ToString("n2");
        var rc = Math.Abs(ReverseCost - double.MaxValue) < 0.000001 ? "INF" : ReverseCost.ToString("n2");
        return $"Id={LinkId}, Cost={c} / {rc}";
    }

    public override LinkData GenerateLinkParts(double segmentLength = TransportLink.StandardSegmentLength)
    {
        var data = new LinkData(LengthM, segmentLength);
        data.Generate(this, new LinkPart
        {
            LinkId = LinkId,
            WidthM = RoadWidth,
            LaneInfo = LaneReader.TryOrGuess(LaneCode),
            NodeA = FromNodeId,
            NodeB = ToNodeId,
            IsFerry = IsFerry,
            IsRoundabout = IsRoundabout,
            IsTunnel = IsTunnel,
            IsBridge = IsBridge,
            SpeedLimitKmH = SpeedLimitKmH,
            VehiclesPerHour = 0,
            RoadType = 'r' //TODO: Fix!
        });

        return data;
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
        writer.Write(SpeedLimitKmH);
        writer.Write(SpeedLimitKmHReversed);
        writer.Write(Cost);
        writer.Write(ReverseCost);
        writer.Write(strings[LaneCode ?? ""]);
        writer.Write(RoadWidth);

        byte flags = 0;
        flags |= (byte)((IsFerry ? 1 : 0) << 0);       // Set bit 0 for IsFerry
        flags |= (byte)((IsRoundabout ? 1 : 0) << 1);  // Set bit 1 for IsRoundabout
        flags |= (byte)((IsBridge ? 1 : 0) << 2);      // Set bit 2 for IsBridge
        flags |= (byte)((IsTunnel ? 1 : 0) << 3);      // Set bit 3 for IsTunnel
        writer.Write(flags);

        if (!writePoints) return;
        foreach (var p in Geometry)
        {
            writer.Write(p.X);
            writer.Write(p.Y);
            writer.Write(p.Z);
        }
    }

    public void ReadFrom(BinaryReader reader, Dictionary<int, string> strings, byte[] buffer, int formatVersion)
    {
        LinkId = reader.ReadInt32();

        // Fetch the point count, and calculate the length of the rest of this link object, and read it all in at the same time
        var pointCount = reader.ReadInt32();

        var itemSize = CalculateItemSize(pointCount, formatVersion);

        reader.Read(buffer, 0, itemSize);

        Direction = (RoadLinkDirection)buffer[0];
        RoadClass = buffer[1];
        FromRelativeLength = BitConverter.ToDouble(buffer, 2);
        ToRelativeLength = BitConverter.ToDouble(buffer, 10);
        FromNodeId = BitConverter.ToInt32(buffer, 18);
        ToNodeId = BitConverter.ToInt32(buffer, 22);
        NetworkGroup = BitConverter.ToInt32(buffer, 26);
        SpeedLimitKmH = buffer[30];
        SpeedLimitKmHReversed = buffer[31];
        Cost = BitConverter.ToSingle(buffer, 32);
        ReverseCost = BitConverter.ToSingle(buffer, 36);
        LaneCode = strings[BitConverter.ToInt32(buffer, 40)];

        // Update the position to the end of the normal properties, and read all points
        var pos = 44;

        if (formatVersion >= 4)
        {
            RoadWidth = BitConverter.ToSingle(buffer, 44);

            var flags = buffer[pos + 4];

            IsFerry = (flags & (1 << 0)) != 0;       // Read bit 0 for IsFerry
            IsRoundabout = (flags & (1 << 1)) != 0;  // Read bit 1 for IsRoundabout
            IsBridge = (flags & (1 << 2)) != 0;      // Read bit 2 for IsBridge
            IsTunnel = (flags & (1 << 3)) != 0;      // Read bit 3 for IsTunnel

            pos += 5;
        }

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

    public static int CalculateItemSize(int pointCount, int formatVersion)
    {
        var size = 1 + 1 + 8 + 8 + 4 + 4 + 4 + 1 + 1 + 4 + 4 + 4 + pointCount * (8 + 8 + 8);
        if (formatVersion >= 4)
        {
            size += 4 + 1;
        }

        return size;
    }

    public override RoadLink ConvertCoordinates(CoordinateConverter converter)
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
            SpeedLimit = " + SpeedLimitKmH + @",
            SpeedLimitReversed = " + SpeedLimitKmHReversed + @",
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
            Length = LengthM,
            Cost,
            ReverseCost,
            Direction,
            FromNodeId,
            ToNodeId,
            FromRelativeLength,
            ToRelativeLength,
            LinkId,
            Reference.Id,
            NetworkGroup,
            SpeedLimit = SpeedLimitKmH,
            SpeedLimitReversed = SpeedLimitKmHReversed
        });
    }

    private string ToCsharp(Point3D p)
    {
        return $"new Point3D({p.X.ToString(CultureInfo.InvariantCulture)}, {p.Y.ToString(CultureInfo.InvariantCulture)}, {p.Z.ToString(CultureInfo.InvariantCulture)})";
    }
}
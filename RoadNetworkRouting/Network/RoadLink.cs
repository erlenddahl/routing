using System;
using System.Globalization;
using System.IO;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;

namespace RoadNetworkRouting.Network;

public class RoadLink
{
    public string Reference => $"{FromRelativeLength.ToString("n8", CultureInfo.InvariantCulture).Replace(".", ",")}-{ToRelativeLength.ToString("n8", CultureInfo.InvariantCulture).Replace(".", ",")}@{LinkId}";
    public int RoadClass { get; set; }
    public int LinkId { get; set; }
    public double FromRelativeLength { get; set; }
    public double ToRelativeLength { get; set; }
    public int FromNodeId { get; set; }
    public int ToNodeId { get; set; }
    public short RoadNumber { get; set; }
    public short SpeedLimit { get; set; }
    public short SpeedLimitReversed { get; set; }
    public string RoadType { get; set; } = "";
    public PolyLineZ Geometry { get; set; }
    public string LaneCode { get; set; } = "";
    public double Cost { get; set; }
    public double ReverseCost { get; set; }
    public RoadLinkDirection Direction { get; set; }

    public BoundingBox2D Bounds { get; set; }

    /// <summary>
    /// Which interconnected part of the road network this link belongs to. If this is a connected/complete graph, all links will be in group 0.
    /// If the graph is disconnected, all links in each sub group will share NetworkGroup.
    /// </summary>
    public int NetworkGroup { get; set; } = -1;

    public RoadLink Clone(PolyLineZ newGeometry = null)
    {
        return new RoadLink()
        {
            RoadClass = RoadClass,
            LinkId = LinkId,
            FromRelativeLength = FromRelativeLength,
            ToRelativeLength = ToRelativeLength,
            FromNodeId = FromNodeId,
            ToNodeId = ToNodeId,
            RoadNumber = RoadNumber,
            Direction = Direction,
            SpeedLimit = SpeedLimit,
            SpeedLimitReversed = SpeedLimitReversed,
            RoadType = RoadType,
            Geometry = newGeometry ?? Geometry,
            LaneCode = LaneCode,
            Cost = Cost,
            ReverseCost = ReverseCost,
            Bounds = Bounds
        };
    }

    public override string ToString()
    {
        var c = Math.Abs(Cost - double.MaxValue) < 0.000001 ? "INF" : Cost.ToString("n2");
        var rc = Math.Abs(ReverseCost - double.MaxValue) < 0.000001 ? "INF" : ReverseCost.ToString("n2");
        return $"Id={LinkId}, Cost={c} / {rc}";
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

    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(LinkId);

        writer.Write(Geometry.Points.Length);

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

        foreach (var p in Geometry.Points)
        {
            writer.Write(p.X);
            writer.Write(p.Y);
            writer.Write(p.Z);
        }

        writer.Write(RoadType);
        writer.Write(LaneCode ?? "");
    }

    public void ReadFrom(BinaryReader reader, bool skipLinkId = false)
    {
        if (!skipLinkId) LinkId = reader.ReadInt32();

        // Fetch the point count, and calculate the length of the rest of this link object, and read it all in at the same time
        var pointCount = reader.ReadInt32();

        var itemSize = 1 + 4 + 8 + 8 + 4 + 4 + 4 + 2 + 2 + 8 + 8 + pointCount * (8 + 8 + 8);

        var buffer = new byte[itemSize];
        reader.Read(buffer, 0, buffer.Length);

        // Then read the dynamic strings.
        RoadType = reader.ReadString();
        LaneCode = reader.ReadString();

        // Read all the normal properties from the already read byte array

        Direction = (RoadLinkDirection)buffer[0];
        RoadClass = BitConverter.ToInt32(buffer, 1);
        FromRelativeLength = BitConverter.ToDouble(buffer, 5);
        ToRelativeLength = BitConverter.ToDouble(buffer, 13);
        FromNodeId = BitConverter.ToInt32(buffer, 21);
        ToNodeId = BitConverter.ToInt32(buffer, 25);
        NetworkGroup = BitConverter.ToInt32(buffer, 29);
        SpeedLimit = BitConverter.ToInt16(buffer, 33);
        SpeedLimitReversed = BitConverter.ToInt16(buffer, 35);
        Cost = BitConverter.ToDouble(buffer, 37);
        ReverseCost = BitConverter.ToDouble(buffer, 45);

        // Update the position to the end of the normal properties, and read all points
        var pos = 53;
        var points = new Point3D[pointCount];
        for (var j = 0; j < pointCount; j++)
        {
            points[j] = new Point3D(BitConverter.ToDouble(buffer, pos), BitConverter.ToDouble(buffer, pos + 8), BitConverter.ToDouble(buffer, pos + 16));
            pos += 24;
        }

        // Initialize a polyline from the read points
        Geometry = new PolyLineZ(points, false);
        Bounds = BoundingBox2D.FromPoints(Geometry.Points);
    }
}
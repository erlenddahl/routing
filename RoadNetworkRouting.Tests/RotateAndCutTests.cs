using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using EnergyModule.Network;
using RoadNetworkRouting.Network;
using RoadNetworkRouting.Service;

namespace RoadNetworkRouting.Tests;

[TestClass]
public class RotateAndCutTests : RoadNetworkRouter
{
    public RotateAndCutTests() : base(Array.Empty<RoadLink>())
    {

    }

    private static void AssertNodeIdsAndLength(RoadLink link, int from, int to, double length)
    {
        Assert.AreEqual(from, link.FromNodeId);
        Assert.AreEqual(to, link.ToNodeId);
        Assert.AreEqual(length, link.Length, 0.5);
    }

    private static void AssertXCoordinates(RoadLink link, params double[] xs)
    {
        Assert.AreEqual(xs.Length, link.Geometry.Length);
        for (var i = 0; i < xs.Length; i++)
            Assert.AreEqual(xs[i], link.Geometry[i].X, 0.5);
    }

    private static void AssertReference(RoadLink link, double fromRel, double toRel, int linkId, string reference, RoadLinkDirection direction)
    {
        Assert.AreEqual(fromRel, link.FromRelativeLength);
        Assert.AreEqual(toRel, link.ToRelativeLength);
        Assert.AreEqual(linkId, link.LinkId);
        Assert.AreEqual(reference, link.Reference.DbId);
        Assert.AreEqual(direction, link.Direction);
    }

    [TestMethod]
    public void Original_Unmodified_Cut()
    {
        var original = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) },
            FromRelativeLength = 0,
            ToRelativeLength = 1,
            LinkId = 123,
            Direction = RoadLinkDirection.AlongGeometry
        };

        var links = new[]
        {
            original
        };

        RotateAndCut(links, 7, 2, 17);

        // The modified link should have new values
        AssertNodeIdsAndLength(links[0], 7, 13, 15);
        AssertXCoordinates(links[0], 17, 25, 32);
        AssertReference(links[0], 0.1, 0.85, 123, "0,1-0,85@123", RoadLinkDirection.AlongGeometry);

        // While the original link should be unchanged
        AssertNodeIdsAndLength(original, 7, 13, 20);
        AssertXCoordinates(original, 15, 25, 35);
        AssertReference(original, 0, 1, 123, "0-1@123", RoadLinkDirection.AlongGeometry);
    }

    [TestMethod]
    public void Original_Unmodified_Reversal()
    {
        var original = new RoadLink()
        {
            FromNodeId = 13,
            ToNodeId = 7,
            Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) },
            FromRelativeLength = 0,
            ToRelativeLength = 1,
            LinkId = 123,
            Direction = RoadLinkDirection.AlongGeometry
        };
        var links = new[]
        {
            original
        };

        RotateAndCut(links, 7, 20, 0);

        // The modified link should have new values
        AssertNodeIdsAndLength(links[0], 7, 13, 20);
        AssertXCoordinates(links[0], 35, 25, 15);
        AssertReference(links[0], 0, 1, 123, "0-1@123", RoadLinkDirection.AlongGeometry);

        // While the original link should be unchanged
        AssertNodeIdsAndLength(original, 13, 7, 20);
        AssertXCoordinates(original, 15, 25, 35);
        AssertReference(original, 0, 1, 123, "0-1@123", RoadLinkDirection.AlongGeometry);
    }

    [TestMethod]
    public void SingleLink_NoModifications()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 7,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            }
        };

        RotateAndCut(links, 7, 0, 20);
        AssertNodeIdsAndLength(links[0], 7, 13, 20);
        AssertXCoordinates(links[0], 15, 25, 35);
        AssertReference(links[0], 0, 1, 123, "0-1@123", RoadLinkDirection.AlongGeometry);
    }

    [TestMethod]
    public void SingleLink_Reversed()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 13,
                ToNodeId = 7,
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            }
        };

        RotateAndCut(links, 7, 20, 0);
        AssertNodeIdsAndLength(links[0], 7, 13, 20);
        AssertXCoordinates(links[0], 35, 25, 15);
        AssertReference(links[0], 0, 1, 123, "0-1@123", RoadLinkDirection.AlongGeometry);
    }

    [TestMethod]
    public void SingleLink_CutStart()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 7,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            }
        };

        RotateAndCut(links, 7, 3, 20);
        AssertNodeIdsAndLength(links[0], 7, 13, 17);
        AssertXCoordinates(links[0], 18, 25, 35);
        AssertReference(links[0], 0.15, 1, 123, "0,15-1@123", RoadLinkDirection.AlongGeometry);
    }

    [TestMethod]
    public void SingleLink_CutEnd()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 7,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            }
        };

        RotateAndCut(links, 7, 0, 17);
        AssertNodeIdsAndLength(links[0], 7, 13, 17);
        AssertXCoordinates(links[0], 15, 25, 32);
        AssertReference(links[0], 0, 0.85, 123, "0-0,85@123", RoadLinkDirection.AlongGeometry);
    }

    [TestMethod]
    public void SingleLink_CutBoth()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 7,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            }
        };

        RotateAndCut(links, 7, 2, 17);
        AssertNodeIdsAndLength(links[0], 7, 13, 15);
        AssertXCoordinates(links[0], 17, 25, 32);
        AssertReference(links[0], 0.1, 0.85, 123, "0,1-0,85@123", RoadLinkDirection.AlongGeometry);
    }

    [TestMethod]
    public void SingleLink_Reversed_CutStart()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 13,
                ToNodeId = 7,
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            }
        };

        RotateAndCut(links, 7, 20, 2);
        AssertNodeIdsAndLength(links[0], 7, 13, 18);
        AssertXCoordinates(links[0], 35, 25, 17);
        AssertReference(links[0], 0, 0.9, 123, "0-0,9@123", RoadLinkDirection.AlongGeometry);
    }

    [TestMethod]
    public void SingleLink_Reversed_CutEnd()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 13,
                ToNodeId = 7,
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            }
        };

        RotateAndCut(links, 7, 17, 0);
        AssertNodeIdsAndLength(links[0], 7, 13, 17);
        AssertXCoordinates(links[0], 32, 25, 15);
        AssertReference(links[0], 0.15, 1, 123, "0,15-1@123", RoadLinkDirection.AlongGeometry);
    }

    [TestMethod]
    public void SingleLink_Reversed_CutBoth()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 13,
                ToNodeId = 7,
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            }
        };

        RotateAndCut(links, 7, 17, 2);
        AssertNodeIdsAndLength(links[0], 7, 13, 15);
        AssertXCoordinates(links[0], 32, 25, 17);
        AssertReference(links[0], 0.15, 0.9, 123, "0,15-0,9@123", RoadLinkDirection.AlongGeometry);
    }

    [TestMethod]
    public void ThreeLinks_RotateTwoFirst()
    {
        var links = new RoadLink[]
        {
            new()
            {
                FromNodeId = 134,
                ToNodeId = 003,
                Geometry = new[] { new Point3D(271204.6, 7040349, 78.236), new Point3D(271212, 7040344.2, 78.336), new Point3D(271259.7, 7040286.4, 79.136) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            },
            new()
            {
                FromNodeId = 807,
                ToNodeId = 134,
                Geometry = new[] { new Point3D(271190.8, 7040365.2, 78.036), new Point3D(271204.6, 7040349, 78.236) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            },
            new()
            {
                FromNodeId = 807,
                ToNodeId = 808,
                Geometry = new[] { new Point3D(271190.8, 7040365.2, 78.036), new Point3D(271157.2, 7040327.5, 72.936) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            }
        };

        RotateAndCut(links, 7, double.MaxValue, double.MaxValue);

        // Link 1 has been rotated.
        AssertNodeIdsAndLength(links[0], 3, 134, 84);
        AssertXCoordinates(links[0], 271259.7, 271212, 271204.6);

        // Link 2 has been rotated.
        AssertNodeIdsAndLength(links[1], 134, 807, 21);
        AssertXCoordinates(links[1], 271204.6, 271190.8);

        // Link 3 is untouched.
        AssertNodeIdsAndLength(links[2], 807, 808, 51);
        AssertXCoordinates(links[2], 271190.8, 271157.2);

        Assert.AreEqual(156, links.Sum(p => p.Length), 1);
        Assert.AreEqual(156, LineTools.CalculateLength(links.SelectMany(p => p.Geometry).ToArray()), 1);
    }

    [TestMethod]
    public void ThreeLinks_RotateTwoFirst_CutFirstAndLast()
    {
        var links = new RoadLink[]
        {
            new()
            {
                FromNodeId = 134,
                ToNodeId = 003,
                Geometry = new[] { new Point3D(271204.6, 7040349, 78.236), new Point3D(271212, 7040344.2, 78.336), new Point3D(271259.7, 7040286.4, 79.136) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            },
            new()
            {
                FromNodeId = 807,
                ToNodeId = 134,
                Geometry = new[] { new Point3D(271190.8, 7040365.2, 78.036), new Point3D(271204.6, 7040349, 78.236) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            },
            new()
            {
                FromNodeId = 807,
                ToNodeId = 808,
                Geometry = new[] { new Point3D(271190.8, 7040365.2, 78.036), new Point3D(271157.2, 7040327.5, 72.936) },
                FromRelativeLength = 0,
                ToRelativeLength = 1,
                LinkId = 123,
                Direction = RoadLinkDirection.AlongGeometry
            }
        };

        RotateAndCut(links, 7, 25.878567945574169, 14.553077179694942);

        // Link 1 has been rotated.
        AssertNodeIdsAndLength(links[0], 3, 134, 26);

        Assert.AreEqual(3, links[0].Geometry.Length);
        AssertXCoordinates(links[0], 271223, 271212, 271204.6);

        // Link 2 has been rotated.
        AssertNodeIdsAndLength(links[1], 134, 807, 21);
        AssertXCoordinates(links[1], 271204.6, 271190.8);

        // Link 3 is untouched.
        AssertNodeIdsAndLength(links[2], 807, 808, 15);
        AssertXCoordinates(links[2], 271190.8, 271181);

        Assert.AreEqual(62, links.Sum(p => p.Length), 1);
        Assert.AreEqual(62, LineTools.CalculateLength(links.SelectMany(p => p.Geometry).ToArray()), 1);
    }
}
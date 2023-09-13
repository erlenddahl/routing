using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using EnergyModule.Network;
using RoadNetworkRouting.Network;

namespace RoadNetworkRouting.Tests;

[TestClass]
public class RotateAndCutTests : RoadNetworkRouter
{
    public RotateAndCutTests() : base(Array.Empty<RoadLink>())
    {

    }

    [TestMethod]
    public void Cut_Original_Unmodified()
    {
        var link = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) }
        };

        var links = new[]
        {
            link
        };

        RotateAndCut(links, 7, 2, 17);

        Assert.AreEqual(7, links[0].FromNodeId);
        Assert.AreEqual(13, links[0].ToNodeId);

        Assert.AreEqual(15, links[0].Length);

        Assert.AreEqual(17, links[0].Geometry[0].X);
        Assert.AreEqual(25, links[0].Geometry[1].X);
        Assert.AreEqual(32, links[0].Geometry[2].X);


        Assert.AreEqual(7, link.FromNodeId);
        Assert.AreEqual(13, link.ToNodeId);

        Assert.AreEqual(20, link.Length);

        Assert.AreEqual(15, link.Geometry[0].X);
        Assert.AreEqual(25, link.Geometry[1].X);
        Assert.AreEqual(35, link.Geometry[2].X);
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
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) }
            }
        };

        RotateAndCut(links, 7, 0, 20);
        Assert.AreEqual(7, links[0].FromNodeId);
        Assert.AreEqual(13, links[0].ToNodeId);

        Assert.AreEqual(20, links[0].Length);

        Assert.AreEqual(15, links[0].Geometry[0].X);
        Assert.AreEqual(25, links[0].Geometry[1].X);
        Assert.AreEqual(35, links[0].Geometry[2].X);
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
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) }
            }
        };

        RotateAndCut(links, 7, 20, 0);
        Assert.AreEqual(7, links[0].FromNodeId);
        Assert.AreEqual(13, links[0].ToNodeId);

        Assert.AreEqual(20, links[0].Length);

        Assert.AreEqual(35, links[0].Geometry[0].X);
        Assert.AreEqual(25, links[0].Geometry[1].X);
        Assert.AreEqual(15, links[0].Geometry[2].X);
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
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) }
            }
        };

        RotateAndCut(links, 7, 3, 20);
        Assert.AreEqual(7, links[0].FromNodeId);
        Assert.AreEqual(13, links[0].ToNodeId);

        Assert.AreEqual(17, links[0].Length);

        Assert.AreEqual(18, links[0].Geometry[0].X);
        Assert.AreEqual(25, links[0].Geometry[1].X);
        Assert.AreEqual(35, links[0].Geometry[2].X);
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
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) }
            }
        };

        RotateAndCut(links, 7, 0, 17);
        Assert.AreEqual(7, links[0].FromNodeId);
        Assert.AreEqual(13, links[0].ToNodeId);

        Assert.AreEqual(17, links[0].Length);

        Assert.AreEqual(15, links[0].Geometry[0].X);
        Assert.AreEqual(25, links[0].Geometry[1].X);
        Assert.AreEqual(32, links[0].Geometry[2].X);
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
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) }
            }
        };

        RotateAndCut(links, 7, 2, 17);
        Assert.AreEqual(7, links[0].FromNodeId);
        Assert.AreEqual(13, links[0].ToNodeId);

        Assert.AreEqual(15, links[0].Length);

        Assert.AreEqual(17, links[0].Geometry[0].X);
        Assert.AreEqual(25, links[0].Geometry[1].X);
        Assert.AreEqual(32, links[0].Geometry[2].X);
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
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) }
            }
        };

        RotateAndCut(links, 7, 20, 2);
        Assert.AreEqual(7, links[0].FromNodeId);
        Assert.AreEqual(13, links[0].ToNodeId);

        Assert.AreEqual(18, links[0].Length);

        Assert.AreEqual(35, links[0].Geometry[0].X);
        Assert.AreEqual(25, links[0].Geometry[1].X);
        Assert.AreEqual(17, links[0].Geometry[2].X);
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
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) }
            }
        };

        RotateAndCut(links, 7, 17, 0);
        Assert.AreEqual(7, links[0].FromNodeId);
        Assert.AreEqual(13, links[0].ToNodeId);

        Assert.AreEqual(17, links[0].Length);

        Assert.AreEqual(32, links[0].Geometry[0].X);
        Assert.AreEqual(25, links[0].Geometry[1].X);
        Assert.AreEqual(15, links[0].Geometry[2].X);
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
                Geometry = new[] { new Point3D(15, 0), new Point3D(25, 0), new Point3D(35, 0) }
            }
        };

        RotateAndCut(links, 7, 17, 2);
        Assert.AreEqual(7, links[0].FromNodeId);
        Assert.AreEqual(13, links[0].ToNodeId);

        Assert.AreEqual(15, links[0].Length);

        Assert.AreEqual(32, links[0].Geometry[0].X);
        Assert.AreEqual(25, links[0].Geometry[1].X);
        Assert.AreEqual(17, links[0].Geometry[2].X);
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
            },
            new()
            {
                FromNodeId = 807,
                ToNodeId = 134,
                Geometry = new[] { new Point3D(271190.8, 7040365.2, 78.036), new Point3D(271204.6, 7040349, 78.236) },
            },
            new()
            {
                FromNodeId = 807,
                ToNodeId = 808,
                Geometry = new[] { new Point3D(271190.8, 7040365.2, 78.036), new Point3D(271157.2, 7040327.5, 72.936) },
            }
        };

        RotateAndCut(links, 7, double.MaxValue, double.MaxValue);

        // Link 1 has been rotated.
        Assert.AreEqual(3, links[0].FromNodeId);
        Assert.AreEqual(134, links[0].ToNodeId);

        Assert.AreEqual(83, links[0].Length, 1);

        Assert.AreEqual(3, links[0].Geometry.Length);
        Assert.AreEqual(271259.7, links[0].Geometry[0].X, 1);
        Assert.AreEqual(271212, links[0].Geometry[1].X, 1);
        Assert.AreEqual(271204.6, links[0].Geometry[2].X, 1);

        // Link 2 has been rotated.
        Assert.AreEqual(134, links[1].FromNodeId);
        Assert.AreEqual(807, links[1].ToNodeId);

        Assert.AreEqual(21, links[1].Length, 1);

        Assert.AreEqual(2, links[1].Geometry.Length);
        Assert.AreEqual(271204.6, links[1].Geometry[0].X, 1);
        Assert.AreEqual(271190.8, links[1].Geometry[1].X, 1);

        // Link 3 is untouched.
        Assert.AreEqual(807, links[2].FromNodeId);
        Assert.AreEqual(808, links[2].ToNodeId);

        Assert.AreEqual(51, links[2].Length, 1);

        Assert.AreEqual(2, links[2].Geometry.Length);
        Assert.AreEqual(271190.8, links[2].Geometry[0].X, 1);
        Assert.AreEqual(271157.2, links[2].Geometry[1].X, 1);

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
            },
            new()
            {
                FromNodeId = 807,
                ToNodeId = 134,
                Geometry = new[] { new Point3D(271190.8, 7040365.2, 78.036), new Point3D(271204.6, 7040349, 78.236) },
            },
            new()
            {
                FromNodeId = 807,
                ToNodeId = 808,
                Geometry = new[] { new Point3D(271190.8, 7040365.2, 78.036), new Point3D(271157.2, 7040327.5, 72.936) },
            }
        };

        RotateAndCut(links, 7, 25.878567945574169, 14.553077179694942);

        // Link 1 has been rotated.
        Assert.AreEqual(3, links[0].FromNodeId);
        Assert.AreEqual(134, links[0].ToNodeId);

        Assert.AreEqual(26, links[0].Length, 1);

        Assert.AreEqual(3, links[0].Geometry.Length);
        Assert.AreEqual(271223, links[0].Geometry[0].X, 1);
        Assert.AreEqual(271212, links[0].Geometry[1].X, 1);
        Assert.AreEqual(271204.6, links[0].Geometry[2].X, 1);

        // Link 2 has been rotated.
        Assert.AreEqual(134, links[1].FromNodeId);
        Assert.AreEqual(807, links[1].ToNodeId);

        Assert.AreEqual(21, links[1].Length, 1);

        Assert.AreEqual(2, links[1].Geometry.Length);
        Assert.AreEqual(271204.6, links[1].Geometry[0].X, 1);
        Assert.AreEqual(271190.8, links[1].Geometry[1].X, 1);

        // Link 3 is untouched.
        Assert.AreEqual(807, links[2].FromNodeId);
        Assert.AreEqual(808, links[2].ToNodeId);

        Assert.AreEqual(15, links[2].Length, 1);

        Assert.AreEqual(2, links[2].Geometry.Length);
        Assert.AreEqual(271190.8, links[2].Geometry[0].X, 1);
        Assert.AreEqual(271181, links[2].Geometry[1].X, 1);

        Assert.AreEqual(62, links.Sum(p => p.Length), 1);
        Assert.AreEqual(62, LineTools.CalculateLength(links.SelectMany(p => p.Geometry).ToArray()), 1);
    }
}
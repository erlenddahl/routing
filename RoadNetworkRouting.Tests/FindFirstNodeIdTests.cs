using EnergyModule.Geometry.SimpleStructures;
using RoadNetworkRouting.Network;

namespace RoadNetworkRouting.Tests;

[TestClass]
public class FindFirstNodeIdTests : RoadNetworkRouter
{
    public FindFirstNodeIdTests() : base(Array.Empty<RoadLink>())
    {

    }

    [TestMethod]
    public void SingleLink_StartPoint_AtA()
    {
        var link = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7), new Point3D(18, 18, 4) }
        };

        Assert.AreEqual(7, FindFirstNodeId(new[] { link }, new Point3D(15, 15, 10)));
    }

    [TestMethod]
    public void SingleLink_StartPoint_NearA()
    {
        var link = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7), new Point3D(18, 18, 4) }
        };

        Assert.AreEqual(7, FindFirstNodeId(new[] { link }, new Point3D(10, 10)));
    }

    [TestMethod]
    public void SingleLink_StartPoint_AtB()
    {
        var link = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7), new Point3D(18, 18, 4) }
        };

        Assert.AreEqual(13, FindFirstNodeId(new[] { link }, new Point3D(18, 18, 10)));
    }

    [TestMethod]
    public void SingleLink_StartPoint_NearB()
    {
        var link = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7), new Point3D(18, 18, 4) }
        };

        Assert.AreEqual(13, FindFirstNodeId(new[] { link }, new Point3D(19, 19)));
    }

    [TestMethod]
    public void TwoLinks_CorrectlyRotated()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 7,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7) }
            },
            new RoadLink()
            {
                FromNodeId = 13,
                ToNodeId = 17,
                Geometry = new[] { new Point3D(25, 25, 10), new Point3D(35, 35, 7) }
            }
        };

        Assert.AreEqual(7, FindFirstNodeId(links, new Point3D(15, 15)));
    }

    [TestMethod]
    public void TwoLinks_CorrectlyRotated_RegardlessOfFromPoint()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 7,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7) }
            },
            new RoadLink()
            {
                FromNodeId = 13,
                ToNodeId = 17,
                Geometry = new[] { new Point3D(25, 25, 10), new Point3D(35, 35, 7) }
            }
        };

        Assert.AreEqual(7, FindFirstNodeId(links, new Point3D(25, 25)));
    }

    [TestMethod]
    public void TwoLinks_FirstNeedsRotation()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 13,
                ToNodeId = 7,
                Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7) }
            },
            new RoadLink()
            {
                FromNodeId = 13,
                ToNodeId = 17,
                Geometry = new[] { new Point3D(25, 25, 10), new Point3D(35, 35, 7) }
            }
        };

        Assert.AreEqual(7, FindFirstNodeId(links, new Point3D(15, 15)));
    }

    [TestMethod]
    public void TwoLinks_SecondNeedsRotation()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 7,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7) }
            },
            new RoadLink()
            {
                FromNodeId = 17,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(25, 25, 10), new Point3D(35, 35, 7) }
            }
        };

        Assert.AreEqual(7, FindFirstNodeId(links, new Point3D(15, 15)));
    }
}
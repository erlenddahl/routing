using System.Drawing;
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
    public void SingleLink_NearB_To_B_Given_A()
    {
        // This test simulates a search from x=170 to x=200 on a link
        // that goes from 100 to 200. The routing will start from a 
        // fake node at x=170, and end at x=200. 
        // But the FindFirstNodeId needs to know more than just the
        // first search point, otherwise it will select node 13 as the
        // start node, since it is closer to the start point.

        var link = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(100, 0), new Point3D(200, 0) }
        };

        Assert.AreEqual(7, FindFirstNodeId(new[] { link }, new[] { 7, int.MinValue }, new Point3D(170, 10, 0), new Point3D(200, 10)));
    }

    [TestMethod]
    public void SingleLink_NearB_To_B_Given_B()
    {
        // This test simulates a search from x=170 to x=200 on a link
        // that goes from 100 to 200. The routing will start from a 
        // fake node at x=170, and end at x=200. 
        // But the FindFirstNodeId needs to know more than just the
        // first search point, otherwise it will select node 13 as the
        // start node, since it is closer to the start point.

        var link = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(100, 0), new Point3D(200, 0) }
        };

        Assert.AreEqual(7, FindFirstNodeId(new[] { link }, new[] { int.MinValue, 13 }, new Point3D(170, 10, 0), new Point3D(200, 10)));
    }

    [TestMethod]
    public void SingleLink_NearB_To_B_Given_Both()
    {
        // This test simulates a search from x=170 to x=200 on a link
        // that goes from 100 to 200. The routing will start from a 
        // fake node at x=170, and end at x=200. 
        // But the FindFirstNodeId needs to know more than just the
        // first search point, otherwise it will select node 13 as the
        // start node, since it is closer to the start point.

        var link = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(100, 0), new Point3D(200, 0) }
        };

        Assert.AreEqual(7, FindFirstNodeId(new[] { link }, new[] { 7, 13 }, new Point3D(170, 10, 0), new Point3D(200, 10)));
    }

    [TestMethod]
    public void SingleLink_NearB_To_B_Given_None()
    {
        // This test simulates a search from x=170 to x=200 on a link
        // that goes from 100 to 200. The routing will start from a 
        // fake node at x=170, and end at x=200. 
        // But the FindFirstNodeId needs to know more than just the
        // first search point, otherwise it will select node 13 as the
        // start node, since it is closer to the start point.

        var link = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(100, 0), new Point3D(200, 0) }
        };

        Assert.AreEqual(7, FindFirstNodeId(new[] { link }, new[] { int.MinValue, int.MinValue + 1 }, new Point3D(170, 10, 0), new Point3D(200, 10)));
    }

    [TestMethod]
    public void TwoLinks_NearB_To_B_To_C_Given_None()
    {
        // This test simulates a search from x=170 to x=200 on a link
        // that goes from 100 to 200. The routing will start from a 
        // fake node at x=170, and end at x=200. 
        // But the FindFirstNodeId needs to know more than just the
        // first search point, otherwise it will select node 13 as the
        // start node, since it is closer to the start point.

        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 7,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(100, 0), new Point3D(200, 0) }
            },

            new RoadLink()
            {
                FromNodeId = 13,
                ToNodeId = 15,
                Geometry = new[] { new Point3D(200, 0), new Point3D(300, 0) }
            }
        };

        Assert.AreEqual(7, FindFirstNodeId(links, new[] { int.MinValue, 13, int.MinValue + 1 }, new Point3D(170, 10, 0), new Point3D(230, 10)));
    }

    [TestMethod]
    public void TwoLinks_NearB_To_B_To_C_Given_A()
    {
        // This test simulates a search from x=170 to x=200 on a link
        // that goes from 100 to 200. The routing will start from a 
        // fake node at x=170, and end at x=200. 
        // But the FindFirstNodeId needs to know more than just the
        // first search point, otherwise it will select node 13 as the
        // start node, since it is closer to the start point.

        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 7,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(100, 0), new Point3D(200, 0) }
            },

            new RoadLink()
            {
                FromNodeId = 13,
                ToNodeId = 15,
                Geometry = new[] { new Point3D(200, 0), new Point3D(300, 0) }
            }
        };

        Assert.AreEqual(7, FindFirstNodeId(links, new[] { 7, 13, int.MinValue + 1 }, new Point3D(170, 10, 0), new Point3D(230, 10)));
    }

    [TestMethod]
    public void TwoLinks_NearB_To_B_To_C_Given_B()
    {
        // This test simulates a search from x=170 to x=200 on a link
        // that goes from 100 to 200. The routing will start from a 
        // fake node at x=170, and end at x=200. 
        // But the FindFirstNodeId needs to know more than just the
        // first search point, otherwise it will select node 13 as the
        // start node, since it is closer to the start point.

        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 7,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(100, 0), new Point3D(200, 0) }
            },

            new RoadLink()
            {
                FromNodeId = 13,
                ToNodeId = 15,
                Geometry = new[] { new Point3D(200, 0), new Point3D(300, 0) }
            }
        };

        Assert.AreEqual(7, FindFirstNodeId(links, new[] { int.MinValue, 13, 15 }, new Point3D(170, 10, 0), new Point3D(230, 10)));
    }

    [TestMethod]
    public void TwoLinks_NearB_To_B_To_C_Given_Both()
    {
        // This test simulates a search from x=170 to x=200 on a link
        // that goes from 100 to 200. The routing will start from a 
        // fake node at x=170, and end at x=200. 
        // But the FindFirstNodeId needs to know more than just the
        // first search point, otherwise it will select node 13 as the
        // start node, since it is closer to the start point.

        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 7,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(100, 0), new Point3D(200, 0) }
            },

            new RoadLink()
            {
                FromNodeId = 13,
                ToNodeId = 15,
                Geometry = new[] { new Point3D(200, 0), new Point3D(300, 0) }
            }
        };

        Assert.AreEqual(7, FindFirstNodeId(links, new[] { 7, 13, 15 }, new Point3D(170, 10, 0), new Point3D(230, 10)));
    }

    [TestMethod]
    public void SingleLink_A_To_B()
    {
        var link = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7), new Point3D(18, 18, 4) }
        };

        Assert.AreEqual(7, FindFirstNodeId(new[] { link }, new []{ 7, 13 }, new Point3D(15, 15, 10), new Point3D(18, 18, 4)));
    }

    [TestMethod]
    public void SingleLink_NearA_To_NearB()
    {
        var link = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7), new Point3D(18, 18, 4) }
        };

        Assert.AreEqual(7, FindFirstNodeId(new[] { link }, new []{ int.MinValue, int.MinValue + 1 }, new Point3D(10, 10), new Point3D(20, 20)));
    }

    [TestMethod]
    public void SingleLink_AtB_To_NearA()
    {
        var link = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7), new Point3D(18, 18, 4) }
        };

        Assert.AreEqual(13, FindFirstNodeId(new[] { link }, new []{ 13, int.MinValue }, new Point3D(18, 18, 10), new Point3D(16, 16, 4)));
    }

    [TestMethod]
    public void SingleLink_NearB_To_A()
    {
        var link = new RoadLink()
        {
            FromNodeId = 7,
            ToNodeId = 13,
            Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7), new Point3D(18, 18, 4) }
        };

        Assert.AreEqual(13, FindFirstNodeId(new[] { link }, new []{ int.MinValue, 7 }, new Point3D(19, 19), new Point3D(15, 15, 10)));
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

        Assert.AreEqual(7, FindFirstNodeId(links, new []{ 7, 13, 17 }, new Point3D(15, 15), new Point3D(35, 35)));
    }

    [TestMethod]
    public void OneLink_CorrectlyRotated_BothEndsOverloaded()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 7,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7) }
            }
        };

        Assert.AreEqual(7, FindFirstNodeId(links, new[] { int.MinValue, 7, int.MinValue + 1 }, new Point3D(17, 17), new Point3D(19, 19)));
    }

    [TestMethod]
    public void OneLink_IncorrectlyRotated_BothEndsOverloaded()
    {
        var links = new[]
        {
            new RoadLink()
            {
                FromNodeId = 7,
                ToNodeId = 13,
                Geometry = new[] { new Point3D(15, 15, 10), new Point3D(25, 25, 7) }
            }
        };

        Assert.AreEqual(7, FindFirstNodeId(links, new[] { int.MinValue, 13, int.MinValue + 1 }, new Point3D(17, 17), new Point3D(19, 19)));
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

        Assert.AreEqual(13, FindFirstNodeId(links, new[] { int.MinValue, 13, int.MinValue + 1 }, new Point3D(25, 25), new Point3D(35, 35)));
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

        Assert.AreEqual(7, FindFirstNodeId(links, new []{ int.MinValue, 13, 17 }, new Point3D(15, 15), new Point3D(35, 35)));
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

        Assert.AreEqual(7, FindFirstNodeId(links, new []{ int.MinValue, 13, int.MinValue + 1 }, new Point3D(15, 15), new Point3D(35, 35)));
    }
}
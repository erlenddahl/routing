using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnergyModule.Geometry;
using EnergyModule.Geometry.SimpleStructures;
using RoadNetworkRouting.Network;

namespace RoadNetworkRouting.Tests.GeometrySmoothingTests.CatmullRomTests
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void Works()
        {
            var points = new[]
            {
                new Point3D(0, 0, 0),
                new Point3D(10, 0, 0),
                new Point3D(20, 0, 0),
                new Point3D(30, 0, 0)
            };
            var cat = new CatmullRomCurve(points, 0);
        }

        [TestMethod]
        public void HorizontalLine_SameResults_XYZ()
        {
            var points = new[]
            {
                new Point3D(0, 0, 0),
                new Point3D(10, 0, 0),
                new Point3D(20, 0, 0),
                new Point3D(30, 0, 0),
                new Point3D(40, 0, 0),
                new Point3D(50, 0, 0),
                new Point3D(60, 0, 0),
                new Point3D(70, 0, 0),
                new Point3D(80, 0, 0),
                new Point3D(90, 0, 0),
                new Point3D(100, 0, 0)
            };
            var cache = new CachedLineTools(points);
            var cat = new CatmullRomCurve(points, 0);

            for (var i = 0; i <= 100; i += 5)
            {
                var o = cache.QueryPointInfo(i);
                var c = cat.QueryPointInfo(i);

                Assert.AreEqual(o.X, c.X);
                Assert.AreEqual(o.Y, c.Y);
                Assert.AreEqual(o.Z, c.Z);
            }
        }

        [TestMethod]
        public void HorizontalLine_SameResults_HorizontalAngle()
        {
            var points = new[]
            {
                new Point3D(0, 0, 0),
                new Point3D(10, 0, 0),
                new Point3D(20, 0, 0),
                new Point3D(30, 0, 0),
                new Point3D(40, 0, 0),
                new Point3D(50, 0, 0),
                new Point3D(60, 0, 0),
                new Point3D(70, 0, 0),
                new Point3D(80, 0, 0),
                new Point3D(90, 0, 0),
                new Point3D(100, 0, 0)
            };
            var cache = new CachedLineTools(points);
            var cat = new CatmullRomCurve(points, 0);

            for (var i = 0; i <= 100; i += 5)
            {
                var o = cache.QueryPointInfo(i);
                var c = cat.QueryPointInfo(i);

                Assert.AreEqual(o.Angle, c.Angle);
            }
        }

        [TestMethod]
        public void HorizontalLine_SameResults_VerticalAngle()
        {
            var points = new[]
            {
                new Point3D(0, 0, 0),
                new Point3D(10, 0, 0),
                new Point3D(20, 0, 0),
                new Point3D(30, 0, 0),
                new Point3D(40, 0, 0),
                new Point3D(50, 0, 0),
                new Point3D(60, 0, 0),
                new Point3D(70, 0, 0),
                new Point3D(80, 0, 0),
                new Point3D(90, 0, 0),
                new Point3D(100, 0, 0)
            };
            var cache = new CachedLineTools(points);
            var cat = new CatmullRomCurve(points, 0);

            for (var i = 0; i <= 100; i += 5)
            {
                var o = cache.QueryPointInfo(i);
                var c = cat.QueryPointInfo(i);

                Assert.AreEqual(o.VerticalAngle, c.VerticalAngle);
            }
        }

        [TestMethod]
        public void HorizontalLine_Sparse_SameResults_XYZ()
        {
            var points = new[]
            {
                new Point3D(0, 0, 0),
                new Point3D(10, 0, 0),
                new Point3D(20, 0, 0),
                new Point3D(40, 0, 0),
                new Point3D(50, 0, 0),
                new Point3D(60, 0, 0),
                new Point3D(100, 0, 0)
            };
            var cache = new CachedLineTools(points);
            var cat = new CatmullRomCurve(points, 0);

            for (var i = 0; i <= 100; i += 5)
            {
                var o = cache.QueryPointInfo(i);
                var c = cat.QueryPointInfo(i);

                Assert.AreEqual(o.X, c.X, 2);
                Assert.AreEqual(o.Y, c.Y, 2);
                Assert.AreEqual(o.Z, c.Z, 2);
            }
        }

        [TestMethod]
        public void RealExample_NoNans()
        {
            var points = GetRealData();
            var cache = new CachedLineTools(points);
            var cat = new CatmullRomCurve(points, CatmullRomType.Chordal);

            for (var i = 0; i <= cache.LengthM; i += 5)
            {
                var o = cache.QueryPointInfo(i);
                var c = cat.QueryPointInfo(i);

                Assert.IsFalse(double.IsNaN(c.X));
                Assert.IsFalse(double.IsNaN(c.Y));
                Assert.IsFalse(double.IsNaN(c.Z));
            }
        }

        [TestMethod]
        public void RealExample_SimilarAngles()
        {
            var points = GetRealData();
            var cache = new CachedLineTools(points);
            var cat = new CatmullRomCurve(points, CatmullRomType.Chordal);

            for (var i = 0; i <= cache.LengthM; i += 5)
            {
                var o = cache.QueryPointInfo(i);
                var c = cat.QueryPointInfo(i);

                //Assert.AreEqual(o.Angle, c.Angle);
                //Debug.WriteLine(o.Angle.ToString("n5") + " - " + c.Angle.ToString("n5"));
                Debug.WriteLine(c.Angle.ToString("n5"));
            }
        }



        private Point3D[] GetRealData()
        {
            return new[]
            {
                new Point3D(265099.70785, 6758479.67103, 129.89971),
                new Point3D(265089.50968, 6758527.80788, 129.89971),
                new Point3D(265086.35805, 6758540.30361, 129.98282),
                new Point3D(265082.32293, 6758554.74557, 130.09188),
                new Point3D(265077.90045, 6758569.09029, 130.21417),
                new Point3D(265073.42288, 6758582.35517, 130.34025),
                new Point3D(265068.25378, 6758596.43655, 130.48814),
                new Point3D(265062.69439, 6758610.37623, 130.64926),
                new Point3D(265057.17276, 6758623.24107, 130.81159),
                new Point3D(265050.89091, 6758636.87114, 130.99830),
                new Point3D(265044.24367, 6758650.32434, 131.19826),
                new Point3D(265037.70537, 6758662.70918, 131.39683),
                new Point3D(265030.35294, 6758675.78461, 131.62238),
                new Point3D(265022.64991, 6758688.65988, 131.86117),
                new Point3D(265014.60095, 6758701.32348, 132.11320),
                new Point3D(265006.77619, 6758712.93344, 132.36038),
                new Point3D(264998.87032, 6758724.05731, 132.61231),
                new Point3D(264989.88580, 6758736.06802, 132.90199),
                new Point3D(264981.29538, 6758747.12555, 133.18430),
                new Point3D(264971.92192, 6758758.84930, 133.49958),
                new Point3D(264962.45593, 6758770.48985, 133.82477),
                new Point3D(264954.77757, 6758779.86739, 134.08788),
                new Point3D(264945.27699, 6758791.47677, 134.41364),
                new Point3D(264935.77105, 6758803.08655, 134.73940),
                new Point3D(264926.90001, 6758813.92824, 135.04344),
                new Point3D(264917.39962, 6758825.53770, 135.36920),
                new Point3D(264907.89387, 6758837.14755, 135.69496),
                new Point3D(264899.82338, 6758847.00918, 135.97172),
                new Point3D(264890.32933, 6758858.62948, 136.29748),
                new Point3D(264880.89862, 6758870.30147, 136.62324),
                new Point3D(264872.19933, 6758881.26600, 136.92434),
                new Point3D(264863.03345, 6758893.15508, 137.23633),
                new Point3D(264854.09543, 6758905.19665, 137.53706),
                new Point3D(264845.99837, 6758916.62403, 137.80759),
                new Point3D(264838.59692, 6758927.62556, 138.05459),
                new Point3D(264830.54309, 6758940.27884, 138.32362),
                new Point3D(264823.34500, 6758952.29461, 138.56456),
                new Point3D(264815.96625, 6758965.36119, 138.81183),
                new Point3D(264808.94831, 6758978.61612, 139.04784),
                new Point3D(264802.73218, 6758991.15891, 139.25797),
                new Point3D(264796.42430, 6759004.78013, 139.47222),
                new Point3D(264790.49126, 6759018.55525, 139.67523),
                new Point3D(264785.29057, 6759031.55579, 139.85454),
                new Point3D(264780.09098, 6759045.63979, 140.03579),
                new Point3D(264775.28028, 6759059.84324, 140.20579),
                new Point3D(264771.13861, 6759073.22044, 140.35430),
                new Point3D(264767.07857, 6759087.66460, 140.50254),
                new Point3D(264763.41750, 6759102.21636, 140.63953),
                new Point3D(264760.14925, 6759116.86497, 140.76527),
                new Point3D(264757.46285, 6759130.60374, 140.87247),
                new Point3D(264754.97726, 6759145.40155, 140.97646),
                new Point3D(264752.89310, 6759160.26212, 141.06920),
                new Point3D(264751.31197, 6759174.17340, 141.14560),
                new Point3D(264750.01686, 6759189.11573, 141.21659),
                new Point3D(264749.12711, 6759204.09821, 141.27632),
                new Point3D(264748.66157, 6759218.09193, 141.32192),
                new Point3D(264748.56257, 6759233.10020, 141.35991),
                new Point3D(264748.87606, 6759248.09230, 141.38664),
                new Point3D(264749.53175, 6759262.08991, 141.40145),
                new Point3D(264750.63228, 6759277.05218, 141.40643),
                new Point3D(264752.13839, 6759291.97641, 141.40016),
                new Point3D(264753.91331, 6759305.86632, 141.38417),
                new Point3D(264756.20743, 6759320.69363, 141.35615),
                new Point3D(264758.89948, 6759335.44989, 141.31689),
                new Point3D(264761.78302, 6759349.15462, 141.27009),
                new Point3D(264765.24853, 6759363.75908, 141.20907),
                new Point3D(264769.11444, 6759378.24765, 141.13681),
                new Point3D(264773.07897, 6759391.67967, 141.06062),
                new Point3D(264777.70921, 6759405.96120, 140.97788),
                new Point3D(264782.72136, 6759420.09455, 140.89514),
                new Point3D(264787.74471, 6759433.16598, 140.81791),
                new Point3D(264793.49507, 6759447.02701, 140.73517),
                new Point3D(264799.62044, 6759460.71798, 140.65243),
                new Point3D(264805.67575, 6759473.35242, 140.57520),
                new Point3D(264811.95097, 6759485.69304, 140.49886),
                new Point3D(264818.44799, 6759498.09716, 140.42164),
                new Point3D(264824.91863, 6759510.51421, 140.34441),
                new Point3D(264831.18927, 6759523.03387, 140.26718),
                new Point3D(264837.56312, 6759536.61906, 140.18444),
                new Point3D(264843.17421, 6759549.45053, 140.10721),
                new Point3D(264848.81810, 6759563.35219, 140.02447),
                new Point3D(264854.08544, 6759577.40167, 139.94173),
                new Point3D(264858.96466, 6759591.58857, 139.85898),
                new Point3D(264863.16882, 6759604.94893, 139.78176),
                new Point3D(264867.29055, 6759619.36475, 139.69901),
                new Point3D(264871.02344, 6759633.90687, 139.61627),
                new Point3D(264874.14729, 6759647.55104, 139.53905),
                new Point3D(264877.10746, 6759662.25607, 139.45630),
                new Point3D(264879.66502, 6759677.04362, 139.37356),
                new Point3D(264881.68792, 6759690.90595, 139.28852),
                new Point3D(264883.45827, 6759705.80151, 139.16912),
                new Point3D(264884.82923, 6759720.74587, 139.02612),
                new Point3D(264885.73302, 6759734.71585, 138.89099),
                new Point3D(264886.31312, 6759749.71261, 138.74620),
                new Point3D(264886.48548, 6759764.71404, 138.60141),
                new Point3D(264886.27046, 6759778.71347, 138.46627),
                new Point3D(264885.64756, 6759793.70057, 138.32148),
                new Point3D(264884.62089, 6759808.66974, 138.17669),
                new Point3D(264883.29205, 6759822.60893, 138.04155),
                new Point3D(264881.47187, 6759837.50781, 137.89676),
                new Point3D(264879.25041, 6759852.34392, 137.75197),
                new Point3D(264876.80730, 6759866.13354, 137.61683),
                new Point3D(264873.80961, 6759880.83119, 137.47326),
                new Point3D(264870.84965, 6759893.71676, 137.37241),
                new Point3D(264867.52375, 6759907.31914, 137.30367),
                new Point3D(264864.19247, 6759920.92189, 137.27412),
                new Point3D(264861.22870, 6759933.58432, 137.28115),
                new Point3D(264858.26160, 6759947.26348, 137.30056),
                new Point3D(264855.37465, 6759961.98733, 137.32136),
                new Point3D(264852.77996, 6759976.77002, 137.34216),
                new Point3D(264850.62319, 6759990.60773, 137.36157),
                new Point3D(264848.59999, 6760005.47542, 137.38237),
                new Point3D(264846.87840, 6760020.37898, 137.40316),
                new Point3D(264845.53908, 6760034.31891, 137.42257),
                new Point3D(264844.39216, 6760049.27375, 137.44337),
                new Point3D(264843.54611, 6760064.25334, 137.46417),
                new Point3D(264842.99477, 6760079.24691, 137.48497),
                new Point3D(264842.75305, 6760093.24814, 137.50438),
                new Point3D(264842.79039, 6760108.25855, 137.52517),
                new Point3D(264843.12096, 6760123.26069, 137.54597),
                new Point3D(264843.70008, 6760137.25220, 137.56538),
                new Point3D(264844.61635, 6760152.22670, 137.58618),
                new Point3D(264845.79758, 6760166.84853, 137.60652),
                new Point3D(264847.07406, 6760179.78850, 137.62454),
                new Point3D(264848.79119, 6760194.69875, 137.64534),
                new Point3D(264850.71808, 6760209.57276, 137.66614),
                new Point3D(264852.66529, 6760223.44010, 137.69606),
                new Point3D(264854.86224, 6760238.28504, 137.76368),
                new Point3D(264857.13426, 6760253.11384, 137.86880),
                new Point3D(264858.97954, 6760265.03312, 137.98044),
                new Point3D(264861.27863, 6760279.86013, 138.14606),
                new Point3D(264863.57771, 6760294.68713, 138.31443),
                new Point3D(264865.71838, 6760308.51931, 138.47158),
                new Point3D(264867.73113, 6760321.48868, 138.61885),
                new Point3D(264870.00384, 6760336.32860, 138.78722),
                new Point3D(264872.04232, 6760350.17872, 138.94437),
                new Point3D(264874.07744, 6760365.04556, 139.11055),
                new Point3D(264875.89666, 6760379.93788, 139.25247),
                new Point3D(264877.33616, 6760393.86121, 139.35785),
                new Point3D(264878.42936, 6760407.23780, 139.43432),
                new Point3D(264879.26977, 6760422.21733, 139.49140),
                new Point3D(264879.68640, 6760436.21961, 139.51761),
                new Point3D(264879.73390, 6760451.21818, 139.51669),
                new Point3D(264879.37916, 6760466.22106, 139.48576),
                new Point3D(264878.67427, 6760480.20831, 139.42984),
                new Point3D(264877.63118, 6760494.01691, 139.34885),
                new Point3D(264876.12886, 6760508.93947, 139.23225),
                new Point3D(264874.44503, 6760522.84639, 139.09634),
                new Point3D(264872.39455, 6760537.70475, 138.92559),
                new Point3D(264870.16317, 6760552.54159, 138.75118),
                new Point3D(264867.81120, 6760567.36409, 138.57678),
                new Point3D(264865.82193, 6760579.61572, 138.43241),
                new Point3D(264863.40970, 6760594.43105, 138.25801),
                new Point3D(264861.00218, 6760609.23490, 138.08361),
                new Point3D(264858.75334, 6760623.06759, 137.92084),
                new Point3D(264856.34587, 6760637.87144, 137.74643),
                new Point3D(264853.93303, 6760652.67566, 137.57203),
                new Point3D(264851.68426, 6760666.50835, 137.40926),
                new Point3D(264849.27689, 6760681.31221, 137.23769),
                new Point3D(264846.87028, 6760696.12719, 137.07925),
                new Point3D(264844.62085, 6760709.94877, 136.94406),
                new Point3D(264842.21356, 6760724.75264, 136.81280),
                new Point3D(264839.80162, 6760739.56799, 136.69562),
                new Point3D(264837.55228, 6760753.38957, 136.59893),
                new Point3D(264835.14508, 6760768.19344, 136.50650),
                new Point3D(264832.73322, 6760783.00880, 136.41526),
                new Point3D(264830.78765, 6760795.01183, 136.34129),
                new Point3D(264828.43074, 6760809.83472, 136.25006),
                new Point3D(264826.17748, 6760824.66191, 136.15882),
                new Point3D(264824.23597, 6760838.53013, 136.07367),
                new Point3D(264822.37856, 6760853.42045, 135.98243),
                new Point3D(264820.81729, 6760868.34699, 135.89120),
                new Point3D(264819.82318, 6760880.34281, 135.81797),
                new Point3D(264818.94547, 6760895.32456, 135.72673),
                new Point3D(264818.47687, 6760910.32388, 135.63550),
                new Point3D(264818.40982, 6760924.32475, 135.55034),
                new Point3D(264818.73011, 6760939.32761, 135.45911),
                new Point3D(264819.46269, 6760954.31431, 135.36787),
                new Point3D(264820.51337, 6760968.27459, 135.28272),
                new Point3D(264822.03581, 6760983.19774, 135.19148),
                new Point3D(264823.95821, 6760998.08321, 135.10025),
                new Point3D(264826.12610, 6761011.91357, 135.01510),
                new Point3D(264828.83391, 6761026.66877, 134.92386),
                new Point3D(264831.94488, 6761041.35257, 134.83262),
                new Point3D(264835.45211, 6761055.94307, 134.74582),
                new Point3D(264838.60786, 6761067.77546, 134.70264),
                new Point3D(264842.79856, 6761082.17544, 134.69060),
                new Point3D(264847.27796, 6761096.50043, 134.72356),
                new Point3D(264851.66168, 6761109.79292, 134.79492),
                new Point3D(264856.49792, 6761124.00490, 134.91489),
                new Point3D(264875.78675, 6761180.17517, 134.91489)
            };
        }
    }
}
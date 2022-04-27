using System.Globalization;
using Extensions.StringExtensions;

namespace RoutingApi.Geometry
{
    public class LinkReference
    {
        public string Id => $"{FromRelLen.ToString("n8", CultureInfo.InvariantCulture).Replace(".",",")}-{ToRelLen.ToString("n8", CultureInfo.InvariantCulture).Replace(".", ",")}@{LinkId}";

        public string DbId => $"{FromRelLen.ToString("n8", CultureInfo.InvariantCulture).Replace(".", ",").RemoveTrailingZeroes()}-{ToRelLen.ToString("n8", CultureInfo.InvariantCulture).Replace(".", ",").RemoveTrailingZeroes()}@{LinkId}";

        public int Direction { get; set; }
        public double ToRelLen { get; set; }
        public double FromRelLen { get; set; }
        public string LinkId { get; set; }

        public string S
        {
            set
            {
                Direction = 1;
                if (value.EndsWith("-2"))
                {
                    value = value.Substring(0, value.Length - 2);
                    Direction = 2;
                }

                var p = value.Split('@');
                LinkId = p[1];

                bool isScientific = false;
                if (p[0].Contains("E-"))
                {
                    isScientific = true;
                    p[0] = p[0].Replace("E-", "E_");
                }

                p = p[0].Split('-');

                if (isScientific)
                {
                    p[0] = p[0].Replace("E_", "E-");
                    p[1] = p[1].Replace("E_", "E-");
                }

                FromRelLen = double.Parse(p[0].Replace(",", "."), CultureInfo.InvariantCulture);
                ToRelLen = double.Parse(p[1].Replace(",", "."), CultureInfo.InvariantCulture);
            }
        }

        public override string ToString()
        {
            return Id;
        }

        public string ToShortRepresentation()
        {
            var s = Id;
            if (Direction != 1) s += "-2";
            return s;
        }
    }
}
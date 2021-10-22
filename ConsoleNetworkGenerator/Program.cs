using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions.StringExtensions;
using NetworkGenerator;

namespace ConsoleNetworkGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var networkGdb = @"C:\Users\erlendd\Desktop\Søppel\2021-03 - GeoFlow - map-matching\vegnettRuteplan_GDB_190703.gdb";

            new Generator().Generate(networkGdb, networkGdb.ChangeExtension(".bin"));
        }
    }
}

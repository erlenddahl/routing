using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions.Utilities;

namespace RoadNetworkRouting.Utils
{
    public class RoutingTimer
    {
        private readonly TaskTimer _timer = new();

        public double EntryPointsMs => Get(nameof(EntryPointsMs));

        public double GraphCreationMs => Get(nameof(GraphCreationMs));
        public double LoadLinksMs => Get(nameof(LoadLinksMs));
        public double RoutingMs => Get(nameof(RoutingMs));
        public double PostprocessingMs => Get(nameof(PostprocessingMs));

        public RoutingTimer()
        {
            _timer.Restart();
        }

        private double Get(string key)
        {
            return _timer.Timings.TryGetValue(key, out var v) ? v / 10_000d : 0d;
        }

        internal void Start()
        {
            _timer.Restart();
        }

        internal void Time(string key)
        {
            _timer.Time(key);
        }

        public void Append(RoutingTimer other)
        {
            _timer.Append(other._timer);
        }
    }
}

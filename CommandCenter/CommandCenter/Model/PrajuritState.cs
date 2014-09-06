using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
    public class PrajuritState
    {
        public Location location;
        public double orientation;

        public PrajuritState(Location location, double orientation)
        {
            this.location = location;
            this.orientation = orientation;
        }

        public PrajuritState()
        {
            this.location = null;
            this.orientation = 0;
        }
    }
}

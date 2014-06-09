using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
    class Prajurit
    {
        public string nama;
        public Location currentLocation;
        public Pushpin assignedPushPin = null;

        public Prajurit(string nama, Location currentLocation)
        {
            this.nama = nama;
            this.currentLocation = currentLocation;
        }
    }
}

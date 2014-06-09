using CommandCenter.Model;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CommandCenter.View
{
    class MapDrawer
    {
        private Map map;
        private ArrayList prajurits;

        public MapDrawer(Map map, ArrayList prajurits)
        {
            this.map = map;
            this.prajurits = prajurits;
        }

        public void updateMap()
        {
            foreach (Prajurit prajurit in prajurits)
            {
                // Create a push pin if not yet done
                if (prajurit.assignedPushPin == null && prajurit.currentLocation != null)
                {
                    prajurit.assignedPushPin = new Pushpin();
                    prajurit.assignedPushPin.Location = prajurit.currentLocation;
                    map.Children.Add(prajurit.assignedPushPin);
                }
                // Update and draw the push pin if available
                if (prajurit.assignedPushPin != null)
                {
                    prajurit.assignedPushPin.Location = prajurit.currentLocation;
                }
            }
        }

        public void showEveryone()
        {
            LocationCollection locations = new LocationCollection();
            foreach (Prajurit prajurit in prajurits)
            {
                locations.Add(prajurit.currentLocation);
            }
            LocationRect bounds = new LocationRect(locations);
            map.SetView(bounds);
        }
    }
}

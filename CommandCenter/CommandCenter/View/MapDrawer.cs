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
        private List<Prajurit> prajurits;

        public MapDrawer(Map map, List<Prajurit> prajurits)
        {
            this.map = map;
            this.prajurits = prajurits;
        }

        public void updateMap()
        {
            map.Dispatcher.InvokeAsync((Action)(() =>
            {
                foreach (Prajurit prajurit in prajurits)
                {
                    // Create a push pin if not yet done
                    if (prajurit.assignedPushPin == null && prajurit.currentState != null && prajurit.currentState.location != null)
                    {
                        prajurit.assignedPushPin = new Pushpin();
                        prajurit.assignedPushPin.Location = prajurit.currentState.location;
                        ToolTipService.SetToolTip(prajurit.assignedPushPin, prajurit.nama);
                        map.Children.Add(prajurit.assignedPushPin);
                    }
                    // Update and draw the push pin if available
                    if (prajurit.assignedPushPin != null)
                    {
                        prajurit.assignedPushPin.Location = prajurit.currentState.location;
                        prajurit.assignedPushPin.Heading = prajurit.currentState.orientation;
                    }
                }
            }));
        }

        public void showEveryone()
        {
            LocationCollection locations = new LocationCollection();
            foreach (Prajurit prajurit in prajurits)
            {
                locations.Add(prajurit.currentState.location);
            }
            LocationRect bounds = new LocationRect(locations);
            map.SetView(bounds);
        }
    }
}

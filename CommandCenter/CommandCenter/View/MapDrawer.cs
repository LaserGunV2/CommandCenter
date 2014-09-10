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
    public class MapDrawer
    {
        Map map;
        List<Prajurit> prajurits;

        public MapDrawer(Map map, List<Prajurit> prajurits)
        {
            this.map = map;
            this.prajurits = prajurits;
        }

        public void updateMap()
        {
            // TODO Optimization tip: create another version that only update a single prajurit. 
            map.Dispatcher.InvokeAsync((Action)(() =>
            {
                foreach (Prajurit prajurit in prajurits)
                {
                    // Create a push pin if not yet done
                    if (prajurit.assignedPushPin == null && prajurit.location != null)
                    {
                        prajurit.assignedPushPin = new Pushpin();
                        prajurit.assignedPushPin.Location = prajurit.location;
                        ToolTipService.SetToolTip(prajurit.assignedPushPin, prajurit.nama);
                        map.Children.Add(prajurit.assignedPushPin);
                    }
                    // Update and draw the push pin if available
                    if (prajurit.assignedPushPin != null)
                    {
                        prajurit.assignedPushPin.Location = prajurit.location;
                        prajurit.assignedPushPin.Heading = prajurit.heading;
                    }
                }
                // Refresh map, if map is ready.
                if (map.ActualHeight > 0 && map.ActualWidth > 0)
                {
                    map.SetView(map.BoundingRectangle);
                }
            }));
        }

        public void showEveryone()
        {
            LocationCollection locations = new LocationCollection();
            foreach (Prajurit prajurit in prajurits)
            {
                if (prajurit.location != null)
                {
                    locations.Add(prajurit.location);
                }
            }
            LocationRect bounds = new LocationRect(locations);
            map.SetView(bounds);
            map.ZoomLevel--;
        }
    }
}

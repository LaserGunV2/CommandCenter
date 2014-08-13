using CommandCenter.Model;
using CommandCenter.View;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CommandCenter.Dummy
{
    class DummyPrajuritMovement
    {

        private ArrayList prajurits;
        private MapDrawer mapDrawer;

        public DummyPrajuritMovement(ArrayList prajurits, MapDrawer mapDrawer)
        {
            this.prajurits = prajurits;
            this.mapDrawer = mapDrawer;
            Timer timer = new Timer(1000);
            timer.Elapsed += TimerTriggered;
            timer.Enabled = true;
        }

        private void TimerTriggered(Object source, ElapsedEventArgs e)
        {
            Random random = new Random();
            foreach (Prajurit prajurit in this.prajurits)
            {
                if (prajurit.currentState != null)
                {
                    PrajuritState newState = new PrajuritState();
                    newState.location = new Location(prajurit.currentState.location.Latitude + (random.NextDouble() - 0.5) * 1e-4, prajurit.currentState.location.Longitude + (random.NextDouble() - 0.5) * 1e-4);
                    newState.orientation = ((int)prajurit.currentState.orientation + random.Next(11) - 5) % 360;
                    prajurit.updateState(newState);
                }
            }
            mapDrawer.updateMap();
        }
    }
}

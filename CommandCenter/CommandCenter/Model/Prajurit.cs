using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
    class Prajurit
    {
        public string nama;
        public PrajuritState currentState;
        public ArrayList stateHistory;
        public Pushpin assignedPushPin = null;

        public Prajurit(string nama, PrajuritState initialState)
        {
            this.nama = nama;
            this.stateHistory = new ArrayList();
            if (initialState == null)
            {
                this.currentState = null;
            }
            else
            {
                updateState(initialState);
            }
        }

        public void updateState(PrajuritState newState)
        {
            stateHistory.Add(newState);
            currentState = newState;
        }
    }
}

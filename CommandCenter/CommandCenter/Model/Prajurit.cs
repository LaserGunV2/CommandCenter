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
            this.currentState = initialState == null ? new PrajuritState() : initialState;
            this.stateHistory = new ArrayList();
        }
    }
}

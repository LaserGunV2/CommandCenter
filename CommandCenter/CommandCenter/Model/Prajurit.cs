using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
    class Prajurit
    {
        public int nomerUrut { get; set; }
        public string nomerInduk { get; set; }
        public string nama {get; set; }
        public IPAddress ipAddress;
        public PrajuritState currentState;
        public Pushpin assignedPushPin = null;

        public Prajurit(int nomerUrut, string nomerInduk, IPAddress ipAddress, PrajuritState initialState)
        {
            this.nomerUrut = nomerUrut;
            this.nomerInduk = nomerInduk;
            this.ipAddress = ipAddress;
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
            currentState = newState;
        }
    }
}

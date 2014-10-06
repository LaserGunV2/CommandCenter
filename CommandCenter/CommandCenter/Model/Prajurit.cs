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
    public class Prajurit
    {

        public enum State { NORMAL, SHOOT, HIT, DEAD }
        public enum Posture { STAND, CRAWL }

        public IPAddress ipAddress;
        public int nomerUrut { get; set; }
        public string nomerInduk { get; set; }
        public string nama {get; set; }
        public Location location { get; set; }
        public double heading { get; set; }
        public string group { get; set; }
        public DateTime lastUpdate { get; set; }
        public Senjata senjata { get; set; }
        public State state { get; set; }
        public Posture posture { get; set; }
        public int accuracy { get; set; }

        public Pushpin assignedPushPin = null;

        public static List<string> GROUPS_AVAILABLE = new List<string>() { "A", "B" };

        public Prajurit(int nomerUrut, string nomerInduk, IPAddress ipAddress, string group, Location location)
        {
            this.nomerUrut = nomerUrut;
            this.nomerInduk = nomerInduk;
            this.ipAddress = ipAddress;
            this.group = group;
            this.posture = (Posture)0;
            this.state = (State)0;
            this.accuracy = 200; //sementara - test doang
            if (location == null)
            {
                this.location = null;
            }
            else
            {
                this.location = location;
            }
            lastUpdate = DateTime.Now;
        }

        public static int findPrajuritByNomerInduk(List<Prajurit> prajurits, String nomerInduk)
        {
            for (int i = 0; i < prajurits.Count; i++)
            {
                if (prajurits[i].nomerInduk.Equals(nomerInduk))
                {
                    return i;
                }
            }
            return -1;
        }

        public void setLocation(String locationString)
        {
            string[] latlon = locationString.Split(',');
            if (location == null)
            {
                location = new Location();
            }
            location.Latitude = Double.Parse(latlon[0]);
            location.Longitude = Double.Parse(latlon[1]);
            lastUpdate = DateTime.Now;
        }
    }
}

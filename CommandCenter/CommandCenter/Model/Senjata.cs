using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
    public class Senjata
    {
        public int idSenjata;
        public Prajurit owner;
        public int initialCounter, currentCounter, initialAmmo;

        public Senjata(int idSenjata, Prajurit owner, int counter, int initialAmmo)
        {
            this.idSenjata = idSenjata;
            this.owner = owner;
            this.initialCounter = counter;
            this.currentCounter = counter;
            this.initialAmmo = initialAmmo;
        }

        public int getRemainingAmmo()
        {
            return (initialCounter + initialAmmo - currentCounter);
        }

        override public String ToString()
        {
            return "#" + idSenjata + " / " + getRemainingAmmo();
        }
    }
}

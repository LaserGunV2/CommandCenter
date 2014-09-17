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
        public int counter;

        public Senjata(int idSenjata, Prajurit owner, int counter)
        {
            this.idSenjata = idSenjata;
            this.owner = owner;
            this.counter = counter;
        }

        override public String ToString()
        {
            return "" + idSenjata;
        }
    }
}

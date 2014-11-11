using CommandCenter.View;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using CommandCenter.Model.Events;

namespace CommandCenter.Controller
{
    class LiveGameController : AbstractGameController
    {

        public LiveGameController(MainWindow parent)
            : base(parent, new UDPCommunication(parent), parent.recorder)
        {
            // void
        }
    }
}
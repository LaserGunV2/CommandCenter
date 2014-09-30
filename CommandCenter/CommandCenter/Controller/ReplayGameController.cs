using CommandCenter.Model;
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CommandCenter.Controller
{
    class ReplayGameController : AbstractGameController
    {

        Timer timer;
        EventsRecorder player;
        Int64 currentTime;
        Event scheduledEvent;

        public ReplayGameController(MainWindow parent)
            : base(parent, new MyUDPCommunication(parent), new MyEventsRecorder())
        {
            timer = new Timer();
            timer.Elapsed += OnTimedEvent;
            player = new EventsRecorder();
        }

        public void startPlayback()
        {
            player.startReplaying();
            currentTime = 0;
            parent.updateReplayProgress(0);
            scheduledEvent = null;
            executePacketAndScheduleNext();
            startRegistration();
            timer.Enabled = true;
        }

        public void stopPlayback()
        {
            player.stopReplaying();
            scheduledEvent = null;
            this.state = State.IDLE;
        }

        private void executePacketAndScheduleNext()
        {
            if (scheduledEvent != null)
            {
                currentTime = scheduledEvent.timeOffset;
                parent.updateReplayProgress(1e-3 * currentTime);
                if (scheduledEvent.packet.Equals(EventsRecorder.START))
                {
                    startPlaying();
                }
                else if (scheduledEvent.packet.Equals(EventsRecorder.STOP))
                {
                    stopPlaying();
                }
                else
                {
                    this.handlePacket(scheduledEvent.sender, JSONPacket.createFromJSONBytes(Encoding.UTF8.GetBytes(scheduledEvent.packet)));
                }
            }
            Event nextEvent = player.getNextPlayEvent();
            if (nextEvent != null)
            {
                scheduledEvent = nextEvent;
                timer.Interval = nextEvent.timeOffset - currentTime;
            }
            else
            {
                stopPlayback();
            }
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            timer.Enabled = false;
            executePacketAndScheduleNext();
            timer.Enabled = true;
        }
    }

    class MyUDPCommunication : UDPCommunication
    {
        public MainWindow parent;

        public MyUDPCommunication(MainWindow parent) : base(parent)
        {
            this.parent = parent;
        }

        public override void send(IPAddress address, JSONPacket outPacket)
        {
            string sendString = outPacket.ToString();
            this.parent.writeLog("Pura-pura kirim ke " + address + ": " + sendString);
        }
    }

    class MyEventsRecorder : EventsRecorder
    {
        public override void startRecording()
        {
            // void
        }

        public override void record(IPAddress sender, string eventText)
        {
            // void
        }

        public override void stopRecording()
        {
            // void 
        }
    }
}

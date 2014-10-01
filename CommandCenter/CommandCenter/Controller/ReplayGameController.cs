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

        Timer eventTimer, heartbeatTimer;
        EventsRecorder player;
        Int64 currentTime, heartbeatTime;
        Event scheduledEvent;

        public ReplayGameController(MainWindow parent)
            : base(parent, new MyUDPCommunication(parent), new MyEventsRecorder())
        {
            eventTimer = new Timer();
            eventTimer.Elapsed += OnEventTimedEvent;
            heartbeatTimer = new Timer(1000);
            heartbeatTimer.Elapsed += OnHeartbeatTimedEvent;
            player = new EventsRecorder();
        }

        public void startPlayback()
        {
            player.startReplaying();
            currentTime = heartbeatTime = 0;
            parent.updateReplayProgress(0);
            scheduledEvent = null;
            executePacketAndScheduleNext();
            eventTimer.Enabled = true;
            heartbeatTimer.Enabled = true;
            parent.setReplayingEnabled(true);
        }

        public void stopPlayback()
        {
            eventTimer.Enabled = false;
            heartbeatTimer.Enabled = false;
            player.stopReplaying();
            scheduledEvent = null;
            this.state = State.IDLE;
            parent.setReplayingEnabled(false);
        }

        private void executePacketAndScheduleNext()
        {
            if (scheduledEvent != null)
            {
                currentTime = scheduledEvent.timeOffset;
                parent.updateReplayProgress(1e-3 * currentTime);
                if (scheduledEvent.packet.StartsWith(EventsRecorder.REGISTER))
                {
                    startRegistration(scheduledEvent.packet.Substring(scheduledEvent.packet.Length - 3));
                }
                else if (scheduledEvent.packet.Equals(EventsRecorder.START))
                {
                    startExercise();
                }
                else if (scheduledEvent.packet.Equals(EventsRecorder.STOP))
                {
                    stopExercise();
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
                long interval = nextEvent.timeOffset - currentTime;
                eventTimer.Interval = interval == 0 ? 1 : interval;
            }
            else
            {
                stopPlayback();
            }
        }

        private void OnEventTimedEvent(Object source, ElapsedEventArgs e)
        {
            eventTimer.Enabled = false;
            executePacketAndScheduleNext();
            eventTimer.Enabled = true;
        }

        private void OnHeartbeatTimedEvent(Object source, ElapsedEventArgs e)
        {
            heartbeatTime++;
            parent.updateReplayProgress(heartbeatTime);
        }
    }

    class MyUDPCommunication : UDPCommunication
    {
        public MainWindow parent;

        public MyUDPCommunication(MainWindow parent) : base(parent)
        {
            this.parent = parent;
        }

        public override void listenAsync(AbstractGameController controller)
        {
            // void
        }

        public override void send(IPAddress address, JSONPacket outPacket)
        {
            string sendString = outPacket.ToString();
            this.parent.writeLog("Pura-pura kirim ke " + address + ": " + sendString);
        }
    }

    class MyEventsRecorder : EventsRecorder
    {
        public override void startRecording(string gameId)
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

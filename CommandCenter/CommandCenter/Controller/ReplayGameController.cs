using CommandCenter.Model;
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        Stopwatch stopwatch;
        Event scheduledEvent;

        public ReplayGameController(MainWindow parent)
            : base(parent, new MyUDPCommunication(parent), new MyEventsRecorder())
        {
            stopwatch = new Stopwatch();
            eventTimer = new Timer();
            eventTimer.Elapsed += OnEventTimedEvent;
            heartbeatTimer = new Timer();
            heartbeatTimer.Elapsed += OnHeartbeatTimedEvent;
            player = new EventsRecorder();
        }

        public void startPlayback()
        {
            player.startReplaying();
            stopwatch.Restart();
            parent.updateReplayProgress(0);
            scheduledEvent = null;
            executePacketAndScheduleNext();
            eventTimer.Enabled = true;
            heartbeatTimer.Interval = 1000 / parent.playSpeed;
            heartbeatTimer.Enabled = true;
            parent.setReplayingEnabled(true);
        }

        public void stopPlayback()
        {
            eventTimer.Enabled = false;
            heartbeatTimer.Enabled = false;
            stopwatch.Stop();
            player.stopReplaying();
            scheduledEvent = null;
            this.state = State.IDLE;
            parent.setReplayingEnabled(false);
        }

        private void executePacketAndScheduleNext()
        {
            eventTimer.Enabled = false;
            if (scheduledEvent != null)
            {
                parent.updateReplayProgress(1e-3 * (stopwatch.ElapsedMilliseconds * parent.playSpeed));
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
                long currentTime = (long)(stopwatch.ElapsedMilliseconds * parent.playSpeed);
                long interval = (long)((nextEvent.timeOffset - currentTime) / parent.playSpeed);
                eventTimer.Interval = interval <= 0 ? 1 : interval;
                eventTimer.Enabled = true;
            }
            else
            {
                stopPlayback();
            }
        }

        private void OnEventTimedEvent(Object source, ElapsedEventArgs e)
        {
            executePacketAndScheduleNext();
        }

        private void OnHeartbeatTimedEvent(Object source, ElapsedEventArgs e)
        {
            parent.updateReplayProgress(1e-3 * (stopwatch.ElapsedMilliseconds * parent.playSpeed));
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

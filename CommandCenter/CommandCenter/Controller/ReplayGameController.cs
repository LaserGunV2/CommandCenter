﻿using CommandCenter.Model;
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using NLog;
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
        long skippedMilliseconds;

        public ReplayGameController(MainWindow parent)
            : base(parent, new ReplaySilentUDPCommunication(parent), new ReplaySilentEventsRecorder())
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
            skippedMilliseconds = 0;
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
            bool updateUI = !(parent.skipRegistration && state == State.REGISTRATION);
            if (scheduledEvent != null)
            {
                if (updateUI)
                {
                    parent.updateReplayProgress(1e-3 * ((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed));
                }
                if (scheduledEvent.packet.Equals(EventsRecorder.REGISTER))
                {
                    startRegistration(player.getProperty(EventsRecorder.PROP_GAMEID), Int32.Parse(player.getProperty(EventsRecorder.PROP_AMMO)));
                }
                else if (scheduledEvent.packet.StartsWith(EventsRecorder.START))
                {
                    string[] tokens = scheduledEvent.packet.Split('/');
                    for (int i = 0; i < tokens[1].Length; i++)
                    {
                        prajurits[i].group = "" + tokens[1][i];
                    }
                    startExercise();
                }
                else if (scheduledEvent.packet.Equals(EventsRecorder.STOP))
                {
                    stopExercise(true);
                }
                else
                {
                    parent.writeLog(LogLevel.Info, "Pura-pura terima dari " + scheduledEvent.sender + ": " + scheduledEvent.packet);
                    parent.pesertaDataGrid.Dispatcher.Invoke((Action)(() =>
                    {
                        this.handlePacket(scheduledEvent.sender, JSONPacket.createFromJSONBytes(Encoding.UTF8.GetBytes(scheduledEvent.packet)), updateUI);
                    }));            

                }
            }
            Event nextEvent = player.getNextPlayEvent();
            if (nextEvent != null)
            {
                scheduledEvent = nextEvent;
                long currentTime = (long)((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed);
                long interval = (long)((nextEvent.timeOffset - currentTime) / parent.playSpeed);
                if (parent.skipRegistration && state == State.REGISTRATION)
                {
                    skippedMilliseconds += interval;
                    interval = 0;
                }
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
            if (parent.skipRegistration && state == State.REGISTRATION)
            {
                parent.updateReplayProgress(1e-3 * ((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed), "(>>)");
            }
            else
            {
                parent.updateReplayProgress(1e-3 * ((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed));
            }
        }
    }

    class ReplaySilentUDPCommunication : UDPCommunication
    {
        public ReplaySilentUDPCommunication(MainWindow parent) : base(parent)
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
            this.parent.writeLog(LogLevel.Info, "Pura-pura kirim ke " + address + ": " + sendString);
        }
    }

    class ReplaySilentEventsRecorder : EventsRecorder
    {
        public override void startRecording()
        {
            // silenced
        }

        public override void record(IPAddress sender, string eventText)
        {
            // silenced
        }

        public override void stopRecording()
        {
            // silenced
        }

        public override void setProperty(string name, string value)
        {
            // silenced
        }
    }
}

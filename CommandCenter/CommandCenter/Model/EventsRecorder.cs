using CommandCenter.Model.Protocol;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model.Events
{
    public class EventsRecorder
    {
        public const string START = "START";
        public const string STOP = "STOP";
        public const string FILENAME = "events.sqlite";

        SQLiteConnection connection;
        SQLiteDataReader reader;
        Stopwatch stopwatch;

        public EventsRecorder()
        {
        }

        public virtual void startRecording()
        {
            if (File.Exists(FILENAME))
            {
                File.Delete(FILENAME);
            }

            connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
            SQLiteConnection.CreateFile(FILENAME);
            connection.Open();
            SQLiteCommand command = new SQLiteCommand("CREATE TABLE EVENTS (timeOffset INTEGER, sender TEXT, packet TEXT)", connection);
            command.ExecuteNonQuery();

            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        public virtual void record(IPAddress sender, string eventText)
        {
            long timeOffset = stopwatch.ElapsedMilliseconds;
            SQLiteCommand command = new SQLiteCommand("INSERT INTO events (timeOffset, sender, packet) VALUES (@TIMEOFFSET, @SENDER, @PACKET)", connection);
            command.Parameters.AddWithValue("@TIMEOFFSET", timeOffset);
            command.Parameters.AddWithValue("@SENDER", sender);
            command.Parameters.AddWithValue("@PACKET", eventText);
            int returnValue = command.ExecuteNonQuery();
            if (returnValue != 1)
            {
                throw new Exception("Warning: event not inserted + " + command.ToString());
            }
        }

        public virtual void stopRecording()
        {
            record(null, STOP);
            if (connection != null)
            {
                connection.Close();
                connection = null;
            }
        }

        public Int64 getRecordingLength()
        {
            SQLiteConnection connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
            connection.Open();
            SQLiteCommand command = new SQLiteCommand("SELECT MAX(timeOffset) FROM events", connection);
            Int64 length = (Int64)command.ExecuteScalar();
            connection.Close();
            return length;
        }

        public void startReplaying()
        {
            SQLiteConnection connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
            SQLiteCommand command = new SQLiteCommand("SELECT timeOffset, sender, packet FROM events", connection);
            reader = command.ExecuteReader();
            connection.Open();
        }

        public Event getNextPlayEvent()
        {
            if (reader.Read())
            {
                return new Event(Int64.Parse((string)reader["timeOffset"]), IPAddress.Parse((string)reader["sender"]), (string)reader["packet"]);
            }
            else
            {
                return null;
            }
        }

        public void stopReplaying()
        {
            connection.Close();
        }
    }
}

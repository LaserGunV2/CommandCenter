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
        public const string REGISTER = "REGISTER";
        public const string START = "START";
        public const string STOP = "STOP";
        public const string PROP_GAMEID = "gameId";
        public const string PROP_AMMO = "ammo";
        public const string FILENAME = "events.internal-sqlite";

        public SQLiteConnection connection;
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
            SQLiteCommand command = new SQLiteCommand("CREATE TABLE events (timeOffset INTEGER, sender TEXT, packet TEXT)", connection);
            command.ExecuteNonQuery();
            command = new SQLiteCommand("CREATE TABLE properties (name TEXT PRIMARY KEY UNIQUE, value TEXT)", connection);
            command.ExecuteNonQuery();

            stopwatch = new Stopwatch();
            stopwatch.Start();
            record(null, REGISTER);
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

        public virtual void setProperty(string name, string value)
        {
            SQLiteCommand command = new SQLiteCommand("INSERT OR REPLACE INTO properties(name, value) VALUES(@NAME, @VALUE)", connection);
            command.Parameters.AddWithValue("@NAME", name);
            command.Parameters.AddWithValue("@VALUE", value);
            command.ExecuteNonQuery();
        }

        public string getProperty(string name)
        {
            SQLiteCommand command = new SQLiteCommand("SELECT value FROM properties WHERE name=@NAME", connection);
            command.Parameters.AddWithValue("@NAME", name);
            return (string)command.ExecuteScalar();
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
            connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
            connection.Open();
            SQLiteCommand command = new SQLiteCommand("SELECT timeOffset, sender, packet FROM events", connection);
            reader = command.ExecuteReader();
        }

        public Event getNextPlayEvent()
        {
            if (reader.Read())
            {
                Event newEvent = new Event();
                newEvent.timeOffset = (Int64)reader["timeOffset"];
                newEvent.sender = reader["sender"] is DBNull ? null : IPAddress.Parse((string)reader["sender"]);
                newEvent.packet = reader["packet"] is DBNull ? null : (string)reader["packet"];
                return newEvent;
            }
            else
            {
                return null;
            }
        }

        public void stopReplaying()
        {
            if (connection != null)
            {
                connection.Close();
            }
        }
    }
}

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
        protected const int COMMIT_PERIOD = 100;

        SQLiteDataReader reader;
        SQLiteTransaction transaction;
        Stopwatch stopwatch;
        protected int commitCounter;

        public EventsRecorder()
        {
        }

        public virtual void startRecording()
        {
            ConnectionSingleton.getInstance().resetDatabase();
            stopwatch = new Stopwatch();
            stopwatch.Start();
            commitCounter = 0;
            transaction = ConnectionSingleton.getInstance().connection.BeginTransaction();
            record(null, REGISTER);
        }

        public virtual void record(IPAddress sender, string eventText)
        {
            long timeOffset = stopwatch.ElapsedMilliseconds;
            SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
            SQLiteCommand command = new SQLiteCommand("INSERT INTO events (timeOffset, sender, packet) VALUES (@TIMEOFFSET, @SENDER, @PACKET)", connection);
            command.Parameters.AddWithValue("@TIMEOFFSET", timeOffset);
            command.Parameters.AddWithValue("@SENDER", sender);
            command.Parameters.AddWithValue("@PACKET", eventText);
            int returnValue = command.ExecuteNonQuery();
            if (returnValue != 1)
            {
                throw new Exception("Warning: event not inserted + " + command.ToString());
            }
            commitCounter++;
            if (commitCounter > COMMIT_PERIOD)
            {
                commitCounter = 0;
                transaction.Commit();
                transaction = connection.BeginTransaction();
            }
        }

        public virtual void stopRecording()
        {
            record(null, STOP);
            transaction.Commit();
        }

        public virtual void setProperty(string name, string value)
        {
            SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
            SQLiteCommand command = new SQLiteCommand("INSERT OR REPLACE INTO properties(name, value) VALUES(@NAME, @VALUE)", connection);
            command.Parameters.AddWithValue("@NAME", name);
            command.Parameters.AddWithValue("@VALUE", value);
            command.ExecuteNonQuery();
        }

        public string getProperty(string name)
        {
            SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
            SQLiteCommand command = new SQLiteCommand("SELECT value FROM properties WHERE name=@NAME", connection);
            command.Parameters.AddWithValue("@NAME", name);
            return (string)command.ExecuteScalar();
        }

        public Int64 getRecordingLength()
        {
            SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
            SQLiteCommand command = new SQLiteCommand("SELECT MAX(timeOffset) FROM events", connection);
            Int64 length = (Int64)command.ExecuteScalar();
            return length;
        }

        public void startReplaying()
        {
            SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
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
            // void
        }

        public static void loadFrom(string filename)
        {
            SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
            SQLiteConnection connection2 = new SQLiteConnection("Data Source=" + filename + "; Version=3;");
            connection2.Open();
            ConnectionSingleton.getInstance().resetDatabase();
            new SQLiteCommand("BEGIN", connection).ExecuteNonQuery();
            SQLiteCommand command2 = new SQLiteCommand("SELECT timeOffset, sender, packet FROM events", connection2);
            SQLiteDataReader reader2 = command2.ExecuteReader();
            while (reader2.Read())
            {
                SQLiteCommand command = new SQLiteCommand("INSERT INTO events (timeOffset, sender, packet) VALUES (@TIMEOFFSET, @SENDER, @PACKET)", connection);
                command.Parameters.AddWithValue("@TIMEOFFSET", reader2["timeOffset"]);
                command.Parameters.AddWithValue("@SENDER", reader2["sender"]);
                command.Parameters.AddWithValue("@PACKET", reader2["packet"]);
                command.ExecuteNonQuery();
            }
            command2 = new SQLiteCommand("SELECT name, value FROM properties", connection2);
            reader2 = command2.ExecuteReader();
            while (reader2.Read())
            {
                SQLiteCommand command = new SQLiteCommand("INSERT INTO properties(name, value) VALUES(@NAME, @VALUE)", connection);
                command.Parameters.AddWithValue("@NAME", reader2["name"]);
                command.Parameters.AddWithValue("@VALUE", reader2["value"]);
                command.ExecuteNonQuery();
            }
            new SQLiteCommand("COMMIT", connection).ExecuteNonQuery();
            connection2.Close();
        }

        public static void closeConnection()
        {
            ConnectionSingleton.getInstance().connection.Close();
        }
    }

    class ConnectionSingleton
    {
        protected const string FILENAME = EventsRecorder.FILENAME;
        public SQLiteConnection connection;
        protected static ConnectionSingleton instance = null;

        protected ConnectionSingleton() {
            SQLiteConnection.CreateFile(FILENAME);
            connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
            connection.Open();
            SQLiteCommand command = new SQLiteCommand("CREATE TABLE events (timeOffset INTEGER, sender TEXT, packet TEXT)", connection);
            command.ExecuteNonQuery();
            command = new SQLiteCommand("CREATE TABLE properties (name TEXT PRIMARY KEY UNIQUE, value TEXT)", connection);
            command.ExecuteNonQuery();
        }

        public static ConnectionSingleton getInstance()
        {
            if (instance == null)
            {
                instance = new ConnectionSingleton();
            }
            return instance;
        }

        public void resetDatabase()
        {
            SQLiteCommand command = new SQLiteCommand("DELETE FROM events", connection);
            command.ExecuteNonQuery();
            command = new SQLiteCommand("DELETE FROM properties", connection);
            command.ExecuteNonQuery();
        }
    }
}

using CommandCenter.Model.Protocol;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
    class EventsRecorder
    {
        public const string START = "START";
        public const string STOP = "STOP";

        const string FILENAME = "events.sqlite";

        SQLiteConnection connection;
        Stopwatch stopwatch;

        public EventsRecorder()
        {
            if (File.Exists(FILENAME))
            {
                File.Delete(FILENAME);
            }

            connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
            SQLiteConnection.CreateFile(FILENAME);
            connection.Open();
            SQLiteCommand command = new SQLiteCommand("CREATE TABLE EVENTS (timeOffset INTEGER, packet TEXT)", connection);
            command.ExecuteNonQuery();

            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        public void record(string eventText)
        {
            long timeOffset = stopwatch.ElapsedMilliseconds;
            SQLiteCommand command = new SQLiteCommand("INSERT INTO EVENTS (timeOffset, packet) VALUES (@TIMEOFFSET, @PACKET)", connection);
            command.Parameters.AddWithValue("@TIMEOFFSET", timeOffset);
            command.Parameters.AddWithValue("@PACKET", eventText);
            int returnValue = command.ExecuteNonQuery();
            if (returnValue != 1)
            {
                throw new Exception("Warning: event not inserted + " + command.ToString());
            }
        }

        public void startPlaying()
        {
            record(START);
        }

        public void stopPlaying()
        {
            record(STOP);
            if (connection != null) {
                connection.Close();
            }
        }
    }
}

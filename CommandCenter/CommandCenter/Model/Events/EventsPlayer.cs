using CommandCenter.Model.Protocol;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model.Events
{
    class EventsPlayer
    {
        public const string FILENAME = "events.sqlite";
        public Int64 getLength()
        {
            SQLiteConnection connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
            connection.Open();
            SQLiteCommand command = new SQLiteCommand("SELECT MAX(timeOffset) FROM events", connection);
            Int64 length = (Int64)command.ExecuteScalar();
            connection.Close();
            return length;
        }
    }
}

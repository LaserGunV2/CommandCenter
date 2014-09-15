using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
    class PrajuritDatabase
    {
        SQLiteConnection connection;

        public PrajuritDatabase()
        {
            connection = new SQLiteConnection("Data Source=prajurits.sqlite; Version=3;");
            if (!File.Exists("prajurits.sqlite"))
            {
                createDatabase();
            }
        }

        void createDatabase()
        {
            SQLiteConnection.CreateFile("prajurits.sqlite");
            connection.Open();
            SQLiteCommand command = new SQLiteCommand("CREATE TABLE PRAJURITS (nomerInduk TEXT, name TEXT)", connection);
            command.ExecuteNonQuery();
            connection.Close();
        }

        // TODO storing and retrieving prajurit names.
    }
}

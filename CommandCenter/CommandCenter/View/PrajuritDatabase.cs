using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
    public class PrajuritDatabase
    {
        SQLiteConnection connection;
        public const string FILENAME = "prajurits.internal-sqlite";

        public PrajuritDatabase()
        {
            if (!File.Exists(FILENAME))
            {
                createDatabase();
            }
            connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
            connection.Open();
        }

        void createDatabase()
        {
            SQLiteConnection.CreateFile("prajurits.sqlite");
            connection = new SQLiteConnection("Data Source=" + FILENAME +"; Version=3;");
            connection.Open();
            SQLiteCommand command = new SQLiteCommand("CREATE TABLE prajurits (nomerInduk TEXT PRIMARY KEY UNIQUE, name TEXT)", connection);
            command.ExecuteNonQuery();
            connection.Close();
        }

        public bool retrieveNameFromDatabase(Prajurit prajurit)
        {
            SQLiteCommand command = new SQLiteCommand("SELECT name FROM prajurits WHERE nomerInduk=@NOMERINDUK", connection);
            command.Parameters.AddWithValue("@NOMERINDUK", prajurit.nomerInduk);
            Object result = command.ExecuteScalar();
            if (result == System.DBNull.Value)
            {
                return false;
            }
            else
            {
                prajurit.nama = (String)result;
                return true;
            }
        }

        public void saveNamesToDatabase(List<Prajurit> prajurits)
        {
            foreach (Prajurit prajurit in prajurits)
            {
                SQLiteCommand command = new SQLiteCommand("INSERT OR REPLACE INTO prajurits(nomerInduk, name) VALUES(@NOMERINDUK, @NAME)", connection);
                command.Parameters.AddWithValue("@NOMERINDUK", prajurit.nomerInduk);
                command.Parameters.AddWithValue("@NAME", prajurit.nama);
                command.ExecuteNonQuery();
            }
        }

        public void closeConnection()
        {
            connection.Close();
        }
    }
}

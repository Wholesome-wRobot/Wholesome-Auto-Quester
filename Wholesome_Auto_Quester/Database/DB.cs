using System;
using System.Data;
using System.Data.SQLite;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Helpers;

namespace Wholesome_Auto_Quester.Database
{
    public class DB : IDisposable
    {
        public static SQLiteConnection _con;
        private readonly SQLiteCommand _cmd;

        public DB()
        {
            string baseDirectory = "";

            if (ToolBox.GetWoWVersion() == "2.4.3")
                baseDirectory = AppContext.BaseDirectory + "Data\\WoWDb243";

            if (ToolBox.GetWoWVersion() == "3.3.5")
                baseDirectory = AppContext.BaseDirectory + "Data\\WoWDb335-quests";

            _con = new SQLiteConnection("Data Source=" + baseDirectory);
             _con.Open();
            _cmd = _con.CreateCommand();
        }

        public void Dispose()
        {
            _con?.Close();
        }

        public DataTable SelectQuery(string query)
        {
            var dt = new DataTable();

            try
            {
                _cmd.CommandText = query;
                var ad = new SQLiteDataAdapter(_cmd);
                ad.Fill(dt);
            }
            catch (SQLiteException ex)
            {
                Logging.WriteError("Failed to execute query. " + ex.Message);
            }

            return dt;
        }

        public void ExecuteQuery(string query)
        {
            _cmd.CommandText = query;
            _cmd.ExecuteNonQuery();
        }

        public string GetQuery(string query)
        {
            _cmd.CommandText = query;
            return _cmd.ExecuteScalar().ToString();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using robotManager.Helpful;
using SafeDapper;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;

namespace Wholesome_Auto_Quester.Database
{
    public class DB : IDisposable
    {
        private readonly SQLiteConnection _con;
        private readonly SQLiteCommand _cmd;

        public DB()
        {
            string baseDirectory = "";

            if (ToolBox.GetWoWVersion() == "2.4.3")
                baseDirectory = Others.GetCurrentDirectory + @"Data\WoWDb243";

            if (ToolBox.GetWoWVersion() == "3.3.5")
                baseDirectory = Others.GetCurrentDirectory + @"Data\WoWDb335;Cache=Shared;";

            _con = new SQLiteConnection("Data Source=" + baseDirectory);
             _con.Open();
            _cmd = _con.CreateCommand();
        }

        public void Dispose()
        {
            _con?.Close();
        }

        public List<ModelQuest> SafeQueryQuests(string query)
        {
            return _con.SafeQuery<ModelQuest>(query).ToList();
        }

        public List<ModelNpc> SafeQueryNpcs(string query)
        {
            return _con.SafeQuery<ModelNpc>(query).ToList();
        }

        public List<ModelItem> SafeQueryGatherObjects(string query)
        {
            return _con.SafeQuery<ModelItem>(query).ToList();
        }

        public List<ModelWorldObject> SafeQueryInteractObjects(string query)
        {
            return _con.SafeQuery<ModelWorldObject>(query).ToList();
        }

        public List<ModelArea> SafeQueryAreas(string query)
        {
            return _con.SafeQuery<ModelArea>(query).ToList();
        }

        public List<int> SafeQueryListInts(string query)
        {
            return _con.SafeQuery<int>(query).ToList();
        }

        /*
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
        }*/

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
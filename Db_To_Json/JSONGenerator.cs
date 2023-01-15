using Db_To_Json.AutoQuester;
using System;
using System.Data.SQLite;
using System.IO;

namespace Db_To_Json
{
    internal class JSONGenerator
    {
        public static readonly char PathSep = Path.DirectorySeparatorChar;
        public static readonly string WorkingDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName;
        public static readonly string OutputPath = WorkingDirectory + Path.DirectorySeparatorChar + "Output";
        private static readonly string DBName = "WoWDb335_ACore";
        private static readonly string _dbDirectory = $"{WorkingDirectory}{PathSep}WoWDB{PathSep}{DBName};Cache=Shared;";
        private static SQLiteConnection _con;
        private static SQLiteCommand _cmd;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting generation");

                if (!File.Exists($"{WorkingDirectory}{PathSep}WoWDB{PathSep}{DBName}"))
                {
                    Console.WriteLine("WARNING: The database is absent from the WoWDB folder");
                }
                else
                {
                    _con = new SQLiteConnection("Data Source=" + _dbDirectory);
                    _con.Open();
                    _cmd = _con.CreateCommand();

                    // Auto quester JSON
                    AutoQuesterGeneration.Generate(_con, _cmd);

                    Console.WriteLine("Generation finished");
                    _con.Dispose();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.Read();
        }
    }
}

public enum DBType
{
    TRINITY,
    AZEROTH_CORE
}

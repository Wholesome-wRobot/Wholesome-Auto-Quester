using Db_To_Json.AutoQuester;
using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace Db_To_Json
{
    internal class JSONGenerator
    {
        public static readonly char PathSep = Path.DirectorySeparatorChar;
        public static readonly string WorkingDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName;
        public static readonly string OutputPath = WorkingDirectory + Path.DirectorySeparatorChar + "Output";
        private static readonly string _dbDirectory = $"{WorkingDirectory}{PathSep}WoWDB{PathSep}WoWDb335;Cache=Shared;";
        private static SQLiteConnection _con;
        private static SQLiteCommand _cmd;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting generation");

                if (!File.Exists($"{WorkingDirectory}{PathSep}WoWDB{PathSep}WoWDb335"))
                {
                    Console.WriteLine("WARNING: The database is absent from the WoWDB folder");
                }
                else
                {
                    _con = new SQLiteConnection("Data Source=" + _dbDirectory);
                    _con.Open();
                    _cmd = _con.CreateCommand();

                    // Indice creation
                    Stopwatch indiceWatch = Stopwatch.StartNew();
                    CreateIndices();
                    Console.WriteLine($"Indice creation took {indiceWatch.ElapsedMilliseconds}ms");

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

        private static void CreateIndices()
        {
            ExecuteQuery($@"
                CREATE INDEX IF NOT EXISTS `idx_npc_vendor_item` ON `npc_vendor` (`item`);
                CREATE INDEX IF NOT EXISTS `idx_spell_attributes` ON `spell` (`attributes`);
                CREATE INDEX IF NOT EXISTS `idx_item_template_spellid_2` ON `item_template` (`spellid_2`);
                CREATE INDEX IF NOT EXISTS `idx_item_template_spellid_1` ON `item_template` (`spellid_1`);
                CREATE INDEX IF NOT EXISTS `idx_npc_trainer_spellid` ON `npc_trainer` (`SpellID`);
                CREATE INDEX IF NOT EXISTS `idx_areatrigger_id` ON `areatrigger` (`Id`);
                CREATE INDEX IF NOT EXISTS `idx_areatrigger_id` ON `areatrigger` (`Id`);
                CREATE INDEX IF NOT EXISTS `idx_areatrigger_involvedrelation_id` ON `areatrigger_involvedrelation` (`Id`);
                CREATE INDEX IF NOT EXISTS `idx_areatrigger_involvedrelation_quest` ON `areatrigger_involvedrelation` (`quest`);
                CREATE INDEX IF NOT EXISTS `idx_conditions_source_entry` ON `conditions` (`SourceEntry`);
                CREATE INDEX IF NOT EXISTS `idx_creature_addon_guid` ON `creature_addon` (`guid`);
                CREATE INDEX IF NOT EXISTS `idx_creature_id` ON `creature` (`id`);
                CREATE INDEX IF NOT EXISTS `idx_creature_loot_template_entry` ON `creature_loot_template` (`Entry`);
                CREATE INDEX IF NOT EXISTS `idx_creature_loot_template_item` ON `creature_loot_template` (`Item`);
                CREATE INDEX IF NOT EXISTS `idx_creature_questender_quest` ON `creature_questender` (`quest`);
                CREATE INDEX IF NOT EXISTS `idx_creature_queststarter_quest` ON `creature_queststarter` (`quest`);
                CREATE INDEX IF NOT EXISTS `idx_creature_template_entry` ON `creature_template` (`entry`);
                CREATE INDEX IF NOT EXISTS `idx_creature_template_killcredit1` ON `creature_template` (`KillCredit1`);
                CREATE INDEX IF NOT EXISTS `idx_creature_template_killcredit2` ON `creature_template` (`KillCredit2`);
                CREATE INDEX IF NOT EXISTS `idx_gameobject_id` ON `gameobject` (`id`);
                CREATE INDEX IF NOT EXISTS `idx_gameobject_loot_template_entry` ON `gameobject_loot_template` (`Entry`);
                CREATE INDEX IF NOT EXISTS `idx_gameobject_loot_template_item` ON `gameobject_loot_template` (`Item`);
                CREATE INDEX IF NOT EXISTS `idx_gameobject_queststarter_quest` ON `gameobject_queststarter` (`quest`);
                CREATE INDEX IF NOT EXISTS `idx_gameobject_questender_quest` ON `gameobject_questender` (`quest`);
                CREATE INDEX IF NOT EXISTS `idx_gameobject_template_data1` ON `gameobject_template` (`Data1`);
                CREATE INDEX IF NOT EXISTS `idx_gameobject_template_entry` ON `gameobject_template` (`entry`);
                CREATE INDEX IF NOT EXISTS `idx_item_loot_template_entry` ON `item_loot_template` (`Entry`);
                CREATE INDEX IF NOT EXISTS `idx_item_template_entry` ON `item_template` (`entry`);
                CREATE INDEX IF NOT EXISTS `idx_quest_template_id` ON `quest_template` (`ID`);
                CREATE INDEX IF NOT EXISTS `idx_quest_template_addon_ExclusiveGroup` ON `quest_template_addon` (`ExclusiveGroup`);
                CREATE INDEX IF NOT EXISTS `idx_quest_template_addon_id` ON `quest_template_addon` (`ID`);
                CREATE INDEX IF NOT EXISTS `idx_quest_template_addon_nextquestid` ON `quest_template_addon` (`NextQuestId`);
                CREATE INDEX IF NOT EXISTS `idx_quest_template_addon_prevquestid` ON `quest_template_addon` (`PrevQuestId`);
                CREATE INDEX IF NOT EXISTS `idx_reference_loot_template_item` ON `reference_loot_template` (`Item`);
                CREATE INDEX IF NOT EXISTS `idx_spell_id` ON `spell` (`id`);
                CREATE INDEX IF NOT EXISTS `idx_waypoint_data_id` ON `waypoint_data` (`id`);
            ");
        }

        private static void ExecuteQuery(string query)
        {
            _cmd.CommandText = query;
            _cmd.ExecuteNonQuery();
        }
    }
}

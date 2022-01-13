using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using Dapper;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

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
                baseDirectory = Others.GetCurrentDirectory + @"Data\WoWDb243;Cache=Shared;";

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

        public List<int> QueryPreviousQuestsIdsByQuestId(int questId)
        {
            string query = $@"
                    SELECT ID FROM quest_template_addon
                    WHERE NextQuestId = {questId}
                    GROUP BY ID
                ";
            List<int> result = _con.Query<int>(query).ToList();
            return result;
        }

        public List<int> QueryNextQuestsIdsByQuestId(int questId)
        {
            string query = $@"
                    SELECT ID FROM quest_template_addon
                    WHERE PrevQuestId = {questId}
                    GROUP BY ID
                ";
            List<int> result = _con.Query<int>(query).ToList();
            return result;
        }

        public List<ModelCreatureLootTemplate> QueryCreatureLootTemplatesByItemId(int itemId)
        {
            string queryLootTemplate = $@"
                SELECT *
                FROM creature_loot_template
                WHERE item = {itemId}
            ";
            List<ModelCreatureLootTemplate> result = _con.Query<ModelCreatureLootTemplate>(queryLootTemplate).ToList();

            result.ForEach(clt => clt.CreatureTemplate = QueryCreatureTemplateByEntry(clt.Entry));

            return result;
        }

        public List<ModelGameObjectLootTemplate> QueryGameObjectLootTemplateByEntry(int entry)
        {
            string queryLootTemplate = $@"
                SELECT *
                FROM gameobject_loot_template
                WHERE entry = {entry}
            ";
            List<ModelGameObjectLootTemplate> result = _con.Query<ModelGameObjectLootTemplate>(queryLootTemplate).ToList();
            result.ForEach(golt => golt.GameObjectTemplates = QueryGameObjectTemplatesByLootEntry(golt.Entry));
            return result;
        }

        public List<ModelGameObjectLootTemplate> QueryGameObjectLootTemplateByItem(int itemId)
        {
            string queryLootTemplate = $@"
                SELECT *
                FROM gameobject_loot_template
                WHERE Item = {itemId}
            ";
            List<ModelGameObjectLootTemplate> result = _con.Query<ModelGameObjectLootTemplate>(queryLootTemplate).ToList();
            result.ForEach(golt => golt.GameObjectTemplates = QueryGameObjectTemplatesByLootEntry(golt.Entry));
            return result;
        }

        public ModelItemTemplate QueryItemTemplateByItemEntry(int itemEntry)
        {
            if (itemEntry == 0) return null;

            string queryItemTemplate = $@"
                SELECT * 
                FROM item_template
                WHERE entry = {itemEntry}
            ";
            ModelItemTemplate result = _con.Query<ModelItemTemplate>(queryItemTemplate).FirstOrDefault();
            if (result == null) return null;
            
            result.CreatureLootTemplates = QueryCreatureLootTemplatesByItemId(itemEntry);
            result.GameObjectLootTemplates = QueryGameObjectLootTemplateByItem(itemEntry);
            result.Spell1 = QuerySpellById(result.spellid_1);
            result.Spell2 = QuerySpellById(result.spellid_2);
            result.Spell3 = QuerySpellById(result.spellid_3);
            result.Spell4 = QuerySpellById(result.spellid_4);

            return result;
        }

        public ModelSpell QuerySpellById(int spellID)
        {
            if (spellID == 0) return null;

            string query = $@"
                SELECT *
                FROM spell
                WHERE ID = {spellID}
            ";
            ModelSpell result = _con.Query<ModelSpell>(query).FirstOrDefault();
            return result;
        }

        public ModelCreatureTemplate QueryCreatureTemplateByEntry(int creatureEntry)
        {
            string queryTemplate = $@"
                SELECT *
                FROM creature_template
                WHERE entry = {creatureEntry}
            ";
            ModelCreatureTemplate result = _con.Query<ModelCreatureTemplate>(queryTemplate).FirstOrDefault();

            result.Creatures = QueryCreaturesById(creatureEntry);

            return result;
        }

        public List<ModelCreature> QueryCreaturesById(int creatureId)
        {
            string queryCreature = $@"
                SELECT *
                FROM creature
                WHERE id = {creatureId}
            ";
            List<ModelCreature> result = _con.Query<ModelCreature>(queryCreature).ToList();

            return result.Count > 0 ? result : new List<ModelCreature>();
        }

        public List<ModelAreaTrigger> QueryAreasToExplore(int questId)
        {
            string query = $@"
                SELECT a.ContinentID, a.x PositionX, a.y PositionY, a.z PositionZ, a.radius Radius
                FROM areatrigger_involvedrelation ai
                JOIN areatrigger a 
                ON a.ID = ai.id
                WHERE ai.quest = {questId}
            ";
            List<ModelAreaTrigger> result = _con.Query<ModelAreaTrigger>(query).ToList();
            return result;
        }

        public List<ModelGameObjectTemplate> QueryGameObjectTemplatesByLootEntry(int lootEntry)
        {
            string queryGOTemplate = $@"
                Select *
                FROM gameobject_template
                WHERE data1 = {lootEntry};
            ";
            List<ModelGameObjectTemplate> result = _con.Query<ModelGameObjectTemplate>(queryGOTemplate).ToList();
            result.ForEach(got => { got.GameObjects = QueryGameObjectByEntry(got.entry); });

            return result;
        }

        public ModelGameObjectTemplate QueryGameObjectTemplateByEntry(int objectEntry)
        {
            string queryGOTemplate = $@"
                Select *
                FROM gameobject_template
                WHERE entry = {objectEntry}
            ";
            ModelGameObjectTemplate result = _con.Query<ModelGameObjectTemplate>(queryGOTemplate).FirstOrDefault();
            if (result == null) return null;

            result.GameObjects = QueryGameObjectByEntry(result.entry);

            return result;
        }

        public List<ModelGameObject> QueryGameObjectByEntry(int gameObjectId)
        {
            string query = $@"
                Select *
                FROM gameobject
                WHERE id = {gameObjectId}
            ";
            List<ModelGameObject> result = _con.Query<ModelGameObject>(query).ToList();
            return result.Count > 0 ? result : new List<ModelGameObject>();
        }

        public List<ModelGameObjectTemplate> QueryGameObjectQuestGivers(int questId)
        {
            string queryGOGiverssIds = $@"
                    SELECT id
                    FROM gameobject_queststarter
                    WHERE quest = {questId}
                ";
            List<int> ids = _con.Query<int>(queryGOGiverssIds).ToList();

            List<ModelGameObjectTemplate> result = new List<ModelGameObjectTemplate>();
            ids.ForEach(id => { result.Add(QueryGameObjectTemplateByEntry(id)); });

            return result;
        }

        public List<ModelGameObjectTemplate> QueryGameObjectQuestEnders(int questId)
        {
            string queryGOEndersIds = $@"
                    SELECT id
                    FROM gameobject_questender
                    WHERE quest = {questId}
                ";
            List<int> ids = _con.Query<int>(queryGOEndersIds).ToList();

            List<ModelGameObjectTemplate> result = new List<ModelGameObjectTemplate>();
            ids.ForEach(id => { result.Add(QueryGameObjectTemplateByEntry(id)); });

            return result;
        }

        public List<ModelCreatureTemplate> QueryCreatureQuestEnders(int questId)
        {
            string queryQuestEndersIds = $@"
                    SELECT id
                    FROM creature_questender
                    WHERE quest = {questId}
                ";
            List<int> questEndersIds = _con.Query<int>(queryQuestEndersIds).ToList();

            List<ModelCreatureTemplate> result = new List<ModelCreatureTemplate>();
            questEndersIds.ForEach(id => { result.Add(QueryCreatureTemplateByEntry(id)); });

            return result;
        }

        public List<ModelCreatureTemplate> QueryCreatureQuestGiver(int questId)
        {
            string queryQuestGiversIds = $@"
                    SELECT id
                    FROM creature_queststarter
                    WHERE quest = {questId}
                ";
            List<int> questGiversIds = _con.Query<int>(queryQuestGiversIds).ToList();

            List<ModelCreatureTemplate> result = new List<ModelCreatureTemplate>();
            questGiversIds.ForEach(id => { result.Add(QueryCreatureTemplateByEntry(id)); });

            return result;
        }

        public List<ModelQuestTemplate> QueryQuests()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            int levelDeltaMinus = System.Math.Max((int)ObjectManager.Me.Level - WholesomeAQSettings.CurrentSetting.LevelDeltaMinus, 1);
            int levelDeltaPlus = (int)ObjectManager.Me.Level + WholesomeAQSettings.CurrentSetting.LevelDeltaPlus;

            int myClass = (int)ToolBox.GetClass();
            int myFaction = (int)ToolBox.GetFaction();
            int myLevel = (int)ObjectManager.Me.Level;

            string queryQuest = $@"
                    SELECT * 
                    FROM quest_template
                    WHERE MinLevel <= {myLevel}
                    AND ((QuestLevel <= {levelDeltaPlus} AND  QuestLevel >= {levelDeltaMinus}) OR (QuestLevel = -1));
                ";
            List<ModelQuestTemplate> result = _con.Query<ModelQuestTemplate>(queryQuest).ToList();

            result.ForEach(questTemplate =>
            {
                string queryQuestAddon = $@"
                    SELECT * 
                    FROM quest_template_addon
                    WHERE ID = {questTemplate.Id}
                ";
                ModelQuestTemplateAddon addon = _con.Query<ModelQuestTemplateAddon>(queryQuestAddon).FirstOrDefault();
                questTemplate.QuestAddon = addon ?? new ModelQuestTemplateAddon();
            });

            return result;
        }

        public void CreateIndices()
        {
            Stopwatch stopwatchIndices = Stopwatch.StartNew();
            ExecuteQuery($@"
                CREATE INDEX IF NOT EXISTS `idx_areatrigger_id` ON `areatrigger` (`Id`);
                CREATE INDEX IF NOT EXISTS `idx_areatrigger_id` ON `areatrigger` (`Id`);
                CREATE INDEX IF NOT EXISTS `idx_areatrigger_involvedrelation_id` ON `areatrigger_involvedrelation` (`Id`);
                CREATE INDEX IF NOT EXISTS `idx_areatrigger_involvedrelation_quest` ON `areatrigger_involvedrelation` (`quest`);
                CREATE INDEX IF NOT EXISTS `idx_creature_id` ON `creature` (`id`);
                CREATE INDEX IF NOT EXISTS `idx_creature_loot_template_entry` ON `creature_loot_template` (`Entry`);
                CREATE INDEX IF NOT EXISTS `idx_creature_loot_template_item` ON `creature_loot_template` (`Item`);
                CREATE INDEX IF NOT EXISTS `idx_creature_questender_id` ON `creature_questender` (`id`);
                CREATE INDEX IF NOT EXISTS `idx_creature_questender_quest` ON `creature_questender` (`quest`);
                CREATE INDEX IF NOT EXISTS `idx_creature_queststarter_id` ON `creature_queststarter` (`id`);
                CREATE INDEX IF NOT EXISTS `idx_creature_queststarter_quest` ON `creature_queststarter` (`quest`);
                CREATE UNIQUE INDEX IF NOT EXISTS `idx_creature_template_entry` ON `creature_template` (`entry`);
                CREATE INDEX IF NOT EXISTS `idx_gameobject_id` ON `gameobject` (`id`);
                CREATE INDEX IF NOT EXISTS `idx_gameobject_loot_template_entry` ON `gameobject_loot_template` (`Entry`);
                CREATE INDEX IF NOT EXISTS `idx_gameobject_loot_template_item` ON `gameobject_loot_template` (`Item`);
                CREATE INDEX IF NOT EXISTS `idx_gameobject_template_data1` ON `gameobject_template` (`Data1`);
                CREATE UNIQUE INDEX IF NOT EXISTS `idx_gameobject_template_entry` ON `gameobject_template` (`entry`);
                CREATE UNIQUE INDEX IF NOT EXISTS `idx_item_template_entry` ON `item_template` (`entry`);
                CREATE UNIQUE INDEX IF NOT EXISTS `idx_quest_template_id` ON `quest_template` (`ID`);
                CREATE UNIQUE INDEX IF NOT EXISTS `idx_quest_template_addon_id` ON `quest_template_addon` (`ID`);
                CREATE INDEX IF NOT EXISTS `idx_quest_template_addon_nextquestid` ON `quest_template_addon` (`NextQuestId`);
                CREATE INDEX IF NOT EXISTS `idx_quest_template_addon_prevquestid` ON `quest_template_addon` (`PrevQuestId`);
                CREATE UNIQUE INDEX IF NOT EXISTS `idx_spell_id` ON `spell` (`id`);
            ");
            Logger.Log($"Process time (Indices) : {stopwatchIndices.ElapsedMilliseconds} ms");
        }

        public void ExecuteQuery(string query)
        {
            _cmd.CommandText = query;
            _cmd.ExecuteNonQuery();
        }
    }
}
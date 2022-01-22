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
             string baseDirectory = Others.GetCurrentDirectory + @"Data\WoWDb335;Cache=Shared;";
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

        public List<ModelCreatureLootTemplate> QueryCreatureLootTemplatesByItemEntry(int itemId)
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

        public List<ModelGameObjectLootTemplate> QueryGameObjectLootTemplateByItemEntry(int itemId)
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
            List<ModelItemTemplate> result = _con.Query<ModelItemTemplate>(queryItemTemplate).ToList();
            if (result.Count <= 0) return null;
            if (result.Count > 1) Logger.LogError($"Item entry {itemEntry} has more than one template !");

            result.ForEach(it =>
            {
                it.CreatureLootTemplates = QueryCreatureLootTemplatesByItemEntry(itemEntry);
                it.GameObjectLootTemplates = QueryGameObjectLootTemplateByItemEntry(itemEntry);
                it.ItemLootTemplates = QueryItemLootTemplateByEntry(itemEntry);
                it.Spell1 = QuerySpellById(it.spellid_1);
                it.Spell2 = QuerySpellById(it.spellid_2);
                it.Spell3 = QuerySpellById(it.spellid_3);
                it.Spell4 = QuerySpellById(it.spellid_4);
            });

            return result.FirstOrDefault();
        }

        public List<ModelItemLootTemplate> QueryItemLootTemplateByEntry(int lootEntry)
        {
            if (lootEntry == 0) return null;
            string queryItemLootTemplate = $@"
                SELECT * 
                FROM item_loot_template
                WHERE Entry = {lootEntry}
            ";
            List<ModelItemLootTemplate> result = _con.Query<ModelItemLootTemplate>(queryItemLootTemplate).ToList();
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
            List<ModelSpell> result = _con.Query<ModelSpell>(query).ToList();
            if (result.Count <= 0) return null;
            if (result.Count > 1) Logger.LogError($"Spell ID {spellID} has more than one spells !");
            return result.FirstOrDefault();
        }

        public ModelCreatureTemplate QueryCreatureTemplateByEntry(int creatureEntry)
        {
            if (creatureEntry == 0) return null;
            string queryTemplate = $@"
                SELECT *
                FROM creature_template
                WHERE entry = {creatureEntry}
            ";
            List<ModelCreatureTemplate> result = _con.Query<ModelCreatureTemplate>(queryTemplate).ToList();
            if (result.Count <= 0) return null;
            if (result.Count > 1) Logger.LogError($"Creature entry {creatureEntry} has more than one templates !");
            result.ForEach(ct => ct.Creatures = QueryCreaturesById(creatureEntry));
            return result.FirstOrDefault();
        }

        public List<ModelCreatureTemplate> QueryCreatureTemplatesToGrind()
        {
            uint myLevel = ObjectManager.Me.Level;
            string queryTemplates = $@"
                SELECT * FROM creature_template ct
                WHERE 
	                ct.maxlevel <= {myLevel} 
	                AND ct.minlevel >= {myLevel - 3}
	                AND ct.type = 1
            ";
            List<ModelCreatureTemplate> result = _con.Query<ModelCreatureTemplate>(queryTemplates).ToList();
            if (result.Count <= 0) return null;
            result.ForEach(ct => ct.Creatures = QueryCreaturesById(ct.entry));
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
            if (result.Count > 0)
            {
                List<ModelCreature> creaturesToAddWP = new List<ModelCreature>();
                result.ForEach(c =>
                {
                    c.CreatureAddon = QueryCreaturesAddonsByGuid(c.guid);
                    if (c.CreatureAddon?.WayPoints?.Count > 0)
                    {
                        c.CreatureAddon.WayPoints.RemoveAll(cToRemove => c.CreatureAddon.WayPoints.IndexOf(cToRemove) % 2 != 0);
                        c.CreatureAddon.WayPoints.ForEach(wp =>
                            creaturesToAddWP.Add(new ModelCreature(wp.position_x, wp.position_y, wp.position_z, c.guid, c.map, c.spawnTimeSecs)));
                    }
                });
                result.AddRange(creaturesToAddWP);
            }
            return result.Count > 0 ? result : new List<ModelCreature>();
        }

        public ModelCreatureAddon QueryCreaturesAddonsByGuid(uint guid)
        {
            string queryCreatureAddon = $@"
                SELECT *
                FROM creature_addon
                WHERE guid = {guid}
            ";
            ModelCreatureAddon result = _con.Query<ModelCreatureAddon>(queryCreatureAddon).FirstOrDefault();
            if (result != null && result.path_id > 0) result.WayPoints = QueryWayPointDataByPathId(result.path_id);
            return result;
        }

        public List<ModelWayPointData> QueryWayPointDataByPathId(int pathId)
        {
            string queryWPData = $@"
                SELECT *
                FROM waypoint_data
                WHERE id = {pathId}
            ";
            List<ModelWayPointData> result = _con.Query<ModelWayPointData>(queryWPData).ToList();
            return result.Count > 0 ? result : new List<ModelWayPointData>();
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
            List<ModelGameObjectTemplate> result = _con.Query<ModelGameObjectTemplate>(queryGOTemplate).ToList();
            if (result.Count <= 0) return null;
            if (result.Count > 1) Logger.LogError($"Game Object entry {objectEntry} has more than one templates !");
            result.ForEach(got => got.GameObjects = QueryGameObjectByEntry(got.entry));
            return result.FirstOrDefault();
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
            int[] questSortIdsToIgnore =
            { 
                -24, -101, -121, -181, -182, -201, -264, -304, -324, -762, -371, -373, // profession sortIds
                -1, -21, -22, -23, -25, -41, -221, -241, -284, -344, -364, -365, -366, -367, -368, -369, -370, -374, -375, -376 // misc (epic, seasonal etc)
                // we leave the class sortIds in
            };

            string queryQuest = $@"
                    SELECT * 
                    FROM quest_template
                    WHERE MinLevel <= {myLevel}
                    AND (QuestType <> 0 OR Unknown0 <> 1);
                ";
            List<ModelQuestTemplate> result = _con.Query<ModelQuestTemplate>(queryQuest).ToList();

            result.ForEach(q =>
            {
                if (ToolBox.QuestModifiedLevel.TryGetValue(q.Id, out int levelModifier))
                    q.QuestLevel += levelModifier;
            });
            result.RemoveAll(q => (q.QuestLevel > levelDeltaPlus || q.QuestLevel < levelDeltaMinus) && q.QuestLevel != -1);
            result.RemoveAll(q => questSortIdsToIgnore.Contains(q.QuestSortID));

            result.ForEach(questTemplate =>
            {
                string queryQuestAddon = $@"
                    SELECT * 
                    FROM quest_template_addon
                    WHERE ID = {questTemplate.Id}
                ";
                ModelQuestTemplateAddon addon = _con.Query<ModelQuestTemplateAddon>(queryQuestAddon).FirstOrDefault();
                questTemplate.QuestAddon = addon ?? new ModelQuestTemplateAddon();
                questTemplate.QuestAddon.ExclusiveQuests = QueryQuestIdsByExclusiveGroup(questTemplate.QuestAddon.ExclusiveGroup);
            });

            return result;
        }

        public List<int> QueryQuestIdsByExclusiveGroup(int exclusiveGroup)
        {
            if (exclusiveGroup == 0) return new List<int>();
            string queryQuestExcl = $@"
                    SELECT id
                    FROM quest_template_addon
                    WHERE ExclusiveGroup = {exclusiveGroup}
                ";
            List<int> result = _con.Query<int>(queryQuestExcl).ToList();
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
                CREATE INDEX IF NOT EXISTS `idx_creature_addon_guid` ON `creature_addon` (`guid`);
                CREATE INDEX IF NOT EXISTS `idx_creature_id` ON `creature` (`id`);
                CREATE INDEX IF NOT EXISTS `idx_creature_loot_template_entry` ON `creature_loot_template` (`Entry`);
                CREATE INDEX IF NOT EXISTS `idx_creature_loot_template_item` ON `creature_loot_template` (`Item`);
                CREATE INDEX IF NOT EXISTS `idx_creature_questender_quest` ON `creature_questender` (`quest`);
                CREATE INDEX IF NOT EXISTS `idx_creature_queststarter_quest` ON `creature_queststarter` (`quest`);
                CREATE INDEX IF NOT EXISTS `idx_creature_template_entry` ON `creature_template` (`entry`);
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
                CREATE INDEX IF NOT EXISTS `idx_spell_id` ON `spell` (`id`);
                CREATE INDEX IF NOT EXISTS `idx_waypoint_data_id` ON `waypoint_data` (`id`);
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
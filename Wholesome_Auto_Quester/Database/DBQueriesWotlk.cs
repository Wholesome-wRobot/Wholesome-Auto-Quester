using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Bot;
using System;
using System.Diagnostics;
using robotManager.Products;

namespace Wholesome_Auto_Quester.Database {
    public class DBQueriesWotlk {
        private DB _database;

        public DBQueriesWotlk() {
            _database = new DB();
        }

        public void DisposeDb() {
            _database.Dispose();
        }

        private static List<ModelQuest> FilterDBQuests(List<ModelQuest> dbResult)
        {
            List<ModelQuest> result = new List<ModelQuest>();

            var myClass = (int) ToolBox.GetClass();
            var myFaction = (int) ToolBox.GetFaction();
            var myLevel = (int) ObjectManager.Me.Level;

            foreach (ModelQuest q in dbResult)
            {
                // Our level is too low
                if (myLevel < q.MinLevel) continue;
                // Repeatable/escort quest
                if ((q.SpecialFlags & 1) != 0 || (q.SpecialFlags & 2) != 0) continue;
                // Remove -1 quests that are not Class quests
                if (q.QuestLevel == -1 && q.AllowableClasses == 0) continue;
                // Quest is not for my class
                if (q.AllowableClasses > 0 && (q.AllowableClasses & myClass) == 0) continue;
                // Quest is not for my race
                if (q.AllowableRaces > 0 && (q.AllowableRaces & myFaction) == 0) continue;
                // Quest is not for my faction
                if (!q.QuestGivers.Any(qg => qg.IsNeutralOrFriendly)) continue;
                // Quest is Dungeon/Group/Raid/PvP etc..
                if (q.QuestInfoID != 0) continue;

                result.Add(q);
            }

            return result;
        }

        public void GetAvailableQuests() {
            DateTime dateBegin = DateTime.Now;

            /*Logger.Log($"Building quests from JSON");
            var watch = Stopwatch.StartNew();
            List<ModelQuest> questsFromJSON = ToolBox.GetAllQuestsFromJSON();
            Logger.LogDebug($"Loading the JSON took {watch.ElapsedMilliseconds}ms.");

            if (questsFromJSON != null) {
                DisposeDb();
                Logger.Log($"Building quests from JSON complete ({questsFromJSON.Count} quests)");
                WAQTasks.AddQuests(FilterDBQuests(questsFromJSON));
                return;
            }*/

            if (!ToolBox.WoWDBFileIsPresent()) {
                // DOWNLOAD ZIP ETC..
                Logger.LogError("Couldn't find the database in your wRobot/Data folder");
                DisposeDb();
                Products.ProductStop();
                return;
            }

            DateTime dateBeginIndices = DateTime.Now;
            CreateIndices();
            Logger.Log($"Process time (Indices) : {(DateTime.Now.Ticks - dateBeginIndices.Ticks) / 10000} ms");

            int levelDeltaMinus = (int)ObjectManager.Me.Level - WholesomeAQSettings.CurrentSetting.LevelDeltaMinus;
            int levelDeltaPlus = (int)ObjectManager.Me.Level + WholesomeAQSettings.CurrentSetting.LevelDeltaPlus;

            string query = $@"
                    SELECT qt.ID Id, qt.AllowableRaces, qt.QuestSortID, qt.QuestInfoID, qt.QuestType, qt.StartItem, qt.TimeAllowed, qt.Flags,
                        qt.RequiredItemCount1, qt.RequiredItemCount2, qt.RequiredItemCount3, qt.RequiredItemCount4,
                        qt.RequiredItemId1, qt.RequiredItemId2, qt.RequiredItemId3, qt.RequiredItemId4,
                        qt.ItemDrop1, qt.ItemDrop2, qt.ItemDrop3, qt.ItemDrop4, 
                        qt.ItemDropQuantity1, qt.ItemDropQuantity2, qt.ItemDropQuantity3, qt.ItemDropQuantity4,
                        qt.RequiredNpcOrGo1, qt.RequiredNpcOrGo2, qt.RequiredNpcOrGo3, qt.RequiredNpcOrGo4, 
                        qt.RequiredNpcOrGoCount1, qt.RequiredNpcOrGoCount2, qt.RequiredNpcOrGoCount3, qt.RequiredNpcOrGoCount4,
                        qt.ObjectiveText1, qt.ObjectiveText2, qt.ObjectiveText3, qt.ObjectiveText4, qt.AreaDescription,
                        qt.RequiredNpcOrGoCount1, qt.LogTitle, qt.QuestLevel, qt.MinLevel, qta.AllowableClasses, qta.PrevQuestID, qta.NextQuestID,
                        qta.RequiredSkillID, qta.RequiredSkillPoints, qta.SpecialFlags 
                    FROM quest_template qt
                    LEFT JOIN quest_template_addon qta
                    ON qt.ID = qta.ID
                    WHERE MinLevel <= {(int)ObjectManager.Me.Level}
                    AND (QuestLevel <= {levelDeltaPlus} AND QuestLevel > 0 AND QuestLevel > {levelDeltaMinus})
                ";

            DateTime dateBeginMain = DateTime.Now;
            List<ModelQuest> result = _database.SafeQueryQuests(query);
            Logger.Log($"Process time (Main) : {(DateTime.Now.Ticks - dateBeginMain.Ticks) / 10000} ms");


            DateTime dateBeginObjectives = DateTime.Now;
            List<ModelArea> resultListArea;
            foreach (ModelQuest quest in result)
            {
                int nbObjective = 0;
                // Add explore objectives
                if ((resultListArea = GetAreasToExplore(quest.Id)).Count > 0)
                {
                    resultListArea.ForEach(area =>
                    {
                        quest.ExplorationObjectives.Add(new ExplorationObjective((int)area.PositionX, area, ++nbObjective));
                    });
                }

                List<ModelGatherObject> gatherItems1 = quest.RequiredItemId1 != 0 ? GetGatherObjects(quest.RequiredItemId1) : null;
                List<ModelNpc> lootItems1 = quest.RequiredItemId1 != 0 ? GetCreatureToLoot(quest.RequiredItemId1) : null;
                List<ModelGatherObject> gatherItems2 = quest.RequiredItemId2 != 0 ? GetGatherObjects(quest.RequiredItemId2) : null;
                List<ModelNpc> lootItems2 = quest.RequiredItemId2 != 0 ? GetCreatureToLoot(quest.RequiredItemId2) : null;
                List<ModelGatherObject> gatherItems3 = quest.RequiredItemId3 != 0 ? GetGatherObjects(quest.RequiredItemId3) : null;
                List<ModelNpc> lootItems3 = quest.RequiredItemId3 != 0 ? GetCreatureToLoot(quest.RequiredItemId3) : null;
                List<ModelGatherObject> gatherItems4 = quest.RequiredItemId4 != 0 ? GetGatherObjects(quest.RequiredItemId4) : null;
                List<ModelNpc> lootItems4 = quest.RequiredItemId4 != 0 ? GetCreatureToLoot(quest.RequiredItemId4) : null;
                List<ModelGatherObject> gatherItems5 = quest.RequiredItemId5 != 0 ? GetGatherObjects(quest.RequiredItemId5) : null;
                List<ModelNpc> lootItems5 = quest.RequiredItemId5 != 0 ? GetCreatureToLoot(quest.RequiredItemId5) : null;
                List<ModelGatherObject> gatherItems6 = quest.RequiredItemId6 != 0 ? GetGatherObjects(quest.RequiredItemId6) : null;
                List<ModelNpc> lootItems6 = quest.RequiredItemId6 != 0 ? GetCreatureToLoot(quest.RequiredItemId6) : null;

                // Add gather world items
                if (gatherItems1?.Count > 0)
                {
                    if (gatherItems1.Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount1, gatherItems1, ++nbObjective));
                    if (lootItems1.Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount1, lootItems1, nbObjective));
                }
                if (gatherItems2?.Count > 0)
                {
                    if (gatherItems2.Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount2, gatherItems2, ++nbObjective));
                    if (lootItems2.Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount2, lootItems2, nbObjective));
                }
                if (gatherItems3?.Count > 0)
                {
                    if (gatherItems3.Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount3, gatherItems3, ++nbObjective));
                    if (lootItems3.Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount3, lootItems3, nbObjective));
                }
                if (gatherItems4?.Count > 0)
                {
                    if (gatherItems4.Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount4, gatherItems4, ++nbObjective));
                    if (lootItems4.Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount4, lootItems4, nbObjective));
                }
                if (gatherItems5?.Count > 0)
                {
                    if (gatherItems5.Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount5, gatherItems5, ++nbObjective));
                    if (lootItems5.Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount5, lootItems5, nbObjective));
                }
                if (gatherItems6?.Count > 0)
                {
                    if (gatherItems6.Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount6, gatherItems6, ++nbObjective));
                    if (lootItems6.Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount6, lootItems6, nbObjective));
                }

                // Add creatures to kill
                if (quest.RequiredNpcOrGoCount1 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.RequiredNpcOrGoCount1, GetCreaturesToKill(quest.RequiredNpcOrGo1), ++nbObjective));
                if (quest.RequiredNpcOrGoCount2 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.RequiredNpcOrGoCount2, GetCreaturesToKill(quest.RequiredNpcOrGo2), ++nbObjective));
                if (quest.RequiredNpcOrGoCount3 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.RequiredNpcOrGoCount3, GetCreaturesToKill(quest.RequiredNpcOrGo3), ++nbObjective));
                if (quest.RequiredNpcOrGoCount4 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.RequiredNpcOrGoCount4, GetCreaturesToKill(quest.RequiredNpcOrGo4), ++nbObjective));


                // Add creature loot items
                if ((gatherItems1 == null || gatherItems1.Count <= 0) && lootItems1?.Count > 0)
                    quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount1, lootItems1, ++nbObjective));
                if ((gatherItems2 == null || gatherItems2.Count <= 0) && lootItems2?.Count > 0)
                    quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount2, lootItems2, ++nbObjective));
                if ((gatherItems3 == null || gatherItems3.Count <= 0) && lootItems3?.Count > 0)
                    quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount3, lootItems3, ++nbObjective));
                if ((gatherItems4 == null || gatherItems4.Count <= 0) && lootItems4?.Count > 0)
                    quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount4, lootItems4, ++nbObjective));
                if ((gatherItems5 == null || gatherItems5.Count <= 0) && lootItems5?.Count > 0)
                    quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount5, lootItems5, ++nbObjective));
                if ((gatherItems6 == null || gatherItems6.Count <= 0) && lootItems6?.Count > 0)
                    quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount6, lootItems6, ++nbObjective));
            }

            Logger.Log($"Process time (Objectives) : {(DateTime.Now.Ticks - dateBeginObjectives.Ticks) / 10000} ms");

            // Get quest givers
            DateTime dateBeginQuestGivers = DateTime.Now;
            foreach (ModelQuest quest in result) {
                quest.QuestGivers = GetQuestGivers(quest.Id);
            }

            Logger.Log($"Process time (Quest givers) : {(DateTime.Now.Ticks - dateBeginQuestGivers.Ticks) / 10000} ms");

            // Get quest enders
            DateTime dateBeginQuestEnder = DateTime.Now;
            foreach (ModelQuest quest in result) {
                quest.QuestTurners = GetQuestTurners(quest.Id);
            }

            Logger.Log($"Process time (Quest enders) : {(DateTime.Now.Ticks - dateBeginQuestEnder.Ticks) / 10000} ms");

            // Get previous quests Ids
            DateTime dateBeginPreviousQuests = DateTime.Now;
            foreach (ModelQuest quest in result) {
                quest.PreviousQuestsIds = GetPreviousQuestsIds(quest.Id);
                if (quest.PrevQuestID != 0 && !quest.PreviousQuestsIds.Contains(quest.PrevQuestID))
                    quest.PreviousQuestsIds.Add(quest.PrevQuestID);
            }
            Logger.Log($"Process time (Previous quests) : {(DateTime.Now.Ticks - dateBeginPreviousQuests.Ticks) / 10000} ms");

            // Get next quests ids
            DateTime dateBeginNextQuests = DateTime.Now;
            foreach (ModelQuest quest in result) {
                quest.NextQuestsIds = GetNextQuestsIds(quest.Id);
                if (quest.NextQuestID != 0 && !quest.NextQuestsIds.Contains(quest.NextQuestID))
                    quest.NextQuestsIds.Add(quest.NextQuestID);
            }

            Logger.Log($"Process time (Next quests) : {(DateTime.Now.Ticks - dateBeginNextQuests.Ticks) / 10000} ms");

            DisposeDb();

            if (WholesomeAQSettings.CurrentSetting.DevMode)
            {
                DateTime dateBeginNJSON = DateTime.Now;
                Logger.Log($"{result.Count} results. Building JSON. Please wait.");
                ToolBox.UpdateCompletedQuests();
                ToolBox.WriteJSONFromDBResult(result);
                ToolBox.ZipJSONFile();
                Logger.Log($"Process time (JSON processing) : {(DateTime.Now.Ticks - dateBeginNJSON.Ticks) / 10000} ms");
            }

            Logger.Log($"DONE! Process time (TOTAL) : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");

            WAQTasks.AddQuests(FilterDBQuests(result));
        }

        private void CreateIndices()
        {
            _database.ExecuteQuery($@"
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
            ");
        }

        private List<ModelNpc> GetQuestGivers(int questId) {
            string query = $@"
                    SELECT cq.id Id, ct.name Name, c.guid Guid, c.map Map, c.spawntimesecs SpawnTimeSecs,
	                    c.position_x PositionX, c.position_y PositionY, c.position_z PositionZ,
	                    ct.faction FactionTemplateID
                    FROM creature_queststarter cq
                    JOIN creature_template ct
                    ON ct.Entry = cq.id
                    JOIN creature c
                    ON c.id = cq.id
                    WHERE cq.quest = {questId}
                ";

            return _database.SafeQueryNpcs(query);
        }

        private List<ModelNpc> GetQuestTurners(int questId) {
            string query = $@"
                    SELECT ci.id Id, ct.Name, c.guid Guid, c.map Map, c.position_x PositionX, 
                    c.position_y PositionY, c.position_z PositionZ, c.spawntimesecs SpawnTimeSecs,
	                ct.faction FactionTemplateID
                    FROM creature_questender ci
                    JOIN creature_template ct
                    ON ct.Entry = ci.id
                    JOIN creature c
                    ON c.id = ci.id
                    WHERE ci.quest = {questId}
                ";

            return _database.SafeQueryNpcs(query);
        }

        private List<ModelNpc> GetCreatureToLoot(int itemid) {
            string query = $@"
                SELECT clt.entry Id, ct.name Name, c.guid Guid, c.map Map, c.position_x PositionX, 
                    c.position_y PositionY, c.position_z PositionZ, c.spawntimesecs SpawnTimeSecs,
                    it.name ItemName, ct.minlevel MinLevel, ct.maxlevel MaxLevel,
	                ct.faction FactionTemplateID
                FROM creature_loot_template clt
                JOIN creature_template ct
                ON clt.entry = ct.entry
                JOIN creature c
                ON id = ct.entry
                JOIN item_template it
                ON it.entry = {itemid}
                WHERE item = {itemid}
            ";

            return _database.SafeQueryNpcs(query);
        }

        private List<ModelNpc> GetCreaturesToKill(int creatureId) {
            string query = $@"
                SELECT c.Id, ct.Name, c.guid Guid, c.map Map, c.position_x PositionX, 
                c.position_y PositionY, c.position_z PositionZ, c.spawntimesecs SpawnTimeSecs,
	            ct.faction FactionTemplateID, ct.minlevel MinLevel, ct.maxlevel MaxLevel
                FROM creature c
                JOIN creature_template ct
                ON ct.Entry = c.id
                WHERE c.id = {creatureId}
            ";

            return _database.SafeQueryNpcs(query);
        }

        private List<ModelGatherObject> GetGatherObjects(int objectId) {
            string query = $@"
                SELECT it.entry Entry, it.class Class, it.subclass SubClass, it.name Name, it.displayid DisplayId, 
	                it.Quality, it.Flags, glt.Entry GOLootEntry, gt.entry GameObjectEntry, g.guid Guid, g.map Map, 
	                g.position_x PositionX, g.position_y PositionY, g.position_z PositionZ, g.spawntimesecs SpawnTimeSecs
                FROM item_template it
                JOIN gameobject_loot_template glt
                ON glt.item = it.entry
                JOIN gameobject_template gt
                ON gt.data1 = GOLootEntry
                JOIN gameobject g
                ON g.id = GameObjectEntry
                WHERE it.entry == {objectId}
            ";

            return _database.SafeQueryGatherObjects(query);
        }

        private List<ModelArea> GetAreasToExplore(int questId)
        {
            string query = $@"
                SELECT a.ContinentID, a.x PositionX, a.y PositionY, a.z PositionZ, a.radius Radius
                FROM areatrigger_involvedrelation ai
                JOIN areatrigger a 
                ON a.ID = ai.id
                WHERE ai.quest = {questId}
            ";

            return _database.SafeQueryAreas(query);
        }

        private List<int> GetNextQuestsIds(int questId)
        {
            string query = $@"
                    SELECT ID FROM quest_template_addon
                    WHERE PrevQuestId = {questId}
                    GROUP BY ID
                ";

            return _database.SafeQueryListInts(query);
        }

        private List<int> GetPreviousQuestsIds(int questId) {
            string query = $@"
                    SELECT ID FROM quest_template_addon
                    WHERE NextQuestId = {questId}
                    GROUP BY ID
                ";

            return _database.SafeQueryListInts(query);
        }
    }
}

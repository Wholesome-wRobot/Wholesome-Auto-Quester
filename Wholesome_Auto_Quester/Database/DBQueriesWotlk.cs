using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Bot;
using System;
using System.Diagnostics;
using robotManager.Helpful;
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

        private List<ModelQuest> FilterDBQuests(List<ModelQuest> dbResult)
        {
            List<ModelQuest> result = new List<ModelQuest>();

            var myClass = (int) ToolBox.GetClass();
            var myFaction = (int) ToolBox.GetFaction();
            var myLevel = (int) ObjectManager.Me.Level;

            foreach (ModelQuest q in dbResult)
            {
                // Our level is too low
                if (myLevel < q.MinLevel) continue;

                // Repeatable quest
                if (q.SpecialFlags == 1) continue;

                // Quest is too hard
                if (myLevel + 2 < q.QuestLevel) continue;

                // Remove -1 quests that are not Class quests
                if (q.QuestLevel == -1 && q.AllowableClasses == 0) continue;

                // Quest is too low level
                if (q.QuestLevel <= myLevel - 5 || q.QuestLevel == -1) continue;

                // Quest is not for my class
                if (q.AllowableClasses > 0 && (q.AllowableClasses & myClass) == 0) continue;

                // Quest is not for my race
                if (q.AllowableRaces > 0 && (q.AllowableRaces & myFaction) == 0) continue;

                // Quest is not for my faction
                if (!q.QuestGivers.Any(qg => qg.IsNeutralOrFriendly)) continue;

                result.Add(q);
            }

            return result;
            /*
            return dbResult.Where(q =>
                        q.MinLevel <= ObjectManager.Me.Level
                        && q.SpecialFlags != 1
                        && (q.QuestLevel > 0 || (q.QuestLevel == -1 && q.AllowableClasses > 0))
                        && q.QuestLevel <= (int)(ObjectManager.Me.Level + 2)
                        && (q.QuestLevel > (int)(ObjectManager.Me.Level - 5) || (q.QuestLevel == -1 && q.AllowableClasses > 0))

                        && ((q.AllowableClasses & (int)ToolBox.GetClass()) != 0 || q.AllowableClasses == 0)
                        && ((q.AllowableRaces & (int)ToolBox.GetFaction()) != 0 || q.AllowableRaces == 0)
                        && q.QuestGivers.Any(qg => qg.IsNeutralOrFriendly)
                    ).ToList();*/
        }

        public void GetAvailableQuests() {
            DateTime dateBegin = DateTime.Now;

            Logger.Log($"Building quests from JSON");
            var watch = Stopwatch.StartNew();
            List<ModelQuest> questsFromJSON = ToolBox.GetAllQuestsFromJSON();
            Logger.LogDebug($"Loading the JSON took {watch.ElapsedMilliseconds}ms.");

            if (questsFromJSON != null) {
                DisposeDb();
                Logger.Log($"Building quests from JSON complete ({questsFromJSON.Count} quests)");
                WAQTasks.AddQuests(FilterDBQuests(questsFromJSON));
                return;
            }

            if (!ToolBox.WoWDBFileIsPresent()) {
                // DOWNLOAD ZIP ETC..
                DisposeDb();
                return;
            }

            Logger.Log("Rebuilding JSON");

            string query = $@"
                    SELECT qt.ID Id, qt.AllowableRaces, qt.QuestSortID, qt.QuestInfoID, qt.QuestType, qt.StartItem, qt.TimeAllowed,
                        qt.RequiredItemCount1, qt.RequiredItemCount2, qt.RequiredItemCount3, qt.RequiredItemCount4,
                        qt.RequiredItemCount5, qt.RequiredItemCount6, qt.RequiredItemId1, qt.RequiredItemId2, qt.RequiredItemId3,
                        qt.RequiredItemId4, qt.RequiredItemId5, qt.RequiredItemId6, qt.RequiredNpcOrGo1, qt.RequiredNpcOrGo2,
                        qt.RequiredNpcOrGo3, qt.RequiredNpcOrGo4, qt.RequiredNpcOrGoCount1, qt.RequiredNpcOrGoCount2, qt.RequiredNpcOrGoCount3,
                        qt.RequiredNpcOrGoCount1, qt.LogTitle, qt.QuestLevel, qt.MinLevel, qta.AllowableClasses, qta.PrevQuestID, qta.NextQuestID,
                        qta.RequiredSkillID, qta.RequiredSkillPoints, qta.SpecialFlags 
                    FROM quest_template qt
                    LEFT JOIN quest_template_addon qta
                    ON qt.ID = qta.ID
                ";

            DateTime dateBeginMain = DateTime.Now;
            List<ModelQuest> result = _database.SafeQueryQuests(query);
            Logger.Log($"Process time (Main) : {(DateTime.Now.Ticks - dateBeginMain.Ticks) / 10000} ms");

            List<ModelGatherObject> resultListObj;
            List<ModelNpc> resultListCreature;
            List<ModelArea> resultListArea;

            // Get explore objectives
            DateTime dateBeginExplores = DateTime.Now;
            foreach (ModelQuest quest in result)
            {
                if ((resultListArea = GetAreasToExplore(quest.Id)).Count > 0)
                {
                    resultListArea.ForEach(area =>
                    {
                        quest.ExplorationObjectives.Add(new ExplorationObjective((int)area.PositionX, area, resultListArea.IndexOf(area) + 1));
                    });
                }
            }
            Logger.Log($"Process time (Exploration objectives) : {(DateTime.Now.Ticks - dateBeginExplores.Ticks) / 10000} ms");

            // Get gather objects
            DateTime dateBeginGatherObjects = DateTime.Now;
            foreach (ModelQuest quest in result) {
            foreach (ModelQuest quest in result)
            {
                int nbExploreObjectives = quest.ExplorationObjectives.Count;
                // Add gather Objects
                if (quest.RequiredItemId1 != 0) {
                    if ((resultListObj = GetGatherObjects(quest.RequiredItemId1)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount1, quest.RequiredItemId1, resultListObj, 1 + nbExploreObjectives));
                    else if ((resultListCreature = GetCreatureToLoot(quest.RequiredItemId1)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount1, resultListCreature[0].ItemName, resultListCreature, 1 + nbExploreObjectives));
                }

                if (quest.RequiredItemId2 != 0) {
                    if ((resultListObj = GetGatherObjects(quest.RequiredItemId2)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount2, quest.RequiredItemId2, resultListObj, 2 + nbExploreObjectives));
                    else if ((resultListCreature = GetCreatureToLoot(quest.RequiredItemId2)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount2, resultListCreature[0].ItemName, resultListCreature, 2 + nbExploreObjectives));
                }

                if (quest.RequiredItemId3 != 0) {
                    if ((resultListObj = GetGatherObjects(quest.RequiredItemId3)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount3, quest.RequiredItemId3, resultListObj, 3 + nbExploreObjectives));
                    else if ((resultListCreature = GetCreatureToLoot(quest.RequiredItemId3)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount3, resultListCreature[0].ItemName, resultListCreature, 3 + nbExploreObjectives));
                }

                if (quest.RequiredItemId4 != 0) {
                    if ((resultListObj = GetGatherObjects(quest.RequiredItemId4)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount4, quest.RequiredItemId4, resultListObj, 4 + nbExploreObjectives));
                    else if ((resultListCreature = GetCreatureToLoot(quest.RequiredItemId4)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount4, resultListCreature[0].ItemName, resultListCreature, 4 + nbExploreObjectives));
                }

                if (quest.RequiredItemId5 != 0) {
                    if ((resultListObj = GetGatherObjects(quest.RequiredItemId5)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount5, quest.RequiredItemId5, resultListObj, 5 + nbExploreObjectives));
                    else if ((resultListCreature = GetCreatureToLoot(quest.RequiredItemId5)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount5, resultListCreature[0].ItemName, resultListCreature, 5 + nbExploreObjectives));
                }

                if (quest.RequiredItemId6 != 0) {
                    if ((resultListObj = GetGatherObjects(quest.RequiredItemId6)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount6, quest.RequiredItemId6, resultListObj, 6 + nbExploreObjectives));
                    else if ((resultListCreature = GetCreatureToLoot(quest.RequiredItemId6)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount6, resultListCreature[0].ItemName, resultListCreature, 6 + nbExploreObjectives));
                }
            }
            Logger.Log($"Process time (Gather objects) : {(DateTime.Now.Ticks - dateBeginGatherObjects.Ticks) / 10000} ms");

            // Get creatures to kill
            DateTime dateBeginCreaturesTokill = DateTime.Now;
            foreach (ModelQuest quest in result)
            {
                int nbExploreObjectives = quest.ExplorationObjectives.Count;
                // Add creatures to kill
                if (quest.RequiredNpcOrGoCount1 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.RequiredNpcOrGoCount1, quest.RequiredNpcOrGo1, GetCreaturesToKill(quest.RequiredNpcOrGo1), 1 + nbExploreObjectives));
                if (quest.RequiredNpcOrGoCount2 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.RequiredNpcOrGoCount2, quest.RequiredNpcOrGo2, GetCreaturesToKill(quest.RequiredNpcOrGo2), 2 + nbExploreObjectives));
                if (quest.RequiredNpcOrGoCount3 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.RequiredNpcOrGoCount3, quest.RequiredNpcOrGo3, GetCreaturesToKill(quest.RequiredNpcOrGo3), 3 + nbExploreObjectives));
                if (quest.RequiredNpcOrGoCount4 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.RequiredNpcOrGoCount4, quest.RequiredNpcOrGo4, GetCreaturesToKill(quest.RequiredNpcOrGo4), 4 + nbExploreObjectives));
            }
            Logger.Log($"Process time (Creatures to kill) : {(DateTime.Now.Ticks - dateBeginCreaturesTokill.Ticks) / 10000} ms");

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

            Logger.Log($"{result.Count} results. Building JSON. Please wait.");

            DisposeDb();


            DateTime dateBeginNJSON = DateTime.Now;

            ToolBox.WriteJSONFromDBResult(result);
            ToolBox.ZipJSONFile();
            Logger.Log($"Process time (JSON processing) : {(DateTime.Now.Ticks - dateBeginNJSON.Ticks) / 10000} ms");
            Logger.Log($"DONE! Process time (TOTAL) : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
            Products.ProductStop();
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
                    it.name ItemName,
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
	            ct.faction FactionTemplateID
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

using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;
using SafeDapper;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Bot;
using System;

namespace Wholesome_Auto_Quester.Database
{
    public class DBQueriesWotlk
    {
        private static DB _database;
        public static bool QueryIsRunning;

        public static void Initialize(DB database)
        {
            if (_database == null)
                _database = database;
        }

        public static void GetAvailableQuests()
        {
            if (QueryIsRunning)
                return;

            QueryIsRunning = true;
            DateTime dateBegin = DateTime.Now;
            /*
            List<ModelQuest> questsFromJSON = ToolBox.GetAllQuestsFromJSON();
            Logger.Log($"Building quests from JSON complete ({questsFromJSON.Count} quests)");
            if (questsFromJSON != null)
            {
                Logger.Log("Getting quests from JSON");
                WAQTasks.AddQuests(questsFromJSON.Where(q => 
                        q.MinLevel <= ObjectManager.Me.Level
                        && q.QuestLevel > 0
                        && q.QuestLevel <= ObjectManager.Me.Level + 2
                        && q.QuestLevel > ObjectManager.Me.Level - 5
                    ).ToList());
                QueryIsRunning = false;
                return;
            }

            if (!ToolBox.WoWDBFileIsPresent())
            {
                // DOWNLOAD ZIP ETC..
                QueryIsRunning = false;
                return;
            }
            
            Logger.Log("Rebuilding JSON");

            string query = $@"
                    SELECT qt.ID Id, qt.AllowableRaces, qt.RequiredItemCount1, qt.RequiredItemCount2, qt.RequiredItemCount3, qt.RequiredItemCount4,
                        qt.RequiredItemCount5, qt.RequiredItemCount6, qt.RequiredItemId1, qt.RequiredItemId2, qt.RequiredItemId3,
                        qt.RequiredItemId4, qt.RequiredItemId5, qt.RequiredItemId6, qt.RequiredNpcOrGo1, qt.RequiredNpcOrGo2,
                        qt.RequiredNpcOrGo3, qt.RequiredNpcOrGo4, qt.RequiredNpcOrGoCount1, qt.RequiredNpcOrGoCount2, qt.RequiredNpcOrGoCount3,
                        qt.RequiredNpcOrGoCount1, qt.LogTitle, qt.QuestLevel, qt.MinLevel, qta.AllowableClasses, qta.PrevQuestID, qta.NextQuestID,
                        qta.RequiredSkillID, qta.RequiredSkillPoints
                    FROM quest_template qt
                    JOIN creature_queststarter cq
                    ON qt.ID = cq.quest
                    JOIN quest_template_addon qta
                    ON qta.ID = qt.ID
                    WHERE (QuestType = 0 OR QuestType = 2)
                ";

            */
            int MaxQuestLevel = (int)ObjectManager.Me.Level + 2;
            int MinQuestLevel = (int)ObjectManager.Me.Level - 5;

            string query = $@"
                    SELECT qt.ID Id, qt.AllowableRaces, qt.RequiredItemCount1, qt.RequiredItemCount2, qt.RequiredItemCount3, qt.RequiredItemCount4,
                        qt.RequiredItemCount5, qt.RequiredItemCount6, qt.RequiredItemId1, qt.RequiredItemId2, qt.RequiredItemId3,
                        qt.RequiredItemId4, qt.RequiredItemId5, qt.RequiredItemId6, qt.RequiredNpcOrGo1, qt.RequiredNpcOrGo2,
                        qt.RequiredNpcOrGo3, qt.RequiredNpcOrGo4, qt.RequiredNpcOrGoCount1, qt.RequiredNpcOrGoCount2, qt.RequiredNpcOrGoCount3,
                        qt.RequiredNpcOrGoCount1, qt.LogTitle, qt.QuestLevel, qt.MinLevel, qta.AllowableClasses, qta.PrevQuestID, qta.NextQuestID,
                        qta.RequiredSkillID, qta.RequiredSkillPoints
                    FROM quest_template qt
                    JOIN creature_queststarter cq
                    ON qt.ID = cq.quest
                    JOIN quest_template_addon qta
                    ON qta.ID = qt.ID
                    WHERE MinLevel <= {ObjectManager.Me.Level}
                    AND (QuestLevel <= {MaxQuestLevel} AND QuestLevel > 0 AND QuestLevel > {MinQuestLevel})
                    AND (QuestType = 0 OR QuestType = 2)
                    AND (AllowableClasses & {(uint)ToolBox.GetClass()} <> 0 OR AllowableClasses = 0)
                    AND (AllowableRaces & {(uint)ToolBox.GetFaction()} <> 0 OR AllowableRaces = 0)
                ";

            Logger.Log(query);

            List<ModelQuest> result = DB._con.SafeQuery<ModelQuest>(query).ToList();

            List<ModelGatherObject> resultListObj;
            List<ModelNpc> resultListCreature;
            foreach (ModelQuest quest in result)
            {
                // Add gather Objects
                if (quest.RequiredItemId1 != 0)
                {
                    if ((resultListObj = GetGatherObjects(quest.RequiredItemId1)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount1, quest.RequiredItemId1, resultListObj, 1));
                    else if ((resultListCreature = GetCreatureToLoot(quest.RequiredItemId1)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount1, resultListCreature[0].ItemName, resultListCreature, 1));
                }

                if (quest.RequiredItemId2 != 0)
                {
                    if ((resultListObj = GetGatherObjects(quest.RequiredItemId2)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount2, quest.RequiredItemId2, resultListObj, 2));
                    else if ((resultListCreature = GetCreatureToLoot(quest.RequiredItemCount2)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount2, resultListCreature[0].ItemName, resultListCreature, 2));
                }

                if (quest.RequiredItemId3 != 0)
                {
                    if ((resultListObj = GetGatherObjects(quest.RequiredItemId3)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount3, quest.RequiredItemId3, resultListObj, 3));
                    else if ((resultListCreature = GetCreatureToLoot(quest.RequiredItemCount3)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount3, resultListCreature[0].ItemName, resultListCreature, 3));
                }

                if (quest.RequiredItemId4 != 0)
                {
                    if ((resultListObj = GetGatherObjects(quest.RequiredItemId4)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount4, quest.RequiredItemId4, resultListObj, 4));
                    else if ((resultListCreature = GetCreatureToLoot(quest.RequiredItemCount4)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount4, resultListCreature[0].ItemName, resultListCreature, 4));
                }

                if (quest.RequiredItemId5 != 0)
                {
                    if ((resultListObj = GetGatherObjects(quest.RequiredItemId5)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount5, quest.RequiredItemId5, resultListObj, 5));
                    else if ((resultListCreature = GetCreatureToLoot(quest.RequiredItemCount5)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount5, resultListCreature[0].ItemName, resultListCreature, 5));
                }

                if (quest.RequiredItemId6 != 0)
                {
                    if ((resultListObj = GetGatherObjects(quest.RequiredItemId6)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.RequiredItemCount6, quest.RequiredItemId6, resultListObj, 6));
                    else if ((resultListCreature = GetCreatureToLoot(quest.RequiredItemCount6)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.RequiredItemCount6, resultListCreature[0].ItemName, resultListCreature, 6));
                }

                // Add creatures to kill
                if (quest.RequiredNpcOrGoCount1 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.RequiredNpcOrGoCount1, quest.RequiredNpcOrGo1, GetCreaturesToKill(quest.RequiredNpcOrGo1), 1));
                if (quest.RequiredNpcOrGoCount2 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.RequiredNpcOrGoCount2, quest.RequiredNpcOrGo2, GetCreaturesToKill(quest.RequiredNpcOrGo2), 2));
                if (quest.RequiredNpcOrGoCount3 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.RequiredNpcOrGoCount3, quest.RequiredNpcOrGo3, GetCreaturesToKill(quest.RequiredNpcOrGo3), 3));
                if (quest.RequiredNpcOrGoCount4 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.RequiredNpcOrGoCount4, quest.RequiredNpcOrGo4, GetCreaturesToKill(quest.RequiredNpcOrGo4), 4));
                
                // Add quest givers / Turners
                quest.QuestGivers = GetQuestGivers(quest.Id);
                quest.QuestTurners = GetQuestTurners(quest.Id);

                // Add linked quests Ids
                quest.PreviousQuestsIds = GetPreviousQuestsIds(quest.Id);
                if (quest.PrevQuestID != 0 && !quest.PreviousQuestsIds.Contains(quest.PrevQuestID))
                    quest.PreviousQuestsIds.Add(quest.PrevQuestID);

                quest.NextQuestsIds = GetNextQuestsIds(quest.Id);
                if (quest.NextQuestID != 0 && !quest.NextQuestsIds.Contains(quest.NextQuestID))
                    quest.NextQuestsIds.Add(quest.NextQuestID);
            }

            Logger.Log($"Process time (All) : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
            Logger.Log($"{result.Count} results");

            ToolBox.WriteJSONFromDBResult(result);
            ToolBox.ZipJSONFile();
            WAQTasks.AddQuests(result);
            QueryIsRunning = false;
        }

        public static List<ModelNpc> GetQuestGivers(int questId)
        {
            string query = $@"
                    SELECT cq.id Id, ct.name Name, c.guid Guid, c.map Map, 
	                    c.position_x PositionX, c.position_y PositionY, c.position_z PositionZ,
	                    ct.faction FactionTemplateID
                    FROM creature_queststarter cq
                    JOIN creature_template ct
                    ON ct.Entry = cq.id
                    JOIN creature c
                    ON c.id = cq.id
                    WHERE cq.quest = {questId}
                    GROUP BY cq.id
                ";

            return DB._con.SafeQuery<ModelNpc>(query).ToList();
        }

        public static List<ModelNpc> GetQuestTurners(int questId)
        {
            string query =  $@"
                    SELECT ci.id Id, ct.Name, c.guid Guid, c.map Map, c.position_x PositionX, 
                    c.position_y PositionY, c.position_z PositionZ,
	                ct.faction FactionTemplateID
                    FROM creature_questender ci
                    JOIN creature_template ct
                    ON ct.Entry = ci.id
                    JOIN creature c
                    ON c.id = ci.id
                    WHERE ci.quest = {questId}
                    GROUP BY ci.id
                ";

            return DB._con.SafeQuery<ModelNpc>(query).ToList();
        }

        public static List<ModelNpc> GetCreatureToLoot(int itemid)
        {
            string query = $@"
                SELECT clt.entry Id, ct.name Name, c.guid Guid, c.map Map, c.position_x PositionX, 
                    c.position_y PositionY, c.position_z PositionZ,
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
            return DB._con.SafeQuery<ModelNpc>(query).ToList();
        }

        public static List<ModelNpc> GetCreaturesToKill(int creatureId)
        {
            string query = $@"
                SELECT c.Id, ct.Name, c.guid Guid, c.map Map, c.position_x PositionX, 
                c.position_y PositionY, c.position_z PositionZ,
	            ct.faction FactionTemplateID
                FROM creature c
                JOIN creature_template ct
                ON ct.Entry = c.id
                WHERE c.id = {creatureId}
            ";

            return DB._con.SafeQuery<ModelNpc>(query).ToList();
        }

        public static List<ModelGatherObject> GetGatherObjects(int objectId)
        {
            string query = $@"
                SELECT it.entry Entry, it.class Class, it.subclass SubClass, it.name Name, it.displayid DisplayId, 
	                it.Quality, it.Flags, glt.Entry GOLootEntry, gt.entry GameObjectEntry, g.guid Guid, g.map Map, 
	                g.position_x PositionX, g.position_y PositionY, g.position_z PositionZ 
                FROM item_template it
                JOIN gameobject_loot_template glt
                ON glt.item = it.entry
                JOIN gameobject_template gt
                ON gt.data1 = GOLootEntry
                JOIN gameobject g
                ON g.id = GameObjectEntry
                WHERE it.entry == {objectId}
            ";
            return DB._con.SafeQuery<ModelGatherObject>(query).ToList();
        }

        public static List<int> GetNextQuestsIds(int questId)
        {
            string query = $@"
                    SELECT ID FROM quest_template_addon
                    WHERE PrevQuestId = {questId}
                    GROUP BY ID
                ";

            return DB._con.SafeQuery<int>(query).Distinct().ToList();
        }

        public static List<int> GetPreviousQuestsIds(int questId)
        {
            string query = $@"
                    SELECT ID FROM quest_template_addon
                    WHERE NextQuestId = {questId}
                    GROUP BY ID
                ";

            return DB._con.SafeQuery<int>(query).Distinct().ToList();
        }
    }
}

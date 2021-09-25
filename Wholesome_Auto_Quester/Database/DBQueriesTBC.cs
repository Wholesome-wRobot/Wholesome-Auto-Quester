using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;
using Dapper;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Bot;
using System;

namespace Wholesome_Auto_Quester.Database
{
    public class DBQueriesTBC
    {
        private static DB _database;

        public static void Initialize(DB database)
        {
            if (_database == null)
                _database = database;
        }

        public static void GetAvailableQuests(int radius)
        {
            DateTime dateBegin = DateTime.Now;

            string query = $@"
                    SELECT qt.*, cq.id as QuestGiver FROM quest_template qt
                    JOIN creature_questrelation cq
                    ON qt.entry = cq.quest
                    WHERE MinLevel <= {ObjectManager.Me.Level}
                    AND (QuestLevel < {ObjectManager.Me.Level + 2} AND QuestLevel > 0)
                    AND Type = 0
                    AND (RequiredRaces & {(uint)ToolBox.GetFaction()} <> 0 OR RequiredRaces = 0)
                    AND (RequiredClasses & {(uint)ToolBox.GetClass()} <> 0 OR RequiredClasses = 0)
                ";

            List<ModelQuest> result = DB._con.Query<ModelQuest>(query).ToList();

            DateTime dateBeginAG = DateTime.Now;

            List<ModelGatherObject> resultListObj;
            List<ModelNpc> resultListCreature;
            foreach (ModelQuest quest in result)
            {
                // Add gather Objects
                if (quest.ReqItemId1 != 0)
                {
                    if ((resultListObj = GetGatherObjects(quest.ReqItemId1)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.ReqItemCount1, quest.ReqItemId1, resultListObj, 1));
                    else if ((resultListCreature = GetCreatureToLoot(quest.ReqItemId1)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.ReqItemCount1, resultListCreature[0].ItemName, resultListCreature, 1));
                }

                if (quest.ReqItemId2 != 0)
                {
                    if ((resultListObj = GetGatherObjects(quest.ReqItemId2)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.ReqItemCount2, quest.ReqItemId2, resultListObj, 2));
                    else if ((resultListCreature = GetCreatureToLoot(quest.ReqItemCount2)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.ReqItemCount2, resultListCreature[0].ItemName, resultListCreature, 2));
                }

                if (quest.ReqItemId3 != 0)
                {
                    if ((resultListObj = GetGatherObjects(quest.ReqItemId3)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.ReqItemCount3, quest.ReqItemId3, resultListObj, 3));
                    else if ((resultListCreature = GetCreatureToLoot(quest.ReqItemCount3)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.ReqItemCount3, resultListCreature[0].ItemName, resultListCreature, 3));
                }

                if (quest.ReqItemId4 != 0)
                {
                    if ((resultListObj = GetGatherObjects(quest.ReqItemId4)).Count > 0)
                        quest.GatherObjectsObjectives.Add(new GatherObjectObjective(quest.ReqItemCount4, quest.ReqItemId4, resultListObj, 4));
                    else if ((resultListCreature = GetCreatureToLoot(quest.ReqItemCount4)).Count > 0)
                        quest.CreaturesToLootObjectives.Add(new CreatureToLootObjective(quest.ReqItemCount4, resultListCreature[0].ItemName, resultListCreature, 4));
                }

                // Add creatures to kill
                if (quest.ReqCreatureOrGOCount1 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.ReqCreatureOrGOCount1, quest.ReqCreatureOrGOId1, GetCreaturesToKill(quest.ReqCreatureOrGOId1), 1));
                if (quest.ReqCreatureOrGOCount2 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.ReqCreatureOrGOCount2, quest.ReqCreatureOrGOId2, GetCreaturesToKill(quest.ReqCreatureOrGOId2), 2));
                if (quest.ReqCreatureOrGOCount3 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.ReqCreatureOrGOCount3, quest.ReqCreatureOrGOId3, GetCreaturesToKill(quest.ReqCreatureOrGOId3), 3));
                if (quest.ReqCreatureOrGOCount4 != 0)
                    quest.CreaturesToKillObjectives.Add(new CreaturesToKillObjective(quest.ReqCreatureOrGOCount4, quest.ReqCreatureOrGOId4, GetCreaturesToKill(quest.ReqCreatureOrGOId4), 4));
                
                // Add quest givers / Turners
                quest.QuestGivers = GetQuestGivers(quest.entry);
                quest.QuestTurners = GetQuestTurners(quest.entry);

                // Add linked quests Ids
                quest.PreviousQuestsIds = GetPreviousQuestsIds(quest.entry);
                if (quest.PrevQuestId != 0 && !quest.PreviousQuestsIds.Contains(quest.PrevQuestId)) 
                    quest.PreviousQuestsIds.Add(quest.PrevQuestId);

                quest.NextQuestsIds = GetNextQuestsIds(quest.entry);
                if (quest.NextQuestId != 0 && !quest.NextQuestsIds.Contains(quest.NextQuestId)) 
                    quest.NextQuestsIds.Add(quest.NextQuestId);
                if (quest.NextQuestInChain != 0 && !quest.NextQuestsIds.Contains(quest.NextQuestInChain)) 
                    quest.NextQuestsIds.Add(quest.NextQuestInChain);
            }

            Logger.Log($"Process time (GetQuestGivers) : {(DateTime.Now.Ticks - dateBeginAG.Ticks) / 10000} ms");

            Logger.Log($"Process time (All) : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");

            Logger.Log($"{result.Count} results");

            WAQTasks.AddQuests(result);
        }

        public static List<ModelNpc> GetQuestGivers(int questId)
        {
            string query = $@"
                    SELECT cq.id Id, ct.Name, c.guid Guid, c.map Map, c.position_x PositionX, c.position_y PositionY, c.position_z PositionZ,
                    c.spawntimesecsmin SpawnTimeMin, c.spawntimesecsmax SpawnTimeMax
                    FROM creature_questrelation cq
                    JOIN creature_template ct
                    ON ct.Entry = cq.id
                    JOIN creature c
                    ON c.id = cq.id
                    WHERE cq.quest = {questId}
                    GROUP BY cq.id
                ";

            return DB._con.Query<ModelNpc>(query).ToList();
        }

        public static List<ModelNpc> GetQuestTurners(int questId)
        {
            string query = $@"
                    SELECT ci.id Id, ct.Name, c.guid Guid, c.map Map, c.position_x PositionX, c.position_y PositionY, c.position_z PositionZ,
                    c.spawntimesecsmin SpawnTimeMin, c.spawntimesecsmax SpawnTimeMax
                    FROM creature_involvedrelation ci
                    JOIN creature_template ct
                    ON ct.Entry = ci.id
                    JOIN creature c
                    ON c.id = ci.id
                    WHERE ci.quest = {questId}
                    GROUP BY ci.id
                ";

            return DB._con.Query<ModelNpc>(query).ToList();
        }

        public static List<ModelNpc> GetCreatureToLoot(int itemid)
        {
            string query = $@"
                SELECT clt.entry Id, ct.name Name, c.guid Guid, c.map Map, c.position_x PositionX, c.position_y PositionY, c.position_z PositionZ,
                     c.spawntimesecsmin SpawnTimeMin, c.spawntimesecsmax SpawnTimeMax, it.name ItemName
                FROM creature_loot_template clt
                JOIN creature_template ct
                ON clt.entry = ct.entry
                JOIN creature c
                ON id = ct.entry
                JOIN item_template it
                ON it.entry = {itemid}
                WHERE item = {itemid}
            ";
            return DB._con.Query<ModelNpc>(query).ToList();
        }

        public static List<ModelNpc> GetCreaturesToKill(int creatureId)
        {
            string query = $@"
                SELECT ct.Name, c.guid Guid, c.map Map, c.position_x PositionX, c.position_y PositionY, c.position_z PositionZ,
                c.spawntimesecsmin SpawnTimeMin, c.spawntimesecsmax SpawnTimeMax
                FROM creature c
                JOIN creature_template ct
                ON ct.Entry = c.id
                WHERE c.id = {creatureId}
            ";
            return DB._con.Query<ModelNpc>(query).ToList();
        }

        public static List<ModelGatherObject> GetGatherObjects(int objectId)
        {
            string query = $@"
                SELECT it.entry Entry, it.class Class, it.subclass SubClass, it.name Name, it.displayid DisplayId, 
	                it.Quality, it.Flags, glt.entry GOLootEntry, gt.entry GameObjectEntry, g.guid Guid, g.map Map, 
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
            return DB._con.Query<ModelGatherObject>(query).ToList();
        }

        public static List<int> GetNextQuestsIds(int questId)
        {
            string query = $@"
                    SELECT entry FROM quest_template
                    WHERE PrevQuestId = {questId}
                    GROUP BY entry
                ";

            return DB._con.Query<int>(query).Distinct().ToList();
        }

        public static List<int> GetPreviousQuestsIds(int questId)
        {
            string query = $@"
                    SELECT entry FROM quest_template
                    WHERE NextQuestId = {questId} OR NextQuestInChain = {questId}
                    GROUP BY entry
                ";

            return DB._con.Query<int>(query).Distinct().ToList();
        }
    }
}

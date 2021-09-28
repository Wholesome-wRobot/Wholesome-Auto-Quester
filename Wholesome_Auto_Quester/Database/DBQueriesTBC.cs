using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;
using Dapper;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Bot;
using System;
/*
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
                if (quest.PrevQuestId != 0 && !quest.PreviousQuestsIds.Contains(quest.PrevQuestId)) 
                    quest.PreviousQuestsIds.Add(quest.PrevQuestId);

                quest.NextQuestsIds = GetNextQuestsIds(quest.Id);
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
*/
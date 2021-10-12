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

        public List<int> GetPreviousQuestsIds(int questId)
        {
            string query = $@"
                    SELECT ID FROM quest_template_addon
                    WHERE NextQuestId = {questId}
                    GROUP BY ID
                ";
            List<int> result = _con.Query<int>(query).ToList();
            return result;
        }

        public List<int> GetNextQuestsIds(int questId)
        {
            string query = $@"
                    SELECT ID FROM quest_template_addon
                    WHERE PrevQuestId = {questId}
                    GROUP BY ID
                ";
            List<int> result = _con.Query<int>(query).ToList();
            return result;
        }

        public List<ModelAreaTrigger> GetAreasToExplore(int questId)
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

        public ModelGameObjectTemplate QueryGameObjectsInteract(int objectId)
        {
            string query = $@"
                Select *
                FROM gameobject_template gt
                JOIN gameobject g
                ON g.id = gt.entry
                WHERE gt.entry = {objectId}
            ";

            List<ModelGameObjectTemplate> gameObjects = _con.Query<ModelGameObjectTemplate, ModelGameObject, ModelGameObjectTemplate>(
                query, (gameObjectTemplate, gameObject) =>
                {
                    gameObjectTemplate.GameObjects.Add(gameObject);
                    return gameObjectTemplate;
                }, splitOn: "guid")
            .ToList();

            var result = gameObjects.GroupBy(c => c.entry).Select(g =>
            {
                var groupedResult = g.First();
                groupedResult.GameObjects = g.Select(p => p.GameObjects.Single()).ToList();
                return groupedResult;
            });

            return result.FirstOrDefault();
        }

        public ModelCreatureTemplate QueryCreaturesToKill(int creatureId)
        {
            string query = $@"
                SELECT *
                FROM creature_template ct
                LEFT JOIN creature c
                ON c.id = ct.entry 
                WHERE ct.entry = {creatureId}
            ";

            List<ModelCreatureTemplate> creature = _con.Query<ModelCreatureTemplate, ModelCreature, ModelCreatureTemplate>(
                query, (creatureTemplate, creature) =>
                {
                    creatureTemplate.Creatures.Add(creature);
                    return creatureTemplate;
                }, splitOn: "guid")
            .ToList();

            var result = creature.GroupBy(c => c.entry).Select(g =>
            {
                var groupedResult = g.First();
                groupedResult.Creatures = g.Select(p => p.Creatures.Single()).ToList();
                return groupedResult;
            });

            return result.FirstOrDefault();
        }

        public ModelGameObjectTemplate QueryGameObjectsToGather(int objectId)
        {
            string query = $@"
                SELECT * 
                FROM item_template it
                INNER JOIN gameobject_loot_template glt
                ON glt.item = it.entry
                INNER JOIN gameobject_template gt
                ON gt.data1 = glt.entry
                INNER JOIN gameobject g
                ON g.id = gt.Entry
                WHERE it.entry = {objectId}
            ";

            List<ModelGameObjectTemplate> gatherObjects = _con.Query<ModelGameObjectTemplate, ModelGameObject, ModelGameObjectTemplate>(
                query, (gameObjectTemplate, gameObject) =>
                {
                    gameObjectTemplate.GameObjects.Add(gameObject);
                    return gameObjectTemplate;
                }, splitOn: "Entry,entry,guid")
            .ToList();

            var result = gatherObjects.GroupBy(c => c.entry).Select(g =>
            {
                var groupedResult = g.First();
                groupedResult.GameObjects = g.Select(p => p.GameObjects.Single()).ToList();
                return groupedResult;
            });

            return result.FirstOrDefault();
        }

        public ModelCreatureTemplate QueryCreaturesToLoot(int itemid)
        {
            string query = $@"
                SELECT *
                FROM creature_loot_template clt
                JOIN creature_template ct
                ON clt.entry = ct.entry
                JOIN creature c
                ON id = ct.entry
                JOIN item_template it
                ON it.entry = clt.item
                WHERE clt.item = {itemid}
            ";

            List<ModelCreatureTemplate> creaturesToLoot = _con.Query<ModelCreatureTemplate, ModelCreature, ModelItem, ModelCreatureTemplate>(
                query, (creatureTemplate, creature, item) =>
                {
                    creatureTemplate.Loot = item;
                    creatureTemplate.Creatures.Add(creature);
                    return creatureTemplate;
                }, splitOn: "entry,guid,entry")
            .ToList();

            var result = creaturesToLoot.GroupBy(c => c.entry).Select(g =>
            {
                var groupedResult = g.First();
                groupedResult.Creatures = g.Select(p => p.Creatures.Single()).ToList();
                return groupedResult;
            });

            return result.FirstOrDefault();
        }

        public List<ModelGameObjectTemplate> QueryGameObjectQuestGivers(int questId)
        {
            string query = $@"
                    SELECT got.*, go.*
                    FROM gameobject_template got
                    LEFT JOIN gameobject go
                    ON go.id = got.entry 
                    LEFT JOIN gameobject_queststarter goq
                    ON goq.id = got.entry
                    WHERE goq.quest = {questId}
                ";

            List<ModelGameObjectTemplate> questGiverObjects = _con.Query<ModelGameObjectTemplate, ModelGameObject, ModelGameObjectTemplate>(
                query, (gameObjectTemplate, gameObject) =>
                {
                    gameObjectTemplate.GameObjects.Add(gameObject);
                    return gameObjectTemplate;
                }, splitOn: "guid")
            .Distinct().ToList();

            return questGiverObjects;
        }

        public List<ModelGameObjectTemplate> QueryGameObjectQuestEnders(int questId)
        {
            string query = $@"
                    SELECT got.*, go.*
                    FROM gameobject_template got
                    LEFT JOIN gameobject go
                    ON go.id = got.entry 
                    LEFT JOIN gameobject_questender goq
                    ON goq.id = got.entry
                    WHERE goq.quest = {questId}
                ";

            List<ModelGameObjectTemplate> questGiverObjects = _con.Query<ModelGameObjectTemplate, ModelGameObject, ModelGameObjectTemplate>(
                query, (gameObjectTemplate, gameObject) =>
                {
                    if (gameObject != null)
                        gameObjectTemplate.GameObjects.Add(gameObject);
                    return gameObjectTemplate;
                }, splitOn: "guid")
            .Distinct().ToList();

            return questGiverObjects;
        }

        public List<ModelCreatureTemplate> QueryCreatureQuestEnders(int questId)
        {
            string query = $@"
                    SELECT ct.*, c.*
                    FROM creature_template ct
                    LEFT JOIN creature c
                    ON c.id = ct.entry 
                    LEFT JOIN creature_questender cq
                    ON cq.id = ct.entry
                    WHERE cq.quest = {questId}
                ";

            List<ModelCreatureTemplate> questGivers = _con.Query<ModelCreatureTemplate, ModelCreature, ModelCreatureTemplate>(
                query, (creatureTemplate, creature) =>
                {
                    if (creature != null)
                        creatureTemplate.Creatures.Add(creature);
                    return creatureTemplate;
                }, splitOn: "guid")
            .Distinct().ToList();

            return questGivers;
        }

        public List<ModelCreatureTemplate> QueryCreatureQuestGiver(int questId)
        {
            string query = $@"
                    SELECT ct.*, c.*
                    FROM creature_template ct
                    LEFT JOIN creature c
                    ON c.id = ct.entry 
                    LEFT JOIN creature_queststarter cq
                    ON cq.id = ct.entry
                    WHERE cq.quest = {questId}
                ";

            List<ModelCreatureTemplate> questGivers = _con.Query<ModelCreatureTemplate, ModelCreature, ModelCreatureTemplate>(
                query, (creatureTemplate, creature) =>
                {
                    if (creature != null)
                        creatureTemplate.Creatures.Add(creature);
                    return creatureTemplate;
                }, splitOn: "guid")
            .Distinct().ToList();

            return questGivers;
        }

        public List<ModelQuestTemplate> QueryQuests()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            int levelDeltaMinus = (int)ObjectManager.Me.Level - WholesomeAQSettings.CurrentSetting.LevelDeltaMinus;
            int levelDeltaPlus = (int)ObjectManager.Me.Level + WholesomeAQSettings.CurrentSetting.LevelDeltaPlus;

            string queryQuest = $@"
                    SELECT * 
                    FROM quest_template qt
                    LEFT JOIN quest_template_addon qta
                    ON qt.ID = qta.ID
                    WHERE MinLevel <= {(int)ObjectManager.Me.Level}
                    AND (QuestLevel <= {levelDeltaPlus} AND QuestLevel > 0 AND QuestLevel > {levelDeltaMinus});
                ";

            List<ModelQuestTemplate> result = _con.Query<ModelQuestTemplate, ModelQuestAddon, ModelQuestTemplate>(
                queryQuest,
                (quest, questAddon) =>
                {
                    quest.QuestAddon = questAddon == null ? new ModelQuestAddon() : questAddon;
                    return quest;
                },
                splitOn: "ID")
            .Distinct().ToList();

            Logger.Log($"Process time (Quests) : {stopwatch.ElapsedMilliseconds} ms");
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
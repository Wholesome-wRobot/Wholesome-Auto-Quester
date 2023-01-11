using Dapper;
using Db_To_Json.AutoQuester.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Db_To_Json.AutoQuester
{
    internal class AutoQuesterGeneration
    {
        private static readonly string _jsonFileName = "AQ.json";
        private static readonly string _zipName = "AQ.zip";
        private static readonly string _AQJsonOutputPath = $"{JSONGenerator.OutputPath}{JSONGenerator.PathSep}{_jsonFileName}";
        private static readonly string _AQJsonCopyToPath = $"{Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName}{JSONGenerator.PathSep}Wholesome_Auto_Quester{JSONGenerator.PathSep}Database";
        private static readonly string _zipFilePath = $"{_AQJsonCopyToPath}{JSONGenerator.PathSep}{_zipName}";

        private static List<AQModelCreatureTemplate> _allCreatureTemplates = new List<AQModelCreatureTemplate>();
        private static List<AQModelGameObjectTemplate> _allGameObjectTemplates = new List<AQModelGameObjectTemplate>();
        private static List<AQModelItemTemplate> _allItemTemplates = new List<AQModelItemTemplate>();
        private static List<AQModelSpell> _allSpells = new List<AQModelSpell>();

        public static void Generate(SQLiteConnection con, SQLiteCommand cmd)
        {
            Console.WriteLine("----- Starting generation for Auto Quester -----");
            Stopwatch totalWatch = Stopwatch.StartNew();

            // ---------------- QUEST TEMPLATES ----------------

            int[] questSortIdsToIgnore =
            {
                -24, -101, -121, -181, -182, -201, -264, -304, -324, -762, -371, -373, // profession sortIds
                -1, -21, -22, -23, -25, -41, -221, -241, -284, -344, -364, -365, -366, -367, -368, -369, -370, -374, -375, -376 // misc (epic, seasonal etc)
                // we leave the class sortIds in
            };
            /*
            // Quests to force get from the DB
            List<int> questsIdsToForce = new List<int>();
            if (WTPlayer.IsHorde())
            {
                questsIdsToForce.Add(9407); // Through the dark portal
            }
            else
            {
                questsIdsToForce.Add(10119); // Through the dark portal
            }

            List<int> logQuestsIds = new List<int>();
            foreach (Quest.PlayerQuest quest in Quest.GetLogQuestId())
            {
                logQuestsIds.Add(quest.ID);
            }
            */
            string queryQuest = $@"
                SELECT * 
                FROM quest_template
                WHERE (QuestType <> 0 OR Unknown0 <> 1)
            ";
            List<AQModelQuestTemplate> quests = con.Query<AQModelQuestTemplate>(queryQuest).ToList();

            // Quest adjustments
            foreach (AQModelQuestTemplate template in quests)
            {
                // Reduce starter zone quest levels
                if (template.QuestLevel > 1 && template.QuestLevel < 6)
                {
                    template.QuestLevel--;
                }
                // Reduce all Outlands/Northrend levels by 1 to have more quests enabled
                if (template.QuestLevel > 60)
                {
                    template.QuestLevel--;
                }
                if (QuestModifiedLevel.TryGetValue(template.Id, out int levelModifier))
                {
                    template.QuestLevel += levelModifier;
                }
            }

            /*
            result.RemoveAll(q =>
                !questsIdsToForce.Contains(q.Id)
                && (q.QuestLevel > levelDeltaPlus || q.QuestLevel < levelDeltaMinus)
                && q.QuestLevel != -1
                && (!logQuestsIds.Contains(q.Id) || q.QuestLevel > levelDeltaPlus));
            */

            int qtRemovedBecauseSpecificId = quests
                .RemoveAll(q => q.Id == 338
                    || q.Id == 339
                    || q.Id == 340
                    || q.Id == 341);
            Console.WriteLine($"[AQ] Removed {qtRemovedBecauseSpecificId} specific Ids");
            int qtRemovedBecauseSortId = quests
                .RemoveAll(q => questSortIdsToIgnore.Contains(q.QuestSortID));
            Console.WriteLine($"[AQ] Removed {qtRemovedBecauseSortId} unwanted sort Ids");
            int qtRemovedBecauseSpecial = quests
                .RemoveAll(q => q.QuestInfoID != 0);
            Console.WriteLine($"[AQ] Removed {qtRemovedBecauseSpecial} Dungeon/Group/Raid/PvP quests");
            int qtRemovedBecauseReputation = quests
                .RemoveAll(q => q.RequiredFactionId1 != 0 || q.RequiredFactionId2 != 0);
            Console.WriteLine($"[AQ] Removed {qtRemovedBecauseReputation} Reputation quests");

            //result.RemoveAll(q => myLevel < 60 && (q.Id == 9407 || q.Id == 10119)); // wait for dark portal


            // ---------------- QUEST TEMPLATE ADDONS ----------------
            Stopwatch qtAddonsWatch = Stopwatch.StartNew();
            foreach (AQModelQuestTemplate questTemplate in quests)
            {
                string queryQuestAddon = $@"
                    SELECT * 
                    FROM quest_template_addon
                    WHERE ID = {questTemplate.Id}
                ";
                AQModelQuestTemplateAddon questTemplateAddon = con.Query<AQModelQuestTemplateAddon>(queryQuestAddon).FirstOrDefault();
                if (questTemplateAddon != null)
                {
                    questTemplate.QuestAddon = questTemplateAddon;
                    questTemplate.QuestAddon.ExclusiveQuests = QueryQuestIdsByExclusiveGroup(con, questTemplateAddon.ExclusiveGroup); // Add EQ to addon
                    questTemplate.Conditions = QueryConditionsBySourceEntry(con, questTemplate.Id);
                }
            }
            Console.WriteLine($"[AQ] Quest Template addons took {qtAddonsWatch.ElapsedMilliseconds}ms ({quests.Count} quests)");

            int qtRemovedBecauseRepeatable = quests
                .RemoveAll(q => (q.QuestAddon?.SpecialFlags & 1) != 0);
            Console.WriteLine($"[AQ] Removed {qtRemovedBecauseRepeatable} repeatables quests");
            int qtRemovedBecauseEscort = quests
                .RemoveAll(q => (q.QuestAddon?.SpecialFlags & 2) != 0);
            Console.WriteLine($"[AQ] Removed {qtRemovedBecauseEscort} escorts quests");
            int qtRemovedBecauseNotClass = quests
                .RemoveAll(q => q.QuestLevel == -1 && q.QuestAddon?.AllowableClasses == 0);
            Console.WriteLine($"[AQ] Removed {qtRemovedBecauseNotClass} quests with level -1 and not class quest");

            // ---------------- QUEST GIVERS ----------------
            Stopwatch stopwatchQuestGivers = Stopwatch.StartNew();
            foreach (AQModelQuestTemplate quest in quests)
            {
                quest.CreatureQuestGiversEntries = QueryCreatureQuestGiver(con, quest.Id);
                foreach (int ctEntry in quest.CreatureQuestGiversEntries)
                {
                    AQModelCreatureTemplate questGiver = QueryCreatureTemplateByEntry(con, ctEntry);
                    _allCreatureTemplates.Add(questGiver);
                }
                quest.GameObjectQuestGiversEntries = QueryGameObjectQuestGivers(con, quest.Id);
                foreach (int goEntry in quest.GameObjectQuestGiversEntries)
                {
                    AQModelGameObjectTemplate questGiver = QueryGameObjectTemplateByEntry(con, goEntry);
                    _allGameObjectTemplates.Add(questGiver);
                }
            }
            Console.WriteLine($"[AQ] Quest Givers took {stopwatchQuestGivers.ElapsedMilliseconds}ms");

            // ---------------- QUEST ENDERS ----------------
            Stopwatch stopwatchQuestEnders = Stopwatch.StartNew();
            foreach (AQModelQuestTemplate quest in quests)
            {
                quest.CreatureQuestEndersEntries = QueryCreatureQuestEnders(con, quest.Id);
                foreach (int ctEntry in quest.CreatureQuestEndersEntries)
                {
                    AQModelCreatureTemplate questEnder = QueryCreatureTemplateByEntry(con, ctEntry);
                    _allCreatureTemplates.Add(questEnder);
                }
                quest.GameObjectQuestEndersEntries = QueryGameObjectQuestEnders(con, quest.Id);
                foreach (int goEntry in quest.GameObjectQuestEndersEntries)
                {
                    AQModelGameObjectTemplate questEnder = QueryGameObjectTemplateByEntry(con, goEntry);
                    _allGameObjectTemplates.Add(questEnder);
                }
            }
            Console.WriteLine($"[AQ] Quest Enders took {stopwatchQuestEnders.ElapsedMilliseconds}ms");

            // ---------------- PREVIOUS QUEST IDS ----------------
            Stopwatch stopwatchPrevQuests = Stopwatch.StartNew();
            foreach (AQModelQuestTemplate quest in quests)
            {
                quest.PreviousQuestsIds = QueryPreviousQuestsIdsByQuestId(con, quest.Id);
                if (quest.QuestAddon != null
                    && quest.QuestAddon.PrevQuestID != 0
                    && !quest.PreviousQuestsIds.Contains(quest.QuestAddon.PrevQuestID))
                {
                    quest.PreviousQuestsIds.Add(quest.QuestAddon.PrevQuestID);
                }
            }
            Console.WriteLine($"[AQ] Previous quest ids took {stopwatchPrevQuests.ElapsedMilliseconds}ms");

            // ---------------- NEXT QUEST IDS ----------------
            Stopwatch stopwatchNextQuests = Stopwatch.StartNew();
            foreach (AQModelQuestTemplate quest in quests)
            {
                quest.NextQuestsIds = QueryNextQuestsIdsByQuestId(con, quest.Id);
                if (quest.QuestAddon != null
                    && quest.QuestAddon.NextQuestID != 0
                    && !quest.NextQuestsIds.Contains(quest.QuestAddon.NextQuestID))
                    quest.NextQuestsIds.Add(quest.QuestAddon.NextQuestID);
            }
            Console.WriteLine($"[AQ] Next quest ids took {stopwatchNextQuests.ElapsedMilliseconds}ms");

            // ---------------- AREAS ----------------
            Stopwatch stopwatchAreas = Stopwatch.StartNew();
            foreach (AQModelQuestTemplate quest in quests)
            {
                quest.ModelAreasTriggers = QueryAreasToExplore(con, quest.Id);
            }
            Console.WriteLine($"[AQ] Areas took {stopwatchAreas.ElapsedMilliseconds}ms");

            // ---------------- ITEM DROPS (Prerequisites) ----------------
            Stopwatch stopwatchItemDrops = Stopwatch.StartNew();
            foreach (AQModelQuestTemplate quest in quests)
            {
                AQModelItemTemplate item1 = QueryItemTemplateByItemEntry(con, quest.ItemDrop1); // for record
                AQModelItemTemplate item2 = QueryItemTemplateByItemEntry(con, quest.ItemDrop2); // for record
                AQModelItemTemplate item3 = QueryItemTemplateByItemEntry(con, quest.ItemDrop3); // for record
                AQModelItemTemplate item4 = QueryItemTemplateByItemEntry(con, quest.ItemDrop4); // for record
            }
            Console.WriteLine($"[AQ] Item drops took {stopwatchAreas.ElapsedMilliseconds}ms");

            // ---------------- REQUIRED ITEMS ----------------
            Stopwatch stopwatchRequiredItem = Stopwatch.StartNew();
            foreach (AQModelQuestTemplate quest in quests)
            {
                AQModelItemTemplate item1 = QueryItemTemplateByItemEntry(con, quest.RequiredItemId1); // for record
                AQModelItemTemplate item2 = QueryItemTemplateByItemEntry(con, quest.RequiredItemId2); // for record
                AQModelItemTemplate item3 = QueryItemTemplateByItemEntry(con, quest.RequiredItemId3); // for record
                AQModelItemTemplate item4 = QueryItemTemplateByItemEntry(con, quest.RequiredItemId4); // for record
                AQModelItemTemplate item5 = QueryItemTemplateByItemEntry(con, quest.RequiredItemId5); // for record
                AQModelItemTemplate item6 = QueryItemTemplateByItemEntry(con, quest.RequiredItemId6); // for record
            }
            Console.WriteLine($"[AQ] Required items took {stopwatchRequiredItem.ElapsedMilliseconds}ms");

            // ---------------- START ITEMS ----------------
            Stopwatch stopwatchStartItem = Stopwatch.StartNew();
            foreach (AQModelQuestTemplate quest in quests)
            {
                quest.StartItemTemplate = QueryItemTemplateByItemEntry(con, quest.StartItem);
            }
            Console.WriteLine($"[AQ] Start items took {stopwatchStartItem.ElapsedMilliseconds}ms");

            // ---------------- REQUIRED NPCS/INTERACTS ----------------
            Stopwatch stopwatchRequiredNPC = Stopwatch.StartNew();
            foreach (AQModelQuestTemplate quest in quests)
            {
                AQModelCreatureTemplate req1 = QueryCreatureTemplateByEntry(con, quest.RequiredNpcOrGo1); // for record
                AQModelCreatureTemplate req2 = QueryCreatureTemplateByEntry(con, quest.RequiredNpcOrGo2); // for record
                AQModelCreatureTemplate req3 = QueryCreatureTemplateByEntry(con, quest.RequiredNpcOrGo3); // for record
                AQModelCreatureTemplate req4 = QueryCreatureTemplateByEntry(con, quest.RequiredNpcOrGo4); // for record

                AQModelGameObjectTemplate reqGo1 = QueryGameObjectTemplateByEntry(con, -quest.RequiredNpcOrGo1); // for record
                AQModelGameObjectTemplate reqGo2 = QueryGameObjectTemplateByEntry(con, -quest.RequiredNpcOrGo2); // for record
                AQModelGameObjectTemplate reqGo3 = QueryGameObjectTemplateByEntry(con, -quest.RequiredNpcOrGo3); // for record
                AQModelGameObjectTemplate reqGo4 = QueryGameObjectTemplateByEntry(con, -quest.RequiredNpcOrGo4); // for record
            }
            Console.WriteLine($"[AQ] Required NPCs/Interact took {stopwatchRequiredNPC.ElapsedMilliseconds}ms");

            foreach (AQModelQuestTemplate quest in quests)
            {
                // KILL / INTERACT

                // RequiredNpcOrGo
                // Value > 0:required creature_template ID the player needs to kill/cast on in order to complete the quest.
                // Value < 0:required gameobject_template ID the player needs to cast on in order to complete the quest.
                // If RequiredSpellCast is != 0, the objective is to cast on target, else kill.
                // NOTE: If RequiredSpellCast is != 0 and the spell has effects Send Event or Quest Complete, this field may be left empty.

                // Kill
                if (quest.RequiredNpcOrGo1 > 0)
                {
                    AQModelCreatureTemplate ct1 = _allCreatureTemplates.Find(ct => ct.entry == quest.RequiredNpcOrGo1);
                    ct1.KillCredits.AddRange(QueryCreatureTemplatesByKillCredits(con, quest.RequiredNpcOrGo1));
                    foreach (int kcId in ct1.KillCredits)
                    {
                        AQModelCreatureTemplate ct1k = QueryCreatureTemplateByEntry(con, kcId); // for record
                    }
                }
                if (quest.RequiredNpcOrGo2 > 0)
                {
                    AQModelCreatureTemplate ct2 = _allCreatureTemplates.Find(ct => ct.entry == quest.RequiredNpcOrGo2);
                    ct2.KillCredits.AddRange(QueryCreatureTemplatesByKillCredits(con, quest.RequiredNpcOrGo2));
                    foreach (int kcId in ct2.KillCredits)
                    {
                        AQModelCreatureTemplate ct2k = QueryCreatureTemplateByEntry(con, kcId); // for record
                    }
                }
                if (quest.RequiredNpcOrGo3 > 0)
                {
                    AQModelCreatureTemplate ct3 = _allCreatureTemplates.Find(ct => ct.entry == quest.RequiredNpcOrGo3);
                    ct3.KillCredits.AddRange(QueryCreatureTemplatesByKillCredits(con, quest.RequiredNpcOrGo3));
                    foreach (int kcId in ct3.KillCredits)
                    {
                        AQModelCreatureTemplate ct3k = QueryCreatureTemplateByEntry(con, kcId); // for record
                    }
                }
                if (quest.RequiredNpcOrGo4 > 0)
                {
                    AQModelCreatureTemplate ct4 = _allCreatureTemplates.Find(ct => ct.entry == quest.RequiredNpcOrGo4);
                    ct4.KillCredits.AddRange(QueryCreatureTemplatesByKillCredits(con, quest.RequiredNpcOrGo4));
                    foreach (int kcId in ct4.KillCredits)
                    {
                        AQModelCreatureTemplate ct4k = QueryCreatureTemplateByEntry(con, kcId); // for record
                    }
                }
            }

            // remove duplicates
            _allCreatureTemplates = _allCreatureTemplates
                .GroupBy(c => c.entry)
                .Select(g => g.First())
                .ToList();
            _allGameObjectTemplates = _allGameObjectTemplates
                .GroupBy(c => c.entry)
                .Select(g => g.First())
                .ToList();
            _allItemTemplates = _allItemTemplates
                .GroupBy(c => c.Entry)
                .Select(g => g.First())
                .ToList();

            File.Delete(_AQJsonOutputPath);
            File.Delete(_zipFilePath);
            File.Delete(_AQJsonCopyToPath + $"{JSONGenerator.PathSep}{_jsonFileName}");

            // Create Json in output path
            using (StreamWriter file = File.CreateText(_AQJsonOutputPath))
            {
                var serializer = new JsonSerializer();
                serializer.ContractResolver = ShouldSerializeContractResolver.Instance;
                serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                serializer.DefaultValueHandling = DefaultValueHandling.Ignore;
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.Serialize(file,
                    new AQJsonExport(
                        quests,
                        _allCreatureTemplates,
                        _allGameObjectTemplates,
                        _allItemTemplates)
                    );
                Console.WriteLine($"[AQ] JSON created in {_AQJsonOutputPath}");
                long fileSize = new FileInfo(_AQJsonOutputPath).Length;
                Console.WriteLine($"[AQ] JSON size is {((float)fileSize / 1000000).ToString("0.00")} MB");
            }

            if (Directory.Exists(_AQJsonCopyToPath))
            {
                // Copy file to AQ project
                //File.Copy(_AQJsonOutputPath, _AQJsonCopyToPath + $"{JSONGenerator.PathSep}{_jsonFileName}", true);
                //Console.WriteLine($"[AQ] JSON copied to {_AQJsonCopyToPath + $"{JSONGenerator.PathSep}{_jsonFileName}"}");
                // Zip file to AQ project
                using (ZipArchive zip = ZipFile.Open(_zipFilePath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(_AQJsonOutputPath, _jsonFileName);
                    Console.WriteLine($"[AQ] JSON compressed in {_zipFilePath}");
                    long fileSize = new FileInfo(_zipFilePath).Length;
                    Console.WriteLine($"[AQ] Compressed JSON size is {((float)fileSize / 1000000).ToString("0.00")} MB");
                }
            }
            else
            {
                Console.WriteLine($"ERROR: Directory {_AQJsonCopyToPath} does not exist");
            }

            Console.WriteLine($"[AQ] Total took {totalWatch.ElapsedMilliseconds}ms");
        }

        private static List<int> QueryCreatureTemplatesByKillCredits(SQLiteConnection con, int entry)
        {
            string queryTemplate = $@"
                SELECT *
                FROM creature_template
                WHERE KillCredit1 = {entry} OR KillCredit2 = {entry}
            ";
            List<AQModelCreatureTemplate> result = con.Query<AQModelCreatureTemplate>(queryTemplate).ToList();
            return result.Select(ct => ct.entry).ToList();
        }

        private static List<AQModelReferenceLootTemplate> QueryReferenceLootTemplateByItem(SQLiteConnection con, int itemId)
        {
            string queryReferenceootTemplate = $@"
                SELECT *
                FROM reference_loot_template
                WHERE item = {itemId}
            ";
            List<AQModelReferenceLootTemplate> result = con.Query<AQModelReferenceLootTemplate>(queryReferenceootTemplate).ToList();
            return result;
        }

        private static List<AQModelCreatureLootTemplate> QueryCreatureLootTemplatesByItemEntry(SQLiteConnection con, int itemId)
        {
            // get standard loot templates
            string queryLootTemplate = $@"
                SELECT *
                FROM creature_loot_template
                WHERE item = {itemId}
            ";
            List<AQModelCreatureLootTemplate> result = con.Query<AQModelCreatureLootTemplate>(queryLootTemplate).ToList();

            // add reference loot templates
            List<AQModelReferenceLootTemplate> referenceLootTemplates = QueryReferenceLootTemplateByItem(con, itemId);
            foreach (AQModelReferenceLootTemplate mrlt in referenceLootTemplates)
            {
                string queryLootTemplateByMrlt = $@"
                    SELECT *
                    FROM creature_loot_template
                    WHERE item = {mrlt.Entry}
                ";
                List<AQModelCreatureLootTemplate> clts = con.Query<AQModelCreatureLootTemplate>(queryLootTemplateByMrlt).ToList();
                clts.ForEach(rlt => rlt.Chance = mrlt.Chance);
                result.AddRange(clts);
            }

            foreach (AQModelCreatureLootTemplate clt in result)
            {
                clt.CreatureTemplate = QueryCreatureTemplateByEntry(con, clt.Entry).entry;
            }

            return result;
        }

        private static AQModelItemTemplate QueryItemTemplateByItemEntry(SQLiteConnection con, int itemEntry)
        {
            if (itemEntry == 0) return null;
            string queryItemTemplate = $@"
                SELECT * 
                FROM item_template
                WHERE entry = {itemEntry}
            ";
            List<AQModelItemTemplate> result = con.Query<AQModelItemTemplate>(queryItemTemplate).ToList();
            if (result.Count <= 0) return null;
            if (result.Count > 1) Console.WriteLine($"Item entry {itemEntry} has more than one template !");

            result.ForEach(it =>
            {
                it.CreatureLootTemplates = QueryCreatureLootTemplatesByItemEntry(con, itemEntry);
                it.GameObjectLootTemplates = QueryGameObjectLootTemplateByItemEntry(con, itemEntry);
                it.ItemLootTemplates = QueryItemLootTemplateByEntry(con, itemEntry);
                /*
                AQModelSpell spell1 = QuerySpellById(con, it.spellid_1);
                if (spell1 != null) it.Spell1 = spell1.Id;
                AQModelSpell spell2 = QuerySpellById(con, it.spellid_2);
                if (spell2 != null) it.Spell2 = spell2.Id;
                AQModelSpell spell3 = QuerySpellById(con, it.spellid_3);
                if (spell3 != null) it.Spell3 = spell3.Id;
                AQModelSpell spell4 = QuerySpellById(con, it.spellid_4);
                if (spell4 != null) it.Spell4 = spell4.Id;
                */
            });
            _allItemTemplates.AddRange(result);

            return result.FirstOrDefault();
        }

        private static int QuerySpellById(SQLiteConnection con, int spellID)
        {
            if (spellID == 0) return 0;
            string query = $@"
                SELECT *
                FROM spell
                WHERE ID = {spellID}
            ";
            List<AQModelSpell> spells = con.Query<AQModelSpell>(query).ToList();
            if (spells.Count <= 0) return 0;
            if (spells.Count > 1) Console.WriteLine($"Spell ID {spellID} has more than one spells !");
            AQModelSpell result = spells.FirstOrDefault();
            _allSpells.Add(result);
            return result.Id;
        }

        private static List<AQModelItemLootTemplate> QueryItemLootTemplateByEntry(SQLiteConnection con, int lootEntry)
        {
            if (lootEntry == 0) return null;
            string queryItemLootTemplate = $@"
                SELECT * 
                FROM item_loot_template
                WHERE Entry = {lootEntry}
            ";
            List<AQModelItemLootTemplate> result = con.Query<AQModelItemLootTemplate>(queryItemLootTemplate).ToList();
            return result;
        }

        private static List<int> QueryGameObjectTemplatesByLootEntry(SQLiteConnection con, int lootEntry)
        {
            string queryGOTemplate = $@"
                Select *
                FROM gameobject_template
                WHERE data1 = {lootEntry}
                AND type > 0;
            ";
            List<AQModelGameObjectTemplate> result = con.Query<AQModelGameObjectTemplate>(queryGOTemplate).ToList();
            foreach (AQModelGameObjectTemplate got in result)
            {
                got.GameObjects = QueryGameObjectByEntry(con, got.entry);
            }
            _allGameObjectTemplates.AddRange(result);
            return result.Select(got => got.entry).ToList();
        }

        private static List<AQModelGameObjectLootTemplate> QueryGameObjectLootTemplateByItemEntry(SQLiteConnection con, int itemId)
        {
            string queryLootTemplate = $@"
                SELECT *
                FROM gameobject_loot_template
                WHERE Item = {itemId}
            ";
            List<AQModelGameObjectLootTemplate> result = con.Query<AQModelGameObjectLootTemplate>(queryLootTemplate).ToList();
            foreach (AQModelGameObjectLootTemplate golt in result)
            {
                golt.GameObjectTemplates = QueryGameObjectTemplatesByLootEntry(con, golt.Entry);
            }
            return result;
        }

        private static List<AQModelAreaTrigger> QueryAreasToExplore(SQLiteConnection con, int questId)
        {
            string query = $@"
                SELECT a.ContinentID, a.x PositionX, a.y PositionY, a.z PositionZ, a.radius Radius
                FROM areatrigger_involvedrelation ai
                JOIN areatrigger a 
                ON a.ID = ai.id
                WHERE ai.quest = {questId}
            ";
            List<AQModelAreaTrigger> result = con.Query<AQModelAreaTrigger>(query).ToList();
            return result;
        }

        private static List<int> QueryNextQuestsIdsByQuestId(SQLiteConnection con, int questId)
        {
            string query = $@"
                SELECT ID FROM quest_template_addon
                WHERE PrevQuestId = {questId}
                GROUP BY ID
            ";
            List<int> result = con.Query<int>(query).ToList();
            return result;
        }

        private static List<int> QueryPreviousQuestsIdsByQuestId(SQLiteConnection con, int questId)
        {
            string query = $@"
                SELECT ID FROM quest_template_addon
                WHERE NextQuestId = {questId}
                GROUP BY ID
            ";
            List<int> result = con.Query<int>(query).ToList();
            return result;
        }

        private static List<int> QueryGameObjectQuestEnders(SQLiteConnection con, int questId)
        {
            string queryGOEndersIds = $@"
                SELECT id
                FROM gameobject_questender
                WHERE quest = {questId}
            ";
            List<int> ids = con.Query<int>(queryGOEndersIds).ToList();
            /*
            List<AQModelGameObjectTemplate> result = new List<AQModelGameObjectTemplate>();
            foreach (AQModelGameObjectTemplate got in result)
            {
                result.Add(QueryGameObjectTemplateByEntry(con, id));
            }
            */
            return ids;
        }

        private static List<int> QueryCreatureQuestEnders(SQLiteConnection con, int questId)
        {
            string queryQuestEndersIds = $@"
                SELECT id
                FROM creature_questender
                WHERE quest = {questId}
            ";
            List<int> result = con.Query<int>(queryQuestEndersIds).ToList();
            /*
            List<AQModelCreatureTemplate> result = new List<AQModelCreatureTemplate>();
            questEndersIds.ForEach(id => { result.Add(QueryCreatureTemplateByEntry(con, id)); });
            result.RemoveAll(template => !template.IsNeutralOrFriendly);
            */
            return result;
        }

        private static List<AQModelGameObject> QueryGameObjectByEntry(SQLiteConnection con, int gameObjectId)
        {
            string query = $@"
                Select *
                FROM gameobject
                WHERE id = {gameObjectId}
            ";
            List<AQModelGameObject> result = con.Query<AQModelGameObject>(query).ToList();
            return result;
        }

        private static AQModelGameObjectTemplate QueryGameObjectTemplateByEntry(SQLiteConnection con, int objectEntry)
        {
            string queryGOTemplate = $@"
                Select *
                FROM gameobject_template
                WHERE entry = {objectEntry}
            ";
            List<AQModelGameObjectTemplate> result = con.Query<AQModelGameObjectTemplate>(queryGOTemplate).ToList();
            if (result.Count <= 0) return null;
            if (result.Count > 1) Console.WriteLine($"Game Object entry {objectEntry} has more than one templates !");
            result.ForEach(got => got.GameObjects = QueryGameObjectByEntry(con, got.entry));
            _allGameObjectTemplates.AddRange(result);
            return result.FirstOrDefault();
        }

        private static List<int> QueryGameObjectQuestGivers(SQLiteConnection con, int questId)
        {
            string queryGOGiverssIds = $@"
                SELECT id
                FROM gameobject_queststarter
                WHERE quest = {questId}
            ";
            List<int> ids = con.Query<int>(queryGOGiverssIds).ToList();
            /*
            List<AQModelGameObjectTemplate> result = new List<AQModelGameObjectTemplate>();
            ids.ForEach(id => { result.Add(QueryGameObjectTemplateByEntry(con, id)); });
            */
            return ids;
        }

        private static List<AQModelWayPointData> QueryWayPointDataByPathId(SQLiteConnection con, int pathId)
        {
            string queryWPData = $@"
                SELECT *
                FROM waypoint_data
                WHERE id = {pathId}
            ";
            List<AQModelWayPointData> result = con.Query<AQModelWayPointData>(queryWPData).ToList();
            return result;
        }

        private static AQModelCreatureAddon QueryCreaturesAddonsByGuid(SQLiteConnection con, uint guid)
        {
            string queryCreatureAddon = $@"
                SELECT *
                FROM creature_addon
                WHERE guid = {guid}
            ";
            AQModelCreatureAddon result = con.Query<AQModelCreatureAddon>(queryCreatureAddon).FirstOrDefault();
            if (result != null && result.path_id > 0) result.WayPoints = QueryWayPointDataByPathId(con, result.path_id);
            return result;
        }

        private static List<AQModelCreature> QueryCreaturesById(SQLiteConnection con, int creatureId, bool withWayPoints = true)
        {
            string queryCreature = $@"
                SELECT *
                FROM creature
                WHERE id = {creatureId}
                AND (map = 0 OR map = 1 OR map = 530 OR map = 571)
            ";
            List<AQModelCreature> result = con.Query<AQModelCreature>(queryCreature).ToList();
            if (result.Count > 0)
            {
                List<AQModelCreature> creaturesToAddWP = new List<AQModelCreature>();
                result.ForEach(c =>
                {
                    c.CreatureAddon = QueryCreaturesAddonsByGuid(con, c.guid);
                    if (withWayPoints && c.CreatureAddon?.WayPoints?.Count > 0)
                    {
                        c.CreatureAddon.WayPoints.Reverse();
                        if (c.CreatureAddon.WayPoints.Count > 4)
                        {
                            c.CreatureAddon.WayPoints.RemoveAll(cToRemove => c.CreatureAddon.WayPoints.IndexOf(cToRemove) % 2 != 0);
                        }
                        c.CreatureAddon.WayPoints.ForEach(wp =>
                            creaturesToAddWP.Add(new AQModelCreature(wp.position_x, wp.position_y, wp.position_z, c.guid, c.map, c.spawnTimeSecs)));
                    }
                });
                result.AddRange(creaturesToAddWP);
            }
            return result.Count > 0 ? result : new List<AQModelCreature>();
        }

        private static AQModelCreatureTemplate QueryCreatureTemplateByEntry(SQLiteConnection con, int creatureEntry)
        {
            if (creatureEntry == 0) return null;
            string queryTemplate = $@"
                SELECT *
                FROM creature_template
                WHERE entry = {creatureEntry}
            ";
            List<AQModelCreatureTemplate> result = con.Query<AQModelCreatureTemplate>(queryTemplate).ToList();
            if (result.Count <= 0) return null;
            if (result.Count > 1) Console.WriteLine($"Creature entry {creatureEntry} has more than one templates !");

            foreach (AQModelCreatureTemplate template in result)
            {
                template.Creatures = QueryCreaturesById(con, creatureEntry);
            }

            _allCreatureTemplates.AddRange(result);
            return result.FirstOrDefault();
        }

        public static List<int> QueryCreatureQuestGiver(SQLiteConnection con, int questId)
        {
            string queryQuestGiversIds = $@"
                SELECT id
                FROM creature_queststarter
                WHERE quest = {questId}
            ";
            List<int> questGiversIds = con.Query<int>(queryQuestGiversIds).ToList();
            /*
            List<AQModelCreatureTemplate> result = new List<AQModelCreatureTemplate>();
            questGiversIds.ForEach(id => { result.Add(QueryCreatureTemplateByEntry(con, id)); });
            //result.RemoveAll(template => !template.IsNeutralOrFriendly);
            */
            return questGiversIds;
        }

        private static List<int> QueryQuestIdsByExclusiveGroup(SQLiteConnection con, int exclusiveGroup)
        {
            if (exclusiveGroup == 0) return new List<int>();
            string queryQuestExcl = $@"
                SELECT id
                FROM quest_template_addon
                WHERE ExclusiveGroup = {exclusiveGroup}
            ";
            List<int> result = con.Query<int>(queryQuestExcl).ToList();
            return result;
        }

        private static List<AQModelConditions> QueryConditionsBySourceEntry(SQLiteConnection con, int sourceEntry)
        {
            string query = $@"
                SELECT *
                FROM conditions c
                WHERE c.SourceEntry = {sourceEntry}
            ";
            List<AQModelConditions> result = con.Query<AQModelConditions>(query).ToList();
            return result;
        }

        private static Dictionary<int, int> QuestModifiedLevel = new Dictionary<int, int>()
        {
            { 354, 3 }, // Roaming mobs, hard to find in a hostile zone
            { 843, 3 }, // Bael'Dun excavation, too many mobs
            { 6548, 3 }, // Avenge my village, too many mobs
            { 6629, 3 }, // Avenge my village follow up, too many mobs
            { 216, 2 }, // Between a rock and a Thistlefur, too many mobs
            { 541, 4 }, // Battle of Hillsbrad, too many mobs
            { 5501, 3 }, // Kodo bones, red enemies
            { 8885, 3 }, // Ring of Mmmmmmrgll, too many freaking murlocs
            { 1389, 3 }, // Draenethyst crystals, too many mobs
            { 582, 3 }, // Headhunting, too many mobs
            { 1177, 3 }, // Hungry!, too many murlocs
            { 1054, 3 }, // Culling the threat, too many murlocs
            { 115, 3 }, // Culling the threat, too many murlocs
            { 180, 3 }, // Lieutenant Fangore, too many mobs
            { 323, 3 }, // Proving your worth, too many mobs
            { 464, 3 }, // War banners, too many mobs
            { 203, 3 }, // The second rebellion, too many mobs
            { 505, 3 }, // Syndicate assassins, too many mobs
            { 1439, 5 }, // Search for Tyranis, too many mobs
            { 213, 5 }, // Hostile takeover, too many mobs
            { 1398, 4 }, // Driftwood, too many mobs
            { 2870, 4 }, // Against Lord Shalzaru, too many mobs
            { 12043, 3 }, // Nozzlerust defense, too many mobs
            { 12044, 3 }, // Stocking up, too many mobs
            { 12120, 3 }, // DrakAguul's Mallet, too many mobs
        };
    }

    public class ShouldSerializeContractResolver : DefaultContractResolver
    {
        public static readonly ShouldSerializeContractResolver Instance = new ShouldSerializeContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (property.PropertyType != typeof(string))
            {
                if (property.PropertyType.GetInterface(nameof(IEnumerable)) != null)
                    property.ShouldSerialize =
                        instance => (instance?.GetType().GetProperty(property.UnderlyingName)?.GetValue(instance) as IEnumerable)?.OfType<object>().Count() > 0;
            }

            return property;
        }
    }

    class AQJsonExport
    {
        public List<AQModelQuestTemplate> QuestTemplates { get; }
        public List<AQModelCreatureTemplate> CreatureTemplates { get; }
        public List<AQModelGameObjectTemplate> GameObjectTemplates { get; }
        public List<AQModelItemTemplate> ItemTemplates { get; }

        public AQJsonExport(
            List<AQModelQuestTemplate> waters,
            List<AQModelCreatureTemplate> creatureTemplates,
            List<AQModelGameObjectTemplate> gameObjectTemplates,
            List<AQModelItemTemplate> itemTemplates)
        {
            QuestTemplates = waters;
            CreatureTemplates = creatureTemplates;
            GameObjectTemplates = gameObjectTemplates;
            ItemTemplates = itemTemplates;
        }
    }
}

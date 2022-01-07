using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Bot;
using robotManager.Products;
using Wholesome_Auto_Quester.Database.Objectives;
using System.Diagnostics;

namespace Wholesome_Auto_Quester.Database {
    public class DBQueriesWotlk
    {
        private DB _database;

        public DBQueriesWotlk()
        {
            _database = new DB();
        }

        public void DisposeDb()
        {
            _database.Dispose();
        }

        private static List<ModelQuestTemplate> FilterDBQuests(List<ModelQuestTemplate> dbResult)
        {
            List<ModelQuestTemplate> result = new List<ModelQuestTemplate>();

            var myClass = (int)ToolBox.GetClass();
            var myFaction = (int)ToolBox.GetFaction();
            var myLevel = (int)ObjectManager.Me.Level;

            foreach (ModelQuestTemplate q in dbResult)
            {
                // Our level is too low
                if (myLevel < q.MinLevel) continue;
                // Repeatable/escort quest
                if ((q.QuestAddon.SpecialFlags & 1) != 0 || (q.QuestAddon.SpecialFlags & 2) != 0) continue;
                // Remove -1 quests that are not Class quests
                if (q.QuestLevel == -1 && q.QuestAddon.AllowableClasses == 0) continue;
                // Quest is not for my class
                if (q.QuestAddon.AllowableClasses > 0 && (q.QuestAddon.AllowableClasses & myClass) == 0) continue;
                // Quest is not for my race
                if (q.AllowableRaces > 0 && (q.AllowableRaces & myFaction) == 0) continue;
                // Quest is not for my faction
                if (!q.CreatureQuestGivers.Any(qg => qg.IsNeutralOrFriendly) && q.GameObjectQuestGivers.Count <= 0) continue;
                // Quest is Dungeon/Group/Raid/PvP etc..
                if (q.QuestInfoID != 0) continue;

                result.Add(q);
            }

            return result;
        }

        public void GetAvailableQuests()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            if (!ToolBox.WoWDBFileIsPresent())
            {
                // DOWNLOAD ZIP ETC..
                Logger.LogError("Couldn't find the database in your wRobot/Data folder");
                DisposeDb();
                Products.ProductStop();
                return;
            }

            _database.CreateIndices();

            List<ModelQuestTemplate> quests = _database.QueryQuests();

            // Query quest givers
            Stopwatch stopwatchQuestGivers = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                quest.CreatureQuestGivers = _database.QueryCreatureQuestGiver(quest.Id);
                quest.GameObjectQuestGivers = _database.QueryGameObjectQuestGivers(quest.Id);
            }
            Logger.Log($"Process time (Quest givers) : {stopwatchQuestGivers.ElapsedMilliseconds} ms");

            // Query quest enders
            Stopwatch stopwatchQuestEnders = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                quest.CreatureQuestTurners = _database.QueryCreatureQuestEnders(quest.Id);
                quest.GameObjectQuestTurners = _database.QueryGameObjectQuestEnders(quest.Id);
            }
            Logger.Log($"Process time (Quest enders) : {stopwatchQuestEnders.ElapsedMilliseconds} ms");

            // Query previous quests Ids
            Stopwatch stopwatchPrevQuests = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                quest.PreviousQuestsIds = _database.QueryPreviousQuestsIds(quest.Id);
                if (quest.QuestAddon.PrevQuestID != 0
                    && !quest.PreviousQuestsIds.Contains(quest.QuestAddon.PrevQuestID))
                    quest.PreviousQuestsIds.Add(quest.QuestAddon.PrevQuestID);
            }
            Logger.Log($"Process time (Previous quests) : {stopwatchPrevQuests.ElapsedMilliseconds} ms");

            // Query next quests ids
            Stopwatch stopwatchNextQuests = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                quest.NextQuestsIds = _database.QueryNextQuestsIds(quest.Id);
                if (quest.QuestAddon.NextQuestID != 0
                    && !quest.NextQuestsIds.Contains(quest.QuestAddon.NextQuestID))
                    quest.NextQuestsIds.Add(quest.QuestAddon.NextQuestID);
            }
            Logger.Log($"Process time (Next quests) : {stopwatchNextQuests.ElapsedMilliseconds} ms");

            // Query Areas
            Stopwatch stopwatchAreas = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
                quest.ModelAreasTriggers = _database.QueryAreasToExplore(quest.Id);
            Logger.Log($"Process time (Areas) : {stopwatchAreas.ElapsedMilliseconds} ms");

            // Query Item drops (prerequisites)
            Stopwatch stopwatchItemDrops = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                quest.ItemDrop1Template = _database.QueryItemTemplate(quest.ItemDrop1);
                quest.ItemDrop2Template = _database.QueryItemTemplate(quest.ItemDrop2);
                quest.ItemDrop3Template = _database.QueryItemTemplate(quest.ItemDrop3);
                quest.ItemDrop4Template = _database.QueryItemTemplate(quest.ItemDrop4);
            }
            Logger.Log($"Process time (ItemDrops) : {stopwatchItemDrops.ElapsedMilliseconds} ms");

            // Query required Items
            Stopwatch stopwatchRequiredItem = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                quest.RequiredItem1Template = _database.QueryItemTemplate(quest.RequiredItemId1);
                quest.RequiredItem2Template = _database.QueryItemTemplate(quest.RequiredItemId2);
                quest.RequiredItem3Template = _database.QueryItemTemplate(quest.RequiredItemId3);
                quest.RequiredItem4Template = _database.QueryItemTemplate(quest.RequiredItemId4);
                quest.RequiredItem5Template = _database.QueryItemTemplate(quest.RequiredItemId5);
                quest.RequiredItem6Template = _database.QueryItemTemplate(quest.RequiredItemId6);
            }
            Logger.Log($"Process time (RequiredItems) : {stopwatchRequiredItem.ElapsedMilliseconds} ms");

            // Query required Npcs/Interacts
            Stopwatch stopwatchRequiredNPC = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                // NPCs
                if (quest.RequiredNpcOrGo1 > 0)
                    quest.RequiredNPC1Template = _database.QueryCreatureTemplate(quest.RequiredNpcOrGo1);
                if (quest.RequiredNpcOrGo2 > 0)
                    quest.RequiredNPC2Template = _database.QueryCreatureTemplate(quest.RequiredNpcOrGo2);
                if (quest.RequiredNpcOrGo3 > 0)
                    quest.RequiredNPC3Template = _database.QueryCreatureTemplate(quest.RequiredNpcOrGo3);
                if (quest.RequiredNpcOrGo4 > 0)
                    quest.RequiredNPC4Template = _database.QueryCreatureTemplate(quest.RequiredNpcOrGo4);

                // Interacts
                if (quest.RequiredNpcOrGo1 < 0)
                    quest.RequiredGO1Template = _database.QueryGameObjectTemplate(-quest.RequiredNpcOrGo1);
                if (quest.RequiredNpcOrGo2 < 0)
                    quest.RequiredGO2Template = _database.QueryGameObjectTemplate(-quest.RequiredNpcOrGo2);
                if (quest.RequiredNpcOrGo3 < 0)
                    quest.RequiredGO3Template = _database.QueryGameObjectTemplate(-quest.RequiredNpcOrGo3);
                if (quest.RequiredNpcOrGo4 < 0)
                    quest.RequiredGO4Template = _database.QueryGameObjectTemplate(-quest.RequiredNpcOrGo4);
            }
            Logger.Log($"Process time (RequiredNpcs) : {stopwatchRequiredNPC.ElapsedMilliseconds} ms");

            // Add all objectives
            foreach (ModelQuestTemplate quest in quests)
            {
                // Exploration objectives
                quest.ModelAreasTriggers.ForEach(modelArea =>
                    quest.AddObjective(new ExplorationObjective((int)modelArea.PositionX, modelArea, quest.AreaDescription)));

                // Prerequisite objectives Gather
                quest.ItemDrop1Template?.GatheredOn.ForEach(goTemplate =>
                    quest.AddObjective(new GatherObjective(quest.ItemDropQuantity1, goTemplate, quest.ItemDrop1Template)));
                quest.ItemDrop2Template?.GatheredOn.ForEach(goTemplate =>
                    quest.AddObjective(new GatherObjective(quest.ItemDropQuantity2, goTemplate, quest.ItemDrop2Template)));
                quest.ItemDrop3Template?.GatheredOn.ForEach(goTemplate =>
                    quest.AddObjective(new GatherObjective(quest.ItemDropQuantity3, goTemplate, quest.ItemDrop3Template)));
                quest.ItemDrop4Template?.GatheredOn.ForEach(goTemplate =>
                    quest.AddObjective(new GatherObjective(quest.ItemDropQuantity4, goTemplate, quest.ItemDrop4Template)));

                // Prerequisite objectives Kill&Loot
                quest.ItemDrop1Template?.DroppedBy.ForEach(creaTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.ItemDropQuantity1, creaTemplate, quest.ItemDrop1Template)));
                quest.ItemDrop2Template?.DroppedBy.ForEach(creaTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.ItemDropQuantity2, creaTemplate, quest.ItemDrop2Template)));
                quest.ItemDrop3Template?.DroppedBy.ForEach(creaTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.ItemDropQuantity3, creaTemplate, quest.ItemDrop3Template)));
                quest.ItemDrop4Template?.DroppedBy.ForEach(creaTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.ItemDropQuantity4, creaTemplate, quest.ItemDrop4Template)));

                // Required items Gather/Loot
                quest.RequiredItem1Template?.GatheredOn.ForEach(goTemplate =>
                    quest.AddObjective(new GatherObjective(quest.RequiredItemCount1, goTemplate, quest.RequiredItem1Template)));                
                quest.RequiredItem1Template?.DroppedBy.ForEach(creaTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.RequiredItemCount1, creaTemplate, quest.RequiredItem1Template)));

                quest.RequiredItem2Template?.GatheredOn.ForEach(goTemplate =>
                    quest.AddObjective(new GatherObjective(quest.RequiredItemCount2, goTemplate, quest.RequiredItem2Template)));
                quest.RequiredItem2Template?.DroppedBy.ForEach(creaTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.RequiredItemCount2, creaTemplate, quest.RequiredItem2Template)));

                quest.RequiredItem3Template?.GatheredOn.ForEach(goTemplate =>
                    quest.AddObjective(new GatherObjective(quest.RequiredItemCount3, goTemplate, quest.RequiredItem3Template)));
                quest.RequiredItem3Template?.DroppedBy.ForEach(creaTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.RequiredItemCount3, creaTemplate, quest.RequiredItem3Template)));

                quest.RequiredItem4Template?.GatheredOn.ForEach(goTemplate =>
                    quest.AddObjective(new GatherObjective(quest.RequiredItemCount4, goTemplate, quest.RequiredItem4Template)));
                quest.RequiredItem4Template?.DroppedBy.ForEach(creaTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.RequiredItemCount4, creaTemplate, quest.RequiredItem4Template)));

                quest.RequiredItem5Template?.GatheredOn.ForEach(goTemplate =>
                    quest.AddObjective(new GatherObjective(quest.RequiredItemCount5, goTemplate, quest.RequiredItem5Template)));
                quest.RequiredItem5Template?.DroppedBy.ForEach(creaTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.RequiredItemCount5, creaTemplate, quest.RequiredItem5Template)));

                quest.RequiredItem6Template?.GatheredOn.ForEach(goTemplate =>
                    quest.AddObjective(new GatherObjective(quest.RequiredItemCount6, goTemplate, quest.RequiredItem6Template)));
                quest.RequiredItem6Template?.DroppedBy.ForEach(creaTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.RequiredItemCount6, creaTemplate, quest.RequiredItem6Template)));

                /*
                    KILL / INTERACT

                    RequiredNpcOrGo
                    Value > 0:required creature_template ID the player needs to kill/cast on in order to complete the quest.
                    Value < 0:required gameobject_template ID the player needs to cast on in order to complete the quest.
                    If*RequiredSpellCast*is != 0, the objective is to cast on target, else kill.
                    NOTE: If RequiredSpellCast is != 0 and the spell has effects Send Event or Quest Complete, this field may be left empty.
                */

                // Kill
                if (quest.RequiredNPC1Template != null && !quest.RequiredNPC1Template.IsFriendly)
                    quest.AddObjective(new KillObjective(quest.RequiredNpcOrGoCount1, quest.RequiredNPC1Template, quest.ObjectiveText1));
                if (quest.RequiredNPC2Template != null && !quest.RequiredNPC2Template.IsFriendly)
                    quest.AddObjective(new KillObjective(quest.RequiredNpcOrGoCount2, quest.RequiredNPC2Template, quest.ObjectiveText2));
                if (quest.RequiredNPC3Template != null && !quest.RequiredNPC3Template.IsFriendly)
                    quest.AddObjective(new KillObjective(quest.RequiredNpcOrGoCount3, quest.RequiredNPC3Template, quest.ObjectiveText3));
                if (quest.RequiredNPC4Template != null && !quest.RequiredNPC4Template.IsFriendly)
                    quest.AddObjective(new KillObjective(quest.RequiredNpcOrGoCount4, quest.RequiredNPC4Template, quest.ObjectiveText4));

                // Interact
                if (quest.RequiredGO1Template != null)
                    quest.AddObjective(new InteractObjective(quest.RequiredNpcOrGoCount1, quest.RequiredGO1Template, quest.ObjectiveText1));
                if (quest.RequiredGO2Template != null)
                    quest.AddObjective(new InteractObjective(quest.RequiredNpcOrGoCount2, quest.RequiredGO2Template, quest.ObjectiveText2));
                if (quest.RequiredGO3Template != null)
                    quest.AddObjective(new InteractObjective(quest.RequiredNpcOrGoCount3, quest.RequiredGO3Template, quest.ObjectiveText3));
                if (quest.RequiredGO4Template != null)
                    quest.AddObjective(new InteractObjective(quest.RequiredNpcOrGoCount4, quest.RequiredGO4Template, quest.ObjectiveText4));
            }                

            DisposeDb();

            List<ModelQuestTemplate> allFilteredQuests = FilterDBQuests(quests);

            // Write JSON
            if (WholesomeAQSettings.CurrentSetting.DevMode)
            {
                Stopwatch stopwatchJSON = Stopwatch.StartNew();
                Logger.Log($"{allFilteredQuests.Count} results. Building JSON. Please wait.");
                ToolBox.UpdateCompletedQuests();
                ToolBox.WriteJSONFromDBResult(allFilteredQuests);
                ToolBox.ZipJSONFile();
                Logger.Log($"Process time (JSON processing) : {stopwatchJSON.ElapsedMilliseconds} ms");
            }

            Logger.Log($"DONE! Process time (TOTAL) : {stopwatch.ElapsedMilliseconds} ms");

            WAQTasks.AddQuests(allFilteredQuests);
        }
    }
}

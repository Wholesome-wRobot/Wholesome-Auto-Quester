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
                if (quest.RequiredNPC1Template != null)
                    quest.AddObjective(new KillObjective(quest.RequiredNpcOrGoCount1, quest.RequiredNPC1Template, quest.ObjectiveText1));
                if (quest.RequiredNPC2Template != null)
                    quest.AddObjective(new KillObjective(quest.RequiredNpcOrGoCount2, quest.RequiredNPC2Template, quest.ObjectiveText2));
                if (quest.RequiredNPC3Template != null)
                    quest.AddObjective(new KillObjective(quest.RequiredNpcOrGoCount3, quest.RequiredNPC3Template, quest.ObjectiveText3));
                if (quest.RequiredNPC4Template != null)
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

            /*
            foreach (ModelQuestTemplate quest in quests)
            {
                int nbObjective = 0;
                // Add explore objectives
                if ((resultListArea = _database.QueryAreasToExplore(quest.Id)).Count > 0)
                {
                    resultListArea.ForEach(area =>
                    {
                        quest.ExplorationObjectives.Add(new ExplorationObjective((int)area.PositionX, area, ++nbObjective));
                    });
                }

                // Prerequisite Items
                ModelGameObjectTemplate prerequisiteGathers1 = quest.ItemDrop1 != 0 ? _database.QueryGameObjectsToGather(quest.ItemDrop1) : null;
                ModelCreatureTemplate prerequisiteLoots1 = quest.ItemDrop1 != 0 ? _database.QueryCreaturesToLoot(quest.ItemDrop1) : null;
                ModelGameObjectTemplate prerequisiteGathers2 = quest.ItemDrop2 != 0 ? _database.QueryGameObjectsToGather(quest.ItemDrop2) : null;
                ModelCreatureTemplate prerequisiteLoots2 = quest.ItemDrop2 != 0 ? _database.QueryCreaturesToLoot(quest.ItemDrop2) : null;
                ModelGameObjectTemplate prerequisiteGathers3 = quest.ItemDrop3 != 0 ? _database.QueryGameObjectsToGather(quest.ItemDrop3) : null;
                ModelCreatureTemplate prerequisiteLoots3 = quest.ItemDrop3 != 0 ? _database.QueryCreaturesToLoot(quest.ItemDrop3) : null;
                ModelGameObjectTemplate prerequisiteGathers4 = quest.ItemDrop4 != 0 ? _database.QueryGameObjectsToGather(quest.ItemDrop4) : null;
                ModelCreatureTemplate prerequisiteLoots4 = quest.ItemDrop4 != 0 ? _database.QueryCreaturesToLoot(quest.ItemDrop4) : null;

                // Gather / Loot
                ModelGameObjectTemplate gatherItems1 = quest.RequiredItemId1 != 0 ? _database.QueryGameObjectsToGather(quest.RequiredItemId1) : null;
                ModelCreatureTemplate lootItems1 = quest.RequiredItemId1 != 0 ? _database.QueryCreaturesToLoot(quest.RequiredItemId1) : null;
                ModelGameObjectTemplate gatherItems2 = quest.RequiredItemId2 != 0 ? _database.QueryGameObjectsToGather(quest.RequiredItemId2) : null;
                ModelCreatureTemplate lootItems2 = quest.RequiredItemId2 != 0 ? _database.QueryCreaturesToLoot(quest.RequiredItemId2) : null;
                ModelGameObjectTemplate gatherItems3 = quest.RequiredItemId3 != 0 ? _database.QueryGameObjectsToGather(quest.RequiredItemId3) : null;
                ModelCreatureTemplate lootItems3 = quest.RequiredItemId3 != 0 ? _database.QueryCreaturesToLoot(quest.RequiredItemId3) : null;
                ModelGameObjectTemplate gatherItems4 = quest.RequiredItemId4 != 0 ? _database.QueryGameObjectsToGather(quest.RequiredItemId4) : null;
                ModelCreatureTemplate lootItems4 = quest.RequiredItemId4 != 0 ? _database.QueryCreaturesToLoot(quest.RequiredItemId4) : null;
                ModelGameObjectTemplate gatherItems5 = quest.RequiredItemId5 != 0 ? _database.QueryGameObjectsToGather(quest.RequiredItemId5) : null;
                ModelCreatureTemplate lootItems5 = quest.RequiredItemId5 != 0 ? _database.QueryCreaturesToLoot(quest.RequiredItemId5) : null;
                ModelGameObjectTemplate gatherItems6 = quest.RequiredItemId6 != 0 ? _database.QueryGameObjectsToGather(quest.RequiredItemId6) : null;
                ModelCreatureTemplate lootItems6 = quest.RequiredItemId6 != 0 ? _database.QueryCreaturesToLoot(quest.RequiredItemId6) : null;

                // Add prerequisite items
                if (prerequisiteGathers1?.GameObjects.Count > 0)
                    quest.PrerequisiteGatherItems.Add(new GatherObjective(quest.ItemDropQuantity1, prerequisiteGathers1, -1));
                if (prerequisiteGathers2?.GameObjects.Count > 0)
                    quest.PrerequisiteGatherItems.Add(new GatherObjective(quest.ItemDropQuantity2, prerequisiteGathers2, -2));
                if (prerequisiteGathers3?.GameObjects.Count > 0)
                    quest.PrerequisiteGatherItems.Add(new GatherObjective(quest.ItemDropQuantity3, prerequisiteGathers3, -3));
                if (prerequisiteGathers4?.GameObjects.Count > 0)
                    quest.PrerequisiteGatherItems.Add(new GatherObjective(quest.ItemDropQuantity4, prerequisiteGathers4, -4));

                if (prerequisiteLoots1?.Creatures.Count > 0)
                    quest.PrerequisiteLootItems.Add(new KillLootObjective(quest.ItemDropQuantity1, prerequisiteLoots1, -1));
                if (prerequisiteLoots2?.Creatures.Count > 0)
                    quest.PrerequisiteLootItems.Add(new KillLootObjective(quest.ItemDropQuantity2, prerequisiteLoots2, -2));
                if (prerequisiteLoots3?.Creatures.Count > 0)
                    quest.PrerequisiteLootItems.Add(new KillLootObjective(quest.ItemDropQuantity3, prerequisiteLoots3, -3));
                if (prerequisiteLoots4?.Creatures.Count > 0)
                    quest.PrerequisiteLootItems.Add(new KillLootObjective(quest.ItemDropQuantity4, prerequisiteLoots4, -4));

                // Add gather world items / loots items
                if (gatherItems1?.GameObjects.Count > 0)
                {
                    if (gatherItems1.GameObjects.Count > 0)
                        quest.GatherObjectives.Add(new GatherObjective(quest.RequiredItemCount1, gatherItems1, ++nbObjective));
                    if (lootItems1?.Creatures.Count > 0)
                        quest.KillLootObjectives.Add(new KillLootObjective(quest.RequiredItemCount1, lootItems1, nbObjective));
                }
                if (gatherItems2?.GameObjects.Count > 0)
                {
                    if (gatherItems2.GameObjects.Count > 0)
                        quest.GatherObjectives.Add(new GatherObjective(quest.RequiredItemCount2, gatherItems2, ++nbObjective));
                    if (lootItems2?.Creatures.Count > 0)
                        quest.KillLootObjectives.Add(new KillLootObjective(quest.RequiredItemCount2, lootItems2, nbObjective));
                }
                if (gatherItems3?.GameObjects.Count > 0)
                {
                    if (gatherItems3.GameObjects.Count > 0)
                        quest.GatherObjectives.Add(new GatherObjective(quest.RequiredItemCount3, gatherItems3, ++nbObjective));
                    if (lootItems3?.Creatures.Count > 0)
                        quest.KillLootObjectives.Add(new KillLootObjective(quest.RequiredItemCount3, lootItems3, nbObjective));
                }
                if (gatherItems4?.GameObjects.Count > 0)
                {
                    if (gatherItems4.GameObjects.Count > 0)
                        quest.GatherObjectives.Add(new GatherObjective(quest.RequiredItemCount4, gatherItems4, ++nbObjective));
                    if (lootItems4?.Creatures.Count > 0)
                        quest.KillLootObjectives.Add(new KillLootObjective(quest.RequiredItemCount4, lootItems4, nbObjective));
                }
                if (gatherItems5?.GameObjects.Count > 0)
                {
                    if (gatherItems5.GameObjects.Count > 0)
                        quest.GatherObjectives.Add(new GatherObjective(quest.RequiredItemCount5, gatherItems5, ++nbObjective));
                    if (lootItems5?.Creatures.Count > 0)
                        quest.KillLootObjectives.Add(new KillLootObjective(quest.RequiredItemCount5, lootItems5, nbObjective));
                }
                if (gatherItems6?.GameObjects.Count > 0)
                {
                    if (gatherItems6.GameObjects.Count > 0)
                        quest.GatherObjectives.Add(new GatherObjective(quest.RequiredItemCount6, gatherItems6, ++nbObjective));
                    if (lootItems6?.Creatures.Count > 0)
                        quest.KillLootObjectives.Add(new KillLootObjective(quest.RequiredItemCount6, lootItems6, nbObjective));
                }

                /*
                    KILL / INTERACT

                    RequiredNpcOrGo
                    Value > 0:required creature_template ID the player needs to kill/cast on in order to complete the quest.
                    Value < 0:required gameobject_template ID the player needs to cast on in order to complete the quest.
                    If*RequiredSpellCast*is != 0, the objective is to cast on target, else kill.
                    NOTE: If RequiredSpellCast is != 0 and the spell has effects Send Event or Quest Complete, this field may be left empty.
                */
            /*
            if (quest.RequiredNpcOrGo1 > 0)
                quest.KillObjectives.Add(new KillObjective(quest.RequiredNpcOrGoCount1, _database.QueryCreaturesToKill(quest.RequiredNpcOrGo1), ++nbObjective));
            if (quest.RequiredNpcOrGo1 < 0)
                quest.InteractObjectives.Add(new InteractObjective(quest.RequiredNpcOrGoCount1, _database.QueryGameObjectsInteract(-quest.RequiredNpcOrGo1), ++nbObjective));

            if (quest.RequiredNpcOrGo2 > 0)
                quest.KillObjectives.Add(new KillObjective(quest.RequiredNpcOrGoCount2, _database.QueryCreaturesToKill(quest.RequiredNpcOrGo2), ++nbObjective));
            if (quest.RequiredNpcOrGo2 < 0)
                quest.InteractObjectives.Add(new InteractObjective(quest.RequiredNpcOrGoCount2, _database.QueryGameObjectsInteract(-quest.RequiredNpcOrGo2), ++nbObjective));

            if (quest.RequiredNpcOrGo3 > 0)
                quest.KillObjectives.Add(new KillObjective(quest.RequiredNpcOrGoCount3, _database.QueryCreaturesToKill(quest.RequiredNpcOrGo3), ++nbObjective));
            if (quest.RequiredNpcOrGo3 < 0)
                quest.InteractObjectives.Add(new InteractObjective(quest.RequiredNpcOrGoCount3, _database.QueryGameObjectsInteract(-quest.RequiredNpcOrGo3), ++nbObjective));

            if (quest.RequiredNpcOrGo4 > 0)
                quest.KillObjectives.Add(new KillObjective(quest.RequiredNpcOrGoCount4, _database.QueryCreaturesToKill(quest.RequiredNpcOrGo4), ++nbObjective));
            if (quest.RequiredNpcOrGo4 < 0)
                quest.InteractObjectives.Add(new InteractObjective(quest.RequiredNpcOrGoCount4, _database.QueryGameObjectsInteract(-quest.RequiredNpcOrGo4), ++nbObjective));


            // Add creature loot items
            if ((gatherItems1 == null || gatherItems1.GameObjects.Count <= 0) && lootItems1?.Creatures.Count > 0)
                quest.KillLootObjectives.Add(new KillLootObjective(quest.RequiredItemCount1, lootItems1, ++nbObjective));
            if ((gatherItems2 == null || gatherItems2.GameObjects.Count <= 0) && lootItems2?.Creatures.Count > 0)
                quest.KillLootObjectives.Add(new KillLootObjective(quest.RequiredItemCount2, lootItems2, ++nbObjective));
            if ((gatherItems3 == null || gatherItems3.GameObjects.Count <= 0) && lootItems3?.Creatures.Count > 0)
                quest.KillLootObjectives.Add(new KillLootObjective(quest.RequiredItemCount3, lootItems3, ++nbObjective));
            if ((gatherItems4 == null || gatherItems4.GameObjects.Count <= 0) && lootItems4?.Creatures.Count > 0)
                quest.KillLootObjectives.Add(new KillLootObjective(quest.RequiredItemCount4, lootItems4, ++nbObjective));
            if ((gatherItems5 == null || gatherItems5.GameObjects.Count <= 0) && lootItems5?.Creatures.Count > 0)
                quest.KillLootObjectives.Add(new KillLootObjective(quest.RequiredItemCount5, lootItems5, ++nbObjective));
            if ((gatherItems6 == null || gatherItems6.GameObjects.Count <= 0) && lootItems6?.Creatures.Count > 0)
                quest.KillLootObjectives.Add(new KillLootObjective(quest.RequiredItemCount6, lootItems6, ++nbObjective));

        }*/


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

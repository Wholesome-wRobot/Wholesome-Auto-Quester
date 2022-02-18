using robotManager.Products;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Database.Objectives;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Database
{
    public class DBQueriesWotlk
    {
        private DB _database;

        public DBQueriesWotlk()
        {
            _database = new DB();
        }

        private void DisposeDb()
        {
            _database.Dispose();
        }

        private List<ModelQuestTemplate> FilterDBQuestsBeforeFills(List<ModelQuestTemplate> dbResult)
        {
            List<ModelQuestTemplate> result = new List<ModelQuestTemplate>();

            int myClass = (int)ToolBox.GetClass();
            int myFaction = (int)ToolBox.GetFaction();

            foreach (ModelQuestTemplate q in dbResult)
            {
                if (q.Id == 338
                    || q.Id == 339
                    || q.Id == 340
                    || q.Id == 341) // stranglethorn pages
                {
                    continue;
                }

                if ((q.QuestAddon?.SpecialFlags & 1) != 0)
                {
                    //Logger.LogDebug($"[{q.Id}] {q.LogTitle} has been removed (Repeatable)");
                    continue;
                }
                if ((q.QuestAddon?.SpecialFlags & 2) != 0)
                {
                    //Logger.LogDebug($"[{q.Id}] {q.LogTitle} has been removed (Escort?)");
                    continue;
                }
                if (q.QuestLevel == -1 && q.QuestAddon?.AllowableClasses == 0)
                {
                    //Logger.LogDebug($"[{q.Id}] {q.LogTitle} has been removed (-1, not class quest)");
                    continue;
                }
                if (q.QuestAddon?.AllowableClasses > 0 && (q.QuestAddon?.AllowableClasses & myClass) == 0)
                {
                    //Logger.LogDebug($"[{q.Id}] {q.LogTitle} has been removed (Not for my class)");
                    continue;
                }
                if (q.AllowableRaces > 0 && (q.AllowableRaces & myFaction) == 0)
                {
                    //Logger.LogDebug($"[{q.Id}] {q.LogTitle} has been removed (Not for my race)");
                    continue;
                }
                if (q.QuestInfoID != 0)
                {
                    //Logger.LogDebug($"[{q.Id}] {q.LogTitle} has been removed (Dungeon/Group/Raid/PvP)");
                    continue;
                }
                if (q.RequiredFactionId1 != 0 || q.RequiredFactionId2 != 0)
                {
                    //Logger.LogDebug($"[{q.Id}] {q.LogTitle} has been removed (Reputation quest)");
                    continue;
                }

                result.Add(q);
            }
            return result;
        }

        private List<ModelQuestTemplate> FilterDBQuestsAfterFills(List<ModelQuestTemplate> dbResult)
        {
            List<ModelQuestTemplate> result = new List<ModelQuestTemplate>();

            int myClass = (int)ToolBox.GetClass();
            int myFaction = (int)ToolBox.GetFaction();
            int myLevel = (int)ObjectManager.Me.Level;

            foreach (ModelQuestTemplate q in dbResult)
            {                
                if (q.KillLootObjectives.Count > 0 && q.KillLootObjectives.All(klo => !klo.CreatureLootTemplate.CreatureTemplate.IsValidForKill))
                {
                    //Logger.LogDebug($"[{q.Id}] {q.LogTitle} has been removed (all enemies are invalid 0)");
                    continue;
                }

                if (q.KillObjectives.Count > 0 && q.KillObjectives.All(ko => !ko.CreatureTemplate.IsValidForKill))
                {
                    //Logger.LogDebug($"[{q.Id}] {q.LogTitle} has been removed (all enemies are invalid 1)");
                    continue;
                }

                if (q.KillLootObjectives.Any(klo => klo.ItemTemplate.Class != 12))
                {
                    //Logger.LogDebug($"[{q.Id}] {q.LogTitle} has been removed (kill loot objective is not quest item)");
                    continue;
                }

                if (q.CreatureQuestGivers.Count <= 0 && q.CreatureQuestTurners.Count <= 0)
                {
                    //Logger.LogDebug($"[{q.Id}] {q.LogTitle} has been removed (no quest giver, no quest turner)");
                    continue;
                }

                if (q.CreatureQuestGivers.Count > 0 && !q.CreatureQuestGivers.Any(qg => qg.IsNeutralOrFriendly) && q.GameObjectQuestGivers.Count <= 0)
                {
                    //Logger.LogDebug($"[{q.Id}] {q.LogTitle} has been removed (Not for my faction)");
                    continue;
                }

                if (q.StartItemTemplate != null && q.StartItemTemplate.HasASpellAttached
                    || q.ItemDrop1Template != null && q.ItemDrop1Template.HasASpellAttached
                    || q.ItemDrop2Template != null && q.ItemDrop2Template.HasASpellAttached
                    || q.ItemDrop3Template != null && q.ItemDrop3Template.HasASpellAttached
                    || q.ItemDrop4Template != null && q.ItemDrop4Template.HasASpellAttached)
                {
                    //Logger.LogDebug($"[{q.Id}] {q.LogTitle} has been removed (Active start/prerequisite item)");
                    continue;
                }

                if (q.KillLootObjectives.Any(klo => klo.ItemTemplate.HasASpellAttached))
                {
                    //Logger.Log($"[{q.Id}] {q.LogTitle} has been removed (Active loot item)");
                    continue;
                }
                result.Add(q);
            }
            return result;
        }

        public List<ModelQuestTemplate> GetAvailableQuests()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            if (!ToolBox.WoWDBFileIsPresent())
            {
                // DOWNLOAD ZIP ETC..
                DisposeDb();
                Products.ProductStop();
                throw new System.Exception("Couldn't find the database in your wRobot/Data folder");
            }

            _database.CreateIndices();

            List<ModelQuestTemplate> quests = FilterDBQuestsBeforeFills(_database.QueryQuests());

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
                quest.PreviousQuestsIds = _database.QueryPreviousQuestsIdsByQuestId(quest.Id);
                if (quest.QuestAddon.PrevQuestID != 0
                    && !quest.PreviousQuestsIds.Contains(quest.QuestAddon.PrevQuestID))
                    quest.PreviousQuestsIds.Add(quest.QuestAddon.PrevQuestID);
            }
            Logger.Log($"Process time (Previous quests) : {stopwatchPrevQuests.ElapsedMilliseconds} ms");

            // Query next quests ids
            Stopwatch stopwatchNextQuests = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                quest.NextQuestsIds = _database.QueryNextQuestsIdsByQuestId(quest.Id);
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
                quest.ItemDrop1Template = _database.QueryItemTemplateByItemEntry(quest.ItemDrop1);
                quest.ItemDrop2Template = _database.QueryItemTemplateByItemEntry(quest.ItemDrop2);
                quest.ItemDrop3Template = _database.QueryItemTemplateByItemEntry(quest.ItemDrop3);
                quest.ItemDrop4Template = _database.QueryItemTemplateByItemEntry(quest.ItemDrop4);
            }
            Logger.Log($"Process time (ItemDrops) : {stopwatchItemDrops.ElapsedMilliseconds} ms");

            // Query required Items
            Stopwatch stopwatchRequiredItem = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                quest.RequiredItem1Template = _database.QueryItemTemplateByItemEntry(quest.RequiredItemId1);
                quest.RequiredItem2Template = _database.QueryItemTemplateByItemEntry(quest.RequiredItemId2);
                quest.RequiredItem3Template = _database.QueryItemTemplateByItemEntry(quest.RequiredItemId3);
                quest.RequiredItem4Template = _database.QueryItemTemplateByItemEntry(quest.RequiredItemId4);
                quest.RequiredItem5Template = _database.QueryItemTemplateByItemEntry(quest.RequiredItemId5);
                quest.RequiredItem6Template = _database.QueryItemTemplateByItemEntry(quest.RequiredItemId6);
            }
            Logger.Log($"Process time (RequiredItems) : {stopwatchRequiredItem.ElapsedMilliseconds} ms");

            // Query Start Item
            Stopwatch stopwatchStartItem = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                quest.StartItemTemplate = _database.QueryItemTemplateByItemEntry(quest.StartItem);
            }
            Logger.Log($"Process time (StartItem) : {stopwatchStartItem.ElapsedMilliseconds} ms");

            // Query required Npcs/Interacts
            Stopwatch stopwatchRequiredNPC = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                // NPCs
                if (quest.RequiredNpcOrGo1 > 0)
                {
                    quest.RequiredNPC1Template = _database.QueryCreatureTemplateByEntry(quest.RequiredNpcOrGo1);
                }
                if (quest.RequiredNpcOrGo2 > 0)
                {
                    quest.RequiredNPC2Template = _database.QueryCreatureTemplateByEntry(quest.RequiredNpcOrGo2);
                }
                if (quest.RequiredNpcOrGo3 > 0)
                {
                    quest.RequiredNPC3Template = _database.QueryCreatureTemplateByEntry(quest.RequiredNpcOrGo3);
                }
                if (quest.RequiredNpcOrGo4 > 0)
                {
                    quest.RequiredNPC4Template = _database.QueryCreatureTemplateByEntry(quest.RequiredNpcOrGo4);
                }

                // Interacts
                if (quest.RequiredNpcOrGo1 < 0)
                {
                    quest.RequiredGO1Template = _database.QueryGameObjectTemplateByEntry(-quest.RequiredNpcOrGo1);
                }
                if (quest.RequiredNpcOrGo2 < 0)
                {
                    quest.RequiredGO2Template = _database.QueryGameObjectTemplateByEntry(-quest.RequiredNpcOrGo2);
                }
                if (quest.RequiredNpcOrGo3 < 0)
                {
                    quest.RequiredGO3Template = _database.QueryGameObjectTemplateByEntry(-quest.RequiredNpcOrGo3);
                }
                if (quest.RequiredNpcOrGo4 < 0)
                {
                    quest.RequiredGO4Template = _database.QueryGameObjectTemplateByEntry(-quest.RequiredNpcOrGo4);
                }
            }
            Logger.Log($"Process time (RequiredNpcs) : {stopwatchRequiredNPC.ElapsedMilliseconds} ms");

            // Add all objectives
            foreach (ModelQuestTemplate quest in quests)
            {
                // Exploration objectives
                foreach (ModelAreaTrigger modelAreaTrigger in quest.ModelAreasTriggers)
                {
                    quest.AddObjective(new ExplorationObjective(modelAreaTrigger, quest.AreaDescription));
                }

                // Prerequisite objectives Gather
                if (quest.ItemDrop1Template != null)
                {
                    foreach (ModelGameObjectLootTemplate goLootTemplate in quest.ItemDrop1Template.GameObjectLootTemplates)
                    {
                        quest.AddObjective(new GatherObjective(quest.ItemDropQuantity1, goLootTemplate, quest.ItemDrop1Template));
                    }
                }
                if (quest.ItemDrop2Template != null)
                {
                    foreach (ModelGameObjectLootTemplate goLootTemplate in quest.ItemDrop2Template.GameObjectLootTemplates)
                    {
                        quest.AddObjective(new GatherObjective(quest.ItemDropQuantity2, goLootTemplate, quest.ItemDrop2Template));
                    }
                }
                if (quest.ItemDrop3Template != null)
                {
                    foreach (ModelGameObjectLootTemplate goLootTemplate in quest.ItemDrop3Template.GameObjectLootTemplates)
                    {
                        quest.AddObjective(new GatherObjective(quest.ItemDropQuantity3, goLootTemplate, quest.ItemDrop3Template));
                    }
                }
                if (quest.ItemDrop4Template != null)
                {
                    foreach (ModelGameObjectLootTemplate goLootTemplate in quest.ItemDrop4Template.GameObjectLootTemplates)
                    {
                        quest.AddObjective(new GatherObjective(quest.ItemDropQuantity4, goLootTemplate, quest.ItemDrop4Template));
                    }
                }

                // Prerequisite objectives Kill&Loot
                if (quest.ItemDrop1Template != null)
                {
                    foreach (ModelCreatureLootTemplate creaLootTemplate in quest.ItemDrop1Template.CreatureLootTemplates)
                    {
                        quest.AddObjective(new KillLootObjective(quest.ItemDropQuantity1, creaLootTemplate, quest.ItemDrop1Template));
                    }
                }
                if (quest.ItemDrop2Template != null)
                {
                    foreach (ModelCreatureLootTemplate creaLootTemplate in quest.ItemDrop2Template.CreatureLootTemplates)
                    {
                        quest.AddObjective(new KillLootObjective(quest.ItemDropQuantity2, creaLootTemplate, quest.ItemDrop2Template));
                    }
                }
                if (quest.ItemDrop3Template != null)
                {
                    foreach (ModelCreatureLootTemplate creaLootTemplate in quest.ItemDrop3Template.CreatureLootTemplates)
                    {
                        quest.AddObjective(new KillLootObjective(quest.ItemDropQuantity3, creaLootTemplate, quest.ItemDrop3Template));
                    }
                }
                if (quest.ItemDrop4Template != null)
                {
                    foreach (ModelCreatureLootTemplate creaLootTemplate in quest.ItemDrop4Template.CreatureLootTemplates)
                    {
                        quest.AddObjective(new KillLootObjective(quest.ItemDropQuantity4, creaLootTemplate, quest.ItemDrop4Template));
                    }
                }

                // Required items Gather
                if (quest.RequiredItem1Template != null)
                {
                    foreach (ModelGameObjectLootTemplate goLootTemplate in quest.RequiredItem1Template.GameObjectLootTemplates)
                    {
                        quest.AddObjective(new GatherObjective(quest.RequiredItemCount1, goLootTemplate, quest.RequiredItem1Template));
                    }
                }
                if (quest.RequiredItem2Template != null)
                {
                    foreach (ModelGameObjectLootTemplate goLootTemplate in quest.RequiredItem2Template.GameObjectLootTemplates)
                    {
                        quest.AddObjective(new GatherObjective(quest.RequiredItemCount2, goLootTemplate, quest.RequiredItem2Template));
                    }
                }
                if (quest.RequiredItem3Template != null)
                {
                    foreach (ModelGameObjectLootTemplate goLootTemplate in quest.RequiredItem3Template.GameObjectLootTemplates)
                    {
                        quest.AddObjective(new GatherObjective(quest.RequiredItemCount3, goLootTemplate, quest.RequiredItem3Template));
                    }
                }
                if (quest.RequiredItem4Template != null)
                {
                    foreach (ModelGameObjectLootTemplate goLootTemplate in quest.RequiredItem4Template.GameObjectLootTemplates)
                    {
                        quest.AddObjective(new GatherObjective(quest.RequiredItemCount4, goLootTemplate, quest.RequiredItem4Template));
                    }
                }
                if (quest.RequiredItem5Template != null)
                {
                    foreach (ModelGameObjectLootTemplate goLootTemplate in quest.RequiredItem5Template.GameObjectLootTemplates)
                    {
                        quest.AddObjective(new GatherObjective(quest.RequiredItemCount5, goLootTemplate, quest.RequiredItem5Template));
                    }
                }
                if (quest.RequiredItem6Template != null)
                {
                    foreach (ModelGameObjectLootTemplate goLootTemplate in quest.RequiredItem6Template.GameObjectLootTemplates)
                    {
                        quest.AddObjective(new GatherObjective(quest.RequiredItemCount6, goLootTemplate, quest.RequiredItem6Template));
                    }
                }

                // Required items Loot
                if (quest.RequiredItem1Template != null)
                {
                    foreach (ModelCreatureLootTemplate creaLootTemplate in quest.RequiredItem1Template.CreatureLootTemplates)
                    {
                        quest.AddObjective(new KillLootObjective(quest.RequiredItemCount1, creaLootTemplate, quest.RequiredItem1Template));
                    }
                }
                if (quest.RequiredItem2Template != null)
                {
                    foreach (ModelCreatureLootTemplate creaLootTemplate in quest.RequiredItem2Template.CreatureLootTemplates)
                    {
                        quest.AddObjective(new KillLootObjective(quest.RequiredItemCount2, creaLootTemplate, quest.RequiredItem2Template));
                    }
                }
                if (quest.RequiredItem3Template != null)
                {
                    foreach (ModelCreatureLootTemplate creaLootTemplate in quest.RequiredItem3Template.CreatureLootTemplates)
                    {
                        quest.AddObjective(new KillLootObjective(quest.RequiredItemCount3, creaLootTemplate, quest.RequiredItem3Template));
                    }
                }
                if (quest.RequiredItem4Template != null)
                {
                    foreach (ModelCreatureLootTemplate creaLootTemplate in quest.RequiredItem4Template.CreatureLootTemplates)
                    {
                        quest.AddObjective(new KillLootObjective(quest.RequiredItemCount4, creaLootTemplate, quest.RequiredItem4Template));
                    }
                }
                if (quest.RequiredItem5Template != null)
                {
                    foreach (ModelCreatureLootTemplate creaLootTemplate in quest.RequiredItem5Template.CreatureLootTemplates)
                    {
                        quest.AddObjective(new KillLootObjective(quest.RequiredItemCount5, creaLootTemplate, quest.RequiredItem5Template));
                    }
                }
                if (quest.RequiredItem6Template != null && quest.RequiredItem6Template.Class != 12)
                {
                    foreach (ModelCreatureLootTemplate creaLootTemplate in quest.RequiredItem6Template.CreatureLootTemplates)
                    {
                        quest.AddObjective(new KillLootObjective(quest.RequiredItemCount6, creaLootTemplate, quest.RequiredItem6Template));
                    }
                }

                // KILL / INTERACT

                // RequiredNpcOrGo
                // Value > 0:required creature_template ID the player needs to kill/cast on in order to complete the quest.
                // Value < 0:required gameobject_template ID the player needs to cast on in order to complete the quest.
                // If*RequiredSpellCast*is != 0, the objective is to cast on target, else kill.
                // NOTE: If RequiredSpellCast is != 0 and the spell has effects Send Event or Quest Complete, this field may be left empty.

                // Kill
                if (quest.RequiredNPC1Template != null)
                {
                    quest.AddObjective(new KillObjective(quest.RequiredNpcOrGoCount1, quest.RequiredNPC1Template, quest.ObjectiveText1));
                }
                if (quest.RequiredNPC2Template != null)
                {
                    quest.AddObjective(new KillObjective(quest.RequiredNpcOrGoCount2, quest.RequiredNPC2Template, quest.ObjectiveText2));
                }
                if (quest.RequiredNPC3Template != null)
                {
                    quest.AddObjective(new KillObjective(quest.RequiredNpcOrGoCount3, quest.RequiredNPC3Template, quest.ObjectiveText3));
                }
                if (quest.RequiredNPC4Template != null)
                {
                    quest.AddObjective(new KillObjective(quest.RequiredNpcOrGoCount4, quest.RequiredNPC4Template, quest.ObjectiveText4));
                }

                // Interact
                if (quest.RequiredGO1Template != null)
                {
                    quest.AddObjective(new InteractObjective(quest.RequiredNpcOrGoCount1, quest.RequiredGO1Template, quest.ObjectiveText1));
                }
                if (quest.RequiredGO2Template != null)
                {
                    quest.AddObjective(new InteractObjective(quest.RequiredNpcOrGoCount2, quest.RequiredGO2Template, quest.ObjectiveText2));
                }
                if (quest.RequiredGO3Template != null)
                {
                    quest.AddObjective(new InteractObjective(quest.RequiredNpcOrGoCount3, quest.RequiredGO3Template, quest.ObjectiveText3));
                }
                if (quest.RequiredGO4Template != null)
                {
                    quest.AddObjective(new InteractObjective(quest.RequiredNpcOrGoCount4, quest.RequiredGO4Template, quest.ObjectiveText4));
                }
            }

            DisposeDb();

            List<ModelQuestTemplate> allFilteredQuests = FilterDBQuestsAfterFills(quests);

            // Write JSON
            if (WholesomeAQSettings.CurrentSetting.DevMode)
            {
                Stopwatch stopwatchJSON = Stopwatch.StartNew();
                Logger.Log($"{allFilteredQuests.Count} results. Building JSON. Please wait.");
                ToolBox.WriteJSONFromDBResult(allFilteredQuests);
                Logger.Log($"Process time (JSON processing) : {stopwatchJSON.ElapsedMilliseconds} ms");
            }

            Logger.Log($"DONE! Process time (TOTAL) : {stopwatch.ElapsedMilliseconds} ms");

            return allFilteredQuests;
        }
    }
}

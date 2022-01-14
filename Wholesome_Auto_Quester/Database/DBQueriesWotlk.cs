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

        private static List<ModelQuestTemplate> FilterDBQuestsBeforeFills(List<ModelQuestTemplate> dbResult)
        {
            List<ModelQuestTemplate> result = new List<ModelQuestTemplate>();

            int myClass = (int)ToolBox.GetClass();
            int myFaction = (int)ToolBox.GetFaction();

            foreach (ModelQuestTemplate q in dbResult)
            {
                if ((q.QuestAddon?.SpecialFlags & 1) != 0)
                {
                    //Logger.Log($"[{q.Id}] {q.LogTitle} has been removed (Repeatable)");
                    continue;
                }
                if ((q.QuestAddon?.SpecialFlags & 2) != 0)
                {
                    //Logger.Log($"[{q.Id}] {q.LogTitle} has been removed (Escort?)");
                    continue;
                }
                if (q.QuestLevel == -1 && q.QuestAddon?.AllowableClasses == 0)
                {
                    //Logger.Log($"[{q.Id}] {q.LogTitle} has been removed (-1, not class quest)");
                    continue;
                }
                if (q.QuestAddon?.AllowableClasses > 0 && (q.QuestAddon?.AllowableClasses & myClass) == 0)
                {
                    //Logger.Log($"[{q.Id}] {q.LogTitle} has been removed (Not for my class)");
                    continue;
                }
                if (q.AllowableRaces > 0 && (q.AllowableRaces & myFaction) == 0)
                {
                    //Logger.Log($"[{q.Id}] {q.LogTitle} has been removed (Not for my race)");
                    continue;
                }
                if (q.QuestInfoID != 0)
                {
                    //Logger.Log($"[{q.Id}] {q.LogTitle} has been removed (Dungeon/Group/Raid/PvP)");
                    continue;
                }
                if (q.RequiredFactionId1 != 0 || q.RequiredFactionId2 != 0)
                {
                    //Logger.Log($"[{q.Id}] {q.LogTitle} has been removed (Reputation quest)");
                    continue;
                }

                result.Add(q);
            }
            return result;
        }

        private static List<ModelQuestTemplate> FilterDBQuestsAfterFills(List<ModelQuestTemplate> dbResult)
        {
            List<ModelQuestTemplate> result = new List<ModelQuestTemplate>();

            int myClass = (int)ToolBox.GetClass();
            int myFaction = (int)ToolBox.GetFaction();
            int myLevel = (int)ObjectManager.Me.Level;

            foreach (ModelQuestTemplate q in dbResult)
            {
                if (!q.CreatureQuestGivers.Any(qg => qg.IsNeutralOrFriendly) && q.GameObjectQuestGivers.Count <= 0)
                {
                    //Logger.Log($"[{q.Id}] {q.LogTitle} has been removed (Not for my faction)");
                    continue;
                }
                if (q.StartItemTemplate?.Spell1 != null
                    || q.StartItemTemplate?.Spell2 != null
                    || q.StartItemTemplate?.Spell3 != null
                    || q.StartItemTemplate?.Spell4 != null)
                {
                    //Logger.Log($"[{q.Id}] {q.LogTitle} has been removed (Active item)");
                    continue;
                }
                if (q.KillLootObjectives.Any(klo => klo.ItemSpell1 != null)
                    || q.KillLootObjectives.Any(klo => klo.ItemSpell2 != null)
                    || q.KillLootObjectives.Any(klo => klo.ItemSpell3 != null)
                    || q.KillLootObjectives.Any(klo => klo.ItemSpell4 != null))
                {
                    //Logger.Log($"[{q.Id}] {q.LogTitle} has been removed (Active loot item)");
                    continue;
                }

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

            List<ModelQuestTemplate> quests = FilterDBQuestsBeforeFills(_database.QueryQuests());
            
            // Query quest givers
            Stopwatch stopwatchQuestGivers = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                quest.CreatureQuestGivers = _database.QueryCreatureQuestGiver(quest.Id);
                quest.GameObjectQuestGivers = _database.QueryGameObjectQuestGivers(quest.Id);
            }
            Logger.Log($"Process time (Quest givers) : {stopwatchQuestGivers.ElapsedMilliseconds} ms");

            // Query quest enders 420ko / 21k
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

            // Query next quests ids 420ko / 21k
            Stopwatch stopwatchNextQuests = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                quest.NextQuestsIds = _database.QueryNextQuestsIdsByQuestId(quest.Id);
                if (quest.QuestAddon.NextQuestID != 0
                    && !quest.NextQuestsIds.Contains(quest.QuestAddon.NextQuestID))
                    quest.NextQuestsIds.Add(quest.QuestAddon.NextQuestID);
            }
            Logger.Log($"Process time (Next quests) : {stopwatchNextQuests.ElapsedMilliseconds} ms");

            // Query Areas 420ko / 21k
            Stopwatch stopwatchAreas = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
                quest.ModelAreasTriggers = _database.QueryAreasToExplore(quest.Id);
            Logger.Log($"Process time (Areas) : {stopwatchAreas.ElapsedMilliseconds} ms");

            // Query Item drops (prerequisites) 435ko / 22k
            Stopwatch stopwatchItemDrops = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                quest.ItemDrop1Template = _database.QueryItemTemplateByItemEntry(quest.ItemDrop1);
                quest.ItemDrop2Template = _database.QueryItemTemplateByItemEntry(quest.ItemDrop2);
                quest.ItemDrop3Template = _database.QueryItemTemplateByItemEntry(quest.ItemDrop3);
                quest.ItemDrop4Template = _database.QueryItemTemplateByItemEntry(quest.ItemDrop4);
            }
            Logger.Log($"Process time (ItemDrops) : {stopwatchItemDrops.ElapsedMilliseconds} ms");
            
            // Query required Items 2Mo / 130k
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
            
            // Query Start Item 2mo / 126k
            Stopwatch stopwatchStartItem = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                quest.StartItemTemplate = _database.QueryItemTemplateByItemEntry(quest.StartItem);
            }
            Logger.Log($"Process time (StartItem) : {stopwatchStartItem.ElapsedMilliseconds} ms");
            
            // Query required Npcs/Interacts 2mo / 133k
            Stopwatch stopwatchRequiredNPC = Stopwatch.StartNew();
            foreach (ModelQuestTemplate quest in quests)
            {
                // NPCs
                if (quest.RequiredNpcOrGo1 > 0)
                    quest.RequiredNPC1Template = _database.QueryCreatureTemplateByEntry(quest.RequiredNpcOrGo1);
                if (quest.RequiredNpcOrGo2 > 0)
                    quest.RequiredNPC2Template = _database.QueryCreatureTemplateByEntry(quest.RequiredNpcOrGo2);
                if (quest.RequiredNpcOrGo3 > 0)
                    quest.RequiredNPC3Template = _database.QueryCreatureTemplateByEntry(quest.RequiredNpcOrGo3);
                if (quest.RequiredNpcOrGo4 > 0)
                    quest.RequiredNPC4Template = _database.QueryCreatureTemplateByEntry(quest.RequiredNpcOrGo4);

                // Interacts
                if (quest.RequiredNpcOrGo1 < 0)
                    quest.RequiredGO1Template = _database.QueryGameObjectTemplateByEntry(-quest.RequiredNpcOrGo1);
                if (quest.RequiredNpcOrGo2 < 0)
                    quest.RequiredGO2Template = _database.QueryGameObjectTemplateByEntry(-quest.RequiredNpcOrGo2);
                if (quest.RequiredNpcOrGo3 < 0)
                    quest.RequiredGO3Template = _database.QueryGameObjectTemplateByEntry(-quest.RequiredNpcOrGo3);
                if (quest.RequiredNpcOrGo4 < 0)
                    quest.RequiredGO4Template = _database.QueryGameObjectTemplateByEntry(-quest.RequiredNpcOrGo4);
            }
            Logger.Log($"Process time (RequiredNpcs) : {stopwatchRequiredNPC.ElapsedMilliseconds} ms");
            
            // Add all objectives
            foreach (ModelQuestTemplate quest in quests)
            {
                // Exploration objectives 2mo/133k
                quest.ModelAreasTriggers.ForEach(modelArea =>
                    quest.AddObjective(new ExplorationObjective((int)modelArea.PositionX, modelArea, quest.AreaDescription)));
                
                // Prerequisite objectives Gather 2mo/134k
                quest.ItemDrop1Template?.GameObjectLootTemplates.ForEach(goLootTemplate =>
                    quest.AddObjective(new GatherObjective(quest.ItemDropQuantity1, goLootTemplate, quest.ItemDrop1Template)));
                quest.ItemDrop2Template?.GameObjectLootTemplates.ForEach(goLootTemplate =>
                    quest.AddObjective(new GatherObjective(quest.ItemDropQuantity2, goLootTemplate, quest.ItemDrop2Template)));
                quest.ItemDrop3Template?.GameObjectLootTemplates.ForEach(goLootTemplate =>
                    quest.AddObjective(new GatherObjective(quest.ItemDropQuantity3, goLootTemplate, quest.ItemDrop3Template)));
                quest.ItemDrop4Template?.GameObjectLootTemplates.ForEach(goLootTemplate =>
                    quest.AddObjective(new GatherObjective(quest.ItemDropQuantity4, goLootTemplate, quest.ItemDrop4Template)));
                
                // Prerequisite objectives Kill&Loot 2mo/134k
                quest.ItemDrop1Template?.CreatureLootTemplates.ForEach(creaLootTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.ItemDropQuantity1, creaLootTemplate, quest.ItemDrop1Template)));
                quest.ItemDrop2Template?.CreatureLootTemplates.ForEach(creaLootTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.ItemDropQuantity2, creaLootTemplate, quest.ItemDrop2Template)));
                quest.ItemDrop3Template?.CreatureLootTemplates.ForEach(creaLootTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.ItemDropQuantity3, creaLootTemplate, quest.ItemDrop3Template)));
                quest.ItemDrop4Template?.CreatureLootTemplates.ForEach(creaLootTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.ItemDropQuantity4, creaLootTemplate, quest.ItemDrop4Template)));

                // Required items Gather/Loot 5mo/327k
                quest.RequiredItem1Template?.GameObjectLootTemplates.ForEach(goLootTemplate =>
                    quest.AddObjective(new GatherObjective(quest.RequiredItemCount1, goLootTemplate, quest.RequiredItem1Template)));
                quest.RequiredItem2Template?.GameObjectLootTemplates.ForEach(goLootTemplate =>
                    quest.AddObjective(new GatherObjective(quest.RequiredItemCount2, goLootTemplate, quest.RequiredItem2Template)));
                quest.RequiredItem3Template?.GameObjectLootTemplates.ForEach(goLootTemplate =>
                    quest.AddObjective(new GatherObjective(quest.RequiredItemCount3, goLootTemplate, quest.RequiredItem3Template)));
                quest.RequiredItem4Template?.GameObjectLootTemplates.ForEach(goLootTemplate =>
                    quest.AddObjective(new GatherObjective(quest.RequiredItemCount4, goLootTemplate, quest.RequiredItem4Template)));
                quest.RequiredItem5Template?.GameObjectLootTemplates.ForEach(goLootTemplate =>
                    quest.AddObjective(new GatherObjective(quest.RequiredItemCount5, goLootTemplate, quest.RequiredItem5Template)));
                quest.RequiredItem6Template?.GameObjectLootTemplates.ForEach(goLootTemplate =>
                    quest.AddObjective(new GatherObjective(quest.RequiredItemCount6, goLootTemplate, quest.RequiredItem6Template)));

                // Required items Gather/Loot 300mo/20M                             
                quest.RequiredItem1Template?.CreatureLootTemplates.ForEach(creaLootTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.RequiredItemCount1, creaLootTemplate, quest.RequiredItem1Template)));
                quest.RequiredItem2Template?.CreatureLootTemplates.ForEach(creaLootTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.RequiredItemCount2, creaLootTemplate, quest.RequiredItem2Template)));
                quest.RequiredItem3Template?.CreatureLootTemplates.ForEach(creaLootTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.RequiredItemCount3, creaLootTemplate, quest.RequiredItem3Template)));
                quest.RequiredItem4Template?.CreatureLootTemplates.ForEach(creaLootTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.RequiredItemCount4, creaLootTemplate, quest.RequiredItem4Template)));
                quest.RequiredItem5Template?.CreatureLootTemplates.ForEach(creaLootTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.RequiredItemCount5, creaLootTemplate, quest.RequiredItem5Template)));
                quest.RequiredItem6Template?.CreatureLootTemplates.ForEach(creaLootTemplate =>
                    quest.AddObjective(new KillLootObjective(quest.RequiredItemCount6, creaLootTemplate, quest.RequiredItem6Template)));
               
                // KILL / INTERACT

                // RequiredNpcOrGo
                // Value > 0:required creature_template ID the player needs to kill/cast on in order to complete the quest.
                // Value < 0:required gameobject_template ID the player needs to cast on in order to complete the quest.
                // If*RequiredSpellCast*is != 0, the objective is to cast on target, else kill.
                // NOTE: If RequiredSpellCast is != 0 and the spell has effects Send Event or Quest Complete, this field may be left empty.

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
            
            List<ModelQuestTemplate> allFilteredQuests = FilterDBQuestsAfterFills(quests);
            ToolBox.UpdateCompletedQuests();

            // Write JSON
            if (WholesomeAQSettings.CurrentSetting.DevMode)
            {
                Stopwatch stopwatchJSON = Stopwatch.StartNew();
                Logger.Log($"{allFilteredQuests.Count} results. Building JSON. Please wait.");
                ToolBox.WriteJSONFromDBResult(allFilteredQuests);
                //ToolBox.ZipJSONFile();
                Logger.Log($"Process time (JSON processing) : {stopwatchJSON.ElapsedMilliseconds} ms");
            }
            
            Logger.Log($"DONE! Process time (TOTAL) : {stopwatch.ElapsedMilliseconds} ms");

            WAQTasks.AddQuests(allFilteredQuests);
        }
    }
}

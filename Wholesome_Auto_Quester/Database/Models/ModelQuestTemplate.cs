using System;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Conditions;
using Wholesome_Auto_Quester.Database.DBC;
using Wholesome_Auto_Quester.Database.Objectives;
using Wholesome_Auto_Quester.Helpers;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelQuestTemplate
    {
        public ModelQuestTemplate(
            JSONModelQuestTemplate jsonQuestTemplate,
            Dictionary<int, JSONModelCreatureTemplate> allCreatureTemplates,
            Dictionary<int, JSONModelGameObjectTemplate> allGameObjectTemplates,
            Dictionary<int, JSONModelItemTemplate> allItemTemplates,
            Dictionary<int, JSONModelSpell> allSpells)
        {
            Id = jsonQuestTemplate.Id;
            AllowableRaces = jsonQuestTemplate.AllowableRaces;
            AreaDescription = jsonQuestTemplate.AreaDescription;
            Flags = jsonQuestTemplate.Flags;
            ItemDropQuantity1 = jsonQuestTemplate.ItemDropQuantity1;
            ItemDropQuantity2 = jsonQuestTemplate.ItemDropQuantity2;
            ItemDropQuantity3 = jsonQuestTemplate.ItemDropQuantity3;
            ItemDropQuantity4 = jsonQuestTemplate.ItemDropQuantity4;
            LogTitle = jsonQuestTemplate.LogTitle;
            ObjectiveText1 = jsonQuestTemplate.ObjectiveText1;
            ObjectiveText2 = jsonQuestTemplate.ObjectiveText2;
            ObjectiveText3 = jsonQuestTemplate.ObjectiveText3;
            ObjectiveText4 = jsonQuestTemplate.ObjectiveText4;
            QuestLevel = jsonQuestTemplate.QuestLevel;
            QuestSortID = jsonQuestTemplate.QuestSortID;
            QuestInfoID = jsonQuestTemplate.QuestInfoID;
            RequiredFactionId1 = jsonQuestTemplate.RequiredFactionId1;
            RequiredFactionId2 = jsonQuestTemplate.RequiredFactionId2;
            RequiredItemCount1 = jsonQuestTemplate.RequiredItemCount1;
            RequiredItemCount2 = jsonQuestTemplate.RequiredItemCount2;
            RequiredItemCount3 = jsonQuestTemplate.RequiredItemCount3;
            RequiredItemCount4 = jsonQuestTemplate.RequiredItemCount4;
            RequiredItemCount5 = jsonQuestTemplate.RequiredItemCount5;
            RequiredItemCount6 = jsonQuestTemplate.RequiredItemCount6;
            RequiredNpcOrGoCount1 = jsonQuestTemplate.RequiredNpcOrGoCount1;
            RequiredNpcOrGoCount2 = jsonQuestTemplate.RequiredNpcOrGoCount2;
            RequiredNpcOrGoCount3 = jsonQuestTemplate.RequiredNpcOrGoCount3;
            RequiredNpcOrGoCount4 = jsonQuestTemplate.RequiredNpcOrGoCount4;
            TimeAllowed = jsonQuestTemplate.TimeAllowed;
            NextQuestsIds = jsonQuestTemplate.NextQuestsIds;
            PreviousQuestsIds = jsonQuestTemplate.PreviousQuestsIds;
            MinLevel = jsonQuestTemplate.MinLevel;

            if (jsonQuestTemplate.QuestAddon != null)
            {
                QuestAddon = new ModelQuestTemplateAddon(jsonQuestTemplate.QuestAddon);
            }

            // Conditions
            foreach (JSONModelConditions jmc in jsonQuestTemplate.Conditions)
            {
                Conditions.Add(new ModelConditions(jmc));
            }

            // Creature quest givers
            foreach (int jmctId in jsonQuestTemplate.CreatureQuestGiversEntries)
            {
                if (allCreatureTemplates.TryGetValue(jmctId, out JSONModelCreatureTemplate jmct))
                    CreatureQuestGivers.Add(new ModelCreatureTemplate(jmct, allCreatureTemplates));
                else
                    Logger.LogDevDebug($"WARNING: CreatureQuestGiversEntries template with entry {jmctId} couldn't be found in dictionary");
            }

            // Creature quest enders
            foreach (int jmctId in jsonQuestTemplate.CreatureQuestEndersEntries)
            {
                if (allCreatureTemplates.TryGetValue(jmctId, out JSONModelCreatureTemplate jmct))
                    CreatureQuestEnders.Add(new ModelCreatureTemplate(jmct, allCreatureTemplates));
                else
                    Logger.LogDevDebug($"WARNING: CreatureQuestEndersEntries template with entry {jmctId} couldn't be found in dictionary");
            }

            // GameObject quest givers
            foreach (int jmGotId in jsonQuestTemplate.GameObjectQuestGiversEntries)
            {
                if (allGameObjectTemplates.TryGetValue(jmGotId, out JSONModelGameObjectTemplate jmgot))
                    GameObjectQuestGivers.Add(new ModelGameObjectTemplate(jmgot));
                else
                    Logger.LogDevDebug($"WARNING: GameObjectQuestGiversEntries template with entry {jmGotId} couldn't be found in dictionary");
            }

            // GameObject quest enders
            foreach (int jmGotId in jsonQuestTemplate.GameObjectQuestEndersEntries)
            {
                if (allGameObjectTemplates.TryGetValue(jmGotId, out JSONModelGameObjectTemplate jmgot))
                    GameObjectQuestEnders.Add(new ModelGameObjectTemplate(jmgot));
                else
                    Logger.LogDevDebug($"WARNING: GameObjectQuestEndersEntries template with entry {jmGotId} couldn't be found in dictionary");
            }

            // Model Area Triggers
            foreach (JSONModelAreaTrigger jmat in jsonQuestTemplate.ModelAreasTriggers)
            {
                ModelAreasTriggers.Add(new ModelAreaTrigger(jmat));
            }

            // Start Item
            if (jsonQuestTemplate.StartItem != 0)
            {
                if (allItemTemplates.TryGetValue(jsonQuestTemplate.StartItem, out JSONModelItemTemplate jmit))
                    StartItemTemplate = new ModelItemTemplate(jmit, allSpells, allCreatureTemplates, allGameObjectTemplates);
                else
                    Logger.LogDevDebug($"WARNING: StartItem with entry {jsonQuestTemplate.StartItem} couldn't be found in dictionary");
            }

            // Item drops
            if (jsonQuestTemplate.ItemDrop1 != 0)
            {
                if (allItemTemplates.TryGetValue(jsonQuestTemplate.ItemDrop1, out JSONModelItemTemplate jmit))
                    ItemDrop1Template = new ModelItemTemplate(jmit, allSpells, allCreatureTemplates, allGameObjectTemplates);
                else
                    Logger.LogDevDebug($"WARNING: ItemDrop1 with entry {jsonQuestTemplate.ItemDrop1} couldn't be found in dictionary");
            }
            if (jsonQuestTemplate.ItemDrop2 != 0)
            {
                if (allItemTemplates.TryGetValue(jsonQuestTemplate.ItemDrop2, out JSONModelItemTemplate jmit))
                    ItemDrop2Template = new ModelItemTemplate(jmit, allSpells, allCreatureTemplates, allGameObjectTemplates);
                else
                    Logger.LogDevDebug($"WARNING: ItemDrop2 with entry {jsonQuestTemplate.ItemDrop2} couldn't be found in dictionary");
            }
            if (jsonQuestTemplate.ItemDrop3 != 0)
            {
                if (allItemTemplates.TryGetValue(jsonQuestTemplate.ItemDrop3, out JSONModelItemTemplate jmit))
                    ItemDrop3Template = new ModelItemTemplate(jmit, allSpells, allCreatureTemplates, allGameObjectTemplates);
                else
                    Logger.LogDevDebug($"WARNING: ItemDrop3 with entry {jsonQuestTemplate.ItemDrop3} couldn't be found in dictionary");
            }
            if (jsonQuestTemplate.ItemDrop4 != 0)
            {
                if (allItemTemplates.TryGetValue(jsonQuestTemplate.ItemDrop4, out JSONModelItemTemplate jmit))
                    ItemDrop4Template = new ModelItemTemplate(jmit, allSpells, allCreatureTemplates, allGameObjectTemplates);
                else
                    Logger.LogDevDebug($"WARNING: ItemDrop4 with entry {jsonQuestTemplate.ItemDrop4} couldn't be found in dictionary");
            }

            // Required items
            if (jsonQuestTemplate.RequiredItemId1 != 0)
            {
                if (allItemTemplates.TryGetValue(jsonQuestTemplate.RequiredItemId1, out JSONModelItemTemplate jmit))
                    RequiredItem1Template = new ModelItemTemplate(jmit, allSpells, allCreatureTemplates, allGameObjectTemplates);
                else
                    Logger.LogDevDebug($"WARNING: RequiredItemId1 with entry {jsonQuestTemplate.RequiredItemId1} couldn't be found in dictionary");
            }
            if (jsonQuestTemplate.RequiredItemId2 != 0)
            {
                if (allItemTemplates.TryGetValue(jsonQuestTemplate.RequiredItemId2, out JSONModelItemTemplate jmit))
                    RequiredItem2Template = new ModelItemTemplate(jmit, allSpells, allCreatureTemplates, allGameObjectTemplates);
                else
                    Logger.LogDevDebug($"WARNING: RequiredItemId2 with entry {jsonQuestTemplate.RequiredItemId2} couldn't be found in dictionary");
            }
            if (jsonQuestTemplate.RequiredItemId3 != 0)
            {
                if (allItemTemplates.TryGetValue(jsonQuestTemplate.RequiredItemId3, out JSONModelItemTemplate jmit))
                    RequiredItem3Template = new ModelItemTemplate(jmit, allSpells, allCreatureTemplates, allGameObjectTemplates);
                else
                    Logger.LogDevDebug($"WARNING: RequiredItemId3 with entry {jsonQuestTemplate.RequiredItemId3} couldn't be found in dictionary");
            }
            if (jsonQuestTemplate.RequiredItemId4 != 0)
            {
                if (allItemTemplates.TryGetValue(jsonQuestTemplate.RequiredItemId4, out JSONModelItemTemplate jmit))
                    RequiredItem4Template = new ModelItemTemplate(jmit, allSpells, allCreatureTemplates, allGameObjectTemplates);
                else
                    Logger.LogDevDebug($"WARNING: RequiredItemId4 with entry {jsonQuestTemplate.RequiredItemId4} couldn't be found in dictionary");
            }
            if (jsonQuestTemplate.RequiredItemId5 != 0)
            {
                if (allItemTemplates.TryGetValue(jsonQuestTemplate.RequiredItemId5, out JSONModelItemTemplate jmit))
                    RequiredItem5Template = new ModelItemTemplate(jmit, allSpells, allCreatureTemplates, allGameObjectTemplates);
                else
                    Logger.LogDevDebug($"WARNING: RequiredItemId5 with entry {jsonQuestTemplate.RequiredItemId5} couldn't be found in dictionary");
            }
            if (jsonQuestTemplate.RequiredItemId6 != 0)
            {
                if (allItemTemplates.TryGetValue(jsonQuestTemplate.RequiredItemId6, out JSONModelItemTemplate jmit))
                    RequiredItem6Template = new ModelItemTemplate(jmit, allSpells, allCreatureTemplates, allGameObjectTemplates);
                else
                    Logger.LogDevDebug($"WARNING: RequiredItemId6 with entry {jsonQuestTemplate.RequiredItemId6} couldn't be found in dictionary");
            }

            // Required NPC or GO
            if (jsonQuestTemplate.RequiredNpcOrGo1 != 0)
            {
                // Creature
                if (jsonQuestTemplate.RequiredNpcOrGo1 > 0)
                {
                    if (allCreatureTemplates.TryGetValue(jsonQuestTemplate.RequiredNpcOrGo1, out JSONModelCreatureTemplate jmct))
                        RequiredNPC1Template = new ModelCreatureTemplate(jmct, allCreatureTemplates);
                    else
                        Logger.LogDevDebug($"WARNING: RequiredNpcOrGo1 with entry {jsonQuestTemplate.RequiredNpcOrGo1} couldn't be found in dictionary");
                }
                // Game Object
                else
                {
                    if (allGameObjectTemplates.TryGetValue(-jsonQuestTemplate.RequiredNpcOrGo1, out JSONModelGameObjectTemplate jmgot))
                        RequiredGO1Template = new ModelGameObjectTemplate(jmgot);
                    else
                        Logger.LogDevDebug($"WARNING: RequiredNpcOrGo1 with entry {jsonQuestTemplate.RequiredNpcOrGo1} couldn't be found in dictionary");
                }
            }
            if (jsonQuestTemplate.RequiredNpcOrGo2 != 0)
            {
                // Creature
                if (jsonQuestTemplate.RequiredNpcOrGo2 > 0)
                {
                    if (allCreatureTemplates.TryGetValue(jsonQuestTemplate.RequiredNpcOrGo2, out JSONModelCreatureTemplate jmct))
                        RequiredNPC2Template = new ModelCreatureTemplate(jmct, allCreatureTemplates);
                    else
                        Logger.LogDevDebug($"WARNING: RequiredNpcOrGo2 with entry {jsonQuestTemplate.RequiredNpcOrGo2} couldn't be found in dictionary");
                }
                // Game Object
                else
                {
                    if (allGameObjectTemplates.TryGetValue(-jsonQuestTemplate.RequiredNpcOrGo2, out JSONModelGameObjectTemplate jmgot))
                        RequiredGO2Template = new ModelGameObjectTemplate(jmgot);
                    else
                        Logger.LogDevDebug($"WARNING: RequiredNpcOrGo2 with entry {jsonQuestTemplate.RequiredNpcOrGo2} couldn't be found in dictionary");
                }
            }
            if (jsonQuestTemplate.RequiredNpcOrGo3 != 0)
            {
                // Creature
                if (jsonQuestTemplate.RequiredNpcOrGo3 > 0)
                {
                    if (allCreatureTemplates.TryGetValue(jsonQuestTemplate.RequiredNpcOrGo3, out JSONModelCreatureTemplate jmct))
                        RequiredNPC3Template = new ModelCreatureTemplate(jmct, allCreatureTemplates);
                    else
                        Logger.LogDevDebug($"WARNING: RequiredNpcOrGo3 with entry {jsonQuestTemplate.RequiredNpcOrGo3} couldn't be found in dictionary");
                }
                // Game Object
                else
                {
                    if (allGameObjectTemplates.TryGetValue(-jsonQuestTemplate.RequiredNpcOrGo3, out JSONModelGameObjectTemplate jmgot))
                        RequiredGO3Template = new ModelGameObjectTemplate(jmgot);
                    else
                        Logger.LogDevDebug($"WARNING: RequiredNpcOrGo3 with entry {jsonQuestTemplate.RequiredNpcOrGo3} couldn't be found in dictionary");
                }
            }
            if (jsonQuestTemplate.RequiredNpcOrGo4 != 0)
            {
                // Creature
                if (jsonQuestTemplate.RequiredNpcOrGo4 > 0)
                {
                    if (allCreatureTemplates.TryGetValue(jsonQuestTemplate.RequiredNpcOrGo4, out JSONModelCreatureTemplate jmct))
                        RequiredNPC4Template = new ModelCreatureTemplate(jmct, allCreatureTemplates);
                    else
                        Logger.LogDevDebug($"WARNING: RequiredNpcOrGo4 with entry {jsonQuestTemplate.RequiredNpcOrGo4} couldn't be found in dictionary");
                }
                // Game Object
                else
                {
                    if (allGameObjectTemplates.TryGetValue(-jsonQuestTemplate.RequiredNpcOrGo4, out JSONModelGameObjectTemplate jmgot))
                        RequiredGO4Template = new ModelGameObjectTemplate(jmgot);
                    else
                        Logger.LogDevDebug($"WARNING: RequiredNpcOrGo4 with entry {jsonQuestTemplate.RequiredNpcOrGo4} couldn't be found in dictionary");
                }
            }

            // OBJECTIVES

            // Exploration objectives
            foreach (ModelAreaTrigger modelAreaTrigger in ModelAreasTriggers)
            {
                AddObjective(new ExplorationObjective(modelAreaTrigger, AreaDescription));
            }

            // Prerequisite objectives Gather
            if (ItemDrop1Template != null)
            {
                foreach (ModelGameObjectLootTemplate goLootTemplate in ItemDrop1Template.GameObjectLootTemplates)
                {
                    AddObjective(new GatherObjective(ItemDropQuantity1, goLootTemplate, ItemDrop1Template));
                }
            }
            if (ItemDrop2Template != null)
            {
                foreach (ModelGameObjectLootTemplate goLootTemplate in ItemDrop2Template.GameObjectLootTemplates)
                {
                    AddObjective(new GatherObjective(ItemDropQuantity2, goLootTemplate, ItemDrop2Template));
                }
            }
            if (ItemDrop3Template != null)
            {
                foreach (ModelGameObjectLootTemplate goLootTemplate in ItemDrop3Template.GameObjectLootTemplates)
                {
                    AddObjective(new GatherObjective(ItemDropQuantity3, goLootTemplate, ItemDrop3Template));
                }
            }
            if (ItemDrop4Template != null)
            {
                foreach (ModelGameObjectLootTemplate goLootTemplate in ItemDrop4Template.GameObjectLootTemplates)
                {
                    AddObjective(new GatherObjective(ItemDropQuantity4, goLootTemplate, ItemDrop4Template));
                }
            }

            // Prerequisite objectives Kill&Loot
            if (ItemDrop1Template != null)
            {
                foreach (ModelCreatureLootTemplate creaLootTemplate in ItemDrop1Template.CreatureLootTemplates)
                {
                    if (creaLootTemplate.CreatureTemplate.IsAttackable)
                        AddObjective(new KillLootObjective(ItemDropQuantity1, creaLootTemplate, ItemDrop1Template));
                }
            }
            if (ItemDrop2Template != null)
            {
                foreach (ModelCreatureLootTemplate creaLootTemplate in ItemDrop2Template.CreatureLootTemplates)
                {
                    if (creaLootTemplate.CreatureTemplate.IsAttackable)
                        AddObjective(new KillLootObjective(ItemDropQuantity2, creaLootTemplate, ItemDrop2Template));
                }
            }
            if (ItemDrop3Template != null)
            {
                foreach (ModelCreatureLootTemplate creaLootTemplate in ItemDrop3Template.CreatureLootTemplates)
                {
                    if (creaLootTemplate.CreatureTemplate.IsAttackable)
                        AddObjective(new KillLootObjective(ItemDropQuantity3, creaLootTemplate, ItemDrop3Template));
                }
            }
            if (ItemDrop4Template != null)
            {
                foreach (ModelCreatureLootTemplate creaLootTemplate in ItemDrop4Template.CreatureLootTemplates)
                {
                    if (creaLootTemplate.CreatureTemplate.IsAttackable)
                        AddObjective(new KillLootObjective(ItemDropQuantity4, creaLootTemplate, ItemDrop4Template));
                }
            }

            // Required items Gather
            if (RequiredItem1Template != null)
            {
                foreach (ModelGameObjectLootTemplate goLootTemplate in RequiredItem1Template.GameObjectLootTemplates)
                {
                    AddObjective(new GatherObjective(RequiredItemCount1, goLootTemplate, RequiredItem1Template));
                }
            }
            if (RequiredItem2Template != null)
            {
                foreach (ModelGameObjectLootTemplate goLootTemplate in RequiredItem2Template.GameObjectLootTemplates)
                {
                    AddObjective(new GatherObjective(RequiredItemCount2, goLootTemplate, RequiredItem2Template));
                }
            }
            if (RequiredItem3Template != null)
            {
                foreach (ModelGameObjectLootTemplate goLootTemplate in RequiredItem3Template.GameObjectLootTemplates)
                {
                    AddObjective(new GatherObjective(RequiredItemCount3, goLootTemplate, RequiredItem3Template));
                }
            }
            if (RequiredItem4Template != null)
            {
                foreach (ModelGameObjectLootTemplate goLootTemplate in RequiredItem4Template.GameObjectLootTemplates)
                {
                    AddObjective(new GatherObjective(RequiredItemCount4, goLootTemplate, RequiredItem4Template));
                }
            }
            if (RequiredItem5Template != null)
            {
                foreach (ModelGameObjectLootTemplate goLootTemplate in RequiredItem5Template.GameObjectLootTemplates)
                {
                    AddObjective(new GatherObjective(RequiredItemCount5, goLootTemplate, RequiredItem5Template));
                }
            }
            if (RequiredItem6Template != null)
            {
                foreach (ModelGameObjectLootTemplate goLootTemplate in RequiredItem6Template.GameObjectLootTemplates)
                {
                    AddObjective(new GatherObjective(RequiredItemCount6, goLootTemplate, RequiredItem6Template));
                }
            }

            // Required items Loot
            if (RequiredItem1Template != null)
            {
                foreach (ModelCreatureLootTemplate creaLootTemplate in RequiredItem1Template.CreatureLootTemplates)
                {
                    if (creaLootTemplate.CreatureTemplate.IsAttackable)
                        AddObjective(new KillLootObjective(RequiredItemCount1, creaLootTemplate, RequiredItem1Template));
                }
            }
            if (RequiredItem2Template != null)
            {
                foreach (ModelCreatureLootTemplate creaLootTemplate in RequiredItem2Template.CreatureLootTemplates)
                {
                    if (creaLootTemplate.CreatureTemplate.IsAttackable)
                        AddObjective(new KillLootObjective(RequiredItemCount2, creaLootTemplate, RequiredItem2Template));
                }
            }
            if (RequiredItem3Template != null)
            {
                foreach (ModelCreatureLootTemplate creaLootTemplate in RequiredItem3Template.CreatureLootTemplates)
                {
                    if (creaLootTemplate.CreatureTemplate.IsAttackable)
                        AddObjective(new KillLootObjective(RequiredItemCount3, creaLootTemplate, RequiredItem3Template));
                }
            }
            if (RequiredItem4Template != null)
            {
                foreach (ModelCreatureLootTemplate creaLootTemplate in RequiredItem4Template.CreatureLootTemplates)
                {
                    if (creaLootTemplate.CreatureTemplate.IsAttackable)
                        AddObjective(new KillLootObjective(RequiredItemCount4, creaLootTemplate, RequiredItem4Template));
                }
            }
            if (RequiredItem5Template != null)
            {
                foreach (ModelCreatureLootTemplate creaLootTemplate in RequiredItem5Template.CreatureLootTemplates)
                {
                    if (creaLootTemplate.CreatureTemplate.IsAttackable)
                        AddObjective(new KillLootObjective(RequiredItemCount5, creaLootTemplate, RequiredItem5Template));
                }
            }
            if (RequiredItem6Template != null && RequiredItem6Template.Class != 12)
            {
                foreach (ModelCreatureLootTemplate creaLootTemplate in RequiredItem6Template.CreatureLootTemplates)
                {
                    if (creaLootTemplate.CreatureTemplate.IsAttackable)
                        AddObjective(new KillLootObjective(RequiredItemCount6, creaLootTemplate, RequiredItem6Template));
                }
            }

            // KILL / INTERACT

            // RequiredNpcOrGo
            // Value > 0:required creature_template ID the player needs to kill/cast on in order to complete the 
            // Value < 0:required gameobject_template ID the player needs to cast on in order to complete the 
            // If*RequiredSpellCast*is != 0, the objective is to cast on target, else kill.
            // NOTE: If RequiredSpellCast is != 0 and the spell has effects Send Event or Quest Complete, this field may be left empty.

            // Kill
            if (RequiredNPC1Template != null)
            {
                if (RequiredNPC1Template.IsAttackable)
                {
                    AddObjective(new KillObjective(RequiredNpcOrGoCount1, RequiredNPC1Template, ObjectiveText1));
                }
                
                //RequiredNPC1Template.KillCredits.AddRange(_database.QueryCreatureTemplatesByKillCredits(RequiredNPC1Template.Entry));
                foreach (ModelCreatureTemplate kcTemplate in RequiredNPC1Template.KillCredits)
                {
                    if (kcTemplate.IsAttackable)
                        AddObjective(new KillObjective(RequiredNpcOrGoCount1, kcTemplate, ObjectiveText1 ?? RequiredNPC1Template.Name + " slain"));
                }
            }
            if (RequiredNPC2Template != null)
            {
                if (RequiredNPC2Template.IsAttackable)
                {
                    AddObjective(new KillObjective(RequiredNpcOrGoCount2, RequiredNPC2Template, ObjectiveText2));
                }
                //RequiredNPC2Template.KillCredits.AddRange(_database.QueryCreatureTemplatesByKillCredits(RequiredNPC2Template.Entry));
                foreach (ModelCreatureTemplate kcTemplate in RequiredNPC2Template.KillCredits)
                {
                    if (kcTemplate.IsAttackable)
                        AddObjective(new KillObjective(RequiredNpcOrGoCount2, kcTemplate, ObjectiveText2 ?? RequiredNPC2Template.Name + " slain"));
                }
            }
            if (RequiredNPC3Template != null)
            {
                if (RequiredNPC3Template.IsAttackable)
                {
                    AddObjective(new KillObjective(RequiredNpcOrGoCount3, RequiredNPC3Template, ObjectiveText3));
                }
                //RequiredNPC3Template.KillCredits.AddRange(_database.QueryCreatureTemplatesByKillCredits(RequiredNPC3Template.Entry));
                foreach (ModelCreatureTemplate kcTemplate in RequiredNPC3Template.KillCredits)
                {
                    if (kcTemplate.IsAttackable)
                        AddObjective(new KillObjective(RequiredNpcOrGoCount3, kcTemplate, ObjectiveText3 ?? RequiredNPC3Template.Name + " slain"));
                }
            }
            if (RequiredNPC4Template != null)
            {
                if (RequiredNPC4Template.IsAttackable)
                {
                    AddObjective(new KillObjective(RequiredNpcOrGoCount4, RequiredNPC4Template, ObjectiveText4));
                }
                //RequiredNPC4Template.KillCredits.AddRange(_database.QueryCreatureTemplatesByKillCredits(RequiredNPC4Template.Entry));
                foreach (ModelCreatureTemplate kcTemplate in RequiredNPC4Template.KillCredits)
                {
                    if (kcTemplate.IsAttackable)
                        AddObjective(new KillObjective(RequiredNpcOrGoCount4, kcTemplate, ObjectiveText4 ?? RequiredNPC4Template.Name + " slain"));
                }
            }

            // Interact
            if (RequiredGO1Template != null)
            {
                AddObjective(new InteractObjective(RequiredNpcOrGoCount1, RequiredGO1Template, ObjectiveText1));
            }
            if (RequiredGO2Template != null)
            {
                AddObjective(new InteractObjective(RequiredNpcOrGoCount2, RequiredGO2Template, ObjectiveText2));
            }
            if (RequiredGO3Template != null)
            {
                AddObjective(new InteractObjective(RequiredNpcOrGoCount3, RequiredGO3Template, ObjectiveText3));
            }
            if (RequiredGO4Template != null)
            {
                AddObjective(new InteractObjective(RequiredNpcOrGoCount4, RequiredGO4Template, ObjectiveText4));
            }
        }

        public int Id { get; }
        public int AllowableRaces { get; }
        public string AreaDescription { get; }
        public long Flags { get; }
        public int ItemDropQuantity1 { get; }
        public int ItemDropQuantity2 { get; }
        public int ItemDropQuantity3 { get; }
        public int ItemDropQuantity4 { get; }
        public string LogTitle { get; }
        public string ObjectiveText1 { get; }
        public string ObjectiveText2 { get; }
        public string ObjectiveText3 { get; }
        public string ObjectiveText4 { get; }
        public int QuestLevel { get; set; }
        public int QuestSortID { get; }
        public int QuestInfoID { get; }
        public int RequiredFactionId1 { get; }
        public int RequiredFactionId2 { get; }
        public int RequiredNpcOrGoCount1 { get; set; }
        public int RequiredNpcOrGoCount2 { get; set; }
        public int RequiredNpcOrGoCount3 { get; set; }
        public int RequiredNpcOrGoCount4 { get; set; }
        public int RequiredItemCount1 { get; }
        public int RequiredItemCount2 { get; }
        public int RequiredItemCount3 { get; }
        public int RequiredItemCount4 { get; }
        public int RequiredItemCount5 { get; }
        public int RequiredItemCount6 { get; }
        public int TimeAllowed { get; }
        public int MinLevel { get; }

        public List<int> NextQuestsIds { get; set; } = new List<int>();
        public List<int> PreviousQuestsIds { get; set; } = new List<int>();

        public ModelQuestTemplateAddon QuestAddon { get; set; }
        public List<ModelCreatureTemplate> CreatureQuestGivers { get; set; } = new List<ModelCreatureTemplate>();
        public List<ModelGameObjectTemplate> GameObjectQuestGivers { get; set; } = new List<ModelGameObjectTemplate>();
        public List<ModelCreatureTemplate> CreatureQuestEnders { get; set; } = new List<ModelCreatureTemplate>();
        public List<ModelGameObjectTemplate> GameObjectQuestEnders { get; set; } = new List<ModelGameObjectTemplate>();
        public List<ModelAreaTrigger> ModelAreasTriggers { get; set; } = new List<ModelAreaTrigger>();
        public ModelItemTemplate StartItemTemplate { get; set; }
        public ModelItemTemplate ItemDrop1Template { get; set; }
        public ModelItemTemplate ItemDrop2Template { get; set; }
        public ModelItemTemplate ItemDrop3Template { get; set; }
        public ModelItemTemplate ItemDrop4Template { get; set; }
        public ModelItemTemplate RequiredItem1Template { get; set; }
        public ModelItemTemplate RequiredItem2Template { get; set; }
        public ModelItemTemplate RequiredItem3Template { get; set; }
        public ModelItemTemplate RequiredItem4Template { get; set; }
        public ModelItemTemplate RequiredItem5Template { get; set; }
        public ModelItemTemplate RequiredItem6Template { get; set; }
        public ModelCreatureTemplate RequiredNPC1Template { get; set; }
        public ModelCreatureTemplate RequiredNPC2Template { get; set; }
        public ModelCreatureTemplate RequiredNPC3Template { get; set; }
        public ModelCreatureTemplate RequiredNPC4Template { get; set; }
        public ModelGameObjectTemplate RequiredGO1Template { get; set; }
        public ModelGameObjectTemplate RequiredGO2Template { get; set; }
        public ModelGameObjectTemplate RequiredGO3Template { get; set; }
        public ModelGameObjectTemplate RequiredGO4Template { get; set; }

        public List<ExplorationObjective> ExplorationObjectives { get; set; } = new List<ExplorationObjective>();
        public List<GatherObjective> GatherObjectives { get; set; } = new List<GatherObjective>();
        public List<KillObjective> KillObjectives { get; set; } = new List<KillObjective>();
        public List<KillLootObjective> KillLootObjectives { get; set; } = new List<KillLootObjective>();
        public List<InteractObjective> InteractObjectives { get; set; } = new List<InteractObjective>();
        public List<GatherObjective> PrerequisiteGatherObjectives { get; set; } = new List<GatherObjective>();
        public List<KillLootObjective> PrerequisiteLootObjectives { get; set; } = new List<KillLootObjective>();

        public List<ModelConditions> Conditions { get; set; } = new List<ModelConditions>();

        private List<IDBConditionGroup> _conditions;
        public List<IDBConditionGroup> DBConditionGroups
        {
            get
            {
                if (_conditions == null)
                {
                    List<IDBConditionGroup> result = new List<IDBConditionGroup>();
                    foreach (ModelConditions condition in Conditions)
                    {
                        IDBConditionGroup existingGroup = result.Find(group => group.IsPartOfGroup(condition));
                        if (existingGroup == null)
                        {
                            IDBConditionGroup groupToAdd = new DBConditionGroup(condition.SourceTypeOrReferenceId,
                                condition.SourceGroup, condition.SourceEntry, condition.ElseGroup);
                            groupToAdd.AddConditionToGroup(new DBCondition(condition));
                            result.Add(groupToAdd);
                        }
                        else
                        {
                            existingGroup.AddConditionToGroup(new DBCondition(condition));
                        }
                    }
                    _conditions = result;
                }
                return _conditions;
            }
        }

        public void AddObjective(Objective objective)
        {
            if (objective is ExplorationObjective explorationObjective)
            {
                ExplorationObjectives.Add(explorationObjective);
            }
            if (objective is GatherObjective gatherObjective)
            {
                GatherObjectives.Add(gatherObjective);
            }
            if (objective is InteractObjective interactObjective)
            {
                InteractObjectives.Add(interactObjective);
            }
            if (objective is KillLootObjective killLootObjective)
            {
                if (killLootObjective.CreatureLootTemplate.CreatureTemplate.Entry == 7997) return; // Captured Sprite
                if (killLootObjective.CreatureLootTemplate.Chance <= 1) return;
                KillLootObjectives.Add(killLootObjective);
            }
            if (objective is KillObjective killObjective)
            {
                KillObjectives.Add(killObjective);
            }
        }

        private List<string> _questFlags;
        public List<string> QuestFlags
        {
            get
            {
                if (_questFlags == null) _questFlags = GetMatchingQuestFlags(Flags);
                return _questFlags;
            }
        }

        private List<string> _questSpecialFlags;
        public List<string> QuestSpecialFlags
        {
            get
            {
                if (QuestAddon != null && _questSpecialFlags == null)
                {
                    _questSpecialFlags = GetMatchingQuestSpecialFlags(QuestAddon.SpecialFlags);
                }
                return _questSpecialFlags;
            }
        }

        public List<string> GetMatchingQuestFlags(long flag)
        {
            List<string> result = new List<string>();
            foreach (long i in Enum.GetValues(typeof(QUEST_FLAGS)))
            {
                if ((flag & i) != 0)
                    result.Add(Enum.GetName(typeof(QUEST_FLAGS), i));
            }
            return result;
        }

        public List<string> GetMatchingQuestSpecialFlags(long flag)
        {
            List<string> result = new List<string>();
            foreach (long i in Enum.GetValues(typeof(QUEST_SPECIAL_FLAGS)))
            {
                if ((flag & i) != 0)
                    result.Add(Enum.GetName(typeof(QUEST_SPECIAL_FLAGS), i));
            }
            return result;
        }

        public string ReputationMismatch
        {
            get
            {
                if (QuestAddon?.RequiredMinRepFaction > 0)
                {
                    Reputation rep = DBCFaction.GetReputationById(QuestAddon.RequiredMinRepFaction);
                    if (rep == null)
                    {
                        return $"Min reputation unknown : {QuestAddon.RequiredMinRepFaction}";
                    }
                    if (rep.Amount < QuestAddon.RequiredMinRepValue)
                    {
                        return $"Reputation with {rep.Name} too low";
                    }
                }
                if (QuestAddon?.RequiredMaxRepFaction > 0)
                {
                    Reputation rep = DBCFaction.GetReputationById(QuestAddon.RequiredMaxRepFaction);
                    if (rep == null)
                    {
                        return $"Max reputation unknown : {QuestAddon.RequiredMaxRepFaction}";
                    }
                    if (rep.Amount > QuestAddon.RequiredMaxRepValue)
                    {
                        return $"Reputation with {rep.Name} too high";
                    }
                }
                return null;
            }
        }
    }
}

public enum QUEST_SPECIAL_FLAGS : long
{
    QUEST_REPEATABLE = 1,
    QUEST_EXTERNAL_EVENTS = 2,
    QUEST_AUTO_ACCEPT = 4,
    QUEST_DUNGEON_FINDER = 8,
    QUEST_MONTHLY = 16,
    QUEST_KILL_BUNNY_NPC = 32,
}

public enum QUEST_FLAGS : long
{
    QUEST_FLAGS_NONE = 0,
    QUEST_FLAGS_STAY_ALIVE = 1,
    QUEST_FLAGS_PARTY_ACCEPT = 2,
    QUEST_FLAGS_EXPLORATION = 4,
    QUEST_FLAGS_SHARABLE = 8,
    QUEST_FLAGS_HAS_CONDITION = 16,
    QUEST_FLAGS_HIDE_REWARD_POI = 32,
    QUEST_FLAGS_RAID = 64,
    QUEST_FLAGS_TBC = 128,
    QUEST_FLAGS_NO_MONEY_FROM_XP = 256,
    QUEST_FLAGS_HIDDEN_REWARDS = 512,
    QUEST_FLAGS_TRACKING = 1024,
    QUEST_FLAGS_DEPRECATE_REPUTATION = 2048,
    QUEST_FLAGS_DAILY = 4096,
    QUEST_FLAGS_FLAGS_PVP = 8192,
    QUEST_FLAGS_UNAVAILABLE = 16384,
    QUEST_FLAGS_WEEKLY = 32768,
    QUEST_FLAGS_AUTOCOMPLETE = 65536,
    QUEST_FLAGS_DISPLAY_ITEM_IN_TRACKER = 131072,
    QUEST_FLAGS_OBJ_TEXT = 262144,
    QUEST_FLAGS_AUTO_ACCEPT = 524288,
    QUEST_FLAGS_PLAYER_CAST_ON_ACCEPT = 1048576,
    QUEST_FLAGS_PLAYER_CAST_ON_COMPLETE = 2097152,
    QUEST_FLAGS_UPDATE_PHASE_SHIFT = 4194304,
    QUEST_FLAGS_SOR_WHITELIST = 8388608,
    QUEST_FLAGS_LAUNCH_GOSSIP_COMPLETE = 16777216,
    QUEST_FLAGS_REMOVE_EXTRA_GET_ITEMS = 33554432,
    QUEST_FLAGS_HIDE_UNTIL_DISCOVERED = 67108864,
    QUEST_FLAGS_PORTRAIT_IN_QUEST_LOG = 134217728,
    QUEST_FLAGS_SHOW_ITEM_WHEN_COMPLETED = 268435456,
    QUEST_FLAGS_LAUNCH_GOSSIP_ACCEPT = 536870912,
    QUEST_FLAGS_ITEMS_GLOW_WHEN_DONE = 1073741824,
    QUEST_FLAGS_FAIL_ON_LOGOUT = 2147483648,
}

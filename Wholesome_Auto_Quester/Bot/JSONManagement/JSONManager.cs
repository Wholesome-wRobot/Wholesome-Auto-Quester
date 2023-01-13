using Newtonsoft.Json;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using WholesomeToolbox;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.JSONManagement
{
    public class JSONManager : IJSONManager
    {
        private object _lock = new object();
        private bool _logFilter = false;
        private readonly FullJSONModel _fullJsonModel;
        private readonly Dictionary<int, JSONModelCreatureTemplate> _creatureTemplatesDic = new Dictionary<int, JSONModelCreatureTemplate>();
        private readonly Dictionary<int, JSONModelGameObjectTemplate> _gameObjectTemplatesDic = new Dictionary<int, JSONModelGameObjectTemplate>();
        private readonly Dictionary<int, JSONModelItemTemplate> _itemTemplatesDic = new Dictionary<int, JSONModelItemTemplate>();
        private readonly Dictionary<int, JSONModelSpell> _spellsDic = new Dictionary<int, JSONModelSpell>();
        private readonly Dictionary<int, JSONModelCreatureTemplate> _creatureTemplatesToGrindDic = new Dictionary<int, JSONModelCreatureTemplate>();

        public JSONManager()
        {
            using (StreamReader reader = new StreamReader(Others.GetCurrentDirectory + @"Data\AQ.json"))
            {
                string jsonFile = reader.ReadToEnd();
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) =>
                    {
                        Logger.LogError($"Deserialization error: {args.CurrentObject} => {args.ErrorContext.Error}");
                    }
                };

                _fullJsonModel = JsonConvert.DeserializeObject<FullJSONModel>(jsonFile, settings);
            }

            // fill dictionaries
            foreach (JSONModelCreatureTemplate jmct in _fullJsonModel.CreatureTemplates)
            {
                _creatureTemplatesDic.Add(jmct.entry, jmct);
            }
            foreach (JSONModelGameObjectTemplate jmGot in _fullJsonModel.GameObjectTemplates)
            {
                _gameObjectTemplatesDic.Add(jmGot.entry, jmGot);
            }
            foreach (JSONModelItemTemplate jmit in _fullJsonModel.ItemTemplates)
            {
                _itemTemplatesDic.Add(jmit.Entry, jmit);
            }
            foreach (JSONModelSpell jms in _fullJsonModel.Spells)
            {
                _spellsDic.Add(jms.Id, jms);
            }
            foreach (JSONModelCreatureTemplate jmct in _fullJsonModel.CreaturesToGrind)
            {
                _creatureTemplatesToGrindDic.Add(jmct.entry, jmct);
            }
        }

        public List<ModelWorldMapArea> GetWorldMapAreasFromJSON()
        {
            lock (_lock)
            {
                List<ModelWorldMapArea> result = new List<ModelWorldMapArea>();
                foreach (JSONModelWorldMapArea jmwma in _fullJsonModel.WorldMapAreas)
                {
                    result.Add(new ModelWorldMapArea(jmwma));
                }
                return result;
            }
        }

        public List<ModelCreatureTemplate> GetCreatureTemplatesToGrindFromJSON()
        {
            lock (_lock)
            {
                List<ModelCreatureTemplate> result = new List<ModelCreatureTemplate>();
                foreach (JSONModelCreatureTemplate jmct in _fullJsonModel.CreaturesToGrind)
                {
                    result.Add(new ModelCreatureTemplate(jmct, _creatureTemplatesToGrindDic));
                }
                return result;
            }
        }

        public List<ModelQuestTemplate> GetAvailableQuestsFromJSON()
        {
            lock (_lock)
            {
                List<JSONModelQuestTemplate> JSONquests = new List<JSONModelQuestTemplate>(_fullJsonModel.QuestTemplates);
                List<ModelQuestTemplate> result = new List<ModelQuestTemplate>();

                int levelDeltaMinus = System.Math.Max((int)ObjectManager.Me.Level - WholesomeAQSettings.CurrentSetting.LevelDeltaMinus, 1);
                int levelDeltaPlus = (int)ObjectManager.Me.Level + WholesomeAQSettings.CurrentSetting.LevelDeltaPlus;

                int myClass = (int)ToolBox.GetClass();
                int myFaction = (int)ToolBox.GetFaction();
                int myLevel = (int)ObjectManager.Me.Level;

                List<int> logQuestsIds = Quest.GetLogQuestId().Select(q => q.ID).ToList();

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

                // Load quest list from JSON
                foreach (JSONModelQuestTemplate jsonQuestTemplate in JSONquests)
                {
                    if (jsonQuestTemplate.MinLevel > myLevel)
                    {
                        continue;
                    }

                    if (!questsIdsToForce.Contains(jsonQuestTemplate.Id)
                        && (jsonQuestTemplate.QuestLevel > levelDeltaPlus || jsonQuestTemplate.QuestLevel < levelDeltaMinus)
                        && jsonQuestTemplate.QuestLevel != -1
                        && (!logQuestsIds.Contains(jsonQuestTemplate.Id) || jsonQuestTemplate.QuestLevel > levelDeltaPlus))
                    {
                        //if (_logFilter) Logger.LogDebug($"[{jsonQuestTemplate.Id}] {jsonQuestTemplate.LogTitle} has been removed (invalid level)");
                        continue;
                    }

                    if (myLevel < 60 && (jsonQuestTemplate.Id == 9407 || jsonQuestTemplate.Id == 10119))
                    {
                        if (_logFilter) Logger.LogDebug($"[{jsonQuestTemplate.Id}] {jsonQuestTemplate.LogTitle} has been removed (Wait lvl 60 for dark portal)");
                        continue;
                    }

                    if (jsonQuestTemplate.AllowableRaces > 0 && (jsonQuestTemplate.AllowableRaces & myFaction) == 0)
                    {
                        if (_logFilter) Logger.LogDebug($"[{jsonQuestTemplate.Id}] {jsonQuestTemplate.LogTitle} has been removed (Not for my race)");
                        continue;
                    }

                    ModelQuestTemplate quest = new ModelQuestTemplate(
                            jsonQuestTemplate,
                            _creatureTemplatesDic,
                            _gameObjectTemplatesDic,
                            _itemTemplatesDic,
                            _spellsDic
                        );

                    quest.CreatureQuestGivers.RemoveAll(creature => !creature.IsNeutralOrFriendly);
                    quest.CreatureQuestEnders.RemoveAll(creature => !creature.IsNeutralOrFriendly);

                    if (quest.QuestAddon != null 
                        && quest.QuestAddon.AllowableClasses > 0 
                        && (quest.QuestAddon.AllowableClasses & myClass) == 0)
                    {
                        if (_logFilter) Logger.LogDebug($"[{quest.Id}] {quest.LogTitle} has been removed (Not for my class)");
                        continue;
                    }

                    if (quest.KillLootObjectives.Count > 0 && quest.KillLootObjectives.All(klo => !klo.CreatureLootTemplate.CreatureTemplate.IsValidForKill))
                    {
                        if (_logFilter) Logger.LogDebug($"[{quest.Id}] {quest.LogTitle} has been removed (All enemies are invalid 0)");
                        continue;
                    }

                    if (quest.KillObjectives.Count > 0 && quest.KillObjectives.All(ko => !ko.CreatureTemplate.IsValidForKill))
                    {
                        if (_logFilter) Logger.LogDebug($"[{quest.Id}] {quest.LogTitle} has been removed (All enemies are invalid 1)");
                        continue;
                    }

                    if (quest.KillLootObjectives.Any(klo => klo.ItemTemplate.Class != 12))
                    {
                        if (_logFilter) Logger.LogDebug($"[{quest.Id}] {quest.LogTitle} has been removed (Kill loot objective is not quest item)");
                        continue;
                    }

                    if (quest.CreatureQuestGivers.Count <= 0 && quest.CreatureQuestEnders.Count <= 0)
                    {
                        if (_logFilter) Logger.LogDebug($"[{quest.Id}] {quest.LogTitle} has been removed (no quest giver, no quest turner)");
                        continue;
                    }

                    if (quest.CreatureQuestGivers.Count > 0 
                        && !quest.CreatureQuestGivers.Any(qg => qg.IsNeutralOrFriendly) 
                        && quest.GameObjectQuestGivers.Count <= 0)
                    {
                        if (_logFilter) Logger.LogDebug($"[{quest.Id}] {quest.LogTitle} has been removed (Not for my faction)");
                        continue;
                    }

                    if (quest.StartItemTemplate != null && quest.StartItemTemplate.HasASpellAttached
                        || quest.ItemDrop1Template != null && quest.ItemDrop1Template.HasASpellAttached
                        || quest.ItemDrop2Template != null && quest.ItemDrop2Template.HasASpellAttached
                        || quest.ItemDrop3Template != null && quest.ItemDrop3Template.HasASpellAttached
                        || quest.ItemDrop4Template != null && quest.ItemDrop4Template.HasASpellAttached)
                    {
                        if (_logFilter) Logger.LogDebug($"[{quest.Id}] {quest.LogTitle} has been removed (Active start/prerequisite item)");
                        continue;
                    }

                    if (quest.KillLootObjectives.Any(klo => klo.ItemTemplate.HasASpellAttached))
                    {
                        if (_logFilter) Logger.Log($"[{quest.Id}] {quest.LogTitle} has been removed (Active loot item)");
                        continue;
                    }

                    result.Add(quest);
                }

                if (WholesomeAQSettings.CurrentSetting.DevMode)
                {
                    Stopwatch stopwatchJSON = Stopwatch.StartNew();
                    Logger.Log($"Building Debug JSON ({result.Count} quests). Please wait...");
                    try
                    {
                        File.Delete(Others.GetCurrentDirectory + @"\Data\AQDebug.json");
                        using (StreamWriter file = File.CreateText(Others.GetCurrentDirectory + @"\Data\AQDebug.json"))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.ContractResolver = ShouldSerializeContractResolver.Instance;
                            serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                            serializer.DefaultValueHandling = DefaultValueHandling.Ignore;
                            serializer.NullValueHandling = NullValueHandling.Ignore;
                            serializer.Serialize(file, result);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("WriteJSONFromDBResult > " + e.Message);
                    }
                    Logger.Log($"Process time (Debug JSON processing) : {stopwatchJSON.ElapsedMilliseconds} ms");
                }

                return result;
            }
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
        }
    }
}

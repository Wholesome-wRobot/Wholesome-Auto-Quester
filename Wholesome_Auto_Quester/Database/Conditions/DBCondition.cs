using Wholesome_Auto_Quester.Database.DBC;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;

namespace Wholesome_Auto_Quester.Database.Conditions
{
    public class DBCondition : IDBCondition
    {
        private ModelConditions _modelConditions;
        private string _conditionText = "Unknown";
        public string GetConditionText => _conditionText;

        public DBCondition(ModelConditions modelConditions)
        {
            _modelConditions = modelConditions;
        }

        public bool IsMet
        {
            get
            {
                if (_modelConditions.SourceTypeOrReferenceId == 19) // CONDITION_SOURCE_TYPE_QUEST_AVAILABLE
                {
                    bool positive = _modelConditions.NegativeCondition == 0;
                    string exc = positive ? "" : "!";


                    if (_modelConditions.ConditionTypeOrReference == 12 // CONDITION_ACTIVE_EVENT
                        || _modelConditions.ConditionTypeOrReference == 17 // CONDITION_ACHIEVEMENT
                        || _modelConditions.ConditionTypeOrReference == 43) // CONDITION_DAILY_QUEST_DONE
                    {
                        _conditionText = "Event, Achievement or Daily";
                        return false;
                    }

                    if (_modelConditions.ConditionTypeOrReference == 2) // CONDITION_ITEM
                    {
                        _conditionText = $"{exc}Need {_modelConditions.ConditionValue2}x item {_modelConditions.ConditionValue1}";
                        return positive ?
                            ItemsManager.GetItemCountById((uint)_modelConditions.ConditionValue1) >= _modelConditions.ConditionValue2
                            : ItemsManager.GetItemCountById((uint)_modelConditions.ConditionValue1) < _modelConditions.ConditionValue2;
                    }

                    if (_modelConditions.ConditionTypeOrReference == 8 
                        || _modelConditions.ConditionTypeOrReference == 28) // CONDITION_QUESTREWARDED || CONDITION_QUEST_COMPLETE
                    {
                        _conditionText = $"{exc}Need {_modelConditions.ConditionValue1} complete";
                        return positive ?
                            ToolBox.IsQuestCompleted(_modelConditions.ConditionValue1) 
                            : !ToolBox.IsQuestCompleted(_modelConditions.ConditionValue1);
                    }

                    if (_modelConditions.ConditionTypeOrReference == 9) // CONDITION_QUESTTAKEN
                    {
                        _conditionText = $"{exc}Need {_modelConditions.ConditionValue1} in log";
                        return positive ?
                            Quest.HasQuest(_modelConditions.ConditionValue1) 
                            : !Quest.HasQuest(_modelConditions.ConditionValue1);
                    }

                    if (_modelConditions.ConditionTypeOrReference == 5) // CONDITION_REPUTATION_RANK
                    {
                        Reputation rep = DBCFaction.GetReputationById(_modelConditions.ConditionValue1);
                        _conditionText = $"{exc}Need rep mask {_modelConditions.ConditionValue2} with faction {_modelConditions.ConditionValue1} " +
                            $"({rep?.GetFactionMask})";
                        if (rep != null)
                        {
                            return positive ?
                                (rep.GetFactionMask & _modelConditions.ConditionValue2) != 0
                                : (rep.GetFactionMask & _modelConditions.ConditionValue2) == 0;
                        }
                        return false;
                    }
                }
                //Logger.LogError($"{_modelConditions.SourceEntry} has an unmet condition of type {_modelConditions.SourceTypeOrReferenceId} - {_modelConditions.ConditionTypeOrReference}");
                return true;
            }
        }
    }
}

using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Conditions
{
    public interface IDBConditionGroup
    {
        public bool ConditionsMet { get; }
        string GetGroupConditionsText { get; }
        bool IsPartOfGroup(ModelConditions modelCondition);
        void AddConditionToGroup(DBCondition condition);
    }
}

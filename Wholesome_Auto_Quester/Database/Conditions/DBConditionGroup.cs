using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Conditions
{
    class DBConditionGroup : IDBConditionGroup
    {
        private List<IDBCondition> _conditions = new List<IDBCondition>();
        private int _sourceType;
        private int _sourceEntry;
        private int _sourceGroup;
        private int _elseGroup;

        public DBConditionGroup(int sourceType, int sourceGroup, int sourceEntry, int elseGroup)
        {
            _sourceType = sourceType;
            _sourceGroup = sourceGroup;
            _sourceEntry = sourceEntry;
            _elseGroup = elseGroup;
        }

        public bool IsPartOfGroup(ModelConditions modelCondition)
        {
            return modelCondition.SourceTypeOrReferenceId == _sourceType
                && modelCondition.SourceGroup == _sourceGroup
                && modelCondition.SourceEntry == _sourceEntry
                && modelCondition.ElseGroup == _elseGroup;
        }

        public void AddConditionToGroup(DBCondition condition) => _conditions.Add(condition);
        // TODO to replace once ok
        public bool ConditionsMet
        {
            get
            {
                bool result = true;
                foreach (DBCondition cond in _conditions)
                {
                    if (!cond.IsMet) result = false;
                }
                return result;
            }
        }
        //public bool ConditionsMet => _conditions.TrueForAll(cond => cond.IsMet);
        public string GetGroupConditionsText
        {
            get
            {
                string result = "";
                foreach (IDBCondition condition in _conditions)
                {
                    result += $"{condition.GetConditionText}\n";
                }
                return result;
            }
        }
    }
}

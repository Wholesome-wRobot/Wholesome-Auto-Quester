using System.Collections.Generic;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelQuestTemplateAddon
    {
        public int AllowableClasses { get; }
        public int PrevQuestID { get; }
        public int NextQuestID { get; }
        public int ExclusiveGroup { get; }
        public int RequiredSkillID { get; }
        public int RequiredSkillPoints { get; }
        public int SpecialFlags { get; }
        public int RequiredMaxRepFaction { get; }
        public int RequiredMaxRepValue { get; }
        public int RequiredMinRepFaction { get; }
        public int RequiredMinRepValue { get; }

        public List<int> ExclusiveQuests { get; set; }
    }
}

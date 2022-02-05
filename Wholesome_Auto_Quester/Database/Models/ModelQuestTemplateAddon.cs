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

        public List<int> ExclusiveQuests { get; set; }
    }
}

using System.Collections.Generic;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelQuestTemplateAddon
    {
        public List<int> ExclusiveQuests { get; set; }
        public int AllowableClasses { get; set; }
        public int PrevQuestID { get; set; }
        public int NextQuestID { get; set; }
        public int ExclusiveGroup { get; set; }
        public int RequiredSkillID { get; set; }
        public int RequiredSkillPoints { get; set; }
        public int SpecialFlags { get; set; }
    }
}

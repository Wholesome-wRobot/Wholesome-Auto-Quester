using System.Collections.Generic;

namespace Db_To_Json.AutoQuester.Models
{
    internal class AQModelQuestTemplateAddon
    {
        public int ID { get; set; }
        public int AllowableClasses { get; set; }
        public int PrevQuestID { get; set; }
        public int NextQuestID { get; set; }
        public int ExclusiveGroup { get; set; }
        public int RequiredSkillID { get; set; }
        public int RequiredSkillPoints { get; set; }
        public int SpecialFlags { get; set; }
        public int RequiredMaxRepFaction { get; set; }
        public int RequiredMaxRepValue { get; set; }
        public int RequiredMinRepFaction { get; set; }
        public int RequiredMinRepValue { get; set; }

        public List<int> ExclusiveQuests { get; set; }
    }
}

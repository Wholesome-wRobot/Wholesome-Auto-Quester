namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelConditions
    {
        public ModelConditions(JSONModelConditions jmc)
        {
            SourceTypeOrReferenceId = jmc.SourceTypeOrReferenceId;
            SourceGroup = jmc.SourceGroup;
            SourceEntry = jmc.SourceEntry;
            ElseGroup = jmc.ElseGroup;
            ConditionTypeOrReference = jmc.ConditionTypeOrReference;
            ConditionValue1 = jmc.ConditionValue1;
            ConditionValue2 = jmc.ConditionValue2;
            NegativeCondition = jmc.NegativeCondition;
        }

        public int SourceTypeOrReferenceId { get; set; }
        public int SourceGroup { get; set; }
        public int SourceEntry { get; set; }
        //public int SourceId { get; set; }
        public int ElseGroup { get; set; }
        public int ConditionTypeOrReference { get; set; }
        //public int ConditionTarget { get;  set;}
        public int ConditionValue1 { get; set; }
        public int ConditionValue2 { get; set; }
        //public int ConditionValue3 { get; set; }
        public int NegativeCondition { get; set; }
        //public int ErrorType { get; set; }
        //public int ErrorTextId { get; set; }
        //public string ScriptName { get; set; }
        //public string Comment { get; set; }
    }
}

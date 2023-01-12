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
        public int SourceTypeOrReferenceId { get; }
        public int SourceGroup { get; }
        public int SourceEntry { get; }
        //public int SourceId { get; }
        public int ElseGroup { get; }
        public int ConditionTypeOrReference { get; }
        //public int ConditionTarget { get; }
        public int ConditionValue1 { get; }
        public int ConditionValue2 { get; }
        //public int ConditionValue3 { get; }
        public int NegativeCondition { get; }
        //public int ErrorType { get; }
        //public int ErrorTextId { get; }
        //public string ScriptName { get; }
        //public string Comment { get; }
    }
}

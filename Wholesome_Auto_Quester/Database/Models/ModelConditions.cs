namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelConditions
    {
        public int SourceTypeOrReferenceId { get; }
        public int SourceGroup { get; }
        public int SourceEntry { get; }
        public int SourceId { get; }
        public int ElseGroup { get; }
        public int ConditionTypeOrReference { get; }
        public int ConditionTarget { get; }
        public int ConditionValue1 { get; }
        public int ConditionValue2 { get; }
        public int ConditionValue3 { get; }
        public int NegativeCondition { get; }
        public int ErrorType { get; }
        public int ErrorTextId { get; }
        public string ScriptName { get; }
        public string Comment { get; }
    }
}

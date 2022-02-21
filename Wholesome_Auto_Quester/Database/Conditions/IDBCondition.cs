namespace Wholesome_Auto_Quester.Database.Conditions
{
    public interface IDBCondition
    {
        bool IsMet { get; }
        public string GetConditionText { get; }
    }
}

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class Objective
    {
        public int ObjectiveIndex { get; set; } = -1;
        public int Amount { get; protected set; }
        public string ObjectiveName { get; protected set; }
    }
}

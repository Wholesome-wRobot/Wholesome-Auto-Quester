using robotManager.Helpful;
using Wholesome_Auto_Quester.Helpers;

namespace Wholesome_Auto_Quester.Bot
{
    internal interface IWAQTask
    {
        public TaskType TaskType { get; }
        public Vector3 Location { get; }
    }
}

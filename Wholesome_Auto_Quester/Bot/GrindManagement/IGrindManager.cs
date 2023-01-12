using System.Collections.Generic;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;

namespace Wholesome_Auto_Quester.Bot.GrindManagement
{
    public interface IGrindManager : ICycleable
    {
        List<IWAQTask> GetGrindTasks { get; }
        void RecordGrindTasksFromJSON();
    }
}

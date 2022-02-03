using System.Collections.Generic;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;

namespace Wholesome_Auto_Quester.Bot.TaskManagement
{
    public interface ITaskManager : ICycleable
    {
        IWAQTask ActiveTask { get; }
        //List<IWAQTask> _taskPile { get; }
    }
}

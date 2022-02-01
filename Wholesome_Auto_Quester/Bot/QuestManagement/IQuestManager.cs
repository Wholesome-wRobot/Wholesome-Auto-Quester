using System.Collections.Generic;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;

namespace Wholesome_Auto_Quester.Bot.QuestManagement
{
    public interface IQuestManager : ICycleable
    {
        List<IWAQTask> GetAllQuestTasks();
        //IWAQQuest GetQuestByTask(IWAQTask taskToSearch);
        List<IWAQQuest> Quests { get; }
    }
}

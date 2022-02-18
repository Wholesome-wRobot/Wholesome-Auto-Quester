using System.Collections.Generic;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;

namespace Wholesome_Auto_Quester.Bot.QuestManagement
{
    public interface IQuestManager : ICycleable
    {
        List<IWAQTask> GetAllValidQuestTasks();
        List<IWAQTask> GetAllInvalidQuestTasks();
        public void AddQuestToBlackList(int questId, string reason, bool triggerStatusUpdate = true);
        public void RemoveQuestFromBlackList(int questId, string reason, bool triggerStatusUpdate = true);
    }
}

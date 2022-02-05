using System.Threading;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskGatherGameObject : WAQBaseScannableTask
    {
        public WAQTaskGatherGameObject(ModelQuestTemplate questTemplate, ModelGameObjectTemplate goTemplate, ModelGameObject gameObject)
            : base(gameObject.GetSpawnPosition, gameObject.map, $"Gather {goTemplate.name} for {questTemplate.LogTitle}", goTemplate.entry, gameObject.spawntimesecs)
        {
            if (questTemplate.QuestAddon?.AllowableClasses > 0)
            {
                PriorityShift = 3;
            }
            if (questTemplate.TimeAllowed > 0)
            {
                PriorityShift = 7;
            }
        }

        public new void PutTaskOnTimeout(string reason, int timeInSeconds, bool exponentiallyLonger) 
            => base.PutTaskOnTimeout(reason, timeInSeconds > 0 ? timeInSeconds : DefaultTimeOutDuration, exponentiallyLonger);

        public override bool IsObjectValidForTask(WoWObject wowObject)
        {
            if (wowObject is WoWObject)
            {
                return true;
            }
            return false;
        }

        public override void PostInteraction(WoWObject wowObject)
        {
            Usefuls.WaitIsCastingAndLooting();
            Thread.Sleep(200);
        }

        public override string TrackerColor => IsTimedOut ? "Gray" : "Cyan";
        public override TaskInteraction InteractionType => TaskInteraction.Interact;
    }
}

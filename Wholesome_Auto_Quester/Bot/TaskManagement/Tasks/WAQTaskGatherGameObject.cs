using System.Threading;
using Wholesome_Auto_Quester.Bot.ContinentManagement;
using Wholesome_Auto_Quester.Database.DBC;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskGatherGameObject : WAQBaseScannableTask
    {
        private ModelGameObjectTemplate _gameObjectTemplate;
        private ModelQuestTemplate _questTemplate;

        public WAQTaskGatherGameObject(ModelQuestTemplate questTemplate, ModelGameObjectTemplate goTemplate, ModelGameObject gameObject, IContinentManager continentManager)
            : base(gameObject.GetSpawnPosition, gameObject.map, $"Gather {goTemplate.name} for {questTemplate.LogTitle}", goTemplate.entry,
                  gameObject.spawntimesecs, gameObject.guid, continentManager)
        {
            _gameObjectTemplate = goTemplate;
            _questTemplate = questTemplate;

            if (_questTemplate.QuestAddon != null
                && questTemplate.QuestAddon.AllowableClasses > 0)
            {
                PriorityShift = 2;
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
            Lua.LuaDoString(@$"
                    if GetClickFrame('StaticPopup1Button1'):IsVisible() then
                        StaticPopup1Button1:Click();
                    end
                ");
            if (!wowObject.IsValid)
            {
                PutTaskOnTimeout("Completed");
            }
        }

        protected override string ReputationMismatch => _questTemplate.ReputationMismatch;
        protected override bool HasEnoughSkillForTask => DBCLocks.IsLockValid(_gameObjectTemplate.type, _gameObjectTemplate.Data0);
        public override string TrackerColor => "Cyan";
        public override TaskInteraction InteractionType => TaskInteraction.Interact;
    }
}

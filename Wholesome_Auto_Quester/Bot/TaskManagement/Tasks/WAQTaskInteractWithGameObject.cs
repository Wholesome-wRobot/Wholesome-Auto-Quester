using System.Threading;
using Wholesome_Auto_Quester.Bot.ContinentManagement;
using Wholesome_Auto_Quester.Database.DBC;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskInteractWithGameObject : WAQBaseScannableTask
    {
        private ModelGameObjectTemplate _gameObjectTemplate;
        private ModelQuestTemplate _questTemplate;

        public WAQTaskInteractWithGameObject(ModelQuestTemplate questTemplate, ModelGameObjectTemplate goTemplate, ModelGameObject gameObject, IContinentManager continentManager)
            : base(gameObject.GetSpawnPosition, gameObject.map, $"Interact with {goTemplate.name} for {questTemplate.LogTitle}", goTemplate.entry,
                  gameObject.spawntimesecs, gameObject.guid, continentManager)
        {
            _gameObjectTemplate = goTemplate;
            _questTemplate = questTemplate;

            if (_questTemplate.QuestAddon != null
                && _questTemplate.QuestAddon.AllowableClasses > 0)
            {
                PriorityShift = 2;
            }
            if (_questTemplate.TimeAllowed > 0)
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
            Thread.Sleep(1000);
        }

        public override string TrackerColor => "Aqua";
        public override TaskInteraction InteractionType => TaskInteraction.Interact;
        protected override bool HasEnoughSkillForTask => DBCLocks.IsLockValid(_gameObjectTemplate.type, _gameObjectTemplate.Data0);
        protected override string ReputationMismatch => _questTemplate.ReputationMismatch;
    }
}

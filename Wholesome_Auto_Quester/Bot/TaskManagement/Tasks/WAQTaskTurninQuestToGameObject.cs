using System.Threading;
using Wholesome_Auto_Quester.Bot.ContinentManagement;
using Wholesome_Auto_Quester.Database.DBC;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using WholesomeToolbox;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskTurninQuestToGameObject : WAQBaseScannableTask
    {
        private ModelGameObjectTemplate _gameObjectTemplate;
        private ModelQuestTemplate _questTemplate;

        public WAQTaskTurninQuestToGameObject(ModelQuestTemplate questTemplate, ModelGameObjectTemplate goTemplate, ModelGameObject gameObject, IContinentManager continentManager)
            : base(gameObject.GetSpawnPosition, gameObject.map, $"Turn in {questTemplate.LogTitle} to {goTemplate.name}", goTemplate.entry,
                  gameObject.spawntimesecs, gameObject.guid, continentManager)
        {
            _gameObjectTemplate = goTemplate;
            _questTemplate = questTemplate;

            SpatialWeight = 2.0;
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
            Usefuls.WaitIsCastingAndLooting();
            WoWGameObject turnInTarget = (WoWGameObject)wowObject;
            if (!WTGossip.IsQuestGiverFrameActive)
            {
                MoveHelper.StopAllMove(true);
                Interact.InteractGameObject(turnInTarget.GetBaseAddress);
                Usefuls.WaitIsCasting();
                Thread.Sleep(500);
                if (!WTGossip.IsQuestGiverFrameActive)
                {
                    PutTaskOnTimeout($"Couldn't open quest frame", 15 * 60, true);
                }
            }
            else
            {
                if (!QuestLUAHelper.GossipTurnInQuest(_questTemplate.LogTitle, _questTemplate.Id))
                {
                    PutTaskOnTimeout("Failed turnin Gossip", 15 * 60, true);
                }
            }
        }

        public override string TrackerColor => "Lime";
        public override TaskInteraction InteractionType => TaskInteraction.Interact;
        protected override string ReputationMismatch => _questTemplate.ReputationMismatch;
        protected override bool HasEnoughSkillForTask => DBCLocks.IsLockValid(_gameObjectTemplate.type, _gameObjectTemplate.Data0);
    }
}

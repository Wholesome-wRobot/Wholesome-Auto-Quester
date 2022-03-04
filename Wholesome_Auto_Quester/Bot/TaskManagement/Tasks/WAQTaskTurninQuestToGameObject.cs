using System.Threading;
using Wholesome_Auto_Quester.Database.DBC;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskTurninQuestToGameObject : WAQBaseScannableTask
    {
        private ModelGameObjectTemplate _gameObjectTemplate;
        private ModelQuestTemplate _questTemplate;

        public WAQTaskTurninQuestToGameObject(ModelQuestTemplate questTemplate, ModelGameObjectTemplate goTemplate, ModelGameObject gameObject)
            : base(gameObject.GetSpawnPosition, gameObject.map, $"Turn in {questTemplate.LogTitle} to {goTemplate.name}", goTemplate.entry,
                  gameObject.spawntimesecs, gameObject.guid)
        {
            _gameObjectTemplate = goTemplate;
            _questTemplate = questTemplate;

            SpatialWeight = 2.0;
            if (_questTemplate.QuestAddon?.AllowableClasses > 0)
            {
                PriorityShift = 3;
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
            WoWGameObject turnInTarget = (WoWGameObject)wowObject;
            if (!ToolBox.IsNpcFrameActive())
            {
                MoveHelper.StopAllMove(true);
                Interact.InteractGameObject(turnInTarget.GetBaseAddress);
                Usefuls.WaitIsCasting();
                Thread.Sleep(500);
                if (!ToolBox.IsNpcFrameActive())
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
        protected override bool HasEnoughReputationForTask => _questTemplate.HasEnoughReputationForQuest;
        protected override bool HasEnoughSkillForTask => DBCLocks.IsLockValid(_gameObjectTemplate.type, _gameObjectTemplate.Data0);
    }
}

using System.Threading;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskPickupQuestFromCreature : WAQBaseScannableTask
    {
        private ModelQuestTemplate _questTemplate;

        public WAQTaskPickupQuestFromCreature(ModelQuestTemplate questTemplate, ModelCreatureTemplate creatureTemplate, ModelCreature creature)
            : base(creature.GetSpawnPosition, creature.map, $"Pick up {questTemplate.LogTitle} from {creatureTemplate.name}", creatureTemplate.entry,
                  creature.spawnTimeSecs, creature.guid)
        {
            _questTemplate = questTemplate;

            SpatialWeight = 0.25;
            if (_questTemplate.QuestAddon?.AllowableClasses > 0)
            {
                PriorityShift = 3;
            }
            // Through the Dark Portal
            if (_questTemplate.Id == 9407 || questTemplate.Id == 10119)
            {
                PriorityShift = 20;
            }
        }

        public new void PutTaskOnTimeout(string reason, int timeInSeconds, bool exponentiallyLonger)
            => base.PutTaskOnTimeout(reason, timeInSeconds > 0 ? timeInSeconds : DefaultTimeOutDuration, exponentiallyLonger);

        public override bool IsObjectValidForTask(WoWObject wowObject)
        {
            if (wowObject is WoWUnit unit)
            {
                return unit.IsAlive
                    || unit.Entry == 25328 // Shadowstalker Luther
                    || unit.Entry == 25984; // Crashed recon pilot
            }
            return false;
        }

        public override void PostInteraction(WoWObject wowObject)
        {
            WoWUnit pickUpTarget = (WoWUnit)wowObject;
            if (!ToolBox.IsNpcFrameActive())
            {
                MoveHelper.StopAllMove(true);
                Interact.InteractGameObject(pickUpTarget.GetBaseAddress);
                if (!ToolBox.IsNpcFrameActive())
                {
                    PutTaskOnTimeout($"Couldn't open quest frame", 15 * 60, true);
                }
            }
            else
            {
                if (!ToolBox.GossipPickUpQuest(_questTemplate.LogTitle, _questTemplate.Id))
                {
                    PutTaskOnTimeout("Failed pickup gossip", 15 * 60, true);
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        protected override bool HasEnoughReputationForTask => _questTemplate.HasEnoughReputationForQuest;
        public override string TrackerColor => "DodgerBlue";
        public override TaskInteraction InteractionType => TaskInteraction.Interact;
        protected override bool HasEnoughSkillForTask => true;
    }
}

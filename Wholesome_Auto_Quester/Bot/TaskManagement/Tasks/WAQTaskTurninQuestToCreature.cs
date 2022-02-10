using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskTurninQuestToCreature : WAQBaseScannableTask
    {
        private ModelQuestTemplate _questTemplate;

        public WAQTaskTurninQuestToCreature(ModelQuestTemplate questTemplate, ModelCreatureTemplate creatureTemplate, ModelCreature creature)
            : base(creature.GetSpawnPosition, creature.map, $"Turn in {questTemplate.LogTitle} to {creatureTemplate.name}", creatureTemplate.entry, 
                  creature.spawnTimeSecs, creature.guid)
        {
            SpatialWeight = 2.0;
            if (questTemplate.QuestAddon?.AllowableClasses > 0)
            {
                PriorityShift = 3;
            }
            if (questTemplate.TimeAllowed > 0)
            {
                PriorityShift = 7;
            }
            _questTemplate = questTemplate;
        }

        public new void PutTaskOnTimeout(string reason, int timeInSeconds, bool exponentiallyLonger)
            => base.PutTaskOnTimeout(reason, timeInSeconds > 0 ? timeInSeconds : DefaultTimeOutDuration, exponentiallyLonger);

        public override bool IsObjectValidForTask(WoWObject wowObject)
        {
            if (wowObject is WoWUnit unit)
            {
                return unit.IsAlive;
            }
            return false;
        }

        public override void PostInteraction(WoWObject wowObject)
        {
            WoWUnit turnInTarget = (WoWUnit)wowObject;
            if (!ToolBox.IsNpcFrameActive())
            {
                MoveHelper.StopAllMove(true);
                Interact.InteractGameObject(turnInTarget.GetBaseAddress);
                if (!ToolBox.IsNpcFrameActive())
                {
                    PutTaskOnTimeout($"Couldn't open quest frame", 15 * 60, true);
                }
            }
            else
            {
                if (!ToolBox.GossipTurnInQuest(_questTemplate.LogTitle, _questTemplate.Id))
                {
                    PutTaskOnTimeout("Failed turnin Gossip", 15 * 60, true);
                }
            }
        }

        public override string TrackerColor => IsTimedOut ? "Gray" : "Lime";
        public override TaskInteraction InteractionType => TaskInteraction.Interact;
    }
}

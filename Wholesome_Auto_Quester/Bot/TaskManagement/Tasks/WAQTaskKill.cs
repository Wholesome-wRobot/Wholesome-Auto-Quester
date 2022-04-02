using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskKill : WAQBaseScannableTask
    {
        private ModelQuestTemplate _questTemplate;

        public WAQTaskKill(ModelQuestTemplate questTemplate, ModelCreatureTemplate creatureTemplate, ModelCreature creature)
            : base(creature.GetSpawnPosition, creature.map, $"Kill {creatureTemplate.name} for {questTemplate.LogTitle}", creatureTemplate.entry,
                  creature.spawnTimeSecs, creature.guid)
        {
            _questTemplate = questTemplate;

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
            if (wowObject is WoWUnit unit)
            {
                return unit.IsAlive && unit.IsAttackable && !unit.IsTaggedByOther;
            }
            return false;
        }

        public override void PostInteraction(WoWObject wowObject)
        {
            WoWUnit killTarget = (WoWUnit)wowObject;
            if (killTarget.IsDead && killTarget.Position.DistanceTo(Location) < 30)
            {
                PutTaskOnTimeout("Completed");
                return;
            }
        }

        public override string TrackerColor => "OrangeRed";
        public override TaskInteraction InteractionType => TaskInteraction.Kill;
        protected override bool HasEnoughSkillForTask => true;
        protected override string ReputationMismatch => _questTemplate.ReputationMismatch;
    }
}

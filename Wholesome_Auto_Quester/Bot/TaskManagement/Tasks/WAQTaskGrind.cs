using Wholesome_Auto_Quester.Bot.ContinentManagement;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskGrind : WAQBaseScannableTask
    {
        public WAQTaskGrind(ModelCreatureTemplate creatureTemplate, ModelCreature creature, IContinentManager continentManager)
            : base(creature.GetSpawnPosition, creature.map, $"Grind {creatureTemplate.Name}", creatureTemplate.Entry, creature.spawnTimeSecs,
                  creature.guid, continentManager)
        { }

        public new void PutTaskOnTimeout(string reason, int timeInSeconds, bool exponentiallyLonger)
            => base.PutTaskOnTimeout(reason, timeInSeconds > 0 ? timeInSeconds : DefaultTimeOutDuration, exponentiallyLonger);

        public override bool IsObjectValidForTask(WoWObject wowObject)
        {
            if (wowObject is WoWUnit unit)
            {
                return unit.IsAlive && unit.IsAttackable;
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

        public override string TrackerColor => "PaleGreen";
        public override TaskInteraction InteractionType => TaskInteraction.Kill;
        protected override bool HasEnoughSkillForTask => true;
        protected override string ReputationMismatch => null;
    }
}

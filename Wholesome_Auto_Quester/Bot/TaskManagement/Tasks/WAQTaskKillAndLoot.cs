using Wholesome_Auto_Quester.Bot.ContinentManagement;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskKillAndLoot : WAQBaseScannableTask
    {
        private ModelQuestTemplate _questTemplate;

        public WAQTaskKillAndLoot(ModelQuestTemplate questTemplate, ModelCreatureTemplate creatureTemplate, ModelCreature creature, IContinentManager continentManager)
            : base(creature.GetSpawnPosition, creature.map, $"Kill and Loot {creatureTemplate.Name} for {questTemplate.LogTitle}", creatureTemplate.Entry,
                  creature.spawnTimeSecs, creature.guid, continentManager)
        {
            _questTemplate = questTemplate;

            if (_questTemplate.QuestAddon?.AllowableClasses > 0)
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
            if (wowObject is WoWUnit unit)
            {
                if (unit.IsAlive && unit.IsAttackable && !unit.IsTaggedByOther || unit.IsDead && unit.IsLootable)
                {
                    return true;
                }
            }
            return false;
        }

        public override void PostInteraction(WoWObject wowObject)
        {
            WoWUnit lootTarget = (WoWUnit)wowObject;
            if (lootTarget.IsDead && !lootTarget.IsLootable && lootTarget.Position.DistanceTo(Location) < 30)
            {
                PutTaskOnTimeout("Completed");
            }
        }

        public override string TrackerColor => "Orange";
        public override TaskInteraction InteractionType => TaskInteraction.KillAndLoot;
        protected override bool HasEnoughSkillForTask => true;
        protected override string ReputationMismatch => _questTemplate.ReputationMismatch;
    }
}

using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskKillAndLoot : WAQBaseScannableTask
    {
        public WAQTaskKillAndLoot(ModelQuestTemplate questTemplate, ModelCreatureTemplate creatureTemplate, ModelCreature creature)
            : base(creature.GetSpawnPosition, creature.map, $"Kill and Loot {creatureTemplate.name} for {questTemplate.LogTitle}", creatureTemplate.entry, 
                  creature.spawnTimeSecs, creature.guid)
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
            if (wowObject is WoWUnit unit)
            {
                if (unit.IsAlive && unit.IsAttackable || unit.IsDead && unit.IsLootable)
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

        public override string TrackerColor => IsTimedOut || IsRecordedAsUnreachable ? "Gray" : "Orange";
        public override TaskInteraction InteractionType => TaskInteraction.KillAndLoot;
    }
}

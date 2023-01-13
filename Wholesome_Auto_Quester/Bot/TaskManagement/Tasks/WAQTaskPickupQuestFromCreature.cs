using System.Threading;
using Wholesome_Auto_Quester.Bot.ContinentManagement;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using WholesomeToolbox;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskPickupQuestFromCreature : WAQBaseScannableTask
    {
        private ModelQuestTemplate _questTemplate;

        public WAQTaskPickupQuestFromCreature(ModelQuestTemplate questTemplate, ModelCreatureTemplate creatureTemplate, ModelCreature creature, IContinentManager continentManager)
            : base(creature.GetSpawnPosition, creature.map, $"Pick up {questTemplate.LogTitle} from {creatureTemplate.Name}", creatureTemplate.Entry,
                  creature.spawnTimeSecs, creature.guid, continentManager)
        {
            _questTemplate = questTemplate;

            SpatialWeight = 0.25;
            if (_questTemplate.QuestAddon?.AllowableClasses > 0)
            {
                PriorityShift = 2;
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
                    || unit.Entry == 25984 // Crashed recon pilot
                    || unit.Entry == 3891 // Teronis' Corpse
                    || unit.Entry == 24122 // Pulroy the Archaeologist
                    || unit.Entry == 24145 // Zedd
                    || unit.Entry == 26896 // Nozzlerust Supply Runner
                    || unit.Entry == 16852; // Sedai's Corpse
            }
            return false;
        }

        public override void PostInteraction(WoWObject wowObject)
        {
            WoWUnit pickUpTarget = (WoWUnit)wowObject;
            if (!WTGossip.IsQuestGiverFrameActive)
            {
                MoveHelper.StopAllMove(true);
                Interact.InteractGameObject(pickUpTarget.GetBaseAddress);
                if (!WTGossip.IsQuestGiverFrameActive)
                {
                    PutTaskOnTimeout($"Couldn't open quest frame", 15 * 60, true);
                }
            }
            else
            {
                if (!QuestLUAHelper.GossipPickupQuest(_questTemplate.LogTitle, _questTemplate.Id))
                {
                    PutTaskOnTimeout("Failed pickup gossip", 15 * 60, true);
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        protected override string ReputationMismatch => _questTemplate.ReputationMismatch;
        public override string TrackerColor => "DodgerBlue";
        public override TaskInteraction InteractionType => TaskInteraction.Interact;
        protected override bool HasEnoughSkillForTask => true;
    }
}

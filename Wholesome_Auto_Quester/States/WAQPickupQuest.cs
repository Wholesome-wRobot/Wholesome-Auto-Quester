using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using FlXProfiles;

namespace Wholesome_Auto_Quester.States
{
    class WAQPickupQuest : State
    {
        public override string DisplayName { get; set; } = "Pick up quest";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause 
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.PickupQuest)
                {
                    DisplayName = $"Pick up quest {WAQTasks.TaskInProgress.Quest.LogTitle} at {WAQTasks.TaskInProgress.Npc.Id} - {WAQTasks.TaskInProgress.Npc.Name}";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject npc = WAQTasks.TaskInProgressWoWObject;

            if (Quest.GetQuestCompleted(task.Quest.Id)) {
                task.Quest.MarkAsCompleted();
                return;
            }
            
            if (npc != null)
            {
                Logger.Log($"NPC found - Picking up quest {task.Quest.LogTitle} from {npc.Name}");
                
                do {
                    if (!MoveHelper.ToPositionAndInteractWithNpc(npc.Position, npc.Entry)) {
                        task.PutTaskOnTimeout();
                        return;
                    }
                    Thread.Sleep(500);
                } while (!ToolBox.IsNpcFrameActive());

                if (!ToolBox.GossipPickUpQuest(task.Quest.LogTitle)) {
                    Logger.LogError($"Failed PickUp Gossip for {task.Quest.LogTitle}. Timeout");
                    task.PutTaskOnTimeout();
                    return;
                }
                Thread.Sleep(1000);
            }
            else {
                Logger.Log($"Moving to QuestGiver for {task.Quest.LogTitle} (PickUp).");
                if (!MoveHelper.MoveToWait(task.Location, randomizeEnd: 8,
                    abortIf: () => ToolBox.MoveToHotSpotAbortCondition(task)) || task.GetDistance <= 13f) {
                    Logger.Log($"We are close to {ToolBox.GetTaskId(task)} position and no NPC for pick-up in sight. Time out");
                    task.PutTaskOnTimeout();
                }
            }
        }
    }
}

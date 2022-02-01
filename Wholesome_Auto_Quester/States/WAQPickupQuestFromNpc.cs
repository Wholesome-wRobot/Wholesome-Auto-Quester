/*using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQPickupQuestFromNpc : State
    {
        private IWowObjectScanner _scanner;
        public WAQPickupQuestFromNpc(IWowObjectScanner scanner, int priority)
        {
            _scanner = scanner;
            Priority = priority;
        }

        public override string DisplayName { get; set; } = "Pick up quest [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.PickupQuestFromCreature && WAQTasks.WoWObjectInProgress != null)
                {
                    DisplayName =
                        $"Pick up quest {WAQTasks.TaskInProgress.QuestTitle} from {WAQTasks.TaskInProgress.TargetName} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject npcObject = WAQTasks.WoWObjectInProgress;
            //WAQPath pathToTask = WAQTasks.PathToCurrentTask;

            if (ToolBox.ShouldStateBeInterrupted(task, npcObject, WoWObjectType.Unit))
                return;

            WoWUnit pickUpTarget = (WoWUnit)npcObject;

            if (ToolBox.HostilesAreAround(pickUpTarget))
                return;

            if (!pickUpTarget.InInteractDistance())
            {
                if (!MoveHelper.IsMovementThreadRunning || pickUpTarget.Position.DistanceTo(MoveHelper.CurrentTarget) > 10)
                    MoveHelper.StartGoToThread(pickUpTarget.PositionWithoutType, $"NPC found - Going to {pickUpTarget.Name} to pick up {task.QuestTitle}.");
                return;
            }

            if (!ToolBox.IsNpcFrameActive())
            {
                MoveHelper.StopAllMove(true);
                Interact.InteractGameObject(pickUpTarget.GetBaseAddress);
                Thread.Sleep(500);
                if (!ToolBox.IsNpcFrameActive())
                    task.PutTaskOnTimeout($"Couldn't open quest frame", true);
            }
            else
            {
                if (ToolBox.GossipPickUpQuest(task.QuestTitle, task.QuestId))
                    Main.RequestImmediateTaskReset = true;
                else
                    task.PutTaskOnTimeout("Failed PickUp Gossip", true);
            }
        }
    }
}*/
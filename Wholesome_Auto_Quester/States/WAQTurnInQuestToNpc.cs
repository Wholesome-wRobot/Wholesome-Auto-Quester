/*using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQTurnInQuestToNpc : State
    {
        private IWowObjectScanner _scanner;
        public WAQTurnInQuestToNpc(IWowObjectScanner scanner, int priority)
        {
            _scanner = scanner;
            Priority = priority;
        }

        public override string DisplayName { get; set; } = "Turn in quest [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.TurnInQuestToCreature && WAQTasks.WoWObjectInProgress != null)
                {
                    DisplayName =
                        $"Turning in {WAQTasks.TaskInProgress.QuestTitle} to NPC {WAQTasks.TaskInProgress.TargetName} [SmoothMove - Q]";
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

            WoWUnit turnInTarget = (WoWUnit)npcObject;

            if (ToolBox.HostilesAreAround(turnInTarget))
                return;

            if (!turnInTarget.InInteractDistance())
            {
                if (!MoveHelper.IsMovementThreadRunning || turnInTarget.Position.DistanceTo(MoveHelper.CurrentTarget) > 10)
                    MoveHelper.StartGoToThread(turnInTarget.PositionWithoutType, $"NPC found - Going to {turnInTarget.Name} to turn in {task.QuestTitle}.");
                return;
            }

            if (!ToolBox.IsNpcFrameActive())
            {
                MoveHelper.StopAllMove(true);
                Interact.InteractGameObject(turnInTarget.GetBaseAddress);
            }
            else
            {
                if (ToolBox.GossipTurnInQuest(task.QuestTitle, task.QuestId))
                    Main.RequestImmediateTaskReset = true;
                else
                    task.PutTaskOnTimeout("Failed PickUp Gossip", true);
            }
        }
    }
}*/
using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQTurnInQuestToGameObject : State
    {
        public override string DisplayName { get; set; } = "Turn in quest [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.TurnInQuestToGameObject && WAQTasks.WoWObjectInProgress != null)
                {
                    DisplayName =
                        $"Turning in {WAQTasks.TaskInProgress.QuestTitle} at game object {WAQTasks.TaskInProgress.TargetName} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject gameObject = WAQTasks.WoWObjectInProgress;
            //WAQPath pathToTask = WAQTasks.PathToCurrentTask;

            if (ToolBox.ShouldStateBeInterrupted(task, gameObject, WoWObjectType.GameObject))
                return;

            WoWGameObject turnInTarget = (WoWGameObject)gameObject;

            if (ToolBox.HostilesAreAround(turnInTarget))
                return;

            if (turnInTarget.GetDistance > 4)
            {
                MoveHelper.StartGoToThread(turnInTarget.Position, $"Game Object found - Going to {turnInTarget.Name} to turn in {task.QuestTitle}.");
                return;
            }

            if (!ToolBox.IsNpcFrameActive())
            {
                MoveHelper.StopAllMove(true);
                Interact.InteractGameObject(turnInTarget.GetBaseAddress);
                Usefuls.WaitIsCasting();
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
}
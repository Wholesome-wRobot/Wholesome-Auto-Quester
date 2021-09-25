using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

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
                    DisplayName = $"Pick up quest {WAQTasks.TaskInProgress.Quest.Title} at {WAQTasks.TaskInProgress.Npc.Name}";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WAQTask task = WAQTasks.TaskInProgress;

            if (WAQTasks.TaskInProgressWoWObject != null)
            {
                Logger.Log($"NPC found - Picking up quest at {WAQTasks.TaskInProgressWoWObject.Name}");
                GoToTask.ToPositionAndIntecractWithNpc(WAQTasks.TaskInProgressWoWObject.Position, WAQTasks.TaskInProgressWoWObject.Entry);
                Quest.AcceptQuest();
                Thread.Sleep(1000);
            }
            else
            {
                GoToTask.ToPosition(task.Location, 5f, conditionExit: e => WAQTasks.TaskInProgressWoWObject != null || task.GetDistance < 10f);

                if (task.GetDistance <= 11 && WAQTasks.TaskInProgressWoWObject == null)
                {
                    Logger.Log($"We are close to {ToolBox.GetTaskId(task)} position and no object in sight. Time out");
                    task.PutTaskOnTimeout(200);
                }
            }
        }
    }
}

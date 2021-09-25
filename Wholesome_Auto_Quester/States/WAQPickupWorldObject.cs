using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQPickupWorldObject : State
    {
        public override string DisplayName { get; set; } = "Pick up object";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause 
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.PickupObject)
                {
                    DisplayName = $"Gather {WAQTasks.TaskInProgress.GatherObject.Name} for {WAQTasks.TaskInProgress.Quest.Title}";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WAQTask task = WAQTasks.TaskInProgress;
            //Logger.Log($"******** RUNNING {task.TaskType} TASK {ToolBox.GetTaskId(task)}  ********");

            if (WAQTasks.TaskInProgressWoWObject != null && WAQTasks.TaskInProgressWoWObject.IsValid)
            {
                Logger.Log($"Object found - Gathering {WAQTasks.TaskInProgressWoWObject.Name}");
                GoToTask.ToPositionAndIntecractWithGameObject(WAQTasks.TaskInProgressWoWObject.Position, WAQTasks.TaskInProgressWoWObject.Entry);
                Usefuls.WaitIsCasting();
                Thread.Sleep(500);
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

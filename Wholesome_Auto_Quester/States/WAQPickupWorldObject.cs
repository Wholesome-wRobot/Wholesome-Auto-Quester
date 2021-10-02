using robotManager.FiniteStateMachine;
using System.Threading;
using FlXProfiles;
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

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.GatherObject)
                {
                    DisplayName = $"Gather {WAQTasks.TaskInProgress.GatherObject.Name} for {WAQTasks.TaskInProgress.Quest.LogTitle}";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject wOobject = WAQTasks.TaskInProgressWoWObject;
            //Logger.Log($"******** RUNNING {task.TaskType} TASK {ToolBox.GetTaskId(task)}  ********");

            if (wOobject != null && wOobject.IsValid)
            {
                Logger.Log($"Object found - Gathering {wOobject.Name}");
                MoveHelper.ToPositionAndInteractWithGameObject(wOobject.Position, wOobject.Entry);
                Usefuls.WaitIsCastingAndLooting();
                Thread.Sleep(100);
            }
            else
            {
                Logger.Log($"Moving to Hotspot for {task.Quest.LogTitle} (Gather).");
                if (!MoveHelper.MoveToWait(task.Location, randomizeEnd: 8,
                    abortIf: () => ToolBox.MoveToHotSpotAbortCondition(task)) || task.GetDistance <= 20f) {
                    Logger.Log($"No {task.GatherObject.Name} in sight. Time out for {task.GatherObject.SpawnTimeSecs}s");
                    task.PutTaskOnTimeout();
                }
                // if (GoToTask.ToPosition(task.Location, 10f, conditionExit: e => WAQTasks.TaskInProgressWoWObject != null))
                // {
                //     if (WAQTasks.TaskInProgressWoWObject == null && task.GetDistance <= 13f)
                //     {
                //         Logger.Log($"We are close to {ToolBox.GetTaskId(task)} position and no object to gather in sight. Time out");
                //         task.PutTaskOnTimeout();
                //     }
                // }
            }
        }
    }
}

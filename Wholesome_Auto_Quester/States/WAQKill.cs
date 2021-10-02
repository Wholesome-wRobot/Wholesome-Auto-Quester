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
    class WAQKill : State
    {
        public override string DisplayName { get; set; } = "Kill creature";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause 
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.Kill)
                {
                    DisplayName = $"Kill {WAQTasks.TaskInProgress.Npc.Name} for {WAQTasks.TaskInProgress.Quest.LogTitle}";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WAQTask task = WAQTasks.TaskInProgress;
            //Logger.Log($"******** RUNNING KILL TASK {ToolBox.GetTaskId(task)}  ********");

            if (WAQTasks.TaskInProgressWoWObject != null)
            {
                Logger.Log($"Unit found - Fighting {WAQTasks.TaskInProgressWoWObject.Name}");
                Fight.StartFight(WAQTasks.TaskInProgressWoWObject.Guid);
                Thread.Sleep(1000);
            }
            else
            {
                Logger.Log($"Moving to Hotspot for {task.Quest.LogTitle} (Kill).");
                if (!MoveHelper.MoveToWait(task.Location, randomizeEnd: 8,
                    abortIf: () => ToolBox.MoveToHotSpotAbortCondition(task)) || task.GetDistance <= 20f) {
                    Logger.Log($"No {task.Npc.Name} in sight. Time out for {task.Npc.SpawnTimeSecs}s");
                    task.PutTaskOnTimeout();
                }
                // if (GoToTask.ToPosition(task.Location, 10f, conditionExit: e => WAQTasks.TaskInProgressWoWObject != null))
                // {
                //     if (WAQTasks.TaskInProgressWoWObject == null && task.GetDistance <= 13f)
                //     {
                //         Logger.Log($"We are close to {ToolBox.GetTaskId(task)} position and no npc to kill in sight. Time out");
                //         task.PutTaskOnTimeout();
                //     }
                // }
            }
        }
    }
}

using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using FlXProfiles;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQKillAndLoot : State
    {
        public override string DisplayName { get; set; } = "Kill and Loot";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause 
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.KillAndLoot)
                {
                    DisplayName = $"Kill and Loot {WAQTasks.TaskInProgress.Npc.Name} for {WAQTasks.TaskInProgress.Quest.LogTitle}";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WoWObject npc = WAQTasks.TaskInProgressWoWObject;
            WAQTask task = WAQTasks.TaskInProgress;
            //Logger.Log($"******** [{task.POIEntry}] RUNNING {task.TaskType} TASK {ToolBox.GetTaskId(task)}  ********");

            if (npc != null)
            {
                Logger.Log($"Unit found - Fighting {WAQTasks.TaskInProgressWoWObject.Name}");
                Fight.StartFight(WAQTasks.TaskInProgressWoWObject.Guid);
                LootingTask.Pulse(new List<WoWUnit>() { (WoWUnit)WAQTasks.TaskInProgressWoWObject });
                Thread.Sleep(1000);
            }
            else
            {
                Logger.Log($"Moving to Hotspot for {task.Quest.LogTitle} (Kill&Loot).");
                if (!MoveHelper.MoveToWait(task.Location, randomizeEnd: 8,
                    abortIf: () => ToolBox.MoveToHotSpotAbortCondition(task)) || task.GetDistance <= 20f) {
                    Logger.Log($"No {task.Npc.Name} in sight.. Time out for {task.Npc.SpawnTimeSecs}s");
                    task.PutTaskOnTimeout();
                }
                // Logger.Log("START PATH");
                // if (GoToTask.ToPosition(task.Location, 10f, conditionExit: e => WAQTasks.TaskInProgressWoWObject != null))
                // {
                //     Logger.Log("INTERRUPT");
                //     if (WAQTasks.TaskInProgressWoWObject == null && task.GetDistance <= 13f)
                //     {
                //         Logger.Log($"We are close to {ToolBox.GetTaskId(task)} position and no npc to kill&loot in sight. Time out");
                //         task.PutTaskOnTimeout();
                //     }
                // }
                // Logger.Log("OUT");
            }
        }
    }
}

using robotManager.FiniteStateMachine;
using System.Threading;
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
                    DisplayName = $"Kill {WAQTasks.TaskInProgress.Npc.Name} for {WAQTasks.TaskInProgress.Quest.Title}";
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

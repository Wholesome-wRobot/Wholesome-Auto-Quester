using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.Collections.Generic;

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

            if (npc != null)
            {
                Logger.Log($"NPC found - Picking up quest from {npc.Name}");
                GoToTask.ToPositionAndIntecractWithNpc(npc.Position, npc.Entry);
                Thread.Sleep(1000);

                List<string> gossipOptions = ToolBox.GetAvailableQuestGossips();
                gossipOptions.ForEach(g => Logger.Log(g));

                if (!gossipOptions.Contains(task.Quest.LogTitle))
                {
                    Logger.Log($"Gossips don't contain {task.Quest.LogTitle}");
                    Quest.AcceptQuest();
                }
                else
                {
                    Logger.Log($"Selecting gossip {task.Quest.LogTitle}");
                    Quest.SelectGossipAvailableQuest(gossipOptions.IndexOf(task.Quest.LogTitle) + 1);
                    Thread.Sleep(1000);
                    Quest.AcceptQuest();
                }

                Quest.CloseQuestWindow();
                Thread.Sleep(1000);
            }
            else
            {
                if (GoToTask.ToPosition(task.Location, 10f, conditionExit: e => WAQTasks.TaskInProgressWoWObject != null))
                {
                    if (WAQTasks.TaskInProgressWoWObject == null && task.GetDistance <= 13f)
                    {
                        Logger.Log($"We are close to {ToolBox.GetTaskId(task)} position and no NPC for pickup in sight. Time out");
                        task.PutTaskOnTimeout();
                    }
                }

            }
        }
    }
}

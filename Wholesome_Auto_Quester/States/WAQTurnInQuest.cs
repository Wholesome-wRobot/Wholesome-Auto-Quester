using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using FlXProfiles;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using robotManager.Helpful;

namespace Wholesome_Auto_Quester.States
{
    class WAQTurnInQuest : State
    {
        public override string DisplayName { get; set; } = "Turn in quest";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause 
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.TurnInQuest)
                {
                    DisplayName = $"Turning in {WAQTasks.TaskInProgress.Quest.LogTitle} to {WAQTasks.TaskInProgress.Npc.Name}";
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
                Logger.Log($"NPC found - Turning in quest {task.Quest.LogTitle} to {npc.Name}");
                
                do {
                    if (!MoveHelper.ToPositionAndInteractWithNpc(npc.Position, npc.Entry)) {
                        task.PutTaskOnTimeout();
                        return;
                    }
                    Thread.Sleep(500);
                } while (!ToolBox.IsNpcFrameActive());
                
                ToolBox.GossipTurnInQuest(task.Quest.LogTitle);

                // GoToTask.ToPositionAndIntecractWithNpc(npc.Position, npc.Entry);
                // Thread.Sleep(1000);
                //
                // List<string> gossipOptions = ToolBox.GetActiveQuestGossips();
                //
                // if (!gossipOptions.Contains(task.Quest.LogTitle))
                // {
                //     Logger.Log($"Gossips don't contain {task.Quest.LogTitle}");
                //     for (int i = 1; i <= 5; i++)
                //     {
                //         GoToTask.ToPositionAndIntecractWithNpc(npc.Position, npc.Entry, gossipOptions: i);
                //         Quest.CompleteQuest();
                //         Quest.CompleteQuest(i);
                //         Quest.CloseQuestWindow();
                //     }
                // }
                // else
                // {
                //     Logger.Log($"Selecting gossip {task.Quest.LogTitle}");
                //     Quest.SelectGossipActiveQuest(gossipOptions.IndexOf(task.Quest.LogTitle) + 1);
                //     Thread.Sleep(1000);
                //     Quest.CompleteQuest();
                //     Thread.Sleep(1000);
                //
                //     if (Quest.HasQuest(task.Quest.Id))
                //     {
                //         Lua.LuaDoString($"GetQuestReward(1)");
                //         Thread.Sleep(500);
                //         Quest.CompleteQuest();
                //     }
                // }
                //
                // Quest.CloseQuestWindow();

                Thread.Sleep(1000);

                if (!Quest.HasQuest(task.Quest.Id))
                    task.Quest.MarkAsCompleted();
            }
            else {
                Logger.Log($"Moving to QuestGiver for {task.Quest.LogTitle} (TurnIn).");
                if (!MoveHelper.MoveToWait(task.Location, randomizeEnd: 8,
                    abortIf: () => ToolBox.MoveToHotSpotAbortCondition(task)) || task.GetDistance <= 20f) {
                    Logger.Log($"No {task.Npc.Name} in sight. Time out for {task.Npc.SpawnTimeSecs}s");
                    task.PutTaskOnTimeout();
                }
                // MoveHelper.StartGoToThread(task.Location, randomizeEnd: 3);
                // while (WAQTasks.TaskInProgressWoWObject == null) {
                //     Thread.Sleep(50);
                //     if (task.GetDistance <= 13f) {
                //         Logger.Log($"We are close to {ToolBox.GetTaskId(task)} position and no NPC for turnin in sight. Time out");
                //         task.PutTaskOnTimeout();
                //     }
                // }
                // MoveHelper.StopAllMove();
                // if (GoToTask.ToPosition(task.Location, 10f, conditionExit: e => WAQTasks.TaskInProgressWoWObject != null))
                // {
                //     if (WAQTasks.TaskInProgressWoWObject == null && task.GetDistance <= 13f)
                //     {
                //         
                //     }
                // }

            }
        }
    }
}

using robotManager.FiniteStateMachine;
using System.Threading;
using FlXProfiles;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States {
    class WAQTurnInQuestToNpc : State {
        public override string DisplayName { get; set; } = "Turn in quest [SmoothMove - Q]";

        public override bool NeedToRun {
            get {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.TurnInQuestToCreature) {
                    DisplayName =
                        $"Turning in {WAQTasks.TaskInProgress.QuestTitle} to NPC {WAQTasks.TaskInProgress.TargetName} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run() {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject npcObject = WAQTasks.TaskInProgressWoWObject;

            if (npcObject != null) {
                if (npcObject.Type != WoWObjectType.Unit) {
                    Logger.LogError($"Expected a WoWUnit for TurnIn Quest but got {npcObject.Type} instead.");
                    return;
                }

                ToolBox.CheckSpotAround(npcObject);

                var turnInTarget = (WoWUnit) npcObject;

                if (!turnInTarget.InInteractDistance()) 
                {
                    if (!MoveHelper.IsMovementThreadRunning
                       || MoveHelper.CurrentMovementTarget?.DistanceTo(turnInTarget.PositionWithoutType) > 4) 
                    {
                        MoveHelper.StartGoToThread(turnInTarget.PositionWithoutType, randomizeEnd: 3f);
                        Logger.Log($"NPC found - Going to {turnInTarget.Name} to turn in {task.QuestTitle}.");
                    }
                    return;
                }

                if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();

                if (!ToolBox.IsNpcFrameActive()) 
                {
                    Interact.InteractGameObject(turnInTarget.GetBaseAddress);
                } 
                else 
                {
                    if (ToolBox.GossipTurnInQuest(task.QuestTitle))
                    {
                        Thread.Sleep(1000);
                        if (!Quest.HasQuest(task.QuestId))
                            WAQTasks.MarQuestAsCompleted(task.QuestId);
                        WAQTasks.UpdateTasks();
                    }
                    else
                        task.PutTaskOnTimeout("Failed PickUp Gossip");
                } 
            } 
            else 
            {
                if (!MoveHelper.IsMovementThreadRunning || MoveHelper.CurrentMovementTarget?.DistanceTo(task.Location) > 15) 
                {
                    Logger.Log($"Moving to QuestEnder for {task.QuestTitle}.");
                    MoveHelper.StartGoToThread(task.Location, randomizeEnd: 8f);
                }
                if (task.GetDistance <= 15f)
                {
                    task.PutTaskOnTimeout("No NPC in sight for quest turn-in");
                    MoveHelper.StopAllMove();
                }
            }
        }
    }
}
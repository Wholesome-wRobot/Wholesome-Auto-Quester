﻿using robotManager.Helpful;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Database.Objectives;

namespace Wholesome_Auto_Quester.Bot.QuestManagement
{
    public interface IWAQQuest
    {
        ModelQuestTemplate QuestTemplate { get; }
        public QuestStatus Status { get; }
        bool IsQuestBlackListed { get; }
        string TrackerColor { get; }
        bool AreDbConditionsMet { get; }
        string GetConditionsText { get; }

        List<Objective> GetAllObjectives();
        List<IWAQTask> GetAllTasks();
        List<IWAQTask> GetAllValidTasks();
        List<IWAQTask> GetAllInvalidTasks();
        void ChangeStatusTo(QuestStatus newStatus);
        float GetClosestQuestGiverDistance(Vector3 myPosition);
        void CheckForFinishedObjectives();
    }
}

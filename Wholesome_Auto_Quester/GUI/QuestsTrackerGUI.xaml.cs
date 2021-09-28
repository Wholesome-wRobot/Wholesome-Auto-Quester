using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;

namespace Wholesome_Auto_Quester.GUI
{
    public partial class QuestsTrackerGUI
    {
        public QuestsTrackerGUI()
        {
            InitializeComponent();
            DiscordLink.RequestNavigate += (sender, e) =>
            {
                System.Diagnostics.Process.Start(e.Uri.ToString());
            };
        }

        public void ShowWindow()
        {
            Dispatcher.BeginInvoke((Action)(() => {
                Show();
            }));
        }

        public void HideWindow()
        {
            Dispatcher.BeginInvoke((Action)(() => {
                Hide();
            }));
        }

        public void UpdateQuestsList(List<ModelQuest> quests)
        {
            Dispatcher.BeginInvoke((Action)(() => {
                sourceQuestsList.ItemsSource = null;
                sourceQuestsList.ItemsSource = quests.OrderBy(q => q.Status);
                questTitleTop.Text = $"Quests ({quests.Count})";
            }));
        }

        public void UpdateTasksList(List<WAQTask> tasks)
        {
            Dispatcher.BeginInvoke((Action)(() => {
                sourceTasksList.ItemsSource = null;
                sourceTasksList.ItemsSource = tasks;
                tasksTitleTop.Text = $"Current Tasks ({tasks.Count})";
            }));
        }

        public void SelectQuest(object sender, RoutedEventArgs e)
        {
            ModelQuest selected = (ModelQuest)sourceQuestsList.SelectedItem;
            if (selected != null)
            {
                questTitle.Text = $"{selected.LogTitle}";
                questId.Text = $"Entry: {selected.Id}";
                questLevel.Text = $"Level: {selected.QuestLevel}";

                string qg = "";
                selected.QuestGivers.ForEach(q => qg += q.Id + " ");
                questGivers.Text = $"Quest Givers: {qg}";

                string qt = "";
                selected.QuestTurners.ForEach(q => qt += q.Id + " ");
                questTurners.Text = $"Quest Turners: {qt}";

                questStatus.Text = $"Status: {selected.Status}";

                string qp = "";
                selected.PreviousQuestsIds.ForEach(q => qp += q + " ");
                questPrevious.Text = $"Previous quests: {qp}";

                string qn = "";
                selected.NextQuestsIds.ForEach(q => qn += q + " ");
                questNext.Text = $"Next quests: {qn}";

                string gatherObjectsString = "Gather: ";
                foreach (GatherObjectObjective objGroup in selected.GatherObjectsObjectives)
                    gatherObjectsString += $"\n    [{objGroup.objectiveIndex}] {objGroup.amount} x {objGroup.GetName} ({objGroup.worldObjects.Count} found)";
                questGatherObjects.Text = gatherObjectsString;

                string creaturesToKillString = "Kill: ";
                foreach (CreaturesToKillObjective creaGroup in selected.CreaturesToKillObjectives)
                    creaturesToKillString += $"\n    [{creaGroup.objectiveIndex}] {creaGroup.amount} x {creaGroup.GetName} ({creaGroup.worldCreatures.Count} found)";
                questKillCreatures.Text = creaturesToKillString;

                string creaturesToLootString = "Kill & Loot: ";
                foreach (CreatureToLootObjective creaGroup in selected.CreaturesToLootObjectives)
                    creaturesToLootString += $"\n    [{creaGroup.objectiveIndex}] {creaGroup.amount} x {creaGroup.itemName} on {creaGroup.GetName} ({creaGroup.worldCreatures.Count} found)";
                questLootCreatures.Text = creaturesToLootString;
            }
        }
    }
}

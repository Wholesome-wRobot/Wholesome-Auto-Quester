using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Database.Objectives;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.GUI {
    public partial class QuestsTrackerGUI {
        public QuestsTrackerGUI() {
            InitializeComponent();
            DiscordLink.RequestNavigate += (sender, e) => { System.Diagnostics.Process.Start(e.Uri.ToString()); };
            detailsPanel.Visibility = Visibility.Hidden;
        }

        public void AddToBLClicked(object sender, RoutedEventArgs e) {
            if (sourceQuestsList.SelectedItem != null) {
                ModelQuestTemplate selected = (ModelQuestTemplate) sourceQuestsList.SelectedItem;
                WholesomeAQSettings.AddQuestToBlackList(selected.Id);
                UpdateQuestsList();
            }
        }

        public void RmvFromBLClicked(object sender, RoutedEventArgs e) {
            if (sourceQuestsList.SelectedItem != null) {
                ModelQuestTemplate selected = (ModelQuestTemplate) sourceQuestsList.SelectedItem;
                WholesomeAQSettings.RemoveQuestFromBlackList(selected.Id);
                UpdateQuestsList();
            }
        }

        public void ShowWindow() {
            Dispatcher.BeginInvoke((Action) (() => { Show(); }));
        }

        public void HideWindow() {
            Dispatcher.BeginInvoke((Action) (() => { Hide(); }));
        }

        public void UpdateQuestsList() {
            Dispatcher.BeginInvoke((Action) (() => {                
                object selectedQuest = sourceQuestsList.SelectedItem;
                
                sourceQuestsList.ItemsSource = null;
                Vector3 myPos = ObjectManager.Me.PositionWithoutType;
                
                List<ModelQuestTemplate> items = WAQTasks.Quests
                    .OrderBy(q => q.Status)                    
                    .ThenBy(q => {
                        if (q.CreatureQuestGivers.Count <= 0) return float.MaxValue;
                        return q.GetClosestQuestGiverDistance(myPos);
                    }).ToList();
                sourceQuestsList.ItemsSource = items;
                
                if (selectedQuest != null && sourceQuestsList.Items?.Count > 0 && sourceQuestsList.Items.Contains(selectedQuest))
                    sourceQuestsList.SelectedItem = selectedQuest;
                else
                    detailsPanel.Visibility = Visibility.Hidden;
                
                questTitleTop.Text = $"Quests ({WAQTasks.Quests.Count})";
            }));
        }

        public void UpdateTasksList() {            
            Dispatcher.BeginInvoke((Action) (() => {
                object selectedTask = sourceTasksList.SelectedItem;
                sourceTasksList.ItemsSource = null;
                sourceTasksList.ItemsSource = WAQTasks.TasksPile;
                if (selectedTask != null && sourceTasksList.Items.Contains(selectedTask))
                    sourceTasksList.SelectedItem = selectedTask;
                tasksTitleTop.Text = $"Current Tasks ({WAQTasks.TasksPile.Count})";
            }));
        }

        public void SelectQuest(object sender, RoutedEventArgs e) {
            ModelQuestTemplate selected = (ModelQuestTemplate) sourceQuestsList.SelectedItem;
            if (selected != null) {
                questTitle.Text = $"{selected.LogTitle}";
                questId.Text = $"Entry: {selected.Id}";
                questLevel.Text = $"Level: {selected.QuestLevel}";

                Vector3 myPos = ObjectManager.Me.PositionWithoutType;

                // quest givers
                if (selected.CreatureQuestGivers.Count > 0)
                {
                    string qg = "";
                    selected.CreatureQuestGivers.ForEach(q => qg += $"{q.entry}");
                    selected.GameObjectQuestGivers.ForEach(q => qg += $"{q.entry}");
                    questGivers.Text = $"Quest Givers: {qg}";
                    questGivers.Visibility = Visibility.Visible;
                }
                else
                    questGivers.Visibility = Visibility.Collapsed;

                // quest turners
                if (selected.CreatureQuestTurners.Count > 0)
                {
                    string qt = "";
                    selected.CreatureQuestTurners.ForEach(q => qt += $"{q.entry}");
                    selected.GameObjectQuestTurners.ForEach(q => qt += $"{q.entry}");
                    questTurners.Text = $"Quest Turners: {qt}";
                    questTurners.Visibility = Visibility.Visible;
                }
                else
                    questTurners.Visibility = Visibility.Collapsed;

                // status
                questStatus.Text = $"Status: {selected.Status}";

                // previous quests
                if (selected.PreviousQuestsIds.Count > 0)
                {
                    string qp = "";
                    selected.PreviousQuestsIds.ForEach(q => qp += q + " ");
                    questPrevious.Text = $"Previous quests: {qp}";
                    questPrevious.Visibility = Visibility.Visible;
                }
                else
                    questPrevious.Visibility = Visibility.Collapsed;

                // next quests
                if (selected.NextQuestsIds.Count > 0)
                {
                    string qn = "";
                    selected.NextQuestsIds.ForEach(q => qn += q + " ");
                    questNext.Text = $"Next quests: {qn}";
                    questNext.Visibility = Visibility.Visible;
                }
                else
                    questNext.Visibility = Visibility.Collapsed;

                // exploration objectives
                if (selected.ExplorationObjectives.Count > 0)
                {
                    explorations.Visibility = Visibility.Visible;
                    explorations.Children.RemoveRange(1, explorations.Children.Count - 1);
                    foreach (ExplorationObjective obje in selected.ExplorationObjectives)
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, selected.Id))
                            explorations.Children.Add(CreateListTextBlock(
                                $"[{selected.ExplorationObjectives.IndexOf(obje) + 1}] {obje.Area.GetPosition}"));
                }
                else
                    explorations.Visibility = Visibility.Collapsed;

                // gather objectives
                if (selected.GatherObjectives.Count > 0)
                {
                    questGatherObjects.Visibility = Visibility.Visible;
                    questGatherObjects.Children.RemoveRange(1, questGatherObjects.Children.Count - 1);
                    foreach (GatherObjective obje in selected.GatherObjectives)
                    {
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, selected.Id))
                            questGatherObjects.Children.Add(CreateListTextBlock(
                                $"[{obje.ObjectiveIndex}] {obje.ObjGOTemplates[0].GameObjectName} ({obje.GetAllGameObjects().Count} found)"));
                    }
                }
                else
                    questGatherObjects.Visibility = Visibility.Collapsed;

                // kill objectives
                if (selected.KillObjectives.Count > 0)
                {
                    questKillCreatures.Visibility = Visibility.Visible;
                    questKillCreatures.Children.RemoveRange(1, questKillCreatures.Children.Count - 1);
                    foreach (KillObjective obje in selected.KillObjectives)
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, selected.Id))
                            questKillCreatures.Children.Add(CreateListTextBlock(
                                $"[{obje.ObjectiveIndex}] {obje.CreatureName} ({obje.Creatures.Count} found)"));
                }
                else
                    questKillCreatures.Visibility = Visibility.Collapsed;

                // kill&loot objectives
                if (selected.KillLootObjectives.Count > 0)
                {
                    questLootCreatures.Visibility = Visibility.Visible;
                    questLootCreatures.Children.RemoveRange(1, questLootCreatures.Children.Count - 1);
                    foreach (KillLootObjective obje in selected.KillLootObjectives)
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, selected.Id))
                            questLootCreatures.Children.Add(CreateListTextBlock(
                                $"[{obje.ObjectiveIndex}] {obje.CreatureName} ({obje.Creatures.Count} found)"));
                }
                else
                    questLootCreatures.Visibility = Visibility.Collapsed;

                // Interact objectives
                if (selected.InteractObjectives.Count > 0)
                {
                    interactObjectives.Visibility = Visibility.Visible;
                    interactObjectives.Children.RemoveRange(1, interactObjectives.Children.Count - 1);
                    foreach (InteractObjective obje in selected.InteractObjectives)
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, selected.Id))
                            interactObjectives.Children.Add(CreateListTextBlock(
                                $"[{obje.ObjectiveIndex}] {obje.GameObjectName} ({obje.GameObjects.Count} found)"));
                }
                else
                    interactObjectives.Visibility = Visibility.Collapsed;

                // Prerequisite gathers
                if (selected.PrerequisiteGatherObjectives.Count > 0)
                {
                    prerequisiteGathers.Visibility = Visibility.Visible;
                    prerequisiteGathers.Children.RemoveRange(1, prerequisiteGathers.Children.Count - 1);
                    foreach (GatherObjective obje in selected.PrerequisiteGatherObjectives)
                        prerequisiteGathers.Children.Add(CreateListTextBlock(
                                $"[{obje.ObjectiveIndex}] {obje.ObjGOTemplates[0].GameObjectName} ({obje.GetAllGameObjects().Count} found)"));
                }
                else
                    prerequisiteGathers.Visibility = Visibility.Collapsed;

                // Prerequisite loots
                if (selected.PrerequisiteLootObjectives.Count > 0)
                {
                    prerequisiteLoots.Visibility = Visibility.Visible;
                    prerequisiteLoots.Children.RemoveRange(1, prerequisiteLoots.Children.Count - 1);
                    foreach (KillLootObjective obje in selected.PrerequisiteLootObjectives)
                        prerequisiteLoots.Children.Add(CreateListTextBlock(
                            $"[{obje.ObjectiveIndex}] {obje.Amount} x {obje.ItemName} ({obje.Creatures.Count} found)"));
                }
                else
                    prerequisiteLoots.Visibility = Visibility.Collapsed;

                // objectives
                if (selected.GetAllObjectives().Count > 0)
                {
                    objectiveStack.Visibility = Visibility.Visible;
                    objectiveStack.Children.RemoveRange(1, objectiveStack.Children.Count - 1);
                    List<ObjectiveDisplay> objDisplays = new List<ObjectiveDisplay>();
                    selected.GetAllObjectives().ForEach(obj =>
                    {
                        if (!objDisplays.Exists(o => o.Name == obj.ObjectiveName))
                            objDisplays.Add(new ObjectiveDisplay(obj.ObjectiveIndex, obj.ObjectiveName, obj.Amount));
                    });
                    objDisplays.ForEach(od =>
                    {
                        TextBlock objTextBlock = CreateListTextBlock($"[{od.Index}] {od.Name} (x{od.Amount})");
                        if (ToolBox.IsObjectiveCompleted(od.Index, selected.Id))
                        {
                            objTextBlock.Foreground = Brushes.LightGray;
                            objTextBlock.TextDecorations = TextDecorations.Strikethrough;
                        }
                        objectiveStack.Children.Add(objTextBlock);
                    });
                }
                else
                    objectiveStack.Visibility = Visibility.Collapsed;

                if (WholesomeAQSettings.CurrentSetting.BlacklistesQuests.Contains(selected.Id)) {
                    ButtonAddToBl.IsEnabled = false;
                    ButtonRmvFromBl.IsEnabled = true;
                } else {
                    ButtonAddToBl.IsEnabled = true;
                    ButtonRmvFromBl.IsEnabled = false;
                }

                detailsPanel.Visibility = Visibility.Visible;
            }
        }

        private TextBlock CreateListTextBlock(string text)
        {
            TextBlock objTextBlock = new TextBlock();
            objTextBlock.Text = text;
            objTextBlock.Margin = new Thickness(15, 0, 0, 0);
            objTextBlock.TextWrapping = TextWrapping.Wrap;
            return objTextBlock;
        }

        private struct ObjectiveDisplay
        {
            public int Index;
            public string Name;
            public int Amount;
            public ObjectiveDisplay(int index, string name, int amount)
            {
                Index = index;
                Name = name;
                Amount = amount;
            }
        }
    }
}

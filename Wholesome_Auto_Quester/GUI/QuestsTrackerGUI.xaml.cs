using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wholesome_Auto_Quester.Bot.QuestManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Database.Objectives;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.GUI
{
    public partial class QuestsTrackerGUI
    {
        private IQuestManager _questManager;

        public QuestsTrackerGUI()
        {
            InitializeComponent();
            DiscordLink.RequestNavigate += (sender, e) => { System.Diagnostics.Process.Start(e.Uri.ToString()); };
            Title = $"Wholesome quest tracker ({Main.ProductVersion})";
            detailsPanel.Visibility = Visibility.Hidden;
        }

        public void Initialize(IQuestManager questManager)
        {
            _questManager = questManager;
        }

        private void AddToBLClicked(object sender, RoutedEventArgs e)
        {
            if (sourceQuestsList.SelectedItem != null)
            {
                IWAQQuest selected = (IWAQQuest)sourceQuestsList.SelectedItem;
                _questManager?.AddQuestToBlackList(selected.QuestTemplate.Id, "Blacklisted by user");
            }
        }

        private void RmvFromBLClicked(object sender, RoutedEventArgs e)
        {
            if (sourceQuestsList.SelectedItem != null)
            {
                IWAQQuest selected = (IWAQQuest)sourceQuestsList.SelectedItem;
                _questManager?.RemoveQuestFromBlackList(selected.QuestTemplate.Id, "Removed by user");
            }
        }

        public void ShowWindow()
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                Show();
                if (WholesomeAQSettings.CurrentSetting.QuestTrackerPositionLeft != 0)
                    Left = WholesomeAQSettings.CurrentSetting.QuestTrackerPositionLeft;
                if (WholesomeAQSettings.CurrentSetting.QuestTrackerPositionTop != 0)
                    Top = WholesomeAQSettings.CurrentSetting.QuestTrackerPositionTop;
            }));
        }

        public void HideWindow()
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                Hide();
                WholesomeAQSettings.CurrentSetting.QuestTrackerPositionLeft = Left;
                WholesomeAQSettings.CurrentSetting.QuestTrackerPositionTop = Top;
                WholesomeAQSettings.CurrentSetting.Save();
            }));
        }

        public void UpdateQuestsList(List<IWAQQuest> questList)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                object selectedQuest = sourceQuestsList.SelectedItem;

                sourceQuestsList.ItemsSource = null;
                Vector3 myPos = ObjectManager.Me.PositionWithoutType;

                List<IWAQQuest> items = questList
                    .OrderBy(quest => quest.Status)
                    .ThenBy(quest =>
                    {
                        if (quest.QuestTemplate.CreatureQuestGivers.Count <= 0) return float.MaxValue;
                        return quest.GetClosestQuestGiverDistance(myPos);
                    }).ToList();
                sourceQuestsList.ItemsSource = items;

                if (selectedQuest != null && sourceQuestsList.Items?.Count > 0 && sourceQuestsList.Items.Contains(selectedQuest))
                    sourceQuestsList.SelectedItem = selectedQuest;
                else
                    detailsPanel.Visibility = Visibility.Hidden;

                questTitleTop.Text = $"Quests ({questList.Count})";
            }));
        }

        public void UpdateScanReg(Dictionary<int, List<IWAQTask>> scannerRegistry)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                sourceScanReg.ItemsSource = null;
                List<GUIScanEntry> scanEntries = new List<GUIScanEntry>();
                foreach (KeyValuePair<int, List<IWAQTask>> entry in scannerRegistry)
                {
                    foreach (IWAQTask task in entry.Value)
                    {
                        if (!scanEntries.Exists(entry => entry.TaskName == task.TaskName))
                        {
                            scanEntries.Add(new GUIScanEntry(entry.Key, task));
                        }
                        else
                        {
                            scanEntries.Find(entry => entry.TaskName == task.TaskName).AddOne(task);
                        }
                    }
                }
                sourceScanReg.ItemsSource = scanEntries;
            }));
        }

        public void UpdateTasksList(List<GUITask> guiTaskPile)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                sourceTasksList.ItemsSource = null;
                string countText = "";
                int limit = 200;
                if (guiTaskPile.Count >= limit)
                {
                    sourceTasksList.ItemsSource = guiTaskPile.GetRange(0, limit);
                    countText = $"{limit}+";
                }
                else
                {
                    sourceTasksList.ItemsSource = guiTaskPile;
                    countText = $"{guiTaskPile.Count}";
                }
                tasksTitleTop.Text = $"Current Tasks ({countText})";
            }));
        }

        private void SelectQuest(object sender, RoutedEventArgs e)
        {
            IWAQQuest selected = (IWAQQuest)sourceQuestsList.SelectedItem;
            if (selected != null)
            {
                questTitle.Text = $"{selected.QuestTemplate.LogTitle}";
                questId.Text = $"Entry: {selected.QuestTemplate.Id}";
                questLevel.Text = $"Level: {selected.QuestTemplate.QuestLevel}";

                Vector3 myPos = ObjectManager.Me.PositionWithoutType;

                // blacklisted
                if (selected.IsQuestBlackListed)
                {
                    blacklisted.Text = @$"Blacklisted : { WholesomeAQSettings.CurrentSetting.BlackListedQuests
                        .Find(blq => blq.Id == selected.QuestTemplate.Id).Reason }";
                    blacklisted.Visibility = Visibility.Visible;
                }
                else
                {
                    blacklisted.Visibility = Visibility.Collapsed;
                }

                // quest givers
                if (selected.QuestTemplate.CreatureQuestGivers.Count > 0)
                {
                    string qg = "";
                    selected.QuestTemplate.CreatureQuestGivers.ForEach(q => qg += $"{q.entry}");
                    selected.QuestTemplate.GameObjectQuestGivers.ForEach(q => qg += $"{q.entry}");
                    questGivers.Text = $"Quest Givers: {qg}";
                    questGivers.Visibility = Visibility.Visible;
                }
                else
                    questGivers.Visibility = Visibility.Collapsed;

                // quest turners
                if (selected.QuestTemplate.CreatureQuestTurners.Count > 0)
                {
                    string qt = "";
                    selected.QuestTemplate.CreatureQuestTurners.ForEach(q => qt += $"{q.entry}");
                    selected.QuestTemplate.GameObjectQuestTurners.ForEach(q => qt += $"{q.entry}");
                    questTurners.Text = $"Quest Turners: {qt}";
                    questTurners.Visibility = Visibility.Visible;
                }
                else
                    questTurners.Visibility = Visibility.Collapsed;

                // status
                questStatus.Text = $"Status: {selected.Status}";

                // previous quests
                if (selected.QuestTemplate.PreviousQuestsIds.Count > 0)
                {
                    string qp = "";
                    selected.QuestTemplate.PreviousQuestsIds.ForEach(q => qp += q + " ");
                    questPrevious.Text = $"Previous quests: {qp}";
                    questPrevious.Visibility = Visibility.Visible;
                }
                else
                    questPrevious.Visibility = Visibility.Collapsed;

                // next quests
                if (selected.QuestTemplate.NextQuestsIds.Count > 0)
                {
                    string qn = "";
                    selected.QuestTemplate.NextQuestsIds.ForEach(q => qn += q + " ");
                    questNext.Text = $"Next quests: {qn}";
                    questNext.Visibility = Visibility.Visible;
                }
                else
                    questNext.Visibility = Visibility.Collapsed;

                // exploration objectives
                if (selected.QuestTemplate.ExplorationObjectives.Count > 0)
                {
                    explorations.Visibility = Visibility.Visible;
                    explorations.Children.RemoveRange(1, explorations.Children.Count - 1);
                    foreach (ExplorationObjective obje in selected.QuestTemplate.ExplorationObjectives)
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, selected.QuestTemplate.Id))
                            explorations.Children.Add(CreateListTextBlock(
                                $"[{selected.QuestTemplate.ExplorationObjectives.IndexOf(obje) + 1}] {obje.Area.GetPosition}"));
                }
                else
                    explorations.Visibility = Visibility.Collapsed;

                // gather objectives
                if (selected.QuestTemplate.GatherObjectives.Count > 0)
                {
                    questGatherObjects.Visibility = Visibility.Visible;
                    questGatherObjects.Children.RemoveRange(1, questGatherObjects.Children.Count - 1);
                    foreach (GatherObjective obje in selected.QuestTemplate.GatherObjectives)
                    {
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, selected.QuestTemplate.Id))
                            questGatherObjects.Children.Add(CreateListTextBlock(
                                $"[{obje.ObjectiveIndex}] {obje.GameObjectLootTemplate.GameObjectTemplates[0].name} ({obje.GetNbGameObjects()} found)"));
                    }
                }
                else
                    questGatherObjects.Visibility = Visibility.Collapsed;

                // kill objectives
                if (selected.QuestTemplate.KillObjectives.Count > 0)
                {
                    questKillCreatures.Visibility = Visibility.Visible;
                    questKillCreatures.Children.RemoveRange(1, questKillCreatures.Children.Count - 1);
                    foreach (KillObjective obje in selected.QuestTemplate.KillObjectives)
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, selected.QuestTemplate.Id))
                            questKillCreatures.Children.Add(CreateListTextBlock(
                                $"[{obje.ObjectiveIndex}] {obje.CreatureTemplate.name} ({obje.CreatureTemplate.Creatures.Count} found)"));
                }
                else
                    questKillCreatures.Visibility = Visibility.Collapsed;

                // kill&loot objectives
                if (selected.QuestTemplate.KillLootObjectives.Count > 0)
                {
                    questLootCreatures.Visibility = Visibility.Visible;
                    questLootCreatures.Children.RemoveRange(1, questLootCreatures.Children.Count - 1);
                    foreach (KillLootObjective obje in selected.QuestTemplate.KillLootObjectives)
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, selected.QuestTemplate.Id))
                            questLootCreatures.Children.Add(CreateListTextBlock(
                                $"[{obje.ObjectiveIndex}] {obje.CreatureLootTemplate.CreatureTemplate.name} ({obje.CreatureLootTemplate.CreatureTemplate.Creatures.Count} found)"));
                }
                else
                    questLootCreatures.Visibility = Visibility.Collapsed;

                // Interact objectives
                if (selected.QuestTemplate.InteractObjectives.Count > 0)
                {
                    interactObjectives.Visibility = Visibility.Visible;
                    interactObjectives.Children.RemoveRange(1, interactObjectives.Children.Count - 1);
                    foreach (InteractObjective obje in selected.QuestTemplate.InteractObjectives)
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, selected.QuestTemplate.Id))
                            interactObjectives.Children.Add(CreateListTextBlock(
                                $"[{obje.ObjectiveIndex}] {obje.GameObjectTemplate.name} ({obje.GameObjectTemplate.GameObjects.Count} found)"));
                }
                else
                    interactObjectives.Visibility = Visibility.Collapsed;

                // Prerequisite gathers
                if (selected.QuestTemplate.PrerequisiteGatherObjectives.Count > 0)
                {
                    prerequisiteGathers.Visibility = Visibility.Visible;
                    prerequisiteGathers.Children.RemoveRange(1, prerequisiteGathers.Children.Count - 1);
                    foreach (GatherObjective obje in selected.QuestTemplate.PrerequisiteGatherObjectives)
                        prerequisiteGathers.Children.Add(CreateListTextBlock(
                                $"[{obje.ObjectiveIndex}] {obje.GameObjectLootTemplate.GameObjectTemplates[0].name} ({obje.GetNbGameObjects()} found)"));
                }
                else
                    prerequisiteGathers.Visibility = Visibility.Collapsed;

                // Prerequisite loots
                if (selected.QuestTemplate.PrerequisiteLootObjectives.Count > 0)
                {
                    prerequisiteLoots.Visibility = Visibility.Visible;
                    prerequisiteLoots.Children.RemoveRange(1, prerequisiteLoots.Children.Count - 1);
                    foreach (KillLootObjective obje in selected.QuestTemplate.PrerequisiteLootObjectives)
                        prerequisiteLoots.Children.Add(CreateListTextBlock(
                            $"[{obje.ObjectiveIndex}] {obje.Amount} x {obje.ItemTemplate.Name} ({obje.CreatureLootTemplate.CreatureTemplate.Creatures.Count} found)"));
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
                        if (ToolBox.IsObjectiveCompleted(od.Index, selected.QuestTemplate.Id))
                        {
                            objTextBlock.Foreground = Brushes.LightGray;
                            objTextBlock.TextDecorations = TextDecorations.Strikethrough;
                        }
                        objectiveStack.Children.Add(objTextBlock);
                    });
                }
                else
                    objectiveStack.Visibility = Visibility.Collapsed;

                if (selected.IsQuestBlackListed)
                {
                    ButtonAddToBl.IsEnabled = false;
                    ButtonRmvFromBl.IsEnabled = true;
                }
                else
                {
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

public class GUIScanEntry
{
    public int ObjectId { get; }
    public int Amount { get; private set; } = 0;
    public int AmountTimedOut { get; private set; } = 0;
    public string TaskName { get; }
    public string TrackerColor { get; }

    public GUIScanEntry(int objectId, IWAQTask task)
    {
        ObjectId = objectId;
        TaskName = task.TaskName;
        AddOne(task);
    }

    public void AddOne(IWAQTask task)
    {
        if (task.IsTimedOut)
        {
            AmountTimedOut++;
        }
        else
        {
            Amount++;
        }
    }
}

public class GUITask
{
    public int Priority { get; }
    public string TaskName { get; }
    public string TrackerColor { get; }
    public IWAQTask Task { get; }

    public GUITask(int priority, IWAQTask task)
    {
        Task = task;
        Priority = priority;
        TaskName = task.TaskName;
        TrackerColor = task.TrackerColor;
    }
}
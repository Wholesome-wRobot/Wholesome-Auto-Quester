using System;
using System.Threading;
using wManager.Wow.Helpers;
using Timer = robotManager.Helpful.Timer;

namespace Wholesome_Auto_Quester.Helpers
{
    class QuestLUAHelper
    {
        private static bool QuestFrameCompleteQuestButtonIsVisible => Lua.LuaDoString<bool>("return GetClickFrame('QuestFrameCompleteQuestButton'):IsVisible() == 1");
        private static bool QuestFrameAcceptButtonIsVisible => Lua.LuaDoString<bool>("return GetClickFrame('QuestFrameAcceptButton'):IsVisible() == 1");
        private static bool QuestFrameCompleteButtonIsVisible => Lua.LuaDoString<bool>("return GetClickFrame('QuestFrameCompleteButton'):IsVisible() == 1");
        private static bool QuestFrameIsVisible => Lua.LuaDoString<bool>("return GetClickFrame('QuestFrame'):IsVisible() == 1");
        private static bool GossipFrameIsVisible => Lua.LuaDoString<bool>("return GetClickFrame('GossipFrame'):IsVisible() == 1");
        private static bool QuestFrameCloseButtonIsVisible => Lua.LuaDoString<bool>("return GetClickFrame('QuestFrameCloseButton'):IsVisible() == 1");
        private static bool HasQuestItems => Lua.LuaDoString<bool>("return GetNumQuestItems() > 0;");
        private static bool HasQuestChoices => Lua.LuaDoString<bool>("return GetNumQuestChoices() > 0;");
        private static Func<int, bool> IsQuestCompleted => (questId) => ToolBox.IsQuestCompleted(questId);
        private static Func<int, bool> HasQuest => (questId) => Quest.HasQuest(questId);

        private static void CloseAllGossips()
        {
            Lua.LuaDoString("GetClickFrame('QuestFrameCloseButton'):Click()");
            Lua.LuaDoString("GetClickFrame('GossipFrameCloseButton'):Click()");
        }


        public static bool GossipTurnInQuest(string questName, int questId)
        {
            if (WaitFor(
                QuestFrameAcceptButtonIsVisible
                || QuestFrameCompleteButtonIsVisible
                || QuestFrameCompleteQuestButtonIsVisible
                || QuestFrameIsVisible
                || GossipFrameIsVisible))
            {
                // CHECK FRAME
                if (QuestFrameIsVisible)
                {
                    Lua.LuaDoString($@"
            	        for i=1, 32 do
            		        local button = GetClickFrame('QuestTitleButton' .. i);
            		        if button:IsVisible() ~= 1 then break; end
            		        local text = button:GetText();
            		        text = strsub(text, 11, strlen(text)-2);
            		        if text == '{questName.EscapeLuaString()}' then
                                button:Click();
                            end
            	        end                       
                    ");
                }
                else if (GossipFrameIsVisible)
                {
                    bool questFound = Lua.LuaDoString<bool>($@"
            	        local activeQuests = {{ GetGossipActiveQuests() }};
            	        for j=1, GetNumGossipActiveQuests(), 1 do
            		        local i = j*4-3;
            		        if activeQuests[i] == '{questName.EscapeLuaString()}' then
            			        if activeQuests[i+3] ~= 1 then return false; end
            			        SelectGossipActiveQuest(j);
            			        return true;
            		        end
            	        end
                        return false;
                    ");

                    if (!questFound)
                    {
                        Logger.LogError($"The quest {questName} has been found but is not completed yet.");
                        return false;
                    }
                }

                // TURN IN
                if (WaitFor(QuestFrameCompleteButtonIsVisible, 2000))
                {
                    Lua.LuaDoString("GetClickFrame('QuestFrameCompleteButton'):Click()");
                }

                if (WaitFor(HasQuestItems, 1000))
                {
                    Lua.LuaDoString("CompleteQuest();");
                }

                if (WaitFor(HasQuestChoices, 1000))
                {
                    Quest.CompleteQuest();
                }

                if (WaitFor(QuestFrameIsVisible, 1000))
                {
                    Lua.LuaDoString($"GetQuestReward({(HasQuestChoices ? "1" : "nil")});");
                }

                if (WaitFor(HasQuest, questId, expectedReult: false))
                {
                    Logger.Log($"Turned in quest {questName}.");
                    CloseAllGossips();
                    ToolBox.SaveQuestAsCompleted(questId);
                    return true;
                }

                Logger.LogError($"Failed to turn in quest {questName}.");
                CloseAllGossips();
                return false;
            }
            else
            {
                if (WaitFor(IsQuestCompleted, questId))
                {
                    Logger.LogError($"The quest {questName} has not been found to hand in.");
                    return false;
                }
                return true;
            }
        }

        public static bool GossipPickupQuest(string questName, int questId)
        {
            if (WaitFor(
                QuestFrameCompleteQuestButtonIsVisible
                || QuestFrameAcceptButtonIsVisible
                || QuestFrameCompleteButtonIsVisible
                || QuestFrameIsVisible
                || GossipFrameIsVisible))
            {
                // CHECK FRAME
                if (QuestFrameCompleteQuestButtonIsVisible)
                {
                    Logger.Log($"The quest {questName} is an autocomplete.");
                    Thread.Sleep(200);
                    Quest.CompleteQuest();
                    return WaitFor(HasQuest, questId, expectedReult: false);
                }
                else if (QuestFrameAcceptButtonIsVisible || QuestFrameCompleteButtonIsVisible)
                {
                    //Logger.LogError($"QuestFrameAcceptButtonIsVisible || QuestFrameCompleteButtonIsVisible");
                }
                else if (QuestFrameIsVisible)
                {
                    Lua.LuaDoString($@"
            	        for i=1, 32 do
            		        local button = GetClickFrame('QuestTitleButton' .. i);
            		        if button:IsVisible() ~= 1 then break; end
            		        local text = button:GetText();
            		        text = strsub(text, 11, strlen(text)-2);
            		        if text == '{questName.EscapeLuaString()}' then
                                button:Click();
                            end
            	        end                            
                    ");
                }
                else if (GossipFrameIsVisible)
                {
                    bool isautocomplete = Lua.LuaDoString<bool>($@"
            	        local availableQuests = {{ GetGossipAvailableQuests() }};
            	        for j=1, GetNumGossipAvailableQuests(), 1 do
            		        local i = j*5-4;
            		        if availableQuests[i] == '{questName.EscapeLuaString()}' then
            			        SelectGossipAvailableQuest(j);
                                return false;
            		        end
            	        end
                        local autoCompleteQuests = {{ GetGossipActiveQuests() }}
            	        for j=1, GetNumGossipActiveQuests(), 1 do
            		        local i = j*4-3;
            		        if autoCompleteQuests[i] == '{questName.EscapeLuaString()}' then
            			        SelectGossipActiveQuest(j);
                                return true;
            		        end
            	        end                         
                    ");

                    // Autocomplete
                    if (isautocomplete)
                    {
                        return GossipTurnInQuest(questName, questId);
                    }
                }

                // ACCEPT QUEST
                if (WaitFor(QuestFrameCompleteButtonIsVisible, 1000))
                {
                    Logger.LogError($"The quest {questName} seems to be a trade quest.");
                    Lua.LuaDoString(@"
                        local closeButton = GetClickFrame('QuestFrameCloseButton');
                        if closeButton:IsVisible() then
                	        closeButton:Click();
                        end
                    ");
                    return false;
                }

                if (WaitFor(QuestFrameIsVisible, 1000))
                {
                    Lua.LuaDoString("AcceptQuest();");
                }

                if (WaitFor(HasQuest, questId))
                {
                    Logger.Log($"Picked up quest {questName}.");
                    CloseAllGossips();
                    return true;
                }

                Logger.LogError($"Failed to pick up quest {questName}.");
                CloseAllGossips();
                return false;
            }
            else
            {
                Logger.LogError($"No Gossip or Quest window was open to pick up {questName}");
                return false;
            }
        }

        private static bool WaitFor(Func<int, bool> condition, int questId, int maxWait = 5000, bool expectedReult = true)
        {
            Timer timer = new Timer(maxWait);
            while (!timer.IsReady && condition(questId) != expectedReult)
            {
                Thread.Sleep(200);
            }
            return condition(questId) == expectedReult;
        }
        
        private static bool WaitFor(bool condition, int maxWait = 5000, bool expectedReult = true)
        {
            Timer timer = new Timer(maxWait);
            while (!timer.IsReady && condition != expectedReult)
            {
                Thread.Sleep(200);
            }
            return condition == expectedReult;
        }
    }
}

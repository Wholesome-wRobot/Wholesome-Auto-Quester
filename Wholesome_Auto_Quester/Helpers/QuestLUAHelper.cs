﻿using System;
using System.Threading;
using wManager.Wow.Helpers;
using WholesomeToolbox;
using Timer = robotManager.Helpful.Timer;



namespace Wholesome_Auto_Quester.Helpers
{
    class QuestLUAHelper
    {
        private static Func<int, bool> IsQuestCompleted => (questId) => ToolBox.IsQuestCompleted(questId);
        private static Func<int, bool> HasQuest => (questId) => Quest.HasQuest(questId);

        
            

        private static void CloseAllGossips()
        {
            Lua.LuaDoString("GetClickFrame('QuestFrameCloseButton'):Click()");
            Lua.LuaDoString("GetClickFrame('GossipFrameCloseButton'):Click()");
        }


       
      

        public static bool GossipTurnInQuest(string questName, int questId)
        {            
            Logger.Log("当前交的任务名称english：" + questName + "  任务ID： " + questId.ToString());
            string questNameCN = QuestCNHelper.GetQuestCNName(questId);
            Logger.Log("当前交的任务名称中文：" + questName + "  任务ID： " + questId.ToString());



            if (WaitFor(
                WTGossip.QuestFrameAcceptButtonIsVisible
                || WTGossip.QuestFrameCompleteButtonIsVisible
                || WTGossip.QuestFrameCompleteQuestButtonIsVisible
                || WTGossip.QuestFrameIsVisible
                || WTGossip.GossipFrameIsVisible))
            {
                // CHECK FRAME
                if (WTGossip.QuestFrameIsVisible)
                {
                    Lua.LuaDoString($@"
            	        for i=1, 32 do
            		        local button = GetClickFrame('QuestTitleButton' .. i);
            		        if button:IsVisible() ~= 1 then break; end
            		        local text = button:GetText();
            		        text = strsub(text, 11, strlen(text)-2);
            		        if text == '{questNameCN}' or text == '{questName.EscapeLuaString()}' or text == '{questName.EscapeLuaString()} (low level)' || or  text == '{questNameCN} (low level)' then
                                button:Click();
                            end
            	        end                       
                    ");
                }
                else if (WTGossip.GossipFrameIsVisible)
                {
                    bool questFound = Lua.LuaDoString<bool>($@"
            	        local activeQuests = {{ GetGossipActiveQuests() }};
            	        for j=1, GetNumGossipActiveQuests(), 1 do
            		        local i = j*4-3;
            		        if activeQuests[i] == '{questNameCN}' or activeQuests[i] == '{questName.EscapeLuaString()}' or activeQuests[i] == '{questName.EscapeLuaString()} (low level)'or activeQuests[i] == '{questNameCN} (low level)' then
            			        if activeQuests[i+3] ~= 1 then return false; end
            			        SelectGossipActiveQuest(j);
            			        return true;
            		        end
            	        end
                        return false;
                    ");

                    if (!questFound)
                    {
                        Logger.LogError($"The quest {questName} {questNameCN} has been found but is not completed yet.");
                        return false;
                    }
                }

                // TURN IN
                if (WaitFor(WTGossip.QuestFrameCompleteButtonIsVisible, 2000))
                {
                    Lua.LuaDoString("GetClickFrame('QuestFrameCompleteButton'):Click()");
                }

                if (WaitFor(WTGossip.HasQuestItems, 1000))
                {
                    Lua.LuaDoString("CompleteQuest();");
                }

                if (WaitFor(WTGossip.HasQuestChoices, 1000))
                {
                    Quest.CompleteQuest();
                }

                if (WaitFor(WTGossip.QuestFrameIsVisible, 1000))
                {
                    Lua.LuaDoString($"GetQuestReward({(WTGossip.HasQuestChoices ? "1" : "nil")});");
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
            // 当前任务列表>=25 就不能再接任务了


            Logger.Log("当前接的任务名称english：" + questName + "  任务ID： " + questId.ToString());
            string questNameCN = QuestCNHelper.GetQuestCNName(questId);
            Logger.Log("中文名应该为" + questNameCN);
            Logger.Log("当前接的任务名称中文：" + questName + "  任务ID： "+ questId.ToString());
            if (WaitFor(
                WTGossip.QuestFrameCompleteQuestButtonIsVisible
                || WTGossip.QuestFrameAcceptButtonIsVisible
                || WTGossip.QuestFrameCompleteButtonIsVisible
                || WTGossip.QuestFrameIsVisible
                || WTGossip.GossipFrameIsVisible))
            {
                // CHECK FRAME
                if (WTGossip.QuestFrameCompleteQuestButtonIsVisible)
                {
                    Logger.Log($"The quest {questName} {questNameCN} is an autocomplete.");
                    Thread.Sleep(200);
                    Quest.CompleteQuest();
                    return WaitFor(HasQuest, questId, expectedReult: false);
                }
                else if (WTGossip.QuestFrameAcceptButtonIsVisible || WTGossip.QuestFrameCompleteButtonIsVisible)
                {
                    Logger.Log($"WTGossip.QuestFrameAcceptButtonIsVisible ||WTGossip.QuestFrameCompleteButtonIsVisible ");
                    //Logger.LogError($"QuestFrameAcceptButtonIsVisible || QuestFrameCompleteButtonIsVisible");
                }
                else if (WTGossip.QuestFrameIsVisible)
                {
                    Logger.Log($"WTGossip.QuestFrameIsVisible当前任务名是" + questName.EscapeLuaString() + questNameCN);
                    Lua.LuaDoString($@"
            	        for i=1, 32 do
            		        local button = GetClickFrame('QuestTitleButton' .. i);
            		        if button:IsVisible() ~= 1 then break; end
            		        local text = button:GetText();
            		        text = strsub(text, 11, strlen(text)-2);
            		        if text == '{questName.EscapeLuaString()}' or text == '{questNameCN}' then
                                button:Click();
                            end
            	        end
                    ");
                }
                else if (WTGossip.GossipFrameIsVisible)
                // 这里有很多任务需点击任务再接收
                // 但是服务器任务是中文就会匹配失败
                {
                    Logger.Log($"WTGossip.GossipFrameIsVisible当前任务名是" + questName.EscapeLuaString());
                    Logger.Log($"WTGossip.GossipFrameIsVisible当前任务名是" + questNameCN.EscapeLuaString());

                    bool isautocomplete = Lua.LuaDoString<bool>($@"
            	        local availableQuests = {{ GetGossipAvailableQuests() }};
            	        for j=1, GetNumGossipAvailableQuests(), 1 do
            		        local i = j*5-4;
            		        if availableQuests[i] == '{questName.EscapeLuaString()}' or availableQuests[i] == '{questNameCN}' then
                            --if availableQuests[i] ~= '' then
            			        SelectGossipAvailableQuest(j);
                                return false;
            		        end
            	        end
                        local autoCompleteQuests = {{ GetGossipActiveQuests() }}
            	        for j=1, GetNumGossipActiveQuests(), 1 do
            		        local i = j*4-3;
            		        if autoCompleteQuests[i] == '{questName.EscapeLuaString()}' or autoCompleteQuests[i] == '{questNameCN}' then
                            
            			        SelectGossipActiveQuest(j);
                                return true;
            		        end
                        end
                    ");

                    bool completebuttonVisible = Lua.LuaDoString<bool>($@"return QuestFrameCompleteQuestButton:IsVisible() == 1;");

                    // Autocomplete
                    if (isautocomplete || completebuttonVisible)
                    {
                        return GossipTurnInQuest(questName, questId);
                    }
                }

                // ACCEPT QUEST
                if (WaitFor(WTGossip.QuestFrameCompleteButtonIsVisible, 1000))
                {
                    Logger.LogError($"The quest {questName} {questNameCN} seems to be a trade quest.");
                    Lua.LuaDoString(@"
                        local closeButton = GetClickFrame('QuestFrameCloseButton');
                        if closeButton:IsVisible() then
                	        closeButton:Click();
                        end
                    ");
                    return false;
                }

                if (WaitFor(WTGossip.QuestFrameIsVisible, 1000))
                {
                    Lua.LuaDoString("AcceptQuest();");
                }

                if (WaitFor(HasQuest, questId))
                {
                    Logger.Log($"Picked up quest {questName} {questNameCN}.");
                    CloseAllGossips();
                    return true;
                }

                Logger.LogError($"Failed to pick up quest {questName} {questNameCN}.");
                CloseAllGossips();
                return false;
            }
            else
            {
                Logger.LogError($"No Gossip or Quest window was open to pick up {questName} {questNameCN}");
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

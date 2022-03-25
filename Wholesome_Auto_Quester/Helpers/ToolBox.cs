using Newtonsoft.Json;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Database.Models;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.PathFinder;
using Math = System.Math;

namespace Wholesome_Auto_Quester.Helpers
{
    public static class ToolBox
    {
        private static Dictionary<int, bool[]> _objectiveCompletionDict = new Dictionary<int, bool[]>();

        public static void CheckIfZReachable(Vector3 checkPosition)
        {
            if (checkPosition.DistanceTo2D(ObjectManager.Me.Position) <= 3 && GetZDistance(checkPosition) > 3)
            {
                BlacklistHelper.AddZone(checkPosition, 2, $"Unreachable Z");
            }
        }

        public static bool IHaveLineOfSightOn(WoWObject wowObject)
        {
            Vector3 myPos = ObjectManager.Me.Position;
            Vector3 objectPos = (wowObject is WoWUnit) ? new Vector3(wowObject.Position.X, wowObject.Position.Y, wowObject.Position.Z + 2) : wowObject.Position;
            return !TraceLine.TraceLineGo(new Vector3(myPos.X, myPos.Y, myPos.Z + 2),
                objectPos,
                CGWorldFrameHitFlags.HitTestSpellLoS | CGWorldFrameHitFlags.HitTestLOS);
        }

        public static string GetMinimapZoneText()
        {
            return Lua.LuaDoString<string>("return GetMinimapZoneText();");
        }

        public static int GetAvergaeDurability()
        {
            return Lua.LuaDoString<int>($@"
                local avrgDurability = 0;
                local nbItems = 0;
                for i=1,20 do
                    local durability, max = GetInventoryItemDurability(i);
                    if durability ~= nil and max ~= nil then
                        avrgDurability = avrgDurability + durability;
                        nbItems = nbItems + 1;
                    end
                end

                if nbItems > 0 then
                    return avrgDurability / nbItems;
                else 
                    return 100;
                end
            ");
        }

        // Get item Cooldown (must pass item string as arg)
        public static int GetItemCooldown(int itemId)
        {
            List<WoWItem> _bagItems = Bag.GetBagItem();
            foreach (WoWItem item in _bagItems)
                if (itemId == item.Entry)
                    return Lua.LuaDoString<int>("local startTime, duration, enable = GetItemCooldown(" + itemId + "); " +
                        "return duration - (GetTime() - startTime)");

            Logger.Log("Couldn't find item " + itemId);
            return 0;
        }

        public static float GetZDistance(Vector3 checkPosition)
        {
            Vector3 myPos = ObjectManager.Me.Position;
            if (checkPosition.Z > myPos.Z) return checkPosition.Z - myPos.Z;
            else return myPos.Z - checkPosition.Z;
        }

        public static bool HostilesAreAround(WoWObject POI, IWAQTask task)
        {
            if (POI.Entry == 1776 // swamp of sorrows Magtoor
                || POI.Entry == 19256 // Sergeant SHatterskull
                || POI.Entry == 27266 // Sergeant Thurkin
                || POI.Entry == 191519 // Sparksocket's Tools
                || POI.Entry == 9536 // Maxwort Uberglint
                || POI.Entry == 10267 // Tinkee Steamboil
                || POI.Entry == 9563 // Ragged John
                || POI.Entry == 9836 // Mathredis Firestar
                || POI.Entry == 19442) // Kruush
            {
                return false;
            }

            WoWUnit poiUnit = POI is WoWUnit ? (WoWUnit)POI : null;
            WoWUnit me = ObjectManager.Me;
            Vector3 myPosition = me.Position;
            Vector3 poiPosition = POI.Position;

            if (me.IsMounted && (me.InCombatFlagOnly || POI.Position.DistanceTo(myPosition) < 60 && poiUnit?.Reaction == Reaction.Hostile))
            {
                MountTask.DismountMount(false, false);
            }

            if (ObjectManager.GetNumberAttackPlayer() > 0)
            {
                return true;
            }

            List<WoWUnit> hostiles = GetListObjManagerHostiles();
            Dictionary<WoWUnit, float> hostileUnits = new Dictionary<WoWUnit, float>();
            float myDistanceToPOI = me.Position.DistanceTo(poiPosition);
            foreach (WoWUnit unit in hostiles)
            {
                if (unit.Guid != POI.Guid && unit.Position.DistanceTo(poiPosition) < myDistanceToPOI)
                {
                    WAQPath pathFromPoi = GetWAQPath(unit.Position, poiPosition);
                    if (pathFromPoi.Distance < myDistanceToPOI)
                    {
                        hostileUnits.Add(unit, pathFromPoi.Distance);
                    }
                }
            }

            bool poiIsUnit = poiUnit != null;
            int maxCount = poiIsUnit ? 2 : 3;

            // Detect high concentration of enemies
            if (WholesomeAQSettings.CurrentSetting.BlacklistDangerousZones)
            {
                if (hostileUnits.Where(u => u.Key.Level >= me.Level && poiPosition.DistanceTo(u.Key.Position) < 18).Count() >= maxCount
                    || hostileUnits.Where(u => u.Key.Level >= me.Level - 2 && poiPosition.DistanceTo(u.Key.Position) < 18).Count() >= maxCount + 1)
                {
                    if (Fight.InFight) Fight.StopFight();
                    MoveHelper.StopAllMove(true);
                    BlacklistHelper.AddNPC(POI.Guid, "Surrounded by hostiles");
                    BlacklistHelper.AddZone(poiPosition, 20, "Surrounded by hostiles");
                    task.PutTaskOnTimeout($"{POI.Name} is surrounded by hostiles", 60 * 30);
                    return true;
                }
            }
            return false;
        }

        public static T TakeHighest<T>(this IEnumerable<T> list, Func<T, int> takeValue, out int amount)
        {
            var highest = int.MinValue;
            T curHighestElement = default;

            foreach (T element in list)
            {
                int curValue = takeValue(element);
                if (curValue > highest)
                {
                    highest = curValue;
                    curHighestElement = element;
                }
            }

            amount = highest;
            return curHighestElement;
        }

        public static T TakeHighest<T>(this IEnumerable<T> list, Func<T, int> takeValue) =>
            list.TakeHighest(takeValue, out _);

        public static float PathLength(List<Vector3> path)
        {
            var length = 0f;
            for (var i = 0; i < path.Count - 1; i++) length += path[i].DistanceTo(path[i + 1]);

            return length;
        }

        public static WoWUnit FindClosestUnitByEntry(int entry)
        {
            Vector3 myPos = ObjectManager.Me.PositionWithoutType;
            return ObjectManager.GetWoWUnitByEntry(entry)
                .TakeHighest(unit => (int)-unit.PositionWithoutType.DistanceTo(myPos));
        }

        public static WoWGameObject FindClosestGameObjectByEntry(int entry)
        {
            Vector3 myPos = ObjectManager.Me.PositionWithoutType;
            return ObjectManager.GetWoWGameObjectByEntry(entry)
                .TakeHighest(gameObject => (int)-gameObject.Position.DistanceTo(myPos));
        }

        public static string EscapeLuaString(this string str) => str.Replace("\\", "\\\\").Replace("'", "\\'");

        public static bool IsNpcFrameActive() =>
            Lua.LuaDoString<bool>(
                "return GetClickFrame('GossipFrame'):IsVisible() == 1 or GetClickFrame('QuestFrame'):IsVisible() == 1;");
        /*
        public static bool GossipTurnInQuest(string questName, int questId)
        {
            return QuestLUAHelper.GossipTurnInQuest(questName, questId);
            
            // Select quest
            var exitCodeOpen = Lua.LuaDoString<int>($@"
            if GetClickFrame('QuestFrameAcceptButton'):IsVisible() == 1
                or GetClickFrame('QuestFrameCompleteButton'):IsVisible() == 1
                or GetClickFrame('QuestFrameCompleteQuestButton'):IsVisible() == 1 then return 0; end
            if GetClickFrame('QuestFrame'):IsVisible() == 1 then
            	for i=1, 32 do
            		local button = GetClickFrame('QuestTitleButton' .. i);
            		if button:IsVisible() ~= 1 then break; end
            		local text = button:GetText();
            		text = strsub(text, 11, strlen(text)-2);
            		if text == '{questName.EscapeLuaString()}' then
                        button:Click();
                        return 0;
                    end
            	end
            elseif GetClickFrame('GossipFrame'):IsVisible() == 1 then
            	local activeQuests = {{ GetGossipActiveQuests() }};
            	for j=1, GetNumGossipActiveQuests(), 1 do
            		local i = j*4-3;
            		if activeQuests[i] == '{questName.EscapeLuaString()}' then
            			if activeQuests[i+3] ~= 1 then return 3; end
            			SelectGossipActiveQuest(j);
            			return 0;
            		end
            	end
            else
            	return 1;
            end
            return 2;");

            switch (exitCodeOpen)
            {
                case 1:
                    Logger.LogError($"No Gossip window was open to hand in {questName}");
                    return false;
                case 2:
                    if (!IsQuestCompleted(questId))
                    {
                        Logger.LogError($"The quest {questName} has not been found to hand in.");
                        return false;
                    }
                    return true;
                case 3:
                    Logger.LogError($"The quest {questName} has been found but is not completed yet.");
                    return false;
            }

            Thread.Sleep(200);

            var requiresItems = Lua.LuaDoString<bool>("return GetNumQuestItems() > 0;");
            if (requiresItems)
            {
                Lua.LuaDoString("CompleteQuest();");
                Thread.Sleep(200);
            }

            // Get reward
            var hasQuestReward = Lua.LuaDoString<bool>("return GetNumQuestChoices() > 0;");
            if (hasQuestReward)
            {
                // Ugly workaround to trigger the selection event
                Logger.LogDebug("Letting InventoryManager select quest reward.");
                Quest.CompleteQuest();
            }

            Thread.Sleep(200);

            // Finish it
            Lua.LuaDoString(
                $"if GetClickFrame('QuestFrame'):IsVisible() then GetQuestReward({(hasQuestReward ? "1" : "nil")}); end");
            Thread.Sleep(200);
            Lua.LuaDoString(@"
            local closeButton = GetClickFrame('QuestFrameCloseButton');
            if closeButton:IsVisible() then
            	closeButton:Click();
            end");

            robotManager.Helpful.Timer timer = new robotManager.Helpful.Timer(3000);
            while (Quest.HasQuest(questId) && !timer.IsReady)
            {
                Thread.Sleep(500);
            }
            if (timer.IsReady)
            {
                return false;
            }

            Logger.Log($"Turned in quest {questName}.");
            SaveQuestAsCompleted(questId);

            return true;
        }
        */
        /*
        public static bool GossipPickUpQuest(string questName, int questId)
        {
            return QuestLUAHelper.GossipPickupQuest(questName, questId);
            
            // Select quest
            var exitCodeOpen = Lua.LuaDoString<int>($@"
            if GetClickFrame('QuestFrameCompleteQuestButton'):IsVisible() == 1 then return 3; end
            if GetClickFrame('QuestFrameAcceptButton'):IsVisible() == 1 or GetClickFrame('QuestFrameCompleteButton'):IsVisible() == 1 then return 0; end
            if GetClickFrame('QuestFrame'):IsVisible() == 1 then
            	for i=1, 32 do
            		local button = GetClickFrame('QuestTitleButton' .. i);
            		if button:IsVisible() ~= 1 then break; end
            		local text = button:GetText();
            		text = strsub(text, 11, strlen(text)-2);
            		if text == '{questName.EscapeLuaString()}' then
                        button:Click();
                        return 0;
                    end
            	end
            elseif GetClickFrame('GossipFrame'):IsVisible() == 1 then
            	local availableQuests = {{ GetGossipAvailableQuests() }};
            	for j=1, GetNumGossipAvailableQuests(), 1 do
            		local i = j*5-4;
            		if availableQuests[i] == '{questName.EscapeLuaString()}' then
            			SelectGossipAvailableQuest(j);
            			return 0;
            		end
            	end
                local autoCompleteQuests = {{ GetGossipActiveQuests() }}
            	for j=1, GetNumGossipActiveQuests(), 1 do
            		local i = j*4-3;
            		if autoCompleteQuests[i] == '{questName.EscapeLuaString()}' then
            			SelectGossipActiveQuest(j);
            			return 3;
            		end
            	end
            else
            	return 1;
            end
            return 2;");

            switch (exitCodeOpen)
            {
                case 1:
                    Logger.LogError($"No Gossip or Quest window was open to pick up {questName}");
                    return false;
                case 2:
                    Logger.LogError($"The quest {questName} has not been found to pick up.");
                    return false;
                case 3:
                    Logger.Log($"The quest {questName} is an autocomplete.");
                    Thread.Sleep(200);
                    Quest.CompleteQuest();
                    Thread.Sleep(1000);
                    if (Quest.HasQuest(questId))
                    {
                        return false;
                    }
                    return true;
            }

            Thread.Sleep(200);

            if (Lua.LuaDoString<bool>("return GetClickFrame('QuestFrameCompleteButton'):IsVisible() == 1;"))
            {
                Logger.LogError($"The quest {questName} seems to be a trade quest.");
                Lua.LuaDoString(@"
                    local closeButton = GetClickFrame('QuestFrameCloseButton');
                    if closeButton:IsVisible() then
                	    closeButton:Click();
                    end");
                return false;
            }

            // Finish it
            Lua.LuaDoString("if GetClickFrame('QuestFrame'):IsVisible() then AcceptQuest(); end");
            Thread.Sleep(200);
            Lua.LuaDoString(@"
            local closeButton = GetClickFrame('QuestFrameCloseButton');
            if closeButton:IsVisible() then
            	closeButton:Click();
            end");

            robotManager.Helpful.Timer timer = new robotManager.Helpful.Timer(3000);
            while (!Quest.HasQuest(questId) && !timer.IsReady)
            {
                Thread.Sleep(100);
            }
            if (timer.IsReady)
            {
                return false;
            }

            Logger.Log($"Picked up quest {questName}.");

            return true;
        }
        */
        public static bool SaveQuestAsCompleted(int questId)
        {
            if (!WholesomeAQSettings.CurrentSetting.ListCompletedQuests.Contains(questId) && !Quest.HasQuest(questId))
            {
                Logger.Log($"Saved quest {questId} as completed");
                WholesomeAQSettings.CurrentSetting.ListCompletedQuests.Add(questId);
                return true;
            }
            return false;
        }

        internal static int GetIndexOfClosestPoint(List<Vector3> path)
        {
            if (path == null || path.Count <= 0) return 0;
            Vector3 myPos = ObjectManager.Me.PositionWithoutType;

            var curIndex = 0;
            var curDistance = float.MaxValue;

            for (var i = 0; i < path.Count; i++)
            {
                float distance = myPos.DistanceTo(path[i]);
                if (distance < curDistance)
                {
                    curDistance = distance;
                    curIndex = i;
                }
            }

            return curIndex;
        }

        public static float PointDistanceToLine(Vector3 start, Vector3 end, Vector3 point)
        {
            float vLenSquared = (start.X - end.X) * (start.X - end.X) +
                                (start.Y - end.Y) * (start.Y - end.Y) +
                                (start.Z - end.Z) * (start.Z - end.Z);
            if (vLenSquared == 0f) return point.DistanceTo(start);

            Vector3 ref1 = point - start;
            Vector3 ref2 = end - start;
            float clippedSegment = Math.Max(0, Math.Min(1, Vector3.Dot(ref ref1, ref ref2) / vLenSquared));

            Vector3 projection = start + (end - start) * clippedSegment;
            return point.DistanceTo(projection);
        }

        public static bool IsQuestCompleted(int questId) => WholesomeAQSettings.CurrentSetting.ListCompletedQuests.Contains(questId);

        public static bool WoWDBFileIsPresent() => File.Exists(Others.GetCurrentDirectory + @"\Data\WoWDb335");

        public static bool JSONFileIsPresent() => File.Exists(Others.GetCurrentDirectory + @"\Data\WAQquests.json");

        public static bool ZippedJSONIsPresent() => File.Exists(Others.GetCurrentDirectory + @"\Data\WAQquests.zip");

        public static void ZipJSONFile()
        {
            try
            {
                if (!JSONFileIsPresent())
                {
                    Logger.LogError("The JSON file is not present in Data");
                    return;
                }

                if (ZippedJSONIsPresent())
                    File.Delete(Others.GetCurrentDirectory + @"\Data\WAQquests.zip");

                using (ZipArchive zip = ZipFile.Open(Others.GetCurrentDirectory + @"\Data\WAQquests.zip",
                    ZipArchiveMode.Create))
                {
                    ZipArchiveEntry entry = zip.CreateEntry("WAQquests.json");
                    entry.LastWriteTime = DateTimeOffset.Now;

                    using (FileStream stream = File.OpenRead(Others.GetCurrentDirectory + @"\Data\WAQquests.json"))
                    using (Stream entryStream = entry.Open())
                    {
                        stream.CopyTo(entryStream);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("ZipJSONFile > " + e.Message);
            }
        }

        public static void WriteJSONFromDBResult(List<ModelQuestTemplate> resultFromDB)
        {
            try
            {
                if (File.Exists(Others.GetCurrentDirectory + @"\Data\WAQquests.json"))
                    File.Delete(Others.GetCurrentDirectory + @"\Data\WAQquests.json");

                using (StreamWriter file = File.CreateText(Others.GetCurrentDirectory + @"\Data\WAQquests.json"))
                {
                    var serializer = new JsonSerializer();
                    serializer.Serialize(file, resultFromDB);
                }
            }
            catch (Exception e)
            {
                Logger.LogError("WriteJSONFromDBResult > " + e.Message);
            }
        }

        public static List<string> GetAvailableQuestGossips()
        {
            var result = new List<string>();
            var numGossips = Lua.LuaDoString<int>(@"return GetNumGossipAvailableQuests()");
            var nameIndex = 1;
            for (var i = 1; i <= numGossips; i++)
            {
                result.Add(Lua.LuaDoString<string>(@"
                    local gossips = { GetGossipAvailableQuests() };
                    return gossips[" + nameIndex + "];"));
                nameIndex += 4;
            }

            return result;
        }

        public static List<string> GetActiveQuestGossips()
        {
            var result = new List<string>();
            var numGossips = Lua.LuaDoString<int>(@"return GetNumGossipActiveQuests()");
            var nameIndex = 1;
            for (var i = 1; i <= numGossips; i++)
            {
                result.Add(Lua.LuaDoString<string>(@"
                    local gossips = { GetGossipActiveQuests() };
                    return gossips[" + nameIndex + "];"));
                nameIndex += 4;
            }

            return result;
        }

        public static List<string> GetAllGossips()
        {
            var result = new List<string>();
            var numGossips = Lua.LuaDoString<int>(@"return GetNumGossipOptions()");
            var nameIndex = 1;
            for (var i = 1; i <= numGossips; i++)
            {
                result.Add(Lua.LuaDoString<string>(@"
                    local gossips = { GetGossipOptions() };
                    return gossips[" + nameIndex + "];"));
                nameIndex += 3;
            }

            return result;
        }

        public static bool ShouldStateBeInterrupted(IWAQTask task, WoWObject gameObject)
        {
            if (gameObject == null)
            {
                return true;
            }

            if (wManagerSetting.IsBlackListedZone(gameObject.Position)
                || wManagerSetting.IsBlackListed(gameObject.Guid))
            {
                MoveHelper.StopAllMove(true);
                return true;
            }

            if (wManagerSetting.IsBlackListedZone(task.Location))
            {
                MoveHelper.StopAllMove(true);
                return true;
            }

            return false;
        }

        public static void UpdateObjectiveCompletionDict(int[] questIds)
        {
            _objectiveCompletionDict = GetObjectiveCompletionDict(questIds);
        }
        /*
        public static bool AreAllObjectivesDicCompleted(int questId)
        {
            if (_objectiveCompletionDict.TryGetValue(questId, out bool[] objs))
            {
                return objs.All(obj => obj == true);
            }
            else
            {
                Logger.LogError($"Tried to get completion from an unexisting quest {questId}");
                return false;
            }
        }
        */
        private static Dictionary<int, bool[]> GetObjectiveCompletionDict(int[] questIds)
        {
            var resultDict = new Dictionary<int, bool[]>();
            string[] questIdStrings = questIds.Select(id => id.ToString()).ToArray();
            var inputTable = new StringBuilder("{",
                2 + questIdStrings.Aggregate(0, (last, str) => last + str.Length) + questIdStrings.Length - 1);
            for (var i = 0; i < questIdStrings.Length; i++)
            {
                inputTable.Append(questIdStrings[i]);
                if (i < questIdStrings.Length - 1) inputTable.Append(",");
            }

            inputTable.Append("}");

            bool[] outputTable = Lua.LuaDoString<bool[]>($@"
            local inputTable = {inputTable};
            local outputTable = {{}};
            
            for _, entry in pairs(inputTable) do
                local qId = 0;
                local i = 1
                while GetQuestLogTitle(i) do
            		local questTitle, level, questTag, suggestedGroup, isHeader, isCollapsed, isComplete, isDaily, questID = GetQuestLogTitle(i)
            		if ( not isHeader ) and questID == entry then
            			qId = i;
            		end
            		i = i + 1
                end
            	
            	for j=1, 6 do
            		if not qId then
            			table.insert(outputTable, false);
            		else
            			local description, objectiveType, isCompleted = GetQuestLogLeaderBoard(j,qId);
            			if not (description == nil) then  
            				table.insert(outputTable, isCompleted == 1);
            			else
            				table.insert(outputTable, false);
            			end
            		end
            	end
            end
            return unpack(outputTable)");

            if (outputTable.Length != questIds.Length * 6)
            {
                Logger.Log(
                    $"Expected {questIds.Length * 6} entries in GetObjectiveCompletionArray but got {outputTable.Length} instead.");
                return resultDict;
            }

            for (var i = 0; i < questIds.Length; i++)
            {
                var completionArray = new bool[6];
                for (var j = 0; j < completionArray.Length; j++)
                    completionArray[j] = outputTable[i * completionArray.Length + j];

                resultDict.Add(questIds[i], completionArray);
            }

            return resultDict;
        }

        public static bool IsObjectiveCompleted(int objectiveId, int questId)
        {
            if (objectiveId == -1)
                return false;

            if (objectiveId < 1 || objectiveId > 6)
            {
                Logger.LogError($"Tried to call GetObjectiveCompletion with objectiveId: {objectiveId}");
                return false;
            }

            if (_objectiveCompletionDict.TryGetValue(questId, out bool[] completionArray))
            {
                return completionArray[objectiveId - 1];
            }

            // It's possible that the completion dic hasn't been set yet, so we check the quest individually
            Dictionary<int, bool[]> tempDic = GetObjectiveCompletionDict(new int[] { questId });
            if (tempDic.TryGetValue(questId, out bool[] tempCompArray))
            {
                return tempCompArray[objectiveId - 1];
            }

            Logger.LogError($"Did not have quest {questId} in completion dictionary.");
            return false;
        }
        /*
        public static bool DangerousEnemiesAtLocation(Vector3 location)
        {
            uint myLevel = ObjectManager.Me.Level;
            var unitCounter = 0;
            foreach (WoWUnit unit in ObjectManager.GetWoWUnitHostile())
            {
                float distance = unit.PositionWithoutType.DistanceTo(location);
                if (distance > 40 || distance > unit.AggroDistance + 3) continue;
                uint unitLevel = unit.Level;
                if (unitLevel > myLevel + 2 || unitLevel > myLevel && unit.IsElite) return true;
                if (unitLevel > myLevel - 2) unitCounter++;
                if (unitCounter > 3) break;
            }

            return unitCounter > 3;
        }
        */
        // public static string GetTaskId(WAQTask task) => task.TaskId;

        // public static string GetTaskIdLegacy(WAQTask task) {
        //     string taskType = ((int) task.TaskType).ToString();
        //     string questEntry = task.Quest.Id.ToString();
        //     string objIndex = task.ObjectiveIndex.ToString();
        //     string uniqueId = $"{task.Npc?.Guid}{task.GatherObject?.Guid}";
        //
        //     return $"{taskType}{questEntry}{objIndex}{uniqueId}";
        // }

        // public static string GetTaskId(TaskType taskType, int questEntry, int objIndex, int uniqueId = 0) {
        //     string uid = uniqueId == 0 ? "" : uniqueId.ToString();
        //     return $"{(int) taskType}{questEntry}{objIndex}{uid}";
        // }

        public static string GetWoWVersion() => Lua.LuaDoString<string>("v, b, d, t = GetBuildInfo(); return v");

        public static Factions GetFaction() =>
            (PlayerFactions)ObjectManager.Me.Faction switch
            {
                PlayerFactions.Human => Factions.Human,
                PlayerFactions.Orc => Factions.Orc,
                PlayerFactions.Dwarf => Factions.Dwarf,
                PlayerFactions.NightElf => Factions.NightElf,
                PlayerFactions.Undead => Factions.Undead,
                PlayerFactions.Tauren => Factions.Tauren,
                PlayerFactions.Gnome => Factions.Gnome,
                PlayerFactions.Troll => Factions.Troll,
                PlayerFactions.Goblin => Factions.Goblin,
                PlayerFactions.BloodElf => Factions.BloodElf,
                PlayerFactions.Draenei => Factions.Draenei,
                PlayerFactions.Worgen => Factions.Worgen,
                _ => Factions.Unknown
            };

        public static Classes GetClass() =>
            ObjectManager.Me.WowClass switch
            {
                WoWClass.Warrior => Classes.Warrior,
                WoWClass.Paladin => Classes.Paladin,
                WoWClass.Hunter => Classes.Hunter,
                WoWClass.Rogue => Classes.Rogue,
                WoWClass.Priest => Classes.Priest,
                WoWClass.DeathKnight => Classes.DeathKnight,
                WoWClass.Shaman => Classes.Shaman,
                WoWClass.Mage => Classes.Mage,
                WoWClass.Warlock => Classes.Warlock,
                WoWClass.Druid => Classes.Druid,
                _ => Classes.Unknown
            };

        // Calculate real walking distance, returns 0 is path is broken
        public static WAQPath GetWAQPath(Vector3 from, Vector3 to)
        {
            float distance = 0f;
            List<Vector3> path = FindPath(from, to, skipIfPartiel: false, resultSuccess: out bool isReachable);
            for (var i = 0; i < path.Count - 1; ++i) distance += path[i].DistanceTo(path[i + 1]);
            if (!isReachable && distance < 100)
            {
                return new WAQPath(path, 0);
            }
            return new WAQPath(path, distance);
        }

        public static bool IsHorde()
        {
            uint myFaction = ObjectManager.Me.Faction;
            return myFaction == (uint)PlayerFactions.Orc || myFaction == (uint)PlayerFactions.Tauren
                || myFaction == (uint)PlayerFactions.Undead || myFaction == (uint)PlayerFactions.BloodElf
                || myFaction == (uint)PlayerFactions.Troll;
        }

        public static Dictionary<int, int> QuestModifiedLevel = new Dictionary<int, int>()
        {
            { 354, 3 }, // Roaming mobs, hard to find in a hostile zone
            { 843, 3 }, // Bael'Dun excavation, too many mobs
            { 6548, 3 }, // Avenge my village, too many mobs
            { 6629, 3 }, // Avenge my village follow up, too many mobs
            { 216, 2 }, // Between a rock and a Thistlefur, too many mobs
            { 541, 4 }, // Battle of Hillsbrad, too many mobs
            { 5501, 3 }, // Kodo bones, red enemies
            { 8885, 3 }, // Ring of Mmmmmmrgll, too many freaking murlocs
            { 1389, 3 }, // Draenethyst crystals, too many mobs
            { 582, 3 }, // Headhunting, too many mobs
            { 1177, 3 }, // Hungry!, too many murlocs
            { 1054, 3 }, // Culling the threat, too many murlocs
            { 115, 3 }, // Culling the threat, too many murlocs
            { 180, 3 }, // Lieutenant Fangore, too many mobs
            { 323, 3 }, // Proving your worth, too many mobs
            { 303, 3 }, // Dark iron War, too many mobs
            { 304, 3 }, // A grim task, too many mobs
            { 464, 3 }, // War banners, too many mobs
            { 203, 3 }, // The second rebellion, too many mobs
            { 505, 3 }, // Syndicate assassins, too many mobs
            { 1439, 5 }, // Search for Tyranis, too many mobs
            { 213, 5 }, // Hostile takeover, too many mobs
            { 1398, 4 }, // Driftwood, too many mobs
            { 2870, 4 }, // Against Lord Shalzaru, too many mobs
            { 12462, 4 }, // Breaking off a piece, too many mobs
            { 12043, 3 }, // Nozzlerust defense, too many mobs
            { 12044, 3 }, // Stocking up, too many mobs
            { 12120, 3 }, // DrakAguul's Mallet, too many mobs
        };

        // Returns whether the player has the debuff passed as a string (ex: Weakened Soul)
        public static bool HasDebuff(string debuffName, string unitName = "player", int loops = 25)
        {
            return Lua.LuaDoString<bool>
                (@$"for i=1,{loops} do
                    local n, _, _, _, _  = UnitDebuff('{unitName}',i);
                    if n == '{debuffName}' then
                    return true
                    end
                end");
        }

        public static void PickupQuestFromBagItem(string itemName)
        {
            ItemsManager.UseItemByNameOrId(itemName);
            Thread.Sleep(500);
            Lua.LuaDoString("if GetClickFrame('QuestFrame'):IsVisible() then AcceptQuest(); end");
            Thread.Sleep(500);
            Lua.LuaDoString(@"
                        local closeButton = GetClickFrame('QuestFrameCloseButton');
                        if closeButton:IsVisible() then
            	            closeButton:Click();
                        end");
        }

        public static List<WoWUnit> GetListObjManagerHostiles()
        {
            Vector3 myPosition = ObjectManager.Me.Position;
            return ObjectManager.GetObjectWoWUnit()
               .FindAll(u => u.IsAttackable
                   && u.Reaction == Reaction.Hostile
                   && u.IsAlive
                   && u.Entry != 17578 // Hellfire Training Dummy
                   && u.IsValid
                   && !u.IsElite
                   && !u.IsTaggedByOther
                   && !u.PlayerControlled
                   && u.Position.DistanceTo(myPosition) < 50
                   && u.Level < ObjectManager.Me.Level + 4)
               .OrderBy(u => u.Position.DistanceTo(myPosition))
               .ToList();
        }
        /*
        public static WAQPath AdjustPathToTask(WAQTask task)
        {
            Random rand = new Random();
            Vector3 location = task.Location;
            for (int i = 0; i < 10; i++)
            {
                Vector3 newdest = new Vector3(
                    location.X + rand.NextDouble() * 4 - 2,
                    location.Y + rand.NextDouble() * 4 - 2,
                    location.Z + rand.NextDouble() * 4 - 2);
                WAQPath newPath = GetWAQPath(ObjectManager.Me.Position, newdest);
                Logger.Log($"Trying to adjust path for {task.TaskName} {i}");
                if (newPath.IsReachable)
                {
                    Logger.Log($"FOUND");
                    task.Location = newdest;
                    return newPath;
                }
            }
            Logger.Log($"FAILED");
            return new WAQPath(new List<Vector3>(), 0);
        }
        
        public static WAQPath AdjustPathToObject(WoWObject wObject)
        {
            Random rand = new Random();
            Vector3 location = wObject.Position;
            for (int i = 0; i < 10; i++)
            {
                Vector3 newdest = new Vector3(
                    location.X + rand.NextDouble() * 4 - 2,
                    location.Y + rand.NextDouble() * 4 - 2,
                    location.Z + rand.NextDouble() * 4 - 2);
                WAQPath newPath = GetWAQPath(ObjectManager.Me.Position, newdest);
                Logger.Log($"Trying to adjust path for {wObject.Name} {i}");
                if (newPath.IsReachable)
                {
                    Logger.Log($"FOUND");
                    return newPath;
                }
            }
            Logger.Log($"FAILED");
            return new WAQPath(new List<Vector3>(), 0);
        }

        public static bool PlayerInBloodElfStartingZone()
        {
            string zone = Lua.LuaDoString<string>("return GetRealZoneText();");
            return zone == "Eversong Woods" || zone == "Ghostlands" || zone == "Silvermoon City";
        }

        public static bool PlayerInDraneiStartingZone()
        {
            string zone = Lua.LuaDoString<string>("return GetRealZoneText();");
            return zone == "Azuremyst Isle" || zone == "Bloodmyst Isle" || zone == "The Exodar";
        }*/
    }
}
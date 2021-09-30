using Newtonsoft.Json;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Database.Models;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Helpers {
    public static class ToolBox {
        private static readonly Stopwatch Watch = Stopwatch.StartNew();
        public static long CurTime => Watch.ElapsedMilliseconds;
        public static readonly Random Rnd = new Random();

        public static T TakeHighest<T>(this IEnumerable<T> list, Func<T, int> takeValue, out int amount) {
            var highest = int.MinValue;
            T curHighestElement = default;

            foreach (T element in list) {
                int curValue = takeValue(element);
                if (curValue > highest) {
                    highest = curValue;
                    curHighestElement = element;
                }
            }

            amount = highest;
            return curHighestElement;
        }

        public static T TakeHighest<T>(this IEnumerable<T> list, Func<T, int> takeValue) {
            return list.TakeHighest(takeValue, out _);
        }

        public static bool InInteractDistance(this WoWUnit unit) => unit.GetDistance < unit.CombatReach + 4f;

        public static WoWUnit FindClosestUnitByEntry(int entry) {
            Vector3 myPos = ObjectManager.Me.PositionWithoutType;
            return ObjectManager.GetWoWUnitByEntry(entry)
                .TakeHighest(unit => (int) -unit.PositionWithoutType.DistanceTo(myPos));
        }
        
        public static WoWGameObject FindClosestGameObjectByEntry(int entry) {
            Vector3 myPos = ObjectManager.Me.PositionWithoutType;
            return ObjectManager.GetWoWGameObjectByEntry(entry)
                .TakeHighest(gameObject => (int) -gameObject.Position.DistanceTo(myPos));
        }

        public static string EscapeLuaString(this string str) => str.Replace("\\", "\\\\").Replace("'", "\\'");

        public static bool IsNpcFrameActive() =>
            Lua.LuaDoString<bool>("return GetClickFrame('GossipFrame'):IsVisible() == 1 or GetClickFrame('QuestFrame'):IsVisible() == 1;");
        
        public static bool GossipTurnInQuest(string questName) {
            // Select quest
            int exitCodeOpen = Lua.LuaDoString<int>($@"
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
            			SelectGossipActiveQuest(i);
            			return 0;
            		end
            	end
            else
            	return 1;
            end
            return 2;");
            switch (exitCodeOpen) {
                case 1:
                    Logger.LogError($"No Gossip window was open to hand in {questName}");
                    return false;
                case 2:
                    Logger.LogError($"The quest {questName} has not been found to hand in.");
                    return false;
                case 3:
                    Logger.LogError($"The quest {questName} has been found but is not completed yet.");
                    return false;
            }
            
            Thread.Sleep(200);

            var requiresItems = Lua.LuaDoString<bool>("return GetNumQuestItems() > 0;");
            if (requiresItems) {
                Lua.LuaDoString("CompleteQuest();");
                Thread.Sleep(200);
            }

            // Get reward
            var hasQuestReward = Lua.LuaDoString<bool>("return GetNumQuestChoices() > 0;");
            if (hasQuestReward) {
                // Ugly workaround to trigger the selection event
                Logger.LogDebug("Letting InventoryManager select quest reward.");
                Quest.CompleteQuest();
            }
            
            Thread.Sleep(200);

            // Finish it
            Lua.LuaDoString($"if GetClickFrame('QuestFrame'):IsVisible() then GetQuestReward({(hasQuestReward ? "1" : "nil")}); end");
            Thread.Sleep(200);
            Lua.LuaDoString(@"
            local closeButton = GetClickFrame('QuestFrameCloseButton');
            if closeButton:IsVisible() then
            	closeButton:Click();
            end");
            
            return true;
        }
        
        public static bool GossipPickUpQuest(string questName) {
            // Select quest
            int exitCodeOpen = Lua.LuaDoString<int>($@"
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
            			SelectGossipAvailableQuest(i);
            			return 0;
            		end
            	end
            else
            	return 1;
            end
            return 2;");
            switch (exitCodeOpen) {
                case 1:
                    Logger.LogError($"No Gossip or Quest window was open to pick up {questName}");
                    return false;
                case 2:
                    Logger.LogError($"The quest {questName} has not been found to pick up.");
                    return false;
            }
            
            Thread.Sleep(200);

            if (Lua.LuaDoString<bool>("return GetClickFrame('QuestFrameCompleteButton'):IsVisible() == 1;")) {
                Logger.LogError($"The quest {questName} seems to be a trade quest.");
                Lua.LuaDoString(@"
                local closeButton = GetClickFrame('QuestFrameCloseButton');
                if closeButton:IsVisible() then
                	closeButton:Click();
                end");
                return false;
            }
            
            // Finish it
            Lua.LuaDoString($"if GetClickFrame('QuestFrame'):IsVisible() then AcceptQuest(); end");
            Thread.Sleep(200);
            Lua.LuaDoString(@"
            local closeButton = GetClickFrame('QuestFrameCloseButton');
            if closeButton:IsVisible() then
            	closeButton:Click();
            end");
            
            return true;
        }

        public static List<int> GetCompletedQuests() {
            List<int> completedQuests = new List<int>();
            completedQuests.AddRange(Quest.FinishedQuestSet);
            completedQuests.AddRange(WholesomeAQSettings.CurrentSetting.ListCompletedQuests);
            return completedQuests;
        }

        public static bool IsQuestCompleted(int questId) {
            return GetCompletedQuests().Contains(questId);
        }

        public static bool WoWDBFileIsPresent() {
            return File.Exists(Others.GetCurrentDirectory + @"\Data\WoWDb335-quests");
        }

        public static bool JSONFileIsPresent() {
            return File.Exists(Others.GetCurrentDirectory + @"\Data\WAQquests.json");
        }

        public static bool CompiledJSONFileIsPresent() {
            return File.Exists(@"F:\WoW\Dev\Wholesome-Auto-Quester\Wholesome_Auto_Quester\Compiled\WAQquests.zip");
        }

        public static bool ZippedJSONIsPresent() {
            return File.Exists(Others.GetCurrentDirectory + @"\Data\WAQquests.zip");
        }

        public static List<ModelQuest> GetAllQuestsFromJSON() {
            try {
                if (!JSONFileIsPresent()) {
                    Logger.LogError("The Compiled JSON file is not present.");
                    return null;
                }

                using (StreamReader file = File.OpenText(Others.GetCurrentDirectory + @"\Data\WAQquests.json")) {
                    JsonSerializer serializer = new JsonSerializer();
                    return (List<ModelQuest>) serializer.Deserialize(file, typeof(List<ModelQuest>));
                }
            } catch (Exception e) {
                Logger.LogError(e.Message);
                return null;
            }
        }

        public static void ZipJSONFile() {
            try {
                if (!JSONFileIsPresent()) {
                    Logger.LogError("The JSON file is not present in Data");
                    return;
                }

                if (ZippedJSONIsPresent())
                    File.Delete(Others.GetCurrentDirectory + @"\Data\WAQquests.zip");

                using (var zip = ZipFile.Open(Others.GetCurrentDirectory + @"\Data\WAQquests.zip",
                    ZipArchiveMode.Create)) {
                    var entry = zip.CreateEntry("WAQquests.json");
                    entry.LastWriteTime = DateTimeOffset.Now;

                    using (var stream = File.OpenRead(Others.GetCurrentDirectory + @"\Data\WAQquests.json"))
                    using (var entryStream = entry.Open())
                        stream.CopyTo(entryStream);
                }

                // Copy to Compiled folder
                string compiledzip = @"F:\WoW\Dev\Wholesome-Auto-Quester\Wholesome_Auto_Quester\Compiled\WAQquests.zip";
                if (File.Exists(compiledzip))
                    File.Delete(compiledzip);
                File.Copy(Others.GetCurrentDirectory + @"\Data\WAQquests.zip", compiledzip);
            } catch (Exception e) {
                Logger.LogError("ZipJSONFile > " + e.Message);
            }
        }

        public static void WriteJSONFromDBResult(List<ModelQuest> resultFromDB) {
            try {
                if (File.Exists(Others.GetCurrentDirectory + @"\Data\WAQquests.json"))
                    File.Delete(Others.GetCurrentDirectory + @"\Data\WAQquests.json");

                /*
                Logger.Log("Serialize");
                string jsonString = JsonConvert.SerializeObject(resultFromDB, Formatting.Indented);
                Logger.Log("Write");
                File.WriteAllText(Others.GetCurrentDirectory + @"\Data\WAQquests.json", jsonString);
                */
                using (StreamWriter file = File.CreateText(Others.GetCurrentDirectory + @"\Data\WAQquests.json")) {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, resultFromDB);
                }
            } catch (Exception e) {
                Logger.LogError("WriteJSONFromDBResult > " + e.Message);
            }
        }

        public static List<string> GetAvailableQuestGossips() {
            List<string> result = new List<string>();
            int numGossips = Lua.LuaDoString<int>(@"return GetNumGossipAvailableQuests()");
            int nameIndex = 1;
            for (int i = 1; i <= numGossips; i++) {
                result.Add(Lua.LuaDoString<string>(@"
                    local gossips = { GetGossipAvailableQuests() };
                    return gossips[" + nameIndex + "];"));
                nameIndex += 4;
            }

            return result;
        }

        public static List<string> GetActiveQuestGossips() {
            List<string> result = new List<string>();
            int numGossips = Lua.LuaDoString<int>(@"return GetNumGossipActiveQuests()");
            int nameIndex = 1;
            for (int i = 1; i <= numGossips; i++) {
                result.Add(Lua.LuaDoString<string>(@"
                    local gossips = { GetGossipActiveQuests() };
                    return gossips[" + nameIndex + "];"));
                nameIndex += 4;
            }

            return result;
        }

        public static List<string> GetAllGossips() {
            List<string> result = new List<string>();
            int numGossips = Lua.LuaDoString<int>(@"return GetNumGossipOptions()");
            int nameIndex = 1;
            for (int i = 1; i <= numGossips; i++) {
                result.Add(Lua.LuaDoString<string>(@"
                    local gossips = { GetGossipOptions() };
                    return gossips[" + nameIndex + "];"));
                nameIndex += 3;
            }

            return result;
        }

        public static ulong GetTaskId(WAQTask task) {
            string taskType = $"{(int) task.TaskType}";
            string questEntry = $"{task.Quest.Id}";
            string objIndex = $"{task.ObjectiveIndex}";
            string guid = $"{(task.Npc != null ? task.Npc.Guid : task.GatherObject.Guid)}";
            return ulong.Parse($"{taskType}{questEntry}{objIndex}{guid}");
        }

        public static ulong GetTaskId(TaskType taskType, int questEntry, int objIndex, int guid) {
            return ulong.Parse($"{(int) taskType}{questEntry}{objIndex}{guid}");
        }

        public static string GetWoWVersion() {
            return Lua.LuaDoString<string>("v, b, d, t = GetBuildInfo(); return v");
        }

        public static Factions GetFaction() {
            switch (ObjectManager.Me.Faction) {
                case (uint) PlayerFactions.Human: return Factions.Human;
                case (uint) PlayerFactions.Orc: return Factions.Orc;
                case (uint) PlayerFactions.Dwarf: return Factions.Dwarf;
                case (uint) PlayerFactions.NightElf: return Factions.NightElf;
                case (uint) PlayerFactions.Undead: return Factions.Undead;
                case (uint) PlayerFactions.Tauren: return Factions.Tauren;
                case (uint) PlayerFactions.Gnome: return Factions.Gnome;
                case (uint) PlayerFactions.Troll: return Factions.Troll;
                case (uint) PlayerFactions.Goblin: return Factions.Goblin;
                case (uint) PlayerFactions.BloodElf: return Factions.BloodElf;
                case (uint) PlayerFactions.Draenei: return Factions.Draenei;
                case (uint) PlayerFactions.Worgen: return Factions.Worgen;
                default: return Factions.Unknown;
            }
        }

        public static Classes GetClass() {
            switch (ObjectManager.Me.WowClass) {
                case WoWClass.Warrior: return Classes.Warrior;
                case WoWClass.Paladin: return Classes.Paladin;
                case WoWClass.Hunter: return Classes.Hunter;
                case WoWClass.Rogue: return Classes.Rogue;
                case WoWClass.Priest: return Classes.Priest;
                case WoWClass.DeathKnight: return Classes.DeathKnight;
                case WoWClass.Shaman: return Classes.Shaman;
                case WoWClass.Mage: return Classes.Mage;
                case WoWClass.Warlock: return Classes.Warlock;
                case WoWClass.Druid: return Classes.Druid;
                default: return Classes.Unknown;
            }
        }

        public static readonly Dictionary<int, int> ZoneLevelDictionary = new Dictionary<int, int> {
            {14, 10}, //Kalimdor
            {15, 10}, //Azeroth
            {465, 1}, //AzuremystIsle
            {28, 1}, //DunMorogh
            {5, 1}, //Durotar
            {31, 1}, //Elwynn
            {463, 1}, //EversongWoods
            {42, 1}, //Teldrassil
            {21, 1}, //Tirisfal
            {481, 10}, //SilvermoonCity
            {11, 10}, //Barrens
            {477, 10}, //BloodmystIsle
            {43, 10}, //Darkshore
            {464, 10}, //Ghostlands
            {342, 10}, //Ironforge
            {36, 10}, //LochModan
            {10, 1}, //Mulgore
            {322, 10}, //Ogrimmar
            {22, 10}, //Silverpine
            {302, 10}, //Stormwind
            {472, 10}, //TheExodar
            {363, 10}, //ThunderBluff
            {383, 10}, //Undercity
            {40, 10}, //Westfall
            {37, 15}, //Redridge
            {82, 15}, //StonetalonMountains
            {44, 18}, //Ashenvale
            {35, 18}, //Duskwood
            {25, 20}, //Hilsbrad
            {41, 20}, //Wetlands
            {62, 25}, //ThousandNeedles
            {16, 30}, //Alterac
            {17, 30}, //Arathi
            {102, 30}, //Desolace
            {142, 30}, //Dustwallow
            {38, 30}, //Stranglethorn
            {18, 35}, //Badlands
            {39, 35}, //SwampOfSorrows
            {27, 40}, //Hinterlands
            {162, 40}, //Tanaris
            {122, 42}, //Feralas
            {182, 45}, //Aszhara
            {20, 45}, //BlastedLands
            {29, 45}, //SearingGorge
            {183, 48}, //Felwood
            {202, 48}, //UngoroCrater
            {30, 50}, //BurningSteppes
            {23, 51}, //WesternPlaguelands
            {24, 53}, //EasternPlaguelands
            {282, 53}, //Winterspring
            {242, 55}, //Moonglade
            {262, 55}, //Silithus
            {466, 58}, //Hellfire
            {467, 60}, //Zangarmarsh
            {479, 62}, //TerokkarForest
            {476, 65}, //BladesEdgeMountains
            {478, 65}, //Nagrand
            {480, 67}, //Netherstorm
            {474, 67}, //ShadowmoonValley
            {482, 65}, //ShattrathCity
            {487, 68}, //BoreanTundra
            {32, 68}, //DeadwindPass
            {492, 68}, //HowlingFjord
            {489, 71}, //Dragonblight
            {491, 73}, //GrizzlyHills
            {497, 75}, //ZulDrak
            {494, 76}, //SholazarBasin
            {511, 77}, //CrystalsongForest
            {542, 77}, //HrothgarsLanding
            {605, 77}, //IcecrownCitadel
            {505, 80}, //Dalaran
        };
    }
}
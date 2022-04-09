using Newtonsoft.Json;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Database.Models;
using WholesomeToolbox;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.PathFinder;

namespace Wholesome_Auto_Quester.Helpers
{
    public static class ToolBox
    {
        private static Dictionary<int, bool[]> _objectiveCompletionDict = new Dictionary<int, bool[]>();

        public static void CheckIfZReachable(Vector3 checkPosition)
        {
            if (checkPosition.DistanceTo2D(ObjectManager.Me.Position) <= 3 && WTLocation.GetZDifferential(checkPosition) > 3)
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
            if (questIds.Length <= 0)
                return;
            _objectiveCompletionDict = GetObjectiveCompletionDict(questIds);
        }

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
            Logger.LogDebug($"Individual update");
            Dictionary<int, bool[]> tempDic = GetObjectiveCompletionDict(new int[] { questId });
            if (tempDic.TryGetValue(questId, out bool[] tempCompArray))
            {
                return tempCompArray[objectiveId - 1];
            }

            Logger.LogError($"Did not have quest {questId} in completion dictionary.");
            return false;
        }

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
            { 464, 3 }, // War banners, too many mobs
            { 203, 3 }, // The second rebellion, too many mobs
            { 505, 3 }, // Syndicate assassins, too many mobs
            { 1439, 5 }, // Search for Tyranis, too many mobs
            { 213, 5 }, // Hostile takeover, too many mobs
            { 1398, 4 }, // Driftwood, too many mobs
            { 2870, 4 }, // Against Lord Shalzaru, too many mobs
            { 12043, 3 }, // Nozzlerust defense, too many mobs
            { 12044, 3 }, // Stocking up, too many mobs
            { 12120, 3 }, // DrakAguul's Mallet, too many mobs
        };

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
    }
}
using Newtonsoft.Json;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Database.Models;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Helpers
{
    public class ToolBox
    {
        public static List<int> GetCompletedQuests()
        {
            List<int> completedQuests = new List<int>();
            completedQuests.AddRange(Quest.FinishedQuestSet);
            completedQuests.AddRange(WholesomeAQSettings.CurrentSetting.ListCompletedQuests);
            return completedQuests;
        }

        public static bool IsQuestCompleted(int questId)
        {
            return GetCompletedQuests().Contains(questId);
        }

        public static bool WoWDBFileIsPresent()
        {
            return File.Exists(Others.GetCurrentDirectory + @"\Data\WoWDb335-quests");
        }

        public static bool JSONFileIsPresent()
        {
            return File.Exists(Others.GetCurrentDirectory + @"\Data\WAQquests.json");
        }

        public static bool CompiledJSONFileIsPresent()
        {
            return File.Exists(@"C:\Users\Nico\Dropbox\Programmation\wRobot\Wholesome_Auto_Quester\Wholesome_Auto_Quester\Compiled\WAQquests.zip");
        }

        public static bool ZippedJSONIsPresent()
        {
            return File.Exists(Others.GetCurrentDirectory + @"\Data\WAQquests.zip");
        }

        public static List<ModelQuest> GetAllQuestsFromJSON()
        {
            try
            {
                if (!CompiledJSONFileIsPresent())
                {
                    Logger.LogError("The Compiled JSON file is not present");
                    return null;
                }

                using (StreamReader file = File.OpenText(Others.GetCurrentDirectory + @"\Data\WAQquests.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    return (List<ModelQuest>)serializer.Deserialize(file, typeof(List<ModelQuest>));
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message);
                return null;
            }
        }

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

                using (var zip = ZipFile.Open(Others.GetCurrentDirectory + @"\Data\WAQquests.zip", ZipArchiveMode.Create))
                {
                    var entry = zip.CreateEntry("WAQquests.json");
                    entry.LastWriteTime = DateTimeOffset.Now;

                    using (var stream = File.OpenRead(Others.GetCurrentDirectory + @"\Data\WAQquests.json"))
                    using (var entryStream = entry.Open())
                        stream.CopyTo(entryStream);
                }

                // Copy to Compiled folder
                string compiledzip = @"C:\Users\Nico\Dropbox\Programmation\wRobot\Wholesome_Auto_Quester\Wholesome_Auto_Quester\Compiled\WAQquests.zip";
                if (File.Exists(compiledzip))
                    File.Delete(compiledzip);
                File.Copy(Others.GetCurrentDirectory + @"\Data\WAQquests.zip", compiledzip);
            }
            catch (Exception e)
            {
                Logger.LogError("ZipJSONFile > " + e.Message);
            }
        }

        public static void WriteJSONFromDBResult(List<ModelQuest> resultFromDB)
        {
            try
            {
                if (File.Exists(Others.GetCurrentDirectory + @"\Data\WAQquests.json"))
                    File.Delete(Others.GetCurrentDirectory + @"\Data\WAQquests.json");

                /*
                Logger.Log("Serialize");
                string jsonString = JsonConvert.SerializeObject(resultFromDB, Formatting.Indented);
                Logger.Log("Write");
                File.WriteAllText(Others.GetCurrentDirectory + @"\Data\WAQquests.json", jsonString);
                */
                using (StreamWriter file = File.CreateText(Others.GetCurrentDirectory + @"\Data\WAQquests.json")) 
                { 
                    JsonSerializer serializer = new JsonSerializer(); 
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
            List<string> result = new List<string>();
            int numGossips = Lua.LuaDoString<int>(@"return GetNumGossipAvailableQuests()");
            int nameIndex = 1;
            for (int i = 1; i <= numGossips; i++)
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
            List<string> result = new List<string>();
            int numGossips = Lua.LuaDoString<int>(@"return GetNumGossipActiveQuests()");
            int nameIndex = 1;
            for (int i = 1; i <= numGossips; i++)
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
            List<string> result = new List<string>();
            int numGossips = Lua.LuaDoString<int>(@"return GetNumGossipOptions()");
            int nameIndex = 1;
            for (int i = 1; i <= numGossips; i++)
            {
                result.Add(Lua.LuaDoString<string>(@"
                    local gossips = { GetGossipOptions() };
                    return gossips[" + nameIndex + "];"));
                nameIndex += 3;
            }

            return result;
        }

        public static ulong GetTaskId(WAQTask task)
        {
            string taskType = $"{(int)task.TaskType}";
            string questEntry = $"{task.Quest.Id}";
            string objIndex = $"{task.ObjectiveIndex}";
            string guid = $"{(task.Npc != null ? task.Npc.Guid : task.GatherObject.Guid)}";
            return ulong.Parse($"{taskType}{questEntry}{objIndex}{guid}");
        }
        public static ulong GetTaskId(TaskType taskType, int questEntry, int objIndex, int guid)
        {
            return ulong.Parse($"{(int)taskType}{questEntry}{objIndex}{guid}");
        }

        public static string GetWoWVersion()
        {
            return Lua.LuaDoString<string>("v, b, d, t = GetBuildInfo(); return v");
        }

        public static Factions GetFaction()
        {
            switch(ObjectManager.Me.Faction)
            {
                case (uint)PlayerFactions.Human: return Factions.Human;
                case (uint)PlayerFactions.Orc: return Factions.Orc;
                case (uint)PlayerFactions.Dwarf: return Factions.Dwarf;
                case (uint)PlayerFactions.NightElf: return Factions.NightElf;
                case (uint)PlayerFactions.Undead: return Factions.Undead;
                case (uint)PlayerFactions.Tauren: return Factions.Tauren;
                case (uint)PlayerFactions.Gnome: return Factions.Gnome;
                case (uint)PlayerFactions.Troll: return Factions.Troll;
                case (uint)PlayerFactions.Goblin: return Factions.Goblin;
                case (uint)PlayerFactions.BloodElf: return Factions.BloodElf;
                case (uint)PlayerFactions.Draenei: return Factions.Draenei;
                case (uint)PlayerFactions.Worgen: return Factions.Worgen;
                default: return Factions.Unknown;
            }
        }

        public static Classes GetClass()
        {
            switch (ObjectManager.Me.WowClass)
            {
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

        public static readonly Dictionary<int, int> ZoneLevelDictionary = new Dictionary<int, int>
        {
            {14,10 }, //Kalimdor
            {15,10}, //Azeroth
            {465,1}, //AzuremystIsle
            {28,1}, //DunMorogh
            {5,1}, //Durotar
            {31,1}, //Elwynn
            {463,1}, //EversongWoods
            {42,1}, //Teldrassil
            {21,1}, //Tirisfal
            {481,10}, //SilvermoonCity
            {11,10}, //Barrens
            {477,10}, //BloodmystIsle
            {43,10}, //Darkshore
            {464,10}, //Ghostlands
            {342,10}, //Ironforge
            {36,10}, //LochModan
            {10,1}, //Mulgore
            {322,10}, //Ogrimmar
            {22,10}, //Silverpine
            {302,10}, //Stormwind
            {472,10}, //TheExodar
            {363,10}, //ThunderBluff
            {383,10}, //Undercity
            {40,10}, //Westfall
            {37,15}, //Redridge
            {82,15}, //StonetalonMountains
            {44,18}, //Ashenvale
            {35,18}, //Duskwood
            {25,20}, //Hilsbrad
            {41,20}, //Wetlands
            {62,25}, //ThousandNeedles
            {16,30}, //Alterac
            {17,30}, //Arathi
            {102,30}, //Desolace
            {142,30}, //Dustwallow
            {38,30}, //Stranglethorn
            {18,35}, //Badlands
            {39,35}, //SwampOfSorrows
            {27,40}, //Hinterlands
            {162,40}, //Tanaris
            {122,42}, //Feralas
            {182,45}, //Aszhara
            {20,45}, //BlastedLands
            {29,45}, //SearingGorge
            {183,48}, //Felwood
            {202,48}, //UngoroCrater
            {30,50}, //BurningSteppes
            {23,51}, //WesternPlaguelands
            {24,53}, //EasternPlaguelands
            {282,53}, //Winterspring
            {242,55}, //Moonglade
            {262,55}, //Silithus
            {466,58}, //Hellfire
            {467,60}, //Zangarmarsh
            {479,62}, //TerokkarForest
            {476,65}, //BladesEdgeMountains
            {478,65}, //Nagrand
            {480,67}, //Netherstorm
            {474,67}, //ShadowmoonValley
            {482,65}, //ShattrathCity
            {487,68}, //BoreanTundra
            {32,68}, //DeadwindPass
            {492,68}, //HowlingFjord
            {489,71}, //Dragonblight
            {491,73}, //GrizzlyHills
            {497,75}, //ZulDrak
            {494,76}, //SholazarBasin
            {511,77}, //CrystalsongForest
            {542,77}, //HrothgarsLanding
            {605,77}, //IcecrownCitadel
            {505,80}, //Dalaran
        };
    }
}


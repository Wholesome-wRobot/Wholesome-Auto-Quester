using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Bot.ContinentManagement;
using Wholesome_Auto_Quester.Bot.JSONManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.GrindManagement
{
    public class GrindManager : IGrindManager
    {
        private readonly List<IWAQTask> _grindTasks = new List<IWAQTask>();
        private readonly IJSONManager _jsonManager;
        private readonly IContinentManager _continentManager;

        public GrindManager(IJSONManager jSONManager, IContinentManager continentManager)
        {
            _jsonManager = jSONManager;
            _continentManager = continentManager;
            Initialize();
        }

        public void Initialize()
        {
            RecordGrindTasksFromJSON();
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += LuaEventHandler;
        }

        public void Dispose()
        {
            EventsLuaWithArgs.OnEventsLuaStringWithArgs -= LuaEventHandler;
        }

        public void RecordGrindTasksFromJSON()
        {
            List<ModelCreatureTemplate> creaturesToGrind = _jsonManager.GetCreatureTemplatesToGrindFromJSON();

            _grindTasks.Clear();
            int myLevel = (int)ObjectManager.Me.Level;

            if (myLevel <= 3) myLevel = 3;
            int lowerLvlLimit = myLevel - WholesomeAQSettings.CurrentSetting.LevelDeltaMinus;
            int upperLvlLimit = myLevel + WholesomeAQSettings.CurrentSetting.LevelDeltaPlus;
            creaturesToGrind.RemoveAll(ct => ct.MinLevel < lowerLvlLimit);
            creaturesToGrind.RemoveAll(ct => ct.MaxLevel > upperLvlLimit);
            creaturesToGrind.RemoveAll(ct => ct.rank > 0);

            creaturesToGrind.RemoveAll(ct =>
                ct.Creatures.Any(c =>
                    !_continentManager.PointIsOnMyContinent(c.GetSpawnPosition, c.map) && !WholesomeAQSettings.CurrentSetting.ContinentTravel)
                || ct.IsFriendly
                || ct.Faction == 188);

            if (creaturesToGrind.Exists(ct => ct.Creatures.Count > 10))
                creaturesToGrind.RemoveAll(ct => ct.Creatures.Count < 10);

            Logger.Log($"Level {myLevel}. Found {creaturesToGrind.Count} templates to grind (lvl{lowerLvlLimit} to lvl{upperLvlLimit})");

            foreach (ModelCreatureTemplate template in creaturesToGrind)
            {
                foreach (ModelCreature creature in template.Creatures)
                {
                    _grindTasks.Add(new WAQTaskGrind(template, creature, _continentManager));
                }
            }

            _grindTasks.RemoveAll(task => task.WorldMapArea == null || !task.IsValid);
        }

        public List<IWAQTask> GetGrindTasks => _grindTasks;

        private void LuaEventHandler(string eventid, List<string> args)
        {
            switch (eventid)
            {
                case "PLAYER_LEVEL_UP":
                    if (ObjectManager.Me.Level < WholesomeAQSettings.CurrentSetting.StopAtLevel)
                    {
                        RecordGrindTasksFromJSON();
                    }
                    break;
                case "PLAYER_ENTERING_WORLD":
                    RecordGrindTasksFromJSON();
                    break;
            }
        }
    }
}

using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Bot.JSONManagement
{
    public interface IJSONManager : ICycleable
    {
        List<ModelWorldMapArea> GetWorldMapAreasFromJSON();
        List<ModelCreatureTemplate> GetCreatureTemplatesToGrindFromJSON();
        List<ModelQuestTemplate> GetAvailableQuestsFromJSON();
    }
}

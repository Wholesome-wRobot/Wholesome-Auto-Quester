using robotManager.Helpful;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Bot.TravelManagement
{
    public interface ITravelManager : ICycleable
    {
        bool NeedToTravelTo(IWAQTask task, out (ModelWorldMapArea myPosition, ModelWorldMapArea destination) result);
        ModelWorldMapArea GetWorldMapAreaFromPoint(Vector3 position, int mapdId);
    }
}

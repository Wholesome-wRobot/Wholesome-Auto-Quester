using robotManager.Helpful;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Bot.ContinentManagement
{
    public interface IContinentManager : ICycleable
    {
        ModelWorldMapArea GetWorldMapAreaFromPoint(Vector3 position, int mapdId);
        ModelWorldMapArea MyMapArea { get; }
        bool PointIsOnMyContinent(Vector3 position, int continentId);
    }
}

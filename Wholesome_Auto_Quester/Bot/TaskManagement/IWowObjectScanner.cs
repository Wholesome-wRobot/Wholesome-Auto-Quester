using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement
{
    public interface IWowObjectScanner : ICycleable
    {
        (WoWObject wowObject, IWAQTask task) ActiveWoWObject { get; }
        void AddToScannerRegistry(int entry, IWAQTask task);
        void RemoveFromScannerRegistry(int entry, IWAQTask task);
        IWAQTask GetTaskMatchingWithObject(WoWObject closestObject);
    }
}

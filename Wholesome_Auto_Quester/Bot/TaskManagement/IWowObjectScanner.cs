using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement
{
    public interface IWowObjectScanner : ICycleable
    {
        (WoWObject, IWAQTask) ActiveWoWObject { get; }
        void AddToDictionary(int entry, IWAQTask task);
        void RemoveFromDictionary(int entry, IWAQTask task);
        IWAQTask GetTaskMatchingWithObject(WoWObject closestObject);
    }
}

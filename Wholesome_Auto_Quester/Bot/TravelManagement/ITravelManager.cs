using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;

namespace Wholesome_Auto_Quester.Bot.TravelManagement
{
    public interface ITravelManager : ICycleable
    {
        bool ShouldTravel { get; }
        bool InLoadingScreen { get; }

        bool IsTravelRequired(IWAQTask task);
        void ResetTravel();
        void ResetLoadingScreenLock();
    }
}

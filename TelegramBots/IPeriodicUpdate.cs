using System.Threading.Tasks;

namespace TelegramBots
{
    public interface IPeriodicUpdate
    {
        Task PeriodicUpdate();
    }
}

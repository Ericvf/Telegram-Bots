using System.Collections.Generic;
using System.Threading.Tasks;

namespace TelegramBots.Core
{
    public interface IStateManager<T>
        where T : IChatState
    {
        Task<T> HandleState(long chatId, string chatName, string botName, string message);

        bool HasState(long chatId);

        void LoadStates();

        Task SaveStates();

        IEnumerable<T> GetStates();
    }
}

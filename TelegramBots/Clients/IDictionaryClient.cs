using System.Collections.Generic;
using System.Threading.Tasks;

namespace TelegramBots.Clients
{
    public interface IDictionaryClient
    {
        Task<IEnumerable<string>> GetDefinitions(string input);
    }
}
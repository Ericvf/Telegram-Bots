using System.IO;
using System.Threading.Tasks;

namespace TelegramBots
{
    public interface ITelegramApi
    {
        string ApiKey { get; }

        Task<long> SendTextMessageAsync(long chatId, string message);

        Task<long> SendMarkdownMessageAsync(long chatId, string message, bool disableNotification);

        Task DeleteMessageAsync(long chatId, long messageId);

        Task EditMessageAsync(long chatId, long messageId, string message);

        Task SendVideoAsync(long chatId, string url, string message);

        Task ForwardMessageAsync(long chatId, long id, long messageId);

        Task<long> SendStreamAsync(long chatId, Stream stream, string name, string message);
    }
}

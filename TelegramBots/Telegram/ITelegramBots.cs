using System.Threading.Tasks;

namespace TelegramBots
{
    public interface ITelegramBots
    {
        Task HandleMessage(TelegramMessage telegramMessage);
    }
}

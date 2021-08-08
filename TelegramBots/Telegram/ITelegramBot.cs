using System.Threading.Tasks;

namespace TelegramBots
{
    public interface ITelegramBot
    {
        Task HandleMessage(TelegramMessage telegramMessage);

        string Name { get; }

        string ShortName { get; }

        void Help(long chatId);
    }

}

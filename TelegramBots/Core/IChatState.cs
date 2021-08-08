namespace TelegramBots.Core
{
    public interface IChatState
    {
        long ChatId { get; set; }

        string Name { get; set; }
    }
}

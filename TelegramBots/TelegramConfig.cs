namespace TelegramBots
{
    public class TelegramConfig : ITelegramConfig
    {
        public string Key { get; set; }
    }

    public interface ITelegramConfig
    {
        public string Key { get; }
    }
}
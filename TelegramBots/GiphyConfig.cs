namespace TelegramBots
{
    public class GiphyConfig : IGiphyConfig
    {
        public string Key { get; set; }
    }

    public interface IGiphyConfig
    {
        public string Key { get; }
    }
}
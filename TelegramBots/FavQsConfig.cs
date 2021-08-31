namespace TelegramBots
{
    public class FavQsConfig : IFavQsConfig
    {
        public string Key { get; set; }
    }

    public interface IFavQsConfig
    {
        public string Key { get; }
    }
}
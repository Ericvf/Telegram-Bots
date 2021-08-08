namespace TelegramBots
{
    public class FavQsConfig : IFavQsConfig
    {
        public string Api { get; set; }
    }

    public interface IFavQsConfig
    {
        public string Api { get; }
    }
}
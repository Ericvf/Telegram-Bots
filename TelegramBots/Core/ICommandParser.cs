namespace TelegramBots.Core
{
    public interface ICommandParser
    {
        Command Parse(string botName, string message);
    }
}

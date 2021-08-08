using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TelegramBots.Core
{
    public class CommandParser : ICommandParser
    {
        public Command Parse(string botName, string message)
        {
            if (!message.StartsWith("/" + botName))
                return null;

            var matches = Regex.Split(message, "\\s");
            if (matches.Length <= 1)
            {
                return new Command
                {
                    FirstWord = message,
                    Text = message,
                    List = new List<string>()
                };
            }
            return new Command
            {
                FirstWord = matches[1],
                Text = message.Substring(botName.Length + 1),
                List = matches.Skip(1)
            };
        }
    }
}

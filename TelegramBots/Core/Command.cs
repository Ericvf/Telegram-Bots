using System.Collections.Generic;

namespace TelegramBots.Core
{
    public class Command
    {
        public string FirstWord { get; set; }

        public string Text { get; set; }

        public IEnumerable<string> List { get; set; }
    }
}

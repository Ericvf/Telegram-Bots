using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBots.Core;

namespace TelegramBots
{
    public class BitbucketBot : ITelegramBot, IBitbucketBot
    {
        private readonly IStateManager<BitbucketBotState> stateManager;
        private readonly ICommandParser commandParser;
        private readonly ITelegramApi telegramApi;

        public BitbucketBot(ITelegramApi telegramApi, ICommandParser commandParser, IStateManager<BitbucketBotState> stateManager)
        {
            this.telegramApi = telegramApi;
            this.commandParser = commandParser;
            this.stateManager = stateManager;
        }

        public string Name => nameof(BitbucketBot);

        public string ShortName => "bb";

        public async Task HandleMessage(TelegramMessage telegramMessage)
        {
            var chatId = telegramMessage.ChatId;
            var message = telegramMessage.Text;
            var botName = ShortName ?? Name;

            var chatState = await stateManager.HandleState(chatId, telegramMessage.ChatTitle, botName, message);
            if (chatState == null)
                return;

            var commandValues = commandParser.Parse(botName, message);
            if (commandValues?.FirstWord != null)
            {
                var cmd = commandValues.FirstWord;
                var value = commandValues?.Text;
                string response = null;

                switch (cmd)
                {
                    case "addrepo":
                        if (!chatState.Repositories.Contains(value))
                        {
                            chatState.Repositories.Add(value);
                            response = $"Repository {value} was added";
                        }

                        stateManager.SaveStates();
                        break;

                    case "delrepo":
                        if (chatState.Repositories.Contains(value))
                        {
                            chatState.Repositories.Remove(value);
                            response = $"Repository {value} was removed";
                        }

                        stateManager.SaveStates();
                        break;
                }

                if (!string.IsNullOrEmpty(response))
                    await telegramApi.SendTextMessageAsync(chatId, response);
            }
        }

        public async Task HandlePushWebhook(PushMessage pushMessage)
        {
            if (!stateManager.GetStates().Any())
                return;

            var totalCommitCount = pushMessage.push.changes.Sum(c => c.commits.Length);

            string response = $"{pushMessage.actor} pushed {totalCommitCount} commits. \r\n\r\n";
            foreach (var change in pushMessage.push.changes)
            {
                foreach (var commit in change.commits)
                {
                    response += $"{commit.hash} ({commit.author}): {commit.message}\r\n";
                }
            }

            foreach (var state in stateManager.GetStates())
            {
                if (state.Repositories.Contains(pushMessage.repository))
                {
                    await telegramApi.SendTextMessageAsync(state.ChatId, response);
                }
            }
        }

        public async void Help(long chatId)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Help for bot /{Name}:");
            stringBuilder.AppendLine($"{Name} displays Bitbucket updates");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"{nameof(BitbucketBotState.Repositories)}: e.g. /{ShortName ?? Name} true");
            stringBuilder.AppendLine($"Enables or disables updates for the given repository.");
            stringBuilder.AppendLine();

            await telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }
    }

    public class BitbucketBotState : IChatState
    {
        public long ChatId { get; set; }

        public string Name { get; set; }

        public List<string> Repositories { get; set; } = new List<string>();

        public override string ToString()
        {
            return Name + " - Repositories: " + string.Join(", ", Repositories.ToArray());
        }
    }
}

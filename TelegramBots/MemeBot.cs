using TelegramBots.Clients;
using TelegramBots.Core;
using System.Text;
using System.Threading.Tasks;
using System;

namespace TelegramBots
{
    public class MemeBot : ITelegramBot
    {
        private readonly IStateManager<MemeBotState> stateManager;
        private readonly ICommandParser commandParser;
        private readonly ITelegramApi telegramApi;
        private readonly IGiphyClient giphyClient;

        public MemeBot(ITelegramApi telegramApi, IGiphyClient httpClient, ICommandParser commandParser, IStateManager<MemeBotState> stateManager)
        {
            this.telegramApi = telegramApi;
            this.giphyClient = httpClient;
            this.commandParser = commandParser;
            this.stateManager = stateManager;
        }

        public string Name => nameof(MemeBot);

        public string ShortName => "mb";

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
                var searchText = commandValues.Text;
                await FindAndSendImage(chatId, searchText, telegramMessage.From + ": " + searchText);
                await telegramApi.DeleteMessageAsync(chatId, telegramMessage.MessageId);
            }
        }

        private async Task FindAndSendImage(long chatId, string search, string caption)
        {
            string url = null;

            try
            {
                url = await giphyClient.SearchGifyImage(search);
            }
            catch (Exception ex)
            {
                await Task.Delay(500);
                await telegramApi.SendTextMessageAsync(chatId, $"Exception: {ex.Message} for URL:\r\n{url}");
            }

            if (!string.IsNullOrEmpty(url))
            {
                await telegramApi.SendVideoAsync(chatId, url, caption);
            }
            else
            {
                await telegramApi.SendTextMessageAsync(chatId, $"No images found for query: {search}");
            }
        }

        public async void Help(long chatId)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Help for bot /{Name}:");
            stringBuilder.AppendLine($"{Name} searches for gifs based on your queries");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Query: e.g. /{ShortName ?? Name} [search text]");
            stringBuilder.AppendLine($"Searches and responds to the conversation with a gif image.");
            stringBuilder.AppendLine();

            await telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }
    }

    public class MemeBotState : IChatState
    {
        public long ChatId { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}

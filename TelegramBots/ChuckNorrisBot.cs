using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TelegramBots.Clients;
using TelegramBots.Core;

namespace TelegramBots
{
    public class ChuckNorrisBot : ITelegramBot
    {
        private readonly ITelegramApi telegramApi;
        private readonly IChuckNorrisClient chuckNorrisClient;
        private readonly ICommandParser commandParser;
        private readonly IStateManager<ChuckNorrisBotState> stateManager;

        public ChuckNorrisBot(ITelegramApi telegramApi, ICommandParser commandParser, IStateManager<ChuckNorrisBotState> stateManager, IChuckNorrisClient _chuckNorrisClient)
        {
            this.telegramApi = telegramApi;
            this.chuckNorrisClient = _chuckNorrisClient;
            this.commandParser = commandParser;
            this.stateManager = stateManager;
        }


        public string Name => nameof(ChuckNorrisBot);

        public string ShortName => "cn";

        public async Task HandleMessage(TelegramMessage telegramMessage)
        {
            var chatId = telegramMessage.ChatId;
            var message = telegramMessage.Text;
            var botName = ShortName ?? Name;

            var chatState = await stateManager.HandleState(chatId, telegramMessage.ChatTitle, botName, telegramMessage.Text);
            if (chatState == null)
                return;

            var commandValues = commandParser.Parse(botName, message);
            var cmd = commandValues?.FirstWord;
            switch (cmd)
            {
                case "clear":
                case "reset":
                    chatState.AllUsers.Clear();
                    chatState.LastDate = DateTime.Today;
                    var response = $"{Name} was reset";
                    await telegramApi.SendTextMessageAsync(chatId, response);
                    await stateManager.SaveStates();
                    break;
            }

            if (DateTime.Today > chatState.LastDate.Date)
            {
                chatState.AllUsers.Clear();
                chatState.LastDate = DateTime.Today;
                await stateManager.SaveStates();
            }

            var userId = telegramMessage.UserId;
            if (!chatState.AllUsers.Contains(userId))
            {
                chatState.AllUsers.Add(userId);
                var quote = await chuckNorrisClient.GetRandomQuote();
                await telegramApi.SendTextMessageAsync(chatId, $"Hello {telegramMessage.From}. " + quote);
                await stateManager.SaveStates();
            }
        }

        public async void Help(long chatId)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Help for bot /{Name}:");
            stringBuilder.AppendLine($"{Name} brings a RaboCop fact for each user in the chat, daily.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Clear: e.g. /{ShortName ?? Name} clear");
            stringBuilder.AppendLine($"Clears the state of quotes for today.");
            stringBuilder.AppendLine();

            await telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }
    }


    public class ChuckNorrisBotState : IChatState
    {
        public long ChatId { get; set; }

        public string Name { get; set; }

        public DateTime LastDate { get; set; } = DateTime.Now.AddDays(-1);

        public List<int> AllUsers { get; set; } = new List<int>();

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Name + " - " + LastDate.ToShortDateString());
            stringBuilder.AppendLine($"Number of users done today: {AllUsers.Count}");

            return stringBuilder.ToString();
        }
    }
}

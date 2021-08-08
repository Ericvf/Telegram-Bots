using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TelegramBots.Clients;
using TelegramBots.Core;

namespace TelegramBots
{
    public class ArtOfWarBot : ITelegramBot
    {
        private readonly ITelegramApi _telegramApi;
        private readonly IArtOfWarClient _artOfWarClient;
        private readonly ICommandParser _commandParser;
        private readonly IStateManager<ArtOfWarBotState> _stateManager;

        public ArtOfWarBot(
            ITelegramApi telegramApi,
            ICommandParser commandParser,
            IStateManager<ArtOfWarBotState> stateManager,
            IArtOfWarClient artOfWarClient)
        {
            this._telegramApi = telegramApi;
            this._artOfWarClient = artOfWarClient;
            this._commandParser = commandParser;
            this._stateManager = stateManager;
        }


        public string Name => nameof(ArtOfWarBot);

        public string ShortName => "aow";

        public async Task HandleMessage(TelegramMessage telegramMessage)
        {
            var chatId = telegramMessage.ChatId;
            var message = telegramMessage.Text;
            var botName = ShortName ?? Name;

            var chatState = await _stateManager.HandleState(chatId, telegramMessage.ChatTitle, botName, telegramMessage.Text);
            if (chatState == null)
                return;

            var commandValues = _commandParser.Parse(botName, message);

            var cmd = commandValues?.FirstWord;
            switch (cmd)
            {
                case "clear":
                case "reset":
                    chatState.AllUsers.Clear();
                    chatState.LastDate = DateTime.Today;
                    var response = $"{Name} was reset";
                    await _telegramApi.SendTextMessageAsync(chatId, response);
                    await _stateManager.SaveStates();
                    break;
            }

            if (DateTime.Today > chatState.LastDate.Date)
            {
                chatState.AllUsers.Clear();
                chatState.LastDate = DateTime.Today;
                await _stateManager.SaveStates();
            }

            var userId = telegramMessage.UserId;

            if (!chatState.AllUsers.Contains(userId))
            {
                chatState.AllUsers.Add(userId);

                var quote = await _artOfWarClient.GetRandomQuote();

                await _telegramApi.SendTextMessageAsync(chatId, $"Hello {telegramMessage.From}. " + quote);

                await _stateManager.SaveStates();
            }
        }

        public async void Help(long chatId)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Help for bot /{Name}:");
            stringBuilder.AppendLine($"{Name} brings a Art Of War fact for each user in the chat, daily.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Clear: e.g. /{ShortName ?? Name} clear");
            stringBuilder.AppendLine($"Clears the state of quotes for today.");
            stringBuilder.AppendLine();

            await _telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }
    }


    public class ArtOfWarBotState : IChatState
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

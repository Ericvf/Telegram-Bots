using System;
using System.Text;
using System.Threading.Tasks;
using TelegramBots.Clients;
using TelegramBots.Core;

namespace TelegramBots
{
    public class TipOfADayBot : ITelegramBot
    {
        private readonly ITelegramApi telegramApi;
        private readonly ITipOfADayClient tipOfADayClient;
        private readonly ICommandParser commandParser;
        private readonly IStateManager<TipOfADayBotState> stateManager;

        public TipOfADayBot(ITelegramApi telegramApi, ICommandParser commandParser, IStateManager<TipOfADayBotState> stateManager, ITipOfADayClient tipOfADayClient)
        {
            this.telegramApi = telegramApi;
            this.tipOfADayClient = tipOfADayClient;
            this.commandParser = commandParser;
            this.stateManager = stateManager;
        }

        public string Name => nameof(TipOfADayBot);

        public string ShortName => "td";

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
                case "show":
                    if (string.IsNullOrEmpty(chatState.Tip))
                    {
                        await GetAndSendNewTip(telegramMessage, chatId, chatState);
                    }
                    else
                    {
                        await telegramApi.SendTextMessageAsync(chatId, $"Hello {telegramMessage.From}. " + chatState.Tip);
                    }
                    break;
                case "clear":
                case "reset":
                    chatState.Tip = string.Empty;
                    chatState.LastDate = DateTime.Today;
                    var response = $"{Name} was reset";
                    await telegramApi.SendTextMessageAsync(chatId, response);
                    await stateManager.SaveStates();
                    break;
            }

            if (DateTime.Today > chatState.LastDate.Date)
            {
                chatState.Tip = string.Empty;
                chatState.LastDate = DateTime.Today;
                await stateManager.SaveStates();
            }

            if (string.IsNullOrEmpty(chatState.Tip))
            {
                await GetAndSendNewTip(telegramMessage, chatId, chatState);
            }
        }

        private async Task GetAndSendNewTip(TelegramMessage telegramMessage, long chatId, TipOfADayBotState chatState)
        {
            var tip = await tipOfADayClient.GetTip();
            chatState.Tip = tip;
            await telegramApi.SendTextMessageAsync(chatId, $"Hello {telegramMessage.From}. " + tip);
            await stateManager.SaveStates();
        }

        public async void Help(long chatId)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Help for bot /{Name}:");
            stringBuilder.AppendLine($"{Name} brings a tip of a day from pragprog.com once a day.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Show: e.g. /{ShortName ?? Name} show");
            stringBuilder.AppendLine($"Shows the tip of today.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Clear: e.g. /{ShortName ?? Name} clear");
            stringBuilder.AppendLine($"Clears the tip for today.");
            stringBuilder.AppendLine();

            await telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }
    }


    public class TipOfADayBotState : IChatState
    {
        public long ChatId { get; set; }

        public string Name { get; set; }

        public DateTime LastDate { get; set; } = DateTime.Now.AddDays(-1);

        public string Tip { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Name + " - " + LastDate.ToShortDateString());
            stringBuilder.AppendLine($"Tip of today: {Tip}");

            return stringBuilder.ToString();
        }
    }
}

using System;
using System.Text;
using System.Threading.Tasks;
using TelegramBots.Core;

namespace TelegramBots
{
    public class TemperatureBot : ITelegramBot, ITemperatureBot
    {
        private readonly IStateManager<TemperatureBotState> stateManager;
        private readonly ICommandParser commandParser;
        private readonly ITelegramApi telegramApi;

        private DateTime temperatureTime = DateTime.Now;
        private float temperature = 0f;

        public TemperatureBot(ITelegramApi telegramApi, ICommandParser commandParser, IStateManager<TemperatureBotState> stateManager)
        {
            this.telegramApi = telegramApi;
            this.commandParser = commandParser;
            this.stateManager = stateManager;
        }

        public string Name => nameof(TemperatureBot);

        public string ShortName => "tm";

        public async Task HandleMessage(TelegramMessage telegramMessage)
        {
            var chatId = telegramMessage.ChatId;
            var message = telegramMessage.Text;
            var botName = ShortName ?? Name;

            var chatState = await stateManager.HandleState(chatId, telegramMessage.ChatTitle, botName, message);
            if (chatState == null)
                return;
            
            var talksAboutTemperature = message.IndexOf("temperature", StringComparison.OrdinalIgnoreCase) >= 0;
            if (talksAboutTemperature)
            {
                await SendTemperature(chatId);
                return;
            }

            var commandValues = commandParser.Parse(botName, message);
            var cmd = commandValues?.FirstWord;
            switch (cmd)
            {
                case "list":
                    await List(chatId);
                    break;

                case "show":
                    await SendTemperature(chatId);
                    break;
            }
        }

        private Task List(long chatId)
        {
            var stringBuilder = new StringBuilder();
            foreach (var state in stateManager.GetStates())
            {
                stringBuilder.AppendLine($"Conversation: " + state.Name);
                //stringBuilder.AppendLine(state.ToString());
                stringBuilder.AppendLine();
            }

            return telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }

        private Task SendTemperature(long chatId)
        {
            return telegramApi.SendTextMessageAsync(chatId, $"Temp: {temperature}C at {temperatureTime.ToString("M/dd/yyyy HH:mm:ss")}");
        }

        public async void Help(long chatId)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Help for bot /{Name}:");
            stringBuilder.AppendLine($"{Name} gives you temperature information");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Query: e.g. /{ShortName ?? Name}");
            stringBuilder.AppendLine($"Responds with the current temperature");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"List: e.g. /{ShortName ?? Name} list");
            stringBuilder.AppendLine($"Shows a list of all the conversations and states");
            stringBuilder.AppendLine();

            await telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }

        public void SetTemperature(float temperature)
        {
            this.temperatureTime = DateTime.Now;
            this.temperature = temperature;
        }
    }

    public interface ITemperatureBot : ITelegramBot
    {
        void SetTemperature(float temperature);
    }

    public class TemperatureBotState : IChatState
    {
        public long ChatId { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}

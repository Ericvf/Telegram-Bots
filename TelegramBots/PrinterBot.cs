using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TelegramBots.Core;

namespace TelegramBots
{
    public class PrinterBotState : IChatState
    {
        public long ChatId { get; set; }

        public string Name { get; set; }

        public string IP { get; set; }

        public string PrinterName { get; set; }

        public long? MessageId { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Name: {PrinterName}");
            stringBuilder.AppendLine($"IP Address: {IP}");

            return stringBuilder.ToString();
        }
    }

    public class PrinterBot : ITelegramBot
    {
        private static readonly HttpClient httpClient = new HttpClient();

        private readonly IStateManager<PrinterBotState> stateManager;
        private readonly ICommandParser commandParser;
        private readonly ITelegramApi telegramApi;

        public PrinterBot(IStateManager<PrinterBotState> stateManager, ICommandParser commandParser, ITelegramApi telegramApi)
        {
            this.stateManager = stateManager;
            this.commandParser = commandParser;
            this.telegramApi = telegramApi;
        }

        public string Name => nameof(PrinterBotState);

        public string ShortName => "pb";

        public async Task HandleMessage(TelegramMessage telegramMessage)
        {
            var chatId = telegramMessage.ChatId;
            var message = telegramMessage.Text;

            var chatState = await stateManager.HandleState(chatId, telegramMessage.ChatTitle, ShortName ?? Name, telegramMessage.Text);
            if (chatState == null)
                return;

            var commandValues = commandParser.Parse(ShortName ?? Name, message);
            var cmd = commandValues?.FirstWord;

            switch (cmd)
            {
                case "set":
                    await SetCommand(chatState, commandValues);
                    chatState.MessageId = null;
                    break;

                case "list":
                    await ListCommand(chatState);
                    chatState.MessageId = null;
                    break;

                case "show":
                    chatState.MessageId = await ShowCommand(chatState);
                    break;
            }
        }

        private async Task ListCommand(PrinterBotState chatState)
        {
            await telegramApi.SendTextMessageAsync(chatState.ChatId, chatState.ToString());
        }

        private async Task<long?> ShowCommand(PrinterBotState chatState)
        {
            if (string.IsNullOrEmpty(chatState.PrinterName))
            {
                await telegramApi.SendTextMessageAsync(chatState.ChatId, "No printer name set. Use the set command the set a name for the printer");
                return default(long?);
            }

            if (string.IsNullOrEmpty(chatState.IP))
            {
                await telegramApi.SendTextMessageAsync(chatState.ChatId, "No ip address set. Use the set command the set an ip address for the printer");
                return default(long?);
            }

            try
            {
                if (chatState.MessageId.HasValue)
                    await telegramApi.DeleteMessageAsync(chatState.ChatId, chatState.MessageId.Value);

                using (var response = await httpClient.GetAsync($"http://{chatState.IP}/image.jpg"))
                {
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        return await telegramApi.SendStreamAsync(chatState.ChatId, responseStream, chatState.PrinterName + ".jpg", $"{chatState.PrinterName} ({chatState.IP}) @ {DateTime.Now.ToShortTimeString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                await telegramApi.SendTextMessageAsync(chatState.ChatId, $"Exception: {ex.Message}");
                return default(long?);
            }
        }

        private async Task SetCommand(PrinterBotState chatState, Command command)
        {
            var secondWord = command.List.Skip(1).Take(1).SingleOrDefault();
            var value = string.Join(" ", command.List.Skip(2)).Trim();

            switch (secondWord)
            {
                case "name":
                    chatState.PrinterName = value;
                    await telegramApi.SendTextMessageAsync(chatState.ChatId, $"Set {secondWord} to `{value}`");
                    await stateManager.SaveStates();
                    break;

                case "ip":
                    chatState.IP = value;
                    await telegramApi.SendTextMessageAsync(chatState.ChatId, $"Set {secondWord} to `{value}`");
                    await stateManager.SaveStates();
                    break;
            }
        }

        public async void Help(long chatId)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Help for bot /{Name}:");
            stringBuilder.AppendLine($"{Name} helps you monitor your 3d printer.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Set: e.g. /{ShortName ?? Name} ip Office");
            stringBuilder.AppendLine($"Sets printer properties printer");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Show: e.g. /{ShortName ?? Name} show");
            stringBuilder.AppendLine($"Shows an image of the printer");
            stringBuilder.AppendLine();

            await telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }
    }
}

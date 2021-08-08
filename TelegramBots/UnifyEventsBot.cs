using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBots.Clients;
using TelegramBots.Core;

namespace TelegramBots
{
    public class UnifyEventsBotState : IChatState
    {
        public long ChatId { get; set; }

        public string Name { get; set; }

        public string Controller { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        [JsonIgnore]
        public DateTime LastUpdateNotification { get; set; } = DateTime.MinValue;

        public DateTime LastEventDate { get; set; } = DateTime.MinValue;

        public int Interval { get; set; } = 30;

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{nameof(Name)}: {Name}");
            stringBuilder.AppendLine($"{nameof(Controller)}: {Controller}");
            stringBuilder.AppendLine($"{nameof(UserName)}: {UserName}");
            stringBuilder.AppendLine($"{nameof(Interval)}: {Interval}");

            return stringBuilder.ToString();
        }
    }

    public class UnifyEventsBot : ITelegramBot, IPeriodicUpdate
    {
        private readonly IStateManager<UnifyEventsBotState> stateManager;
        private readonly ICommandParser commandParser;
        private readonly ITelegramApi telegramApi;
        private readonly IUnifyClient unifyClient;

        public UnifyEventsBot(IStateManager<UnifyEventsBotState> stateManager, ICommandParser commandParser, ITelegramApi telegramApi, IUnifyClient unifyClient)
        {
            this.stateManager = stateManager;
            this.commandParser = commandParser;
            this.telegramApi = telegramApi;
            this.unifyClient = unifyClient;
        }

        public string Name => nameof(UnifyEventsBotState);

        public string ShortName => "uie";

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
                    break;

                case "list":
                    await ListCommand(chatState);
                    break;

                case "show":
                    await ShowCommand(chatState);
                    break;
            }
        }

        private async Task ListCommand(UnifyEventsBotState chatState)
        {
            await telegramApi.SendTextMessageAsync(chatState.ChatId, chatState.ToString());
        }

        private async Task<long?> ShowCommand(UnifyEventsBotState chatState)
        {
            if (string.IsNullOrEmpty(chatState.Name))
            {
                await telegramApi.SendTextMessageAsync(chatState.ChatId, "No printer name set. Use the set command the set a name for the printer");
                return default;
            }

            return default;
        }

        private async Task SetCommand(UnifyEventsBotState chatState, Command command)
        {
            var secondWord = command.List.Skip(1).Take(1).SingleOrDefault();
            var value = string.Join(" ", command.List.Skip(2)).Trim();

            switch (secondWord)
            {
                case "controller":
                    chatState.Controller = value;
                    break;

                case "username":
                    chatState.UserName = value;
                    break;

                case "password":
                    chatState.Password = value;
                    break;

                default:
                    return;
            }

            await telegramApi.SendTextMessageAsync(chatState.ChatId, $"Set {secondWord} to `{value}`");
            await stateManager.SaveStates();
        }

        public async void Help(long chatId)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Help for bot /{Name}:");
            stringBuilder.AppendLine($"{Name} sends you Unify Controller events.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Set: e.g. /{ShortName ?? Name} set controller 192.168.1.12:8443");
            stringBuilder.AppendLine($"Sets the controller. Any errors while connecting to the controller will be shown.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Set: e.g. /{ShortName ?? Name} set username admin");
            stringBuilder.AppendLine($"Sets the username.");


            stringBuilder.AppendLine($"Set: e.g. /{ShortName ?? Name} set password P4$$123!");
            stringBuilder.AppendLine($"Sets the password.");
            stringBuilder.AppendLine();

            await telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }

        public async Task PeriodicUpdate()
        {
            var currentTime = DateTime.Now;
            var hasSaved = false;

            foreach (var chatState in stateManager.GetStates())
            {
                var elapsedTime = currentTime - chatState.LastUpdateNotification;
                if (elapsedTime.TotalSeconds <= chatState.Interval)
                {
                    continue;
                }

                hasSaved = await GetAndSendEvents(currentTime, chatState);
            }

            if (hasSaved)
            {
                await stateManager.SaveStates();
            }
        }

        private readonly Dictionary<string, string> unifiEventNames =
            new Dictionary<string, string>() {

                {  "EVT_WU_Connected", "🔵 {0}" },
                {  "EVT_LU_Connected", "🔵 {0}" },
                {  "EVT_WU_Disconnected", "🔴 {0}" },
                {  "EVT_LU_Disconnected", "🔴 {0}" },
            };

        private async Task<bool> GetAndSendEvents(DateTime currentTime, UnifyEventsBotState chatState)
        {
            chatState.LastUpdateNotification = currentTime;

            try
            {
                var allEvents = await unifyClient.GetEvents(chatState.Controller, chatState.UserName, chatState.Password, 25);

                if (allEvents?.Any() != null)
                {
                    var previousLastEventDate = chatState.LastEventDate;
                    chatState.LastEventDate = allEvents.Max(e => e.Date);

                    var list = unifiEventNames.Keys;

                    var allNewEvents = allEvents
                        .Where(e => e.Date > previousLastEventDate)
                        .Where(d => unifiEventNames.Keys.Contains(d.Key, StringComparer.OrdinalIgnoreCase))
                        .OrderBy(d => d.Date)
                        .ToArray();

                    var message = new StringBuilder();

                    foreach (var newEvent in allNewEvents)
                    {
                        var formattedMessage = string.Format(unifiEventNames[newEvent.Key], newEvent.Message);
                        message.AppendLine(formattedMessage);
                        message.AppendLine();

                        Console.WriteLine($"[{DateTime.Now}] {newEvent.Message}");
                    }

                    if (message.Length > 0)
                    {
                        await telegramApi.SendMarkdownMessageAsync(chatState.ChatId, message.ToString(), true);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"* Exception " + ex.Message);
                await telegramApi.SendTextMessageAsync(chatState.ChatId, ex.ToString());
            }

            return false;
        }
    }
}

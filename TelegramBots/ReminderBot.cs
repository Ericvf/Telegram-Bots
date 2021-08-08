using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TelegramBots.Core;

namespace TelegramBots
{
    public class ReminderBotState : IChatState
    {
        public class Reminder
        {
            public DateTime DateTime { get; set; }

            public string Message { get; set; }
        }

        public List<Reminder> Reminders { get; } = new List<Reminder>();

        public long ChatId { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{Name}: {Reminders.Count}");
            foreach (var reminder in Reminders)
            {
                stringBuilder.AppendLine($"{reminder.DateTime.ToString("M/dd/yyyy HH:mm:ss")}: {reminder.Message}");
            }

            return stringBuilder.ToString();
        }
    }

    public class ReminderBot : ITelegramBot, IPeriodicUpdate
    {
        private readonly IStateManager<ReminderBotState> stateManager;
        private readonly ICommandParser commandParser;
        private readonly ITelegramApi telegramApi;

        public ReminderBot(IStateManager<ReminderBotState> stateManager, ICommandParser commandParser, ITelegramApi telegramApi)
        {
            this.stateManager = stateManager;
            this.commandParser = commandParser;
            this.telegramApi = telegramApi;
        }

        public string Name => nameof(ReminderBot);

        public string ShortName => "rb";

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
                case "clear":
                    chatState.Reminders.Clear();
                    await telegramApi.SendTextMessageAsync(chatId, "😼 Cleared");
                    break;

                case "list":
                    var response = GetList(chatState, cmd);
                    if (!string.IsNullOrEmpty(response))
                        await telegramApi.SendTextMessageAsync(chatId, response);
                    break;
            }

            if (commandValues != null)
                await ParseItems(chatState, chatId, commandValues, telegramMessage);
        }

        private string GetList(ReminderBotState chatState, string cmdName)
        {
            var stringBuilder = new StringBuilder();
            int i = 1;

            foreach (var item in chatState.Reminders)
            {
                stringBuilder.AppendLine($"🕓 Reminder {i}: {item}\r\n");
                i++;
            }

            return stringBuilder.ToString();
        }

        private async Task ParseItems(ReminderBotState chatState, long chatId, Command commandValues, TelegramMessage telegramMessage)
        {
            if (DateTime.TryParse(commandValues?.FirstWord, out DateTime parsedReminder))
            {
                if (parsedReminder < DateTime.Now)
                {
                    parsedReminder += TimeSpan.FromDays(1);
                }

                await AddReminder(chatState, chatId, commandValues, telegramMessage, parsedReminder);
            }

            var match = Regex.Match(commandValues?.FirstWord, @"([0-9]+)(s|m|h|u|d)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var durationValue = int.Parse(match.Groups[1].Value);
                var unitValue = match.Groups[2].Value;

                var matchReminder = DateTime.Now;
                switch (unitValue)
                {
                    case "s": matchReminder = matchReminder.AddSeconds(durationValue); break;
                    case "m": matchReminder = matchReminder.AddMinutes(durationValue); break;
                    case "h":
                    case "u": matchReminder = matchReminder.AddHours(durationValue); break;
                    case "d": matchReminder = matchReminder.AddDays(durationValue); break;
                }

                await AddReminder(chatState, chatId, commandValues, telegramMessage, matchReminder);
            }
        }

        private async Task AddReminder(ReminderBotState chatState, long chatId, Command commandValues, TelegramMessage telegramMessage, DateTime reminder)
        {
            chatState.Reminders.Add(new ReminderBotState.Reminder()
            {
                DateTime = reminder,
                Message = telegramMessage.Text.Replace($"/{ShortName} {commandValues.FirstWord}", string.Empty)
            });

            var response = $"🕓 Added reminder on {reminder.ToString("M/dd/yyyy HH:mm:ss")}";
            await telegramApi.SendTextMessageAsync(chatId, response);

            await stateManager.SaveStates();
        }

        public async void Help(long chatId)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Help for bot /{Name}:");
            stringBuilder.AppendLine($"{Name} reminds your forgetful brain stuff.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Add: e.g. /{ShortName ?? Name} 11:45 Lunchtime!!!");
            stringBuilder.AppendLine($"Adds a reminder for a specific time. Pattern: Parse()");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Add: e.g. /{ShortName ?? Name} 24h 24 hours have passed");
            stringBuilder.AppendLine($"Adds a reminder for a duration. Pattern: ([0-9]+)(s|m|h|d)");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Status: e.g. /{ShortName ?? Name} status");
            stringBuilder.AppendLine($"Gives status info about the content and number of reminders.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"List: e.g. /{ShortName ?? Name} list");
            stringBuilder.AppendLine($"Shows a list of reminders.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Clear: e.g. /{ShortName ?? Name} clear");
            stringBuilder.AppendLine($"Removes all reminders for a conversation.");
            stringBuilder.AppendLine();
            await telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }

        public async Task PeriodicUpdate()
        {
            bool saveStates = false;

            foreach (var chatState in stateManager.GetStates())
            {
                var remindersToRemove = new List<ReminderBotState.Reminder>();
                foreach (var reminder in chatState.Reminders)
                {
                    if (reminder.DateTime < DateTime.Now)
                    {
                        await telegramApi.SendTextMessageAsync(chatState.ChatId, "🕓 " + reminder.Message);
                        remindersToRemove.Add(reminder);
                    }
                }

                foreach (var reminder in remindersToRemove)
                {
                    chatState.Reminders.Remove(reminder);
                    saveStates = true;
                }
            }

            if (saveStates)
            {
                await stateManager.SaveStates();
            }
        }
    }
}

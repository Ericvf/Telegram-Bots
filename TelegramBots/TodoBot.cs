using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TelegramBots.Core;

namespace TelegramBots
{
    public class TodoBotState : IChatState
    {
        public List<string> Todos { get; } = new List<string>();

        public List<string> PRs { get; } = new List<string>();

        public List<string> Reviewers { get; } = new List<string>();

        public DateTime LastUpdateNotification { get; set; } = DateTime.Now;

        public int Interval { get; set; } = 1;

        public long ChatId { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return Name + "\r\n" + $"TODO {Todos.Count} - PR {PRs.Count} \r\n";
        }
    }

    public class TodoBot : ITelegramBot, IPeriodicUpdate
    {
        private readonly IStateManager<TodoBotState> stateManager;
        private readonly ICommandParser commandParser;
        private readonly ITelegramApi telegramApi;

        public TodoBot(IStateManager<TodoBotState> stateManager, ICommandParser commandParser, ITelegramApi telegramApi)
        {
            this.stateManager = stateManager;
            this.commandParser = commandParser;
            this.telegramApi = telegramApi;
        }

        public string Name => nameof(TodoBot);

        public string ShortName => "tb";

        public async Task HandleMessage(TelegramMessage telegramMessage)
        {
            var chatId = telegramMessage.ChatId;
            var message = telegramMessage.Text;

            var chatState = await stateManager.HandleState(chatId, telegramMessage.ChatTitle, ShortName ?? Name, telegramMessage.Text);
            if (chatState == null)
                return;

            if (message.Equals($"/{ShortName}", StringComparison.OrdinalIgnoreCase)
               || message.Equals($"/{Name}", StringComparison.OrdinalIgnoreCase))
            {
                var response = GetList(chatState, "list");
                if (!string.IsNullOrEmpty(response))
                    await telegramApi.SendTextMessageAsync(chatId, response);
            }

            var commandValues = commandParser.Parse(ShortName ?? Name, message);
            var cmd = commandValues?.FirstWord;

            switch (cmd)
            {
                case "clear":
                    chatState.Todos.Clear();
                    chatState.PRs.Clear();
                    await telegramApi.SendTextMessageAsync(chatId, "😼 Cleared");
                    await stateManager.SaveStates();
                    break;

                case "todo":
                case "pr":
                case "list":
                    var response = GetList(chatState, cmd);
                    if (!string.IsNullOrEmpty(response))
                        await telegramApi.SendTextMessageAsync(chatId, response);
                    break;

                case "done":
                    if (commandValues.List.Count() > 1)
                    {
                        var value = commandValues?.List.ElementAt(1);
                        if (int.TryParse(value, out var intValue))
                            await RemoveItem(chatState, chatId, intValue);
                    }
                    break;

                case "assign":
                    var assignees = commandValues.List.Skip(1);

                    chatState.Reviewers.Clear();
                    chatState.Reviewers.AddRange(assignees);

                    chatState.LastUpdateNotification = DateTime.Now;

                    var reviewers = !assignees.Any() ? "Nobody" : string.Join(", ", assignees);
                    await telegramApi.SendTextMessageAsync(chatId, $"{reviewers} will take care of your PRs");
                    await stateManager .SaveStates();
                    break;

                case "interval":
                    var intervalInput = commandValues.List.Skip(1).Take(1).SingleOrDefault();

                    if (uint.TryParse(intervalInput, out uint interval))
                    {
                        chatState.Interval = (int)interval;

                        await telegramApi.SendTextMessageAsync(chatId, $"Interval updated to `{interval}` minute(s)");

                        await stateManager .SaveStates();
                    }

                    break;
            }

            await ParseItems(chatState, chatId, telegramMessage);
        }

        private async Task RemoveItem(TodoBotState chatState, long chatId, int itemIndex)
        {
            if (itemIndex <= chatState.Todos.Count)
            {
                var todo = chatState.Todos.ElementAt(itemIndex - 1);
                chatState.Todos.RemoveAt(itemIndex - 1);

                await telegramApi.SendTextMessageAsync(chatId, $"💡 TODO done:\r\n{todo} ❤️");
                await stateManager .SaveStates();
            }
            else if (itemIndex - chatState.Todos.Count - 1 < chatState.PRs.Count)
            {
                var pr = chatState.PRs.ElementAt(itemIndex - chatState.Todos.Count - 1);
                chatState.PRs.RemoveAt(itemIndex - chatState.Todos.Count - 1);

                await telegramApi.SendTextMessageAsync(chatId, $"✏️ PR done:\r\n{pr} ☕️");
                await stateManager.SaveStates();
            }
            else
            {
                await telegramApi.SendTextMessageAsync(chatId,
                    $"Invalid index {itemIndex}, try again 💩");
            }
        }

        private string GetList(TodoBotState chatState, string cmdName)
        {
            var stringBuilder = new StringBuilder();
            int i = 1;

            foreach (var item in chatState.Todos)
            {
                if (cmdName == "list" || cmdName == "todo")
                    stringBuilder.AppendLine($"💡 TODO {i}: {item}\r\n");

                i++;
            }

            foreach (var item in chatState.PRs)
            {
                if (cmdName == "list" || cmdName == "pr")
                    stringBuilder.AppendLine($"✏️ PR {i}: {item}\r\n");

                i++;
            }

            return stringBuilder.ToString();
        }

        private async Task ParseItems(TodoBotState chatState, long chatId, TelegramMessage telegramMessage)
        {
            var splitStrings = Regex.Split(telegramMessage.Text, @"(TODO|PR):", RegexOptions.IgnoreCase);
            var totalMatchesTodo = 0;
            var totalMatchesPR = 0;

            if (splitStrings.Length > 1)
            {
                var currentPRcount = chatState.PRs.Count;
                var currentTODOcount = chatState.Todos.Count;
                for (int i = 1; i < splitStrings.Length; i++)
                {
                    if (splitStrings[i - 1].Equals("PR", StringComparison.OrdinalIgnoreCase))
                    {
                        chatState.PRs.Add($"({telegramMessage.From}) " + splitStrings[i].Trim());
                        totalMatchesPR++;
                    }
                    else if (splitStrings[i - 1].Equals("TODO", StringComparison.OrdinalIgnoreCase))
                    {
                        chatState.Todos.Add($"({telegramMessage.From}) " + splitStrings[i].Trim());
                        totalMatchesTodo++;
                    }
                }

                await telegramApi.SendTextMessageAsync(chatId,
                    (totalMatchesTodo > 0 ? $"💡 TODO += {totalMatchesTodo} items. ({currentTODOcount + totalMatchesTodo}) 👍🏻 \r\n" : "\r\n")
                    + (totalMatchesPR > 0 ? $"✏️ PR += {totalMatchesPR} items. ({currentPRcount + totalMatchesPR}) 👍🏻" : ""));

                await stateManager.SaveStates();
            }
            else if (chatState.Reviewers.Any() && chatState.PRs.Any())
            {
                var currentUserInAssignees = chatState.Reviewers.Any(r => telegramMessage.From.Contains(r));
                if (currentUserInAssignees)
                {
                    await SendNotification(DateTime.Now, chatState);
                }
            }
        }

        public async void Help(long chatId)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Help for bot /{Name}:");
            stringBuilder.AppendLine($"{Name} keeps track of PR's and TODO's.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Status: e.g. /{ShortName ?? Name} status");
            stringBuilder.AppendLine($"Gives status info about the number of conversations and associated states.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"List: e.g. /{ShortName ?? Name} list");
            stringBuilder.AppendLine($"Shows a list of TODO's and PR's.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Todos: e.g. /{ShortName ?? Name} todo");
            stringBuilder.AppendLine($"Shows a list of TODO's and PR's.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"PRs: e.g. /{ShortName ?? Name} pr");
            stringBuilder.AppendLine($"Shows a list of TODO's and PR's.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Done: e.g. /{ShortName ?? Name} done [index]");
            stringBuilder.AppendLine($"Removes an item from the list.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Assign: e.g. /{ShortName ?? Name} assign [user]");
            stringBuilder.AppendLine($"Assigns a reviewer to your task list.");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Interval: e.g. /{ShortName ?? Name} interval [n]");
            stringBuilder.AppendLine($"Changes the notificaton interval to `n` minute(s)");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Clear: e.g. /{ShortName ?? Name} clear");
            stringBuilder.AppendLine($"Removes all items from the list.");
            stringBuilder.AppendLine();

            await telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }

        public async Task PeriodicUpdate()
        {
            var currentTime = DateTime.Now;
            foreach (var chatState in stateManager.GetStates())
            {
                if (!chatState.Reviewers.Any() || !chatState.PRs.Any())
                {
                    continue;
                }

                var elapsedTime = currentTime - chatState.LastUpdateNotification;
                if (elapsedTime.TotalMinutes <= chatState.Interval)
                {
                    continue;
                }

                await SendNotification(currentTime, chatState);
            }
        }

        private async Task SendNotification(DateTime currentTime, TodoBotState chatState)
        {
            chatState.LastUpdateNotification = currentTime;

            await telegramApi.SendTextMessageAsync(chatState.ChatId,
                $"We have {chatState.PRs.Count} outstanding PRs, get reviewing {string.Join(", ", chatState.Reviewers)}!");
        }
    }
}

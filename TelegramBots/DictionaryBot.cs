using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBots.Clients;
using TelegramBots.Core;

namespace TelegramBots
{
    public class DictionaryBot : ITelegramBot
    {
        private readonly IStateManager<DictionaryBotState> stateManager;
        private readonly IDictionaryClient datamuseClient;
        private readonly ICommandParser commandParser;
        private readonly ITelegramApi telegramApi;

        public DictionaryBot(ITelegramApi telegramApi, ICommandParser commandParser, IStateManager<DictionaryBotState> stateManager, IDictionaryClient datamuseClient)
        {
            this.telegramApi = telegramApi;
            this.commandParser = commandParser;
            this.stateManager = stateManager;
            this.datamuseClient = datamuseClient;
        }

        public string Name => nameof(DictionaryBot);

        public string ShortName => "dicbot";

        public async Task HandleMessage(TelegramMessage telegramMessage)
        {
            var chatId = telegramMessage.ChatId;
            var message = telegramMessage.Text;
            var botName = ShortName ?? Name;

            var chatState = await stateManager.HandleState(chatId, telegramMessage.ChatTitle, botName, message);
            if (chatState == null)
                return;

            var commandValues = commandParser.Parse(botName, message);

            var startsWithDefine = message.StartsWith("define", StringComparison.InvariantCultureIgnoreCase);

            if (!startsWithDefine && commandValues?.FirstWord == null)
                return;

            var cmd = commandValues?.FirstWord;
            switch (cmd)
            {
                case "list":
                    await List(chatId);
                    return;

                case "add":
                    var keywordToAdd = commandValues.List.Skip(1).Take(1).SingleOrDefault();
                    var definition = string.Join(" ", commandValues.List.Skip(2));
                    await AddDefinition(chatState, keywordToAdd, definition);
                    return;

                case "remove":
                    var keywordToRemove = commandValues.List.Skip(1).Take(1).SingleOrDefault();
                    await RemoveDefinition(chatState, keywordToRemove);
                    return;
            }

            var searchText = commandValues.Text.Trim();

            if (!string.IsNullOrEmpty(searchText))
            {
                await ResponseWithDefinition(chatState, searchText, telegramMessage.From + ": " + searchText);
            }
        }

        private async Task RemoveDefinition(DictionaryBotState chatState, string keyword)
        {
            var keywordPresent = chatState.Definitions.ContainsKey(keyword);
            if (keywordPresent)
            {
                chatState.Definitions.Remove(keyword);

                await telegramApi.SendTextMessageAsync(chatState.ChatId, $"Removed definition for `{keyword}`");
            }
        }

        private Task AddDefinition(DictionaryBotState chatState, string keyword, string definition)
        {
            var alreadyPresent = chatState.Definitions.ContainsKey(keyword);
            var action = alreadyPresent ? "Updated" : "Added";

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{action} definition for `{keyword}`");
            stringBuilder.AppendLine(definition);

            chatState.Definitions[keyword] = definition;
            stateManager.SaveStates();

            return telegramApi.SendTextMessageAsync(chatState.ChatId, stringBuilder.ToString());
        }

        private async Task ResponseWithDefinition(DictionaryBotState chatState, string searchText, string caption)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Definition for `{searchText}`:");
            stringBuilder.AppendLine();

            if (chatState.Definitions.ContainsKey(searchText))
            {
                var definition = chatState.Definitions[searchText];
                stringBuilder.AppendLine(definition);
            }
            else
            {
                var result = await datamuseClient.GetDefinitions(searchText);
                if (result != null && result.Any())
                {
                    foreach (var item in result)
                    {
                        stringBuilder.AppendLine(item);
                        stringBuilder.AppendLine();
                    }
                }
                else
                {
                    await telegramApi.SendTextMessageAsync(chatState.ChatId, $"No dictionary entries found for `{searchText}`");
                    return;
                }
            }

            await telegramApi.SendTextMessageAsync(chatState.ChatId, stringBuilder.ToString());

        }

        private Task List(long chatId)
        {
            var stringBuilder = new StringBuilder();
            foreach (var state in stateManager.GetStates())
            {
                stringBuilder.AppendLine($"Conversation: " + state.Name);
                stringBuilder.AppendLine(state.ToString());
                stringBuilder.AppendLine();
            }

            return telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }

        public async void Help(long chatId)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Help for bot /{Name}:");
            stringBuilder.AppendLine($"{Name} gives you temperature information");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Query: e.g. /{ShortName ?? Name} [word]");
            stringBuilder.AppendLine($"Responds with the definition of the word");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"List: e.g. /{ShortName ?? Name} list");
            stringBuilder.AppendLine($"Shows a list of all the conversations and states");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Add: e.g. /{ShortName ?? Name} add [word] [message].");
            stringBuilder.AppendLine($"Adds a definition of [message] for the [word]");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"Remove: e.g. /{ShortName ?? Name} remove [word].");
            stringBuilder.AppendLine($"Removes a definition for the [word]");
            stringBuilder.AppendLine();

            await telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }
    }

    public class DictionaryBotState : IChatState
    {
        public long ChatId { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> Definitions { get; set; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public override string ToString()
        {
            if (!Definitions.Any())
                return Name;

            var stringBuilder = new StringBuilder();
            foreach (var item in Definitions)
            {
                stringBuilder.AppendLine(item.Key);
            }

            return stringBuilder.ToString();
        }
    }
}

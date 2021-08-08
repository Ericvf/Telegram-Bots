using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TelegramBots.Core
{
    public class StateManager<T> : IStateManager<T>
        where T : IChatState, new()
    {
        protected Dictionary<long, T> states = new Dictionary<long, T>();
        private readonly ITelegramApi telegramApi;
        private readonly ICommandParser commandParser;

        public StateManager(ITelegramApi telegramApi, ICommandParser commandParser)
        {
            this.telegramApi = telegramApi;
            this.commandParser = commandParser;
            LoadStates();
        }

        public async Task<T> HandleState(long chatId, string chatName, string botName, string message)
        {
            var commands = commandParser.Parse(botName, message);

            if (!states.ContainsKey(chatId))
            {
                if (commands?.FirstWord == "start")
                {
                    var state = new T
                    {
                        ChatId = chatId,
                        Name = chatName
                    };
                    states.Add(chatId, state);
                    await SaveStates();
                    await telegramApi.SendTextMessageAsync(chatId, $"Started {botName} for {chatName}");
                }
            }
            else if (commands?.FirstWord == "stop")
            {
                states.Remove(chatId);
                await SaveStates();
                await telegramApi.SendTextMessageAsync(chatId, $"Stopped {botName} for {chatName}");
            }
            else if (commands?.FirstWord == "status")
            {
                var status = GetStatus();
                await telegramApi.SendTextMessageAsync(chatId, status);
            }
            else
            {
                return states[chatId];
            }

            return default(T);
        }

        public async Task SaveStates()
        {
            var fileName = typeof(T).Name + ".json";

            var path = Path.GetFullPath(Path.Combine("config", fileName));
            var result = JsonConvert.SerializeObject(states);
            await File.WriteAllTextAsync(path, result);
        }

        public void LoadStates()
        {
            var fileName = typeof(T).Name + ".json";

            var path = Path.Combine("config", fileName);
            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);

                var result = JsonConvert.DeserializeObject<Dictionary<long, T>>(content);
                if (result != null)
                    states = result;
            }
        }

        private string GetStatus()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Bot has {states.Count} states.");
            stringBuilder.AppendLine();

            foreach (var state in states)
            {
                stringBuilder.AppendLine(state.Value.ToString());
            }

            return stringBuilder.ToString();
        }

        public IEnumerable<T> GetStates() => states.Values;

        public bool HasState(long chatId)
        {
            return states.ContainsKey(chatId);
        }
    }
}

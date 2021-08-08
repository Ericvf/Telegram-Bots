using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBots
{
    public class TelegramBots : ITelegramBots
    {
        private readonly ITelegramApi telegramApi;
        private readonly IEnumerable<ITelegramBot> bots;

        public TelegramBots(ITelegramApi telegramApi, IEnumerable<ITelegramBot> bots)
        {
            this.telegramApi = telegramApi;
            this.bots = bots;
        }

        public async Task HandleMessage(TelegramMessage telegramMessage)
        {
            var chatId = telegramMessage.ChatId;
            var message = telegramMessage.Text;

            if (message == "/?")
            {
                await Help(chatId);
            }
            else if (message == "/?webhook")
            {
                await telegramApi.SendTextMessageAsync(chatId, "Webhook: https://telegram.appbyfex.com:8443/WebHook");
            }
            else if (message == "/?api")
            {
                await telegramApi.SendTextMessageAsync(chatId, $"Telegram API: http://api.telegram.org/bot{telegramApi.ApiKey}/getWebhookInfo");
            }
            else if (message == "/?git")
            {
                await telegramApi.SendTextMessageAsync(chatId, $"GIT: https://bitbucket.org/mrfex/codeartistsbot/src");
            }
            else
            {
                foreach (var bot in bots)
                {
                    if (message.Equals($"/{bot.ShortName}?", StringComparison.OrdinalIgnoreCase)
                        || message.Equals($"/{bot.Name}?", StringComparison.OrdinalIgnoreCase))
                    {
                        bot.Help(chatId);
                    }
                    else
                    {
                        await bot.HandleMessage(telegramMessage);
                    }

                }
            }
        }

        public async Task Help(long chatId)
        {
            var stringBuilder = new StringBuilder();
            foreach (var bot in bots)
            {
                if (!string.IsNullOrEmpty(bot.ShortName))
                {
                    stringBuilder.AppendLine($"/{bot.Name} (/{bot.ShortName})");
                }
                else
                {
                    stringBuilder.AppendLine("/" + bot.Name);
                }
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Use /[botName]? to display help for each bot. E.g. `/bot?`");

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Webhook: /?webhook to display webhook url");
            stringBuilder.AppendLine("API: /?api to display telegram bot api url");
            stringBuilder.AppendLine("GIT: /?git to display git repository url");

            await telegramApi.SendTextMessageAsync(chatId, stringBuilder.ToString());
        }
    }
}

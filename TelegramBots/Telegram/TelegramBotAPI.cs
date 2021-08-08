using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace TelegramBots
{
    public class TelegramBotAPI : ITelegramApi
    {
        // https://api.telegram.org/bot{}/getWebhookInfo
        // https://api.telegram.org/bot{}/setWebhook?url=https://telegram.appbyfex.com:8443/WebHook
        public TelegramBotAPI(ITelegramConfig telegramConfig)
        {
            Api = new TelegramBotClient(telegramConfig.Key);
            ApiKey = telegramConfig.Key;
        }

        public ITelegramBotClient Api { get; }

        public string ApiKey { get; }

        public Task DeleteMessageAsync(long chatId, long messageId)
        {
            return Api.DeleteMessageAsync(chatId, (int)messageId);
        }

        public Task EditMessageAsync(long chatId, long messageId, string message)
        {
            return Api.EditMessageTextAsync(chatId, (int)messageId, message);
        }

        public Task ForwardMessageAsync(long chatId, long fromChatId, long messageId)
        {
            return Api.ForwardMessageAsync(chatId, fromChatId, (int)messageId);
        }

        public async Task<long> SendStreamAsync(long chatId, Stream stream, string name, string message)
        {
            var file = new InputOnlineFile(stream, name);
            var result = await Api.SendPhotoAsync(chatId, file, caption: message);
            return result.MessageId;
        }

        public async Task<long> SendTextMessageAsync(long chatId, string message)
        {
            var result = await Api.SendTextMessageAsync(chatId, message);
            return result.MessageId;
        }

        public async Task<long> SendMarkdownMessageAsync(long chatId, string message, bool disableNotification)
        {
            var result = await Api.SendTextMessageAsync(chatId, message, ParseMode.Markdown, disableNotification: disableNotification);
            return result.MessageId;
        }

        public Task SendVideoAsync(long chatId, string url, string message)
        {
            var file = new InputOnlineFile(new Uri(url));
            return Api.SendVideoAsync(chatId, file, caption: message);
        }
    }
}

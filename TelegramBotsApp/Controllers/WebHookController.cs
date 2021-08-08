using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TelegramBots;

namespace TelegramBotsApp.Controllers
{
    public class WebHookController : Controller
    {
        private readonly ITelegramBots telegramBots;

        public WebHookController(ITelegramBots telegramBots)
        {
            this.telegramBots = telegramBots;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Json(new
            {
                StatusCode = 200
            });
        }

        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPost([FromBody] UpdateMessage update)
        {
            try
            {
                if (update?.message != null)
                {
                    Console.WriteLine($"[{update.message.chat.title}] [{update.message.from.first_name + " " + update.message.from.last_name}] {update?.message?.text}"); 

                    var messageAge = DateTime.Now - update.message.DateTime;
                    if (messageAge < TimeSpan.FromMinutes(1))
                    {
                        await telegramBots.HandleMessage(new TelegramMessage
                        {
                            Text = update.message.text,
                            ChatId = update.message.chat.id,
                            ChatTitle = update.message.chat.title,
                            From = update.message.from.first_name + " " + update.message.from.last_name,
                            MessageId = update.message.message_id,
                            UserId = (int)update.message.from.id,
                        });
                    }
                    else
                    {
                        Console.WriteLine("skipped");
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    err = ex.ToString()
                });
            }

            return Json(new { });
        }

        public class UpdateMessage
        {
            public long update_id { get; set; }
            public Message message { get; set; }

            public class Message
            {
                public long message_id { get; set; }

                public From from { get; set; }
                public Chat chat { get; set; }

                public long date { get; set; }

                public DateTime DateTime => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(date).ToLocalTime();

                public string text { get; set; }
            }

            public class From
            {
                public long id { get; set; }
                public bool is_bot { get; set; }
                public string first_name { get; set; }
                public string last_name { get; set; }
                public string username { get; set; }
                public string language_code { get; set; }
            }

            public class Chat
            {
                public long id { get; set; }
                public string title { get; set; }
                public string type { get; set; }
                public bool all_members_are_administrators { get; set; }
            }
        }
    }
}

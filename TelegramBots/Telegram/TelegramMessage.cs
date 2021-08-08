namespace TelegramBots
{
    public class TelegramMessage {
        public long MessageId { get; set; }
        public long ChatId { get; set; }
        public int UserId { get; set; }
        public string Text{ get; set; }
        public string From { get; set; }
        public string ChatTitle { get; set; }
    }
}

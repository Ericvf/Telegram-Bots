using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace TelegramBots.Clients
{
    public class TipOfADayClient : ITipOfADayClient
    {
        public Task<string> GetTip()
        {
            var websiteUrl = "https://pragprog.com/";
            return ScrapeTipFromWebsite(websiteUrl);
        }

        private async Task<string> ScrapeTipFromWebsite(string url)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic RWRnYXJTY2huaXR0ZW5maXR0aWNoOlJvY2taeno=");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "d-fens HttpClient");

                var html = await httpClient.GetStringAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var tipOfTheDayNode = 
                    doc.DocumentNode.SelectNodes("//div[@class='tip-of-day']")
                        .Descendants("p")
                        .Where(p => !string.IsNullOrEmpty(p.InnerHtml))
                        .FirstOrDefault();

                if (tipOfTheDayNode != null)
                    return WebUtility.HtmlDecode(tipOfTheDayNode.InnerHtml);
            }

            return null;
        }
    }

    public interface ITipOfADayClient
    {
        Task<string> GetTip();
    }
}
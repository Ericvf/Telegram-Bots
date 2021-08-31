using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace TelegramBots.Clients
{
    public class FavQsClient : IFavQsClient
    {
        private readonly static System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
        private readonly static Random random = new Random();

        public string ApiKey { get; }

        public FavQsClient(IFavQsConfig favQsConfig)
        {
            ApiKey = favQsConfig.Key;
        }

        public async Task<string> SearchQuote()
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", ApiKey);

            var apiUrl = $"https://favqs.com/api/quotes/?filter=happy";

            var jsonResult = await httpClient.GetStringAsync(apiUrl);

            var result = JsonConvert.DeserializeObject<GetQuoteResult>(jsonResult);

            var randomIndex = random.Next(0, result.quotes.Length - 1);

            return result.quotes[randomIndex].body;
        }

        public Task<string> GetStringAsync(string requestUrl)
        {
            return httpClient.GetStringAsync(requestUrl);
        }

        #region Model

        public class GetQuoteResult
        {
            public int page { get; set; }
            public bool last_page { get; set; }
            public Quote[] quotes { get; set; }
        }

        public class Quote
        {
            public string[] tags { get; set; }
            public bool favorite { get; set; }
            public string author_permalink { get; set; }
            public string body { get; set; }
            public int id { get; set; }
            public int favorites_count { get; set; }
            public int upvotes_count { get; set; }
            public int downvotes_count { get; set; }
            public bool dialogue { get; set; }
            public string author { get; set; }
            public string url { get; set; }
        }

        #endregion
    }

    public interface IFavQsClient
    {
        Task<string> GetStringAsync(string requestUrl);

        Task<string> SearchQuote();
    }
}

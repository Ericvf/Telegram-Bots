using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TelegramBots.Clients
{
    public class ChuckNorrisClient : IChuckNorrisClient
    {
        private static readonly System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();

        public async Task<string> GetRandomQuote()
        {
            var apiUrl = $"https://api.chucknorris.io/jokes/random?category=dev";

            var jsonResult = await httpClient.GetStringAsync(apiUrl);

            var model = JsonConvert.DeserializeObject<ChuckNorrisModel>(jsonResult);

            return model.value;
        }

        public Task<string> GetStringAsync(string requestUrl)
        {
            return httpClient.GetStringAsync(requestUrl);
        }

        #region ChuckNorrisModel

        public class ChuckNorrisModel
        {
            public object category { get; set; }
            public string icon_url { get; set; }
            public string id { get; set; }
            public string url { get; set; }
            public string value { get; set; }
        }

        #endregion
    }

    public interface IChuckNorrisClient
    {
        Task<string> GetRandomQuote();
    }
}